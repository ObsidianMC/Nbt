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

    internal static unsafe void ReadInts(ref byte source, ref byte destination, int count)
    {
        int* sPtr = (int*)Unsafe.AsPointer(ref source);
        int* dPtr = (int*)Unsafe.AsPointer(ref destination);

        int i = 0;
        if (Avx2.IsSupported)
        {
            int end = count - 4;
            for (; i <= end; i += 4)
            {
                Vector256<int> vector = Avx2.LoadVector256(sPtr);
                Vector256<int> shuffled = Avx2.Shuffle(
                    vector.AsByte(),
                    Vector256.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12, 19, 18, 17, 16, 23, 22, 21, 20, 27, 26, 25, 24, 31, 30, 29, 28)
                    ).AsInt32();
                Avx2.Store(dPtr, shuffled);
                sPtr++;
                dPtr++;
            }
        }
        else if (Ssse3.IsSupported)
        {
            int end = count - 2;
            for (; i <= end; i += 2)
            {
                Vector128<int> vector = Ssse3.LoadVector128(sPtr);
                Vector128<int> shuffled = Ssse3.Shuffle(
                    vector.AsByte(),
                    Vector128.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)
                    ).AsInt32();
                Ssse3.Store(dPtr, shuffled);
                sPtr++;
                dPtr++;
            }
        }

        count -= i;
        i = 0;
        for (; i < count; i++)
        {
            dPtr[i] = BinaryPrimitives.ReverseEndianness(sPtr[i]);
        }
    }

    internal static unsafe void ReadLongs(ref byte source, ref byte destination, int count)
    {
        long* sPtr = (long*)Unsafe.AsPointer(ref source);
        long* dPtr = (long*)Unsafe.AsPointer(ref destination);

        int i = 0;
        if (Avx2.IsSupported)
        {
            int end = count - 4;
            for (; i <= end; i += 4)
            {
                Vector256<long> vector = Avx2.LoadVector256(sPtr);
                Vector256<long> shuffled = Avx2.Shuffle(
                    vector.AsByte(),
                    Vector256.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24)
                    ).AsInt64();
                Avx2.Store(dPtr, shuffled);
                sPtr++;
                dPtr++;
            }
        }
        else if (Ssse3.IsSupported)
        {
            int end = count - 2;
            for (; i <= end; i += 2)
            {
                Vector128<long> vector = Ssse3.LoadVector128(sPtr);
                Vector128<long> shuffled = Ssse3.Shuffle(
                    vector.AsByte(),
                    Vector128.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)
                    ).AsInt64();
                Ssse3.Store(dPtr, shuffled);
                sPtr++;
                dPtr++;
            }
        }

        count -= i;
        i = 0;
        for (; i < count; i++)
        {
            dPtr[i] = BinaryPrimitives.ReverseEndianness(sPtr[i]);
        }
    }

    internal static string ReadString(ref byte source, int length)
    {
        return ModifiedUtf8.GetString(MemoryMarshal.CreateSpan(ref source, length));
    }
}
