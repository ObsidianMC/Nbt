using System.Buffers.Binary;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Obsidian.Nbt.Utilities;

internal static class RefReader
{
    internal static ushort ReadUShort(ref byte source)
    {
        ushort value = Unsafe.As<byte, ushort>(ref source);
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    internal static short ReadShort(ref byte source)
    {
        short value = Unsafe.As<byte, short>(ref source);
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    internal static int ReadInt(ref byte source)
    {
        int value = Unsafe.As<byte, int>(ref source);
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    internal static long ReadLong(ref byte source)
    {
        long value = Unsafe.As<byte, long>(ref source);
        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    internal static float ReadFloat(ref byte source)
    {
        if (BitConverter.IsLittleEndian)
        {
            int temp = Unsafe.As<byte, int>(ref source);
            temp = BinaryPrimitives.ReverseEndianness(temp);
            return BitConverter.Int32BitsToSingle(temp);
        }
        return Unsafe.As<byte, float>(ref source);
    }

    internal static double ReadDouble(ref byte source)
    {
        if (BitConverter.IsLittleEndian)
        {
            long temp = Unsafe.As<byte, long>(ref source);
            temp = BinaryPrimitives.ReverseEndianness(temp);
            return BitConverter.Int64BitsToDouble(temp);
        }
        return Unsafe.As<byte, double>(ref source);
    }

    internal static void ReadBytes(ref byte source, ref byte destination, int count)
    {
        Unsafe.CopyBlockUnaligned(ref destination, ref source, (uint)count);
    }

    internal static void ReadInts(ref byte source, ref byte destination, int count)
    {
        int i = 0;
        if (Avx2.IsSupported)
        {
            int end = count - 4;
            for (; i <= end; i += 4)
            {
                Vector256<int> vector = Unsafe.ReadUnaligned<Vector256<int>>(ref source);
                if (BitConverter.IsLittleEndian)
                {
                    vector = Avx2.Shuffle(
                        vector.AsByte(),
                        Vector256.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12, 19, 18, 17, 16, 23, 22, 21, 20, 27, 26, 25, 24, 31, 30, 29, 28)
                        ).AsInt32();
                }
                Unsafe.WriteUnaligned(ref destination, vector);
                source = ref Unsafe.Add(ref source, 32);
                destination = ref Unsafe.Add(ref destination, 32);
            }
        }
        else if (Ssse3.IsSupported)
        {
            int end = count - 2;
            for (; i <= end; i += 2)
            {
                Vector128<int> vector = Unsafe.ReadUnaligned<Vector128<int>>(ref source);
                if (BitConverter.IsLittleEndian)
                {
                    vector = Ssse3.Shuffle(
                        vector.AsByte(),
                        Vector128.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)
                        ).AsInt32();
                }
                Unsafe.WriteUnaligned(ref destination, vector);
                source = ref Unsafe.Add(ref source, 16);
                destination = ref Unsafe.Add(ref destination, 16);
            }
        }

        ReadIntsScalar(ref Unsafe.As<byte, int>(ref source), ref Unsafe.As<byte, int>(ref destination), count - i);
    }

    private static void ReadIntsScalar(ref int source, ref int destination, int count)
    {
        ref int end = ref Unsafe.Add(ref source, count);
        while (Unsafe.IsAddressLessThan(ref source, ref end))
        {
            Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref destination), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(source) : source);
            destination = ref Unsafe.Add(ref destination, 1);
            source = ref Unsafe.Add(ref source, 1);
        }
    }

    internal static unsafe void ReadLongs(ref byte source, ref byte destination, int count)
    {
        int i = 0;
        if (Avx2.IsSupported)
        {
            int end = count - 4;
            for (; i <= end; i += 4)
            {
                Vector256<long> vector = Unsafe.ReadUnaligned<Vector256<long>>(ref source);
                if (BitConverter.IsLittleEndian)
                {
                    vector = Avx2.Shuffle(
                        vector.AsByte(),
                        Vector256.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24)
                        ).AsInt64();
                }
                Unsafe.WriteUnaligned(ref destination, vector);
                source = ref Unsafe.Add(ref source, 32);
                destination = ref Unsafe.Add(ref destination, 32);
            }
        }
        else if (Ssse3.IsSupported)
        {
            int end = count - 2;
            for (; i <= end; i += 2)
            {
                Vector128<long> vector = Unsafe.ReadUnaligned<Vector128<long>>(ref source);
                if (BitConverter.IsLittleEndian)
                {
                    vector = Ssse3.Shuffle(
                        vector.AsByte(),
                        Vector128.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)
                        ).AsInt64();
                }
                Unsafe.WriteUnaligned(ref destination, vector);
                source = ref Unsafe.Add(ref source, 16);
                destination = ref Unsafe.Add(ref destination, 16);
            }
        }

        ReadLongsScalar(ref Unsafe.As<byte, long>(ref source), ref Unsafe.As<byte, long>(ref destination), count - i);
    }

    private static void ReadLongsScalar(ref long source, ref long destination, int count)
    {
        ref long end = ref Unsafe.Add(ref source, count);
        while (Unsafe.IsAddressLessThan(ref source, ref end))
        {
            Unsafe.WriteUnaligned(ref Unsafe.As<long, byte>(ref destination), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(source) : source);
            destination = ref Unsafe.Add(ref destination, 1);
            source = ref Unsafe.Add(ref source, 1);
        }
    }

    internal static string ReadString(ref byte source, int length)
    {
        return ModifiedUtf8.GetString(MemoryMarshal.CreateSpan(ref source, length));
    }
}
