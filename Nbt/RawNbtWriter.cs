using Obsidian.Nbt.Interfaces;
using System.Buffers;
using System.IO;

namespace Obsidian.Nbt;

/// <summary>
/// Writes binary NBT into a pooled in-memory buffer; read the result from <see cref="Data"/> before disposing.
/// </summary>
/// <remarks>
/// The writer tracks the open compounds and lists, so each call writes exactly what its context needs: inside a
/// compound a tag's type and name come first, inside a list only its payload is written. Network NBT (since 1.20.2)
/// is the same as file NBT except that the root tag has no name.
/// </remarks>
public sealed partial class RawNbtWriter : INbtWriter
{
    private const int InitialBufferSize = 4096;

    private byte[] data;
    private int offset;
    private bool disposed;

    // When set, full buffers are written here instead of growing (used by NbtWriterStream).
    private readonly Stream? sink;

    private Frame[] frames = new Frame[8];
    private int depth;
    private bool rootWritten;

    /// <summary>Starts a file-style root compound with the given name.</summary>
    public RawNbtWriter(string name) : this(null, false, InitialBufferSize) => this.WriteCompoundStart(name);

    /// <summary>
    /// Starts a root compound. Network NBT has no root name; otherwise the root is named with an empty string.
    /// </summary>
    public RawNbtWriter(bool networked) : this(null, networked, InitialBufferSize) => this.WriteCompoundStart();

    /// <summary>Creates a writer with nothing written; the first tag written becomes the root.</summary>
    internal RawNbtWriter(Stream? sink, bool networked, int bufferSize)
    {
        this.sink = sink;
        this.Networked = networked;
        this.data = ArrayPool<byte>.Shared.Rent(bufferSize);
    }

    /// <summary>The innermost open compound or list, or null when none is open.</summary>
    public NbtTagType? RootType => this.depth > 0 ? this.frames[this.depth - 1].Type : null;

    /// <summary>Whether the root tag is written without a name, as network NBT is.</summary>
    public bool Networked { get; }

    public Span<byte> Data => this.AsSpan();

    public int Offset => this.offset;

    public Span<byte> AsSpan() => new(this.data, 0, this.offset);

    internal ReadOnlyMemory<byte> AsMemory() => new(this.data, 0, this.offset);

    public void WriteCompoundStart(string name = "")
    {
        this.WriteTagHeader(NbtTagType.Compound, name);

        ref var frame = ref this.PushFrame(NbtTagType.Compound);
        frame.Names ??= new HashSet<string>(StringComparer.Ordinal);
        frame.Names.Clear();
    }

    /// <summary>Starts a list of <paramref name="length"/> elements of type <paramref name="listType"/>.</summary>
    /// <param name="writeName">
    /// Only used for a root list: whether to write its name. Elements of a list never have names and entries of a
    /// compound always do.
    /// </param>
    public void WriteListStart(string name, NbtTagType listType, int length, bool writeName = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (listType > NbtTagType.LongArray)
            throw new ArgumentOutOfRangeException(nameof(listType), listType, "Unknown list element type.");

        // Vanilla can't read a non-empty list without an element type.
        if (listType == NbtTagType.End && length > 0)
            throw new ArgumentException("A non-empty list needs an element type.", nameof(listType));

        if (this.depth == 0 && !writeName)
            this.WriteRootHeader(NbtTagType.List, null, false);
        else
            this.WriteTagHeader(NbtTagType.List, name);

        ref var frame = ref this.PushFrame(NbtTagType.List);
        frame.ElementType = listType;
        frame.Length = length;
        frame.Count = 0;

        this.WriteByteRaw((byte)listType);
        this.WriteIntRaw(length);
    }

    public void EndList()
    {
        if (this.RootType != NbtTagType.List)
            throw new InvalidOperationException("There is no open list to end.");

        ref var frame = ref this.frames[this.depth - 1];
        if (frame.Count < frame.Length)
        {
            throw new InvalidOperationException(
                $"List cannot end: it has {frame.Count} of its {frame.Length} elements.");
        }

        this.depth--;
    }

    public void EndCompound()
    {
        if (this.RootType != NbtTagType.Compound)
            throw new InvalidOperationException("There is no open compound to end.");

        this.WriteByteRaw((byte)NbtTagType.End);
        this.depth--;
    }

    /// <summary>Writes a tag, named with its <see cref="INbtTag.Name"/> when it goes into a compound.</summary>
    public void WriteTag(INbtTag tag) => this.WriteTag(tag, tag.Name);

    /// <summary>Writes a tag as an element of the open list, ignoring its name.</summary>
    public void WriteListTag(INbtTag tag) => this.WriteTag(tag, null);

    public void WriteArray(string? name, ReadOnlySpan<int> values)
    {
        this.WriteTagHeader(NbtTagType.IntArray, name);
        this.WriteIntRaw(values.Length);
        this.WriteIntsRaw(values);
    }

    public void WriteArray(string? name, ReadOnlySpan<long> values)
    {
        this.WriteTagHeader(NbtTagType.LongArray, name);
        this.WriteIntRaw(values.Length);
        this.WriteLongsRaw(values);
    }

    public void WriteArray(string? name, ReadOnlySpan<byte> values)
    {
        this.WriteTagHeader(NbtTagType.ByteArray, name);
        this.WriteIntRaw(values.Length);
        this.WriteBytesRaw(values);
    }

    /// <summary>Writes a raw tag type byte, without any checks.</summary>
    public void Write(NbtTagType tagType) => this.WriteByteRaw((byte)tagType);

    public void TryFinish()
    {
        this.ThrowIfUnclosed();

        if (this.sink is not null)
        {
            this.FlushToSink();
            this.sink.Flush();
        }
    }

    public async Task TryFinishAsync()
    {
        this.ThrowIfUnclosed();

        if (this.sink is not null)
        {
            await this.FlushToSinkAsync();
            await this.sink.FlushAsync();
        }
    }

    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;

        ArrayPool<byte>.Shared.Return(this.data);
        this.data = [];
        this.offset = 0;
    }

    public ValueTask DisposeAsync()
    {
        this.Dispose();

        return default;
    }

    /// <summary>Writes the buffered bytes to the sink stream and empties the buffer.</summary>
    internal void FlushToSink()
    {
        if (this.offset == 0)
            return;

        this.sink!.Write(this.data, 0, this.offset);
        this.offset = 0;
    }

    internal async ValueTask FlushToSinkAsync()
    {
        if (this.offset == 0)
            return;

        await this.sink!.WriteAsync(this.data.AsMemory(0, this.offset));
        this.offset = 0;
    }

    private void WriteTag(INbtTag tag, string? name)
    {
        switch (tag)
        {
            case NbtCompound compound:
                this.WriteCompoundStart(name!);

                // The key is what the compound is read back by, even when it differs from the child's Name.
                foreach (var (key, child) in compound)
                    this.WriteTag(child, key);

                this.EndCompound();
                break;
            case NbtList list:
                this.WriteListStart(name!, list.ListType, list.Count);

                foreach (var child in list)
                    this.WriteTag(child, null);

                this.EndList();
                break;
            case NbtTag<string> stringTag:
                this.WriteString(name!, stringTag.Value!);
                break;
            case NbtTag<int> intTag:
                this.WriteInt(name!, intTag.Value);
                break;
            case NbtTag<byte> byteTag:
                this.WriteByte(name!, byteTag.Value);
                break;
            case NbtTag<bool> boolTag:
                this.WriteBool(name!, boolTag.Value);
                break;
            case NbtTag<short> shortTag:
                this.WriteShort(name!, shortTag.Value);
                break;
            case NbtTag<long> longTag:
                this.WriteLong(name!, longTag.Value);
                break;
            case NbtTag<float> floatTag:
                this.WriteFloat(name!, floatTag.Value);
                break;
            case NbtTag<double> doubleTag:
                this.WriteDouble(name!, doubleTag.Value);
                break;
            case NbtArray<long> longArray:
                this.WriteArray(name, longArray.GetArray());
                break;
            case NbtArray<int> intArray:
                this.WriteArray(name, intArray.GetArray());
                break;
            case NbtArray<byte> byteArray:
                this.WriteArray(name, byteArray.GetArray());
                break;
            default:
                throw new InvalidOperationException($"Unsupported tag: {tag.GetType()} ({tag.Type}).");
        }
    }

    /// <summary>
    /// Checks a tag against the open container and writes what precedes its payload there: its type and name in a
    /// compound, nothing in a list.
    /// </summary>
    private void WriteTagHeader(NbtTagType type, string? name)
    {
        if (this.depth == 0)
        {
            this.WriteRootHeader(type, name, true);
            return;
        }

        ref var frame = ref this.frames[this.depth - 1];

        if (frame.Type == NbtTagType.List)
        {
            if (!string.IsNullOrEmpty(name))
                throw new InvalidOperationException("Tags inside lists cannot be named.");

            if (type != frame.ElementType)
                throw new InvalidOperationException($"Expected list type: {frame.ElementType}. Got: {type}");

            if (frame.Count >= frame.Length)
                throw new InvalidOperationException($"Exceeded the list's length of {frame.Length}.");

            frame.Count++;
            return;
        }

        // Empty names are valid NBT: vanilla writes heterogeneous lists as compounds with an empty key.
        if (name is null)
            throw new ArgumentException($"Tags inside a compound tag must have a name. Tag({type})");

        if (!frame.Names!.Add(name))
            throw new ArgumentException($"Tag with name {name} already exists.");

        this.WriteByteRaw((byte)type);
        this.WriteStringRaw(name);
    }

    private void WriteRootHeader(NbtTagType type, string? name, bool writeName)
    {
        if (this.rootWritten)
            throw new InvalidOperationException("The root tag has already been written.");

        this.rootWritten = true;

        this.WriteByteRaw((byte)type);

        if (writeName && !this.Networked)
            this.WriteStringRaw(name ?? string.Empty);
    }

    private ref Frame PushFrame(NbtTagType type)
    {
        if (this.depth == this.frames.Length)
            Array.Resize(ref this.frames, this.frames.Length * 2);

        ref var frame = ref this.frames[this.depth++];
        frame.Type = type;

        return ref frame;
    }

    private void ThrowIfUnclosed()
    {
        if (this.depth > 0)
        {
            throw new InvalidOperationException(
                $"Unable to close writer. {this.depth} compound or list tag(s) have yet to be closed.");
        }
    }

    /// <summary>An open compound or list.</summary>
    private struct Frame
    {
        public NbtTagType Type;

        // Lists: the element type, the declared length and the elements written so far.
        public NbtTagType ElementType;
        public int Length;
        public int Count;

        // Compounds: the names written so far, to reject duplicates. Kept between uses of the frame to reuse it.
        public HashSet<string>? Names;
    }
}
