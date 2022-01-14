using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Obsidian.Nbt.Utilities;

internal class ThrowHelper
{
    internal static void ThrowInvalidOperationException_StringTooLong()
    {
        throw new InvalidOperationException("Received string is longer than allowed.");
    }

    internal static void ThrowArgumentException_WrongTag(NbtTag tag)
    {
        throw new ArgumentException($"Tag '{nameof(NbtTag)}.{tag}' is not valid in this context.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowOutOfRangeException_IfNegative(int value, [CallerArgumentExpression("value")] string? paramName = null)
    {
        if (value < 0)
        {
            ThrowOutOfRangeException_Negative(value, paramName!);
        }
    }

    internal static void ThrowOutOfRangeException_Negative(int value, string paramName)
    {
        throw new ArgumentOutOfRangeException($"Value of {paramName} must be positive or zero.");
    }

    [DoesNotReturn]
    internal static void ThrowInvalidOperationException_NotEnoughData()
    {
        throw new InvalidOperationException("There isn't enough buffered data for this operation.");
    }

    internal static void ThrowInvalidOperationException_InvalidInstace()
    {
        throw new InvalidOperationException("Instance was not properly initialized.");
    }

    internal static void ThrowInvalidOperationException_IncorrectTagType()
    {
        throw new InvalidOperationException("Tag type doesn't match requested data type.");
    }
}
