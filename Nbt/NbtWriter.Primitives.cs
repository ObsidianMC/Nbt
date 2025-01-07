using System.Buffers.Binary;
using System.Text;

namespace Obsidian.Nbt;

public partial struct NbtWriter
{
    public readonly void WriteString(string value) => this.WriteString(null, value);

    public readonly void WriteString(string? name, string value)
    {
        this.Validate(name, NbtTagType.String);

        if (!string.IsNullOrEmpty(value))
        {
            this.Write(NbtTagType.String);
            this.WriteStringInternal(name);
        }

        this.WriteStringInternal(value);
    }

    public readonly void WriteByte(byte value) => this.WriteByte(null, value);

    public readonly void WriteByte(string? name, byte value)
    {
        this.Validate(name, NbtTagType.Byte);

        if (!string.IsNullOrEmpty(name))
        {
            this.Write(NbtTagType.Byte);
            this.WriteStringInternal(name);
        }

        this.WriteByteInternal(value);
    }

    public readonly void WriteBool(bool value) => this.WriteBool(null, value);

    public readonly void WriteBool(string? name, bool value)
    {
        this.Validate(name, NbtTagType.Byte);

        if (!string.IsNullOrEmpty(name))
        {
            this.Write(NbtTagType.Byte);
            this.WriteStringInternal(name);
        }

        this.WriteByteInternal((byte)(value ? 1 : 0));
    }

    public readonly void WriteShort(short value) => this.WriteShort(null, value);

    public readonly void WriteShort(string? name, short value)
    {
        this.Validate(name, NbtTagType.Short);

        if (!string.IsNullOrEmpty(name))
        {
            this.Write(NbtTagType.Short);
            this.WriteStringInternal(name);
        }

        this.WriteShortInternal(value);
    }

    public readonly void WriteInt(int value) => this.WriteInt(null, value);

    public readonly void WriteInt(string? name, int value)
    {
        this.Validate(name, NbtTagType.Int);

        if (!string.IsNullOrEmpty(name))
        {
            this.Write(NbtTagType.Int);
            this.WriteStringInternal(name);
        }

        this.WriteIntInternal(value);
    }

    public readonly void WriteLong(long value) => this.WriteLong(null, value);

    public readonly void WriteLong(string? name, long value)
    {
        this.Validate(name, NbtTagType.Long);

        if (!string.IsNullOrEmpty(name))
        {
            this.Write(NbtTagType.Long);
            this.WriteStringInternal(name);
        }

        this.WriteLongInternal(value);
    }

    public readonly void WriteFloat(float value) => this.WriteFloat(null, value);

    public readonly void WriteFloat(string? name, float value)
    {
        this.Validate(name, NbtTagType.Float);

        if (!string.IsNullOrEmpty(name))
        {
            this.Write(NbtTagType.Float);
            this.WriteStringInternal(name);
        }

        this.WriteFloatInternal(value);
    }

    public readonly void WriteDouble(double value) => this.WriteDouble(null, value);

    public readonly void WriteDouble(string? name, double value)
    {
        this.Validate(name, NbtTagType.Double);

        if (!string.IsNullOrEmpty(name))
        {
            this.Write(NbtTagType.Double);
            this.WriteStringInternal(name);
        }

        this.WriteDoubleInternal(value);
    }

    internal readonly void Write(NbtTagType tagType) => this.WriteByteInternal((byte)tagType);

    internal readonly void WriteByteInternal(byte value) => this.BaseStream.WriteByte(value);

    internal readonly void WriteStringInternal(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length > short.MaxValue)
            throw new InvalidOperationException($"value length must be less than {short.MaxValue}");

        var buffer = Encoding.UTF8.GetBytes(value);

        this.WriteShortInternal((short)buffer.Length);
        this.BaseStream.Write(buffer);
    }
    internal readonly void WriteShortInternal(short value)
    {
        Span<byte> buffer = stackalloc byte[2];

        BinaryPrimitives.WriteInt16BigEndian(buffer, value);

        this.BaseStream.Write(buffer);
    }

    internal readonly void WriteIntInternal(int value)
    {
        Span<byte> buffer = stackalloc byte[4];

        BinaryPrimitives.WriteInt32BigEndian(buffer, value);

        this.BaseStream.Write(buffer);
    }

    internal readonly void WriteFloatInternal(float value)
    {
        Span<byte> buffer = stackalloc byte[4];

        BinaryPrimitives.WriteSingleBigEndian(buffer, value);

        this.BaseStream.Write(buffer);
    }

    internal readonly void WriteLongInternal(long value)
    {
        Span<byte> buffer = stackalloc byte[8];

        BinaryPrimitives.WriteInt64BigEndian(buffer, value);

        this.BaseStream.Write(buffer);
    }

    internal readonly void WriteDoubleInternal(double value)
    {
        Span<byte> buffer = stackalloc byte[8];

        BinaryPrimitives.WriteDoubleBigEndian(buffer, value);

        this.BaseStream.Write(buffer);
    }
}
