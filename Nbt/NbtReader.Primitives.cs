using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace Obsidian.Nbt;
public partial struct NbtReader
{
    private const int StackallocStringThreshold = 256;

    public byte ReadByte()
    {
        var value = this.BaseStream.ReadByte();
        return value >= 0 ? (byte)value : throw new EndOfStreamException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString()
    {
        var length = this.ReadInt16();

        if (length <= 0)
            return string.Empty;

        if (length <= StackallocStringThreshold)
        {
            Span<byte> buffer = stackalloc byte[length];
            this.BaseStream.ReadExactly(buffer);
            return ModifiedUtf8.GetString(buffer);
        }

        var rentedBuffer = GC.AllocateUninitializedArray<byte>(length);
        this.BaseStream.ReadExactly(rentedBuffer);
        return ModifiedUtf8.GetString(rentedBuffer);
    }

    public short ReadInt16()
    {
        Span<byte> scratch = stackalloc byte[2];
        this.BaseStream.ReadExactly(scratch);

        return BinaryPrimitives.ReadInt16BigEndian(scratch);
    }

    public int ReadInt32()
    {
        Span<byte> scratch = stackalloc byte[4];
        this.BaseStream.ReadExactly(scratch);

        return BinaryPrimitives.ReadInt32BigEndian(scratch);
    }

    public long ReadInt64()
    {
        Span<byte> scratch = stackalloc byte[8];
        this.BaseStream.ReadExactly(scratch);

        return BinaryPrimitives.ReadInt64BigEndian(scratch);
    }

    public float ReadSingle()
    {
        Span<byte> scratch = stackalloc byte[4];
        this.BaseStream.ReadExactly(scratch);

        return BinaryPrimitives.ReadSingleBigEndian(scratch);
    }

    public double ReadDouble()
    {
        Span<byte> scratch = stackalloc byte[8];
        this.BaseStream.ReadExactly(scratch);

        return BinaryPrimitives.ReadDoubleBigEndian(scratch);
    }
}
