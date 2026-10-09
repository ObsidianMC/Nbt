using System.Buffers.Binary;
using System.IO;

namespace Obsidian.Nbt;

public partial struct NbtReader
{
    public byte ReadByte()
    {
        var value = this.BaseStream.ReadByte();
        return value >= 0 ? (byte)value : throw new EndOfStreamException();
    }

    /// <summary>Reads a string: an unsigned 16-bit byte count, then modified UTF-8.</summary>
    public string ReadString()
    {
        var parser = new NbtTagParser<StreamNbtInput>(new StreamNbtInput(this.BaseStream), NbtTagParser.Unlimited);
        try
        {
            return parser.ReadString();
        }
        finally
        {
            parser.Input.Dispose();
        }
    }

    public short ReadInt16()
    {
        Span<byte> scratch = stackalloc byte[sizeof(short)];
        this.BaseStream.ReadExactly(scratch);

        return BinaryPrimitives.ReadInt16BigEndian(scratch);
    }

    public int ReadInt32()
    {
        Span<byte> scratch = stackalloc byte[sizeof(int)];
        this.BaseStream.ReadExactly(scratch);

        return BinaryPrimitives.ReadInt32BigEndian(scratch);
    }

    public long ReadInt64()
    {
        Span<byte> scratch = stackalloc byte[sizeof(long)];
        this.BaseStream.ReadExactly(scratch);

        return BinaryPrimitives.ReadInt64BigEndian(scratch);
    }

    public float ReadSingle()
    {
        Span<byte> scratch = stackalloc byte[sizeof(float)];
        this.BaseStream.ReadExactly(scratch);

        return BinaryPrimitives.ReadSingleBigEndian(scratch);
    }

    public double ReadDouble()
    {
        Span<byte> scratch = stackalloc byte[sizeof(double)];
        this.BaseStream.ReadExactly(scratch);

        return BinaryPrimitives.ReadDoubleBigEndian(scratch);
    }
}
