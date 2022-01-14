using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Obsidian.Nbt;

// NBT uses a modification of UTF-8 https://docs.oracle.com/javase/8/docs/api/java/io/DataInput.html#modified-utf-8
public static class ModifiedUtf8
{
    private static readonly UTF8Encoding utf8 = new(false, true);
    private const int MaxStackalloc = 1024;
    private const int MaxBytesPerChar = 3;

    public static int GetByteCount(ReadOnlySpan<char> chars)
    {
        return utf8.GetByteCount(chars);
    }

    public static int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
    {
        return utf8.GetBytes(chars, bytes);
    }

    public static string GetString(ReadOnlySpan<byte> bytes)
    {
        return utf8.GetString(bytes);
    }

    private static unsafe int GetByteCountAvx2(string value)
    {
        const short TwoBytesBorder = (0x0080 >> 1) - 1;
        const short ThreeBytesBorder = (0x0800 >> 1) - 1;

        int byteCount = value.Length;
        fixed (char* valuePtr = value)
        {
            ushort* ptr = (ushort*)valuePtr;
            ushort* ptrEnd = ptr + value.Length - 16;
            Vector256<short> counter = Vector256<short>.Zero;
            for (; ptr <= ptrEnd; ptr += 16)
            {
                Vector256<ushort> vustr = Avx2.LoadVector256(ptr);
                Vector256<ushort> mask = Avx2.CompareEqual(vustr, Vector256<ushort>.Zero);
                Vector256<ushort> blend = Avx2.BlendVariable(vustr, Vector256.Create((ushort)0x0080), mask);
                Vector256<short> vstr = Avx2.ShiftRightLogical(blend, 1).AsInt16();
                counter = Avx2.Subtract(counter, Avx2.CompareGreaterThan(vstr, Vector256.Create(TwoBytesBorder)));
                counter = Avx2.Subtract(counter, Avx2.CompareGreaterThan(vstr, Vector256.Create(ThreeBytesBorder)));
            }

            Vector128<short> counter128 = Avx2.Add(counter.GetLower(), counter.GetUpper());
            counter128 = Avx2.HorizontalAdd(counter128, counter128);
            counter128 = Avx2.HorizontalAdd(counter128, counter128);
            counter128 = Avx2.HorizontalAdd(counter128, counter128);
            byteCount += counter128.ToScalar();

            ptrEnd += 16;
            while (ptr < ptrEnd)
            {
                ushort c = *ptr;
                if ((c >= 0x0001) && (c <= 0x007F))
                {
                }
                else if (c > 0x07FF)
                {
                    byteCount += 2;
                }
                else
                {
                    byteCount++;
                }
                ptr++;
            }
        }
        return byteCount;
    }
}
