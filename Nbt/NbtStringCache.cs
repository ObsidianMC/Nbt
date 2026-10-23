using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace Obsidian.Nbt;

/// <summary>
/// Reuses the strings read for short NBT names and values. Saved and sent NBT repeats the same keys ("Name",
/// "Properties", "id") and values ("minecraft:stone", "north") over and over, so most reads find their string here
/// instead of allocating a new one.
/// </summary>
/// <remarks>
/// Each thread has its own small table, indexed by a hash of the bytes; a string evicts whatever shared its slot.
/// A hit compares every byte, so collisions cost only a miss.
/// </remarks>
internal static class NbtStringCache
{
    // Longer strings are rarely repeated (lore, text components) and cost more to compare.
    private const int MaxLength = 32;

    // A power of two. A chunk has a few dozen distinct keys and palette names; this keeps them with room to spare.
    private const int Size = 256;

    [ThreadStatic]
    private static string?[]? entries;

    /// <summary>
    /// Decodes modified UTF-8 <paramref name="bytes"/>, reusing an earlier string with the same bytes.
    /// </summary>
    /// <exception cref="FormatException">The bytes are not valid modified UTF-8.</exception>
    // Kept out of the parser: inlined into its read loop, this made stream reads about 20% slower.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string GetString(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            return string.Empty;

        if (bytes.Length > MaxLength)
            return ModifiedUtf8.GetString(bytes);

        var table = entries ??= new string?[Size];
        ref var slot = ref table[Hash(bytes) & (Size - 1)];

        // Only ASCII strings are cached, so the same length means one byte for each char.
        if (slot is string cached && cached.Length == bytes.Length && Ascii.Equals(bytes, cached))
            return cached;

        var value = ModifiedUtf8.GetString(bytes);

        // In modified UTF-8 only ASCII decodes to as many chars as bytes.
        if (value.Length == bytes.Length)
            slot = value;

        return value;
    }

    // Mixes the length and up to three overlapping 8-byte words; cheap, and spread enough for a small table.
    private static int Hash(ReadOnlySpan<byte> bytes)
    {
        const ulong Multiplier = 0x9E3779B97F4A7C15;

        var length = bytes.Length;
        ulong first;
        ulong last;
        ulong middle = 0;

        if (length >= sizeof(ulong))
        {
            first = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
            last = BinaryPrimitives.ReadUInt64LittleEndian(bytes[^sizeof(ulong)..]);

            if (length > 2 * sizeof(ulong))
                middle = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(length / 2 - sizeof(uint)));
        }
        else if (length >= sizeof(uint))
        {
            first = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
            last = BinaryPrimitives.ReadUInt32LittleEndian(bytes[^sizeof(uint)..]);
        }
        else
        {
            first = bytes[0];
            last = (ulong)bytes[length / 2] << 8 | (ulong)bytes[^1] << 16;
        }

        var hash = ((ulong)length ^ first) * Multiplier;
        hash = (hash ^ last) * Multiplier;
        hash = (hash ^ middle) * Multiplier;

        // The top bits are the best mixed.
        return (int)(hash >> 32);
    }
}
