using Obsidian.Nbt.Utilities;
using System.Buffers.Binary;

namespace Obsidian.Nbt;

public partial struct NbtWriter
{
    public void WriteList(string? name, byte[]? values)
    {
        WriteList(name, values.AsSpan());
    }

    public void WriteList(string? name, ReadOnlySpan<byte> values)
    {
        WriteByte(NbtTag.List);
        WriteString(name.AsSpan());
        WriteByte(NbtTag.Byte);
        WriteInt(values.Length);
        values.CopyTo(GetSpan(values.Length));
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, byte[]? values)
    {
        WriteList(encodedName, values.AsSpan());
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, ReadOnlySpan<byte> values)
    {
        WriteByte(NbtTag.List);
        WriteString(encodedName);
        WriteByte(NbtTag.Byte);
        WriteInt(values.Length);
        values.CopyTo(GetSpan(values.Length));
    }

    public void WriteList(string? name, short[]? values)
    {
        WriteList(name, values.AsSpan());
    }

    public void WriteList(string? name, ReadOnlySpan<short> values)
    {
        WriteByte(NbtTag.List);
        WriteString(name.AsSpan());
        WriteByte(NbtTag.Short);
        WriteInt(values.Length);

        WriteShortSpan(values);
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, short[]? values)
    {
        WriteList(encodedName, values.AsSpan());
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, ReadOnlySpan<short> values)
    {
        WriteByte(NbtTag.List);
        WriteString(encodedName);
        WriteByte(NbtTag.Short);
        WriteInt(values.Length);

        WriteShortSpan(values);
    }

    public void WriteList(string? name, int[]? values)
    {
        WriteList(name, values.AsSpan());
    }

    public void WriteList(string? name, ReadOnlySpan<int> values)
    {
        WriteByte(NbtTag.List);
        WriteString(name.AsSpan());
        WriteByte(NbtTag.Int);
        WriteInt(values.Length);

        WriteIntSpan(values);
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, int[]? values)
    {
        WriteList(encodedName, values.AsSpan());
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, ReadOnlySpan<int> values)
    {
        WriteByte(NbtTag.List);
        WriteString(encodedName);
        WriteByte(NbtTag.Int);
        WriteInt(values.Length);

        WriteIntSpan(values);
    }

    public void WriteList(string? name, long[]? values)
    {
        WriteList(name, values.AsSpan());
    }

    public void WriteList(string? name, ReadOnlySpan<long> values)
    {
        WriteByte(NbtTag.List);
        WriteString(name.AsSpan());
        WriteByte(NbtTag.Long);
        WriteInt(values.Length);

        WriteLongSpan(values);
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, long[]? values)
    {
        WriteList(encodedName, values.AsSpan());
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, ReadOnlySpan<long> values)
    {
        WriteByte(NbtTag.List);
        WriteString(encodedName);
        WriteByte(NbtTag.Long);
        WriteInt(values.Length);

        WriteLongSpan(values);
    }

    public void WriteList(string? name, float[]? values)
    {
        WriteList(name, values.AsSpan());
    }

    public void WriteList(string? name, ReadOnlySpan<float> values)
    {
        WriteByte(NbtTag.List);
        WriteString(name.AsSpan());
        WriteByte(NbtTag.Float);
        WriteInt(values.Length);

        WriteFloatSpan(values);
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, float[]? values)
    {
        WriteList(encodedName, values.AsSpan());
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, ReadOnlySpan<float> values)
    {
        WriteByte(NbtTag.List);
        WriteString(encodedName);
        WriteByte(NbtTag.Float);
        WriteInt(values.Length);

        WriteFloatSpan(values);
    }

    public void WriteList(string? name, double[]? values)
    {
        WriteList(name, values.AsSpan());
    }

    public void WriteList(string? name, ReadOnlySpan<double> values)
    {
        WriteByte(NbtTag.List);
        WriteString(name.AsSpan());
        WriteByte(NbtTag.Double);
        WriteInt(values.Length);

        WriteDoubleSpan(values);
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, double[]? values)
    {
        WriteList(encodedName, values.AsSpan());
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, ReadOnlySpan<double> values)
    {
        WriteByte(NbtTag.List);
        WriteString(encodedName);
        WriteByte(NbtTag.Double);
        WriteInt(values.Length);

        WriteDoubleSpan(values);
    }

    public void WriteList(string? name, string[]? values)
    {
        WriteByte(NbtTag.List);
        WriteString(name.AsSpan());
        WriteList(values);
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, string[]? values)
    {
        WriteByte(NbtTag.List);
        WriteString(encodedName);
        WriteByte(NbtTag.String);
        WriteStringArray(values);
    }

    public void WriteList(string[]? values)
    {
        WriteByte(NbtTag.List);
        WriteByte(NbtTag.String);
        WriteStringArray(values);
    }

    private void WriteStringArray(string[]? values)
    {
        if (values is null)
        {
            WriteInt(0);
            return;
        }

        WriteInt(values.Length);

        for (int i = 0; i < values.Length; i++)
        {
            WriteString(values[i].AsSpan());
        }
    }

    public void WriteEmptyList(string? name)
    {
        WriteByte(NbtTag.List);
        WriteString(name.AsSpan());
        GetSpan(5).Clear();
    }

    public void WriteEmptyList(ReadOnlySpan<byte> encodedName)
    {
        WriteByte(NbtTag.List);
        WriteString(encodedName);
        GetSpan(5).Clear();
    }

    public void WriteEmptyList()
    {
        WriteByte(NbtTag.List);
        GetSpan(5).Clear(); // 1 byte = End tag, 4 bytes = length
    }

    public void WriteList(string? name, NbtTag listType, int length)
    {
        ArgumentNullException.ThrowIfNull(name);

        // NbtBinaryWriter is not supposed to verify format validity
        //ThrowHelper.ThrowIfNegative(length);
        //if ((listType == NbtTag.End && length > 0) || (listType is < NbtTag.Byte or > NbtTag.LongArray))
        //{
        //    ThrowHelper.ThrowInvalidTag(listType);
        //}

        WriteByte(NbtTag.List);
        WriteString(name.AsSpan());
        WriteByte(listType);
        WriteInt(length);
    }

    public void WriteList(ReadOnlySpan<byte> encodedName, NbtTag listType, int length)
    {
        WriteByte(NbtTag.List);
        WriteString(encodedName);
        WriteByte(listType);
        WriteInt(length);
    }

    public void WriteList(NbtTag listType, int length)
    {
        Span<byte> span = GetSpan(2 * sizeof(byte) + sizeof(int)); // List + ListType + Length
        span[1] = (byte)listType;
        span[0] = (byte)NbtTag.List;
        span = span.Advance(2);
        BinaryPrimitives.WriteInt32BigEndian(span, length);
    }
}
