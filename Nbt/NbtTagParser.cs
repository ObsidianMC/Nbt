using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Obsidian.Nbt;

/// <summary>Limits shared by every <see cref="NbtTagParser{TInput}"/>.</summary>
internal static class NbtTagParser
{
    /// <summary>
    /// The deepest nesting of compounds and lists accepted. Vanilla rejects anything deeper than 512, so this accepts
    /// all data vanilla accepts, and keeps hostile packets from overflowing the stack, which would end the process.
    /// </summary>
    public const int MaxDepth = 512;

    /// <summary>
    /// The memory budget for NBT read from the network: vanilla's FriendlyByteBuf.readNbt gives its NbtAccounter
    /// 2 MiB, so packets vanilla rejects as too big are rejected here too.
    /// </summary>
    public const long NetworkQuota = 2 * 1024 * 1024;

    /// <summary>No memory budget, as for files.</summary>
    public const long Unlimited = long.MaxValue;
}

/// <summary>
/// Parses binary NBT into tags from <typeparamref name="TInput"/>. The format and its limits match vanilla's reader,
/// because clients and saved worlds send exactly that.
/// </summary>
/// <remarks>
/// Reads also charge vanilla's estimate of each tag's memory (NbtAccounter's sizes) against a quota, which bounds how
/// many objects a small packet can expand into. The estimates are vanilla's so the same packets pass or fail.
/// </remarks>
internal ref struct NbtTagParser<TInput>(TInput input, long quota)
    where TInput : INbtInput, allows ref struct
{
    // Lists without an initial size from the input's length (non-seekable streams) start no bigger than this and
    // grow as elements arrive, so a forged length can't allocate more than the data that actually follows.
    private const int MaxUncheckedListCapacity = 1024;

    // Likewise, arrays from such inputs are allocated at most this many bytes ahead of their data.
    private const int UncheckedArrayChunkSize = 1024 * 1024;

    private TInput input = input;
    private long quotaUsed;

    /// <summary>The input, to read how far parsing got or to release it.</summary>
    [UnscopedRef]
    public ref TInput Input => ref this.input;

    /// <summary>
    /// Reads a root tag: its type, its name when <paramref name="readName"/> is set, then its payload. Network NBT
    /// (since 1.20.2) has no root name. Returns null for an end tag or when the input is already empty.
    /// </summary>
    public INbtTag? ReadRoot(bool readName)
    {
        if (!this.input.TryReadByte(out var typeId))
            return null;

        var type = ToTagType(typeId);
        if (type == NbtTagType.End)
            return null;

        var name = readName ? this.ReadString() : string.Empty;

        return this.ReadPayload(type, name, 0);
    }

    public string ReadString()
    {
        // Java's DataInput.readUTF: an unsigned 16-bit byte count, then modified UTF-8.
        var length = BinaryPrimitives.ReadUInt16BigEndian(this.input.Read(sizeof(ushort)));

        return length == 0 ? string.Empty : ModifiedUtf8.GetString(this.input.Read(length));
    }

    // The charges below are vanilla's NbtAccounter estimates for each tag type.
    private INbtTag ReadPayload(NbtTagType type, string name, int depth)
    {
        switch (type)
        {
            case NbtTagType.Byte:
                this.Charge(9);
                return new NbtTag<byte>(name, this.input.Read(sizeof(byte))[0]);
            case NbtTagType.Short:
                this.Charge(10);
                return new NbtTag<short>(name, BinaryPrimitives.ReadInt16BigEndian(this.input.Read(sizeof(short))));
            case NbtTagType.Int:
                this.Charge(12);
                return new NbtTag<int>(name, this.ReadInt());
            case NbtTagType.Long:
                this.Charge(16);
                return new NbtTag<long>(name, BinaryPrimitives.ReadInt64BigEndian(this.input.Read(sizeof(long))));
            case NbtTagType.Float:
                this.Charge(12);
                return new NbtTag<float>(name, BinaryPrimitives.ReadSingleBigEndian(this.input.Read(sizeof(float))));
            case NbtTagType.Double:
                this.Charge(16);
                return new NbtTag<double>(name, BinaryPrimitives.ReadDoubleBigEndian(this.input.Read(sizeof(double))));
            case NbtTagType.String:
                var value = this.ReadString();
                this.Charge(36 + 2L * value.Length);
                return new NbtTag<string>(name, value);
            case NbtTagType.ByteArray:
                return new NbtArray<byte>(name, this.ReadArray<byte>());
            case NbtTagType.IntArray:
                var ints = this.ReadArray<int>();
                if (BitConverter.IsLittleEndian)
                    BinaryPrimitives.ReverseEndianness(ints, ints);

                return new NbtArray<int>(name, ints);
            case NbtTagType.LongArray:
                var longs = this.ReadArray<long>();
                if (BitConverter.IsLittleEndian)
                    BinaryPrimitives.ReverseEndianness(longs, longs);

                return new NbtArray<long>(name, longs);
            case NbtTagType.List:
                return this.ReadList(name, depth);
            case NbtTagType.Compound:
                return this.ReadCompound(name, depth);
            default:
                throw new InvalidDataException($"Unexpected tag type {type}.");
        }
    }

    private NbtCompound ReadCompound(string name, int depth)
    {
        CheckDepth(depth);
        this.Charge(48);

        var compound = new NbtCompound(name);

        while (true)
        {
            var type = this.ReadTagType();
            if (type == NbtTagType.End)
                return compound;

            var key = this.ReadString();
            this.Charge(28 + 2L * key.Length);

            // Vanilla charges an entry once, even when a repeated key replaces it.
            if (compound.SetReadTag(key, this.ReadPayload(type, key, depth + 1)))
                this.Charge(36);
        }
    }

    private NbtList ReadList(string name, int depth)
    {
        CheckDepth(depth);
        this.Charge(36);

        var elementType = this.ReadTagType();
        var count = this.ReadLength(MinimumPayloadSize(elementType));

        // Vanilla rejects this too: the elements' type is unknown.
        if (elementType == NbtTagType.End && count > 0)
            throw new InvalidDataException("A non-empty list has no element type.");

        this.Charge(4L * count);

        var list = new NbtList(elementType, name);
        var lengthKnown = this.input.Remaining != long.MaxValue;
        list.EnsureCapacity(lengthKnown ? count : Math.Min(count, MaxUncheckedListCapacity));

        for (var i = 0; i < count; i++)
            list.Add(this.ReadPayload(elementType, string.Empty, depth + 1));

        return list;
    }

    /// <summary>Reads an array's length, then its elements' raw big-endian bytes straight into the array.</summary>
    private T[] ReadArray<T>() where T : unmanaged
    {
        this.Charge(24);

        var length = this.ReadLength(Unsafe.SizeOf<T>());
        this.Charge((long)Unsafe.SizeOf<T>() * length);

        // Without the input's length the declared length can't be checked, so a large array grows as its data arrives
        // instead of being allocated up front.
        var chunkLength = UncheckedArrayChunkSize / Unsafe.SizeOf<T>();
        var lengthKnown = this.input.Remaining != long.MaxValue;
        var array = GC.AllocateUninitializedArray<T>(lengthKnown ? length : Math.Min(length, chunkLength));
        var filled = 0;

        while (true)
        {
            this.input.ReadExactly(MemoryMarshal.AsBytes(array.AsSpan(filled)));
            filled = array.Length;

            if (filled == length)
                return array;

            Array.Resize(ref array, (int)Math.Min(2L * filled, length));
        }
    }

    private int ReadInt() => BinaryPrimitives.ReadInt32BigEndian(this.input.Read(sizeof(int)));

    /// <summary>
    /// Reads the element count of a list or array, rejecting negative counts and counts the rest of the input is too
    /// short to hold, before anything is allocated for them.
    /// </summary>
    private int ReadLength(int minimumElementSize)
    {
        var length = this.ReadInt();
        if (length < 0)
            throw new InvalidDataException($"Length cannot be negative: {length}.");

        if ((long)length * minimumElementSize > this.input.Remaining)
            throw new EndOfStreamException($"Length {length} is longer than the remaining data.");

        return length;
    }

    private NbtTagType ReadTagType() => ToTagType(this.input.Read(sizeof(byte))[0]);

    private void Charge(long bytes)
    {
        if (bytes > quota - this.quotaUsed)
        {
            throw new InvalidDataException(
                $"NBT is too big: {this.quotaUsed} + {bytes} bytes is more than the allowed {quota}.");
        }

        this.quotaUsed += bytes;
    }

    private static NbtTagType ToTagType(byte id) => id <= (byte)NbtTagType.LongArray
        ? (NbtTagType)id
        : throw new InvalidDataException($"Unknown tag type id {id}.");

    private static void CheckDepth(int depth)
    {
        if (depth >= NbtTagParser.MaxDepth)
        {
            throw new InvalidDataException(
                $"NBT is nested deeper than {NbtTagParser.MaxDepth} compounds and lists.");
        }
    }

    // The fewest bytes one element's payload can take, to bound list lengths by the input left.
    private static int MinimumPayloadSize(NbtTagType type) => type switch
    {
        NbtTagType.End => 0,
        NbtTagType.Byte => sizeof(byte),
        NbtTagType.Short => sizeof(short),
        NbtTagType.Int or NbtTagType.Float => sizeof(int),
        NbtTagType.Long or NbtTagType.Double => sizeof(long),
        NbtTagType.String => sizeof(ushort),
        NbtTagType.ByteArray or NbtTagType.IntArray or NbtTagType.LongArray => sizeof(int),
        NbtTagType.List => sizeof(byte) + sizeof(int),
        _ => sizeof(byte)
    };
}
