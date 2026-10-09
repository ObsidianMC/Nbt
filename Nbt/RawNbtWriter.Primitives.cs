using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Obsidian.Nbt;

public partial class RawNbtWriter
{
    public void WriteString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        this.WriteTagHeader(NbtTagType.String, null);
        this.WriteStringRaw(value);
    }

    public void WriteString(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        this.WriteTagHeader(NbtTagType.String, name);
        this.WriteStringRaw(value);
    }

    public void WriteByte(byte value)
    {
        this.WriteTagHeader(NbtTagType.Byte, null);
        this.WriteByteRaw(value);
    }

    public void WriteByte(string name, byte value)
    {
        this.WriteTagHeader(NbtTagType.Byte, name);
        this.WriteByteRaw(value);
    }

    public void WriteBool(bool value) => this.WriteByte(value ? (byte)1 : (byte)0);

    public void WriteBool(string name, bool value) => this.WriteByte(name, value ? (byte)1 : (byte)0);

    public void WriteShort(short value)
    {
        this.WriteTagHeader(NbtTagType.Short, null);
        this.WriteShortRaw(value);
    }

    public void WriteShort(string name, short value)
    {
        this.WriteTagHeader(NbtTagType.Short, name);
        this.WriteShortRaw(value);
    }

    public void WriteInt(int value)
    {
        this.WriteTagHeader(NbtTagType.Int, null);
        this.WriteIntRaw(value);
    }

    public void WriteInt(string name, int value)
    {
        this.WriteTagHeader(NbtTagType.Int, name);
        this.WriteIntRaw(value);
    }

    public void WriteLong(long value)
    {
        this.WriteTagHeader(NbtTagType.Long, null);
        this.WriteLongRaw(value);
    }

    public void WriteLong(string name, long value)
    {
        this.WriteTagHeader(NbtTagType.Long, name);
        this.WriteLongRaw(value);
    }

    public void WriteFloat(float value)
    {
        this.WriteTagHeader(NbtTagType.Float, null);
        this.WriteIntRaw(BitConverter.SingleToInt32Bits(value));
    }

    public void WriteFloat(string name, float value)
    {
        this.WriteTagHeader(NbtTagType.Float, name);
        this.WriteIntRaw(BitConverter.SingleToInt32Bits(value));
    }

    public void WriteDouble(double value)
    {
        this.WriteTagHeader(NbtTagType.Double, null);
        this.WriteLongRaw(BitConverter.DoubleToInt64Bits(value));
    }

    public void WriteDouble(string name, double value)
    {
        this.WriteTagHeader(NbtTagType.Double, name);
        this.WriteLongRaw(BitConverter.DoubleToInt64Bits(value));
    }

    /// <summary>Makes room for <paramref name="capacity"/> more bytes.</summary>
    public void Reserve(int capacity)
    {
        if (this.data.Length - this.offset < capacity)
            this.Grow(capacity);
    }

    private void Grow(int capacity)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        if (this.sink is not null)
        {
            this.FlushToSink();

            if (this.data.Length >= capacity)
                return;
        }

        var newSize = Math.Max(this.offset + capacity, this.data.Length * 2);
        var newData = ArrayPool<byte>.Shared.Rent(newSize);

        this.data.AsSpan(0, this.offset).CopyTo(newData);
        ArrayPool<byte>.Shared.Return(this.data);

        this.data = newData;
    }

    // Java's DataOutput.writeUTF format: an unsigned 16-bit byte count, then modified UTF-8. Clients can't read more.
    private void WriteStringRaw(string value)
    {
        if (!ModifiedUtf8.TryGetByteCount(value, out var byteCount))
        {
            throw new ArgumentException(
                $"String is longer than NBT allows ({ushort.MaxValue} bytes of modified UTF-8).", nameof(value));
        }

        this.Reserve(sizeof(ushort) + byteCount);

        var destination = this.data.AsSpan(this.offset, sizeof(ushort) + byteCount);
        BinaryPrimitives.WriteUInt16BigEndian(destination, (ushort)byteCount);
        ModifiedUtf8.GetBytesCommon(value, destination[sizeof(ushort)..]);

        this.offset += destination.Length;
    }

    private void WriteByteRaw(byte value)
    {
        this.Reserve(sizeof(byte));
        this.data[this.offset++] = value;
    }

    private void WriteShortRaw(short value)
    {
        this.Reserve(sizeof(short));
        BinaryPrimitives.WriteInt16BigEndian(this.data.AsSpan(this.offset), value);
        this.offset += sizeof(short);
    }

    private void WriteIntRaw(int value)
    {
        this.Reserve(sizeof(int));
        BinaryPrimitives.WriteInt32BigEndian(this.data.AsSpan(this.offset), value);
        this.offset += sizeof(int);
    }

    private void WriteLongRaw(long value)
    {
        this.Reserve(sizeof(long));
        BinaryPrimitives.WriteInt64BigEndian(this.data.AsSpan(this.offset), value);
        this.offset += sizeof(long);
    }

    private void WriteBytesRaw(ReadOnlySpan<byte> values)
    {
        while (!values.IsEmpty)
        {
            var count = this.ChunkLength(values.Length, sizeof(byte));
            this.Reserve(count);

            values[..count].CopyTo(this.data.AsSpan(this.offset));

            this.offset += count;
            values = values[count..];
        }
    }

    // Arrays are converted to big-endian in bulk, which is vectorized, rather than one value at a time.
    private void WriteIntsRaw(ReadOnlySpan<int> values)
    {
        while (!values.IsEmpty)
        {
            var count = this.ChunkLength(values.Length, sizeof(int));
            this.Reserve(count * sizeof(int));

            var destination = MemoryMarshal.Cast<byte, int>(this.data.AsSpan(this.offset, count * sizeof(int)));
            if (BitConverter.IsLittleEndian)
                BinaryPrimitives.ReverseEndianness(values[..count], destination);
            else
                values[..count].CopyTo(destination);

            this.offset += count * sizeof(int);
            values = values[count..];
        }
    }

    private void WriteLongsRaw(ReadOnlySpan<long> values)
    {
        while (!values.IsEmpty)
        {
            var count = this.ChunkLength(values.Length, sizeof(long));
            this.Reserve(count * sizeof(long));

            var destination = MemoryMarshal.Cast<byte, long>(this.data.AsSpan(this.offset, count * sizeof(long)));
            if (BitConverter.IsLittleEndian)
                BinaryPrimitives.ReverseEndianness(values[..count], destination);
            else
                values[..count].CopyTo(destination);

            this.offset += count * sizeof(long);
            values = values[count..];
        }
    }

    /// <summary>
    /// How many of <paramref name="length"/> array elements to write at once: all of them in memory, and at most a
    /// buffer's worth with a sink, so streaming a large array doesn't grow the buffer to hold all of it.
    /// </summary>
    private int ChunkLength(int length, int elementSize) =>
        this.sink is null ? length : Math.Min(length, this.data.Length / elementSize);
}
