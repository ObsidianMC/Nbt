using System.Buffers.Binary;

namespace Obsidian.Nbt.Utilities;

internal static class RefWriter
{
    internal static void WriteUShort(ref byte destination, ushort value)
    {
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
        Unsafe.WriteUnaligned(ref destination, value);
    }

    internal static void WriteShort(ref byte destination, short value)
    {
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
        Unsafe.WriteUnaligned(ref destination, value);
    }

    internal static void WriteInt(ref byte destination, int value)
    {
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
        Unsafe.WriteUnaligned(ref destination, value);
    }

    internal static void WriteLong(ref byte destination, long value)
    {
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
        Unsafe.WriteUnaligned(ref destination, value);
    }

    internal static void WriteFloat(ref byte destination, float value)
    {
        if (BitConverter.IsLittleEndian)
        {
            int temp = BitConverter.SingleToInt32Bits(value);
            value = BinaryPrimitives.ReverseEndianness(temp);
        }
        Unsafe.WriteUnaligned(ref destination, value);
    }

    internal static void WriteDouble(ref byte destination, double value)
    {
        if (BitConverter.IsLittleEndian)
        {
            long temp = BitConverter.DoubleToInt64Bits(value);
            value = BinaryPrimitives.ReverseEndianness(temp);
        }
        Unsafe.WriteUnaligned(ref destination, value);
    }

    internal static void WriteString(Span<byte> destination, ReadOnlySpan<char> value)
    {
        ModifiedUtf8.GetBytes(value, destination);
    }

    internal static void WriteBytes(ref byte destination, ReadOnlySpan<byte> value)
    {
        Unsafe.CopyBlockUnaligned(ref destination, ref MemoryMarshal.GetReference(value), (uint)value.Length);
    }
}
