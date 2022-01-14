using Obsidian.Nbt.Utilities;

namespace Obsidian.Nbt;

public partial struct NbtWriter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteByte(NbtTag tag)
    {
        WriteByte((byte)tag);
    }

    public void WriteByte(byte value)
    {
        if (index == buffer.Length)
        {
            Flush();
        }
        WriteByteDirect(ref value);
    }

    public void WriteBool(bool value)
    {
        if (index == buffer.Length)
        {
            Flush();
        }
        WriteByteDirect(ref Unsafe.As<bool, byte>(ref value));
    }

    public void WriteShort(short value)
    {
        RefWriter.WriteShort(ref GetRef(sizeof(short)), value);
    }

    public void WriteInt(int value)
    {
        RefWriter.WriteInt(ref GetRef(sizeof(int)), value);
    }

    public void WriteLong(long value)
    {
        RefWriter.WriteLong(ref GetRef(sizeof(long)), value);
    }

    public void WriteFloat(float value)
    {
        RefWriter.WriteFloat(ref GetRef(sizeof(float)), value);
    }

    public void WriteDouble(double value)
    {
        RefWriter.WriteDouble(ref GetRef(sizeof(double)), value);
    }

    private void WriteUShort(ushort value)
    {
        RefWriter.WriteUShort(ref GetRef(sizeof(ushort)), value);
    }

    public void WriteString(string? value)
    {
        WriteString(value.AsSpan());
    }

    public void WriteString(ReadOnlySpan<byte> encodedValue)
    {
        if (encodedValue.IsEmpty)
            return;
        if (encodedValue.Length > ushort.MaxValue)
            ThrowHelper.ThrowInvalidOperationException_StringTooLong();
        WriteUShort((ushort)encodedValue.Length);
        RefWriter.WriteBytes(ref GetRef(encodedValue.Length), encodedValue);
    }

    public void WriteString(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            WriteUShort(0);
            return;
        }

        int length = ModifiedUtf8.GetByteCount(value);
        if (length > ushort.MaxValue)
            ThrowHelper.ThrowInvalidOperationException_StringTooLong();
        WriteUShort((ushort)length);
        RefWriter.WriteString(GetSpan(length), value);
    }

    public void WriteByte(string? name, byte value)
    {
        WriteByte(NbtTag.Byte);
        WriteString(name.AsSpan());
        WriteByte(value);
    }

    public void WriteBool(string? name, bool value)
    {
        WriteByte(NbtTag.Byte);
        WriteString(name.AsSpan());
        WriteBool(value);
    }

    public void WriteShort(string? name, short value)
    {
        WriteByte(NbtTag.Short);
        WriteString(name.AsSpan());
        WriteShort(value);
    }

    public void WriteInt(string? name, int value)
    {
        WriteByte(NbtTag.Int);
        WriteString(name.AsSpan());
        WriteInt(value);
    }

    public void WriteLong(string? name, long value)
    {
        WriteByte(NbtTag.Long);
        WriteString(name.AsSpan());
        WriteLong(value);
    }

    public void WriteFloat(string? name, float value)
    {
        WriteByte(NbtTag.Float);
        WriteString(name.AsSpan());
        WriteFloat(value);
    }

    public void WriteDouble(string? name, double value)
    {
        WriteByte(NbtTag.Double);
        WriteString(name.AsSpan());
        WriteDouble(value);
    }

    public void WriteString(string? name, string? value)
    {
        WriteByte(NbtTag.String);
        WriteString(name.AsSpan());
        WriteString(value.AsSpan());
    }

    public void WriteByte(ReadOnlySpan<byte> encodedName, byte value)
    {
        WriteByte(NbtTag.Byte);
        WriteString(encodedName);
        WriteByte(value);
    }

    public void WriteBool(ReadOnlySpan<byte> encodedName, bool value)
    {
        WriteByte(NbtTag.Byte);
        WriteString(encodedName);
        WriteBool(value);
    }

    public void WriteShort(ReadOnlySpan<byte> encodedName, short value)
    {
        WriteByte(NbtTag.Short);
        WriteString(encodedName);
        WriteShort(value);
    }

    public void WriteInt(ReadOnlySpan<byte> encodedName, int value)
    {
        WriteByte(NbtTag.Int);
        WriteString(encodedName);
        WriteInt(value);
    }

    public void WriteLong(ReadOnlySpan<byte> encodedName, long value)
    {
        WriteByte(NbtTag.Long);
        WriteString(encodedName);
        WriteLong(value);
    }

    public void WriteFloat(ReadOnlySpan<byte> encodedName, float value)
    {
        WriteByte(NbtTag.Float);
        WriteString(encodedName);
        WriteFloat(value);
    }

    public void WriteDouble(ReadOnlySpan<byte> encodedName, double value)
    {
        WriteByte(NbtTag.Double);
        WriteString(encodedName);
        WriteDouble(value);
    }

    public void WriteString(ReadOnlySpan<byte> encodedName, string? value)
    {
        WriteString(encodedName, value.AsSpan());
    }

    public void WriteString(ReadOnlySpan<byte> encodedName, ReadOnlySpan<char> value)
    {
        WriteByte(NbtTag.String);
        WriteString(encodedName);
        WriteString(value);
    }

    public void WriteString(ReadOnlySpan<byte> encodedName, ReadOnlySpan<byte> encodedValue)
    {
        WriteByte(NbtTag.String);
        WriteString(encodedName);
        WriteString(encodedValue);
    }
}
