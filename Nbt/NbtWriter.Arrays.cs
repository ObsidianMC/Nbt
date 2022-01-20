using Obsidian.Nbt.Utilities;
using System.Buffers.Binary;

namespace Obsidian.Nbt;

public partial struct NbtWriter
{
    private void WriteShortSpan(ReadOnlySpan<short> valueSpan)
    {
        if (valueSpan.IsEmpty)
            return;

        Span<byte> destination = GetSpan(valueSpan.Length * sizeof(short));
        for (int i = 0; i < valueSpan.Length; i++)
        {
            BinaryPrimitives.WriteInt16BigEndian(destination, valueSpan[i]);
            destination = destination.Advance(sizeof(short));
        }
    }

    private void WriteIntSpan(ReadOnlySpan<int> valueSpan)
    {
        if (valueSpan.IsEmpty)
            return;

        Span<byte> destination = GetSpan(valueSpan.Length * sizeof(int));
        for (int i = 0; i < valueSpan.Length; i++)
        {
            BinaryPrimitives.WriteInt32BigEndian(destination, valueSpan[i]);
            destination = destination.Advance(sizeof(int));
        }
    }

    private void WriteLongSpan(ReadOnlySpan<long> valueSpan)
    {
        if (valueSpan.IsEmpty)
            return;

        Span<byte> destination = GetSpan(valueSpan.Length * sizeof(long));
        for (int i = 0; i < valueSpan.Length; i++)
        {
            BinaryPrimitives.WriteInt64BigEndian(destination, valueSpan[i]);
            destination = destination.Advance(sizeof(long));
        }
    }

    private void WriteFloatSpan(ReadOnlySpan<float> valueSpan)
    {
        if (valueSpan.IsEmpty)
            return;

        Span<byte> destination = GetSpan(valueSpan.Length * sizeof(float));
        for (int i = 0; i < valueSpan.Length; i++)
        {
            BinaryPrimitives.WriteSingleBigEndian(destination, valueSpan[i]);
            destination = destination.Advance(sizeof(float));
        }
    }

    private void WriteDoubleSpan(ReadOnlySpan<double> valueSpan)
    {
        if (valueSpan.IsEmpty)
            return;

        Span<byte> destination = GetSpan(valueSpan.Length * sizeof(double));
        for (int i = 0; i < valueSpan.Length; i++)
        {
            BinaryPrimitives.WriteDoubleBigEndian(destination, valueSpan[i]);
            destination = destination.Advance(sizeof(double));
        }
    }

    public void WriteByteArray(string? name, byte[]? values)
    {
        WriteByteArray(name, values.AsSpan());
    }

    public void WriteByteArray(string? name, ReadOnlySpan<byte> values)
    {
        WriteByte(NbtTag.ByteArray);
        WriteString(name.AsSpan());
        WriteByteArray(values);
    }

    public void WriteByteArray(ReadOnlySpan<byte> encodedName, byte[]? values)
    {
        WriteByteArray(encodedName, values.AsSpan());
    }

    public void WriteByteArray(ReadOnlySpan<byte> encodedName, ReadOnlySpan<byte> values)
    {
        WriteByte(NbtTag.ByteArray);
        WriteString(encodedName);
        WriteByteArray(values);
    }

    public void WriteByteArray(byte[]? values)
    {
        WriteByteArray(values.AsSpan());
    }

    public void WriteByteArray(ReadOnlySpan<byte> values)
    {
        WriteInt(values.Length);
        RefWriter.WriteBytes(ref GetRef(values.Length), values);
    }

    public void WriteIntArray(string? name, int[]? values)
    {
        WriteIntArray(name, values.AsSpan());
    }

    public void WriteIntArray(string? name, ReadOnlySpan<int> values)
    {
        WriteByte(NbtTag.IntArray);
        WriteString(name.AsSpan());
        WriteIntArray(values);
    }

    public void WriteIntArray(ReadOnlySpan<byte> encodedName, int[]? values)
    {
        WriteIntArray(encodedName, values.AsSpan());
    }

    public void WriteIntArray(ReadOnlySpan<byte> encodedName, ReadOnlySpan<int> values)
    {
        WriteByte(NbtTag.IntArray);
        WriteString(encodedName);
        WriteIntArray(values);
    }

    public void WriteIntArray(int[]? values)
    {
        WriteIntArray(values.AsSpan());
    }

    public void WriteIntArray(ReadOnlySpan<int> values)
    {
        WriteInt(values.Length);
        WriteIntSpan(values);
    }

    public void WriteLongArray(string? name, long[]? values)
    {
        WriteLongArray(name, values.AsSpan());
    }

    public void WriteLongArray(string? name, ReadOnlySpan<long> values)
    {
        WriteByte(NbtTag.LongArray);
        WriteString(name.AsSpan());
        WriteLongArray(values);
    }

    public void WriteLongArray(ReadOnlySpan<byte> encodedName, long[]? values)
    {
        WriteLongArray(encodedName, values);
    }

    public void WriteLongArray(ReadOnlySpan<byte> encodedName, ReadOnlySpan<long> values)
    {
        WriteByte(NbtTag.LongArray);
        WriteString(encodedName);
        WriteLongArray(values);
    }

    public void WriteLongArray(long[]? values)
    {
        WriteLongArray(values.AsSpan());
    }

    public void WriteLongArray(ReadOnlySpan<long> values)
    {
        WriteInt(values.Length);
        WriteLongSpan(values);
    }
}
