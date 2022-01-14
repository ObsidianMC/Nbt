using System.Numerics;

namespace Obsidian.Nbt.Utilities;

internal static class SpanHelper
{
    internal static Span<byte> Advance(this Span<byte> span, int advanceBy)
    {
        ref byte oldRef = ref MemoryMarshal.GetReference(span);
        ref byte newRef = ref Unsafe.Add(ref oldRef, advanceBy);
        return MemoryMarshal.CreateSpan(ref newRef, span.Length - advanceBy);
    }
}
