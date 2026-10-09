using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace Obsidian.Nbt;

/// <summary>
/// Reads binary NBT from a stream. For network NBT (a root tag without a name, since 1.20.2) pass
/// <c>readName: false</c>. For data already in memory, <see cref="ReadTag"/> and, for packets,
/// <see cref="ReadNetworkTag"/> avoid the stream entirely.
/// </summary>
/// <remarks>
/// Reading from a <see cref="MemoryStream"/> whose buffer is exposed parses its buffer in place. Other streams are
/// read only as far as the tag goes, so callers can keep reading or seeking after it.
/// </remarks>
public readonly partial struct NbtReader(Stream input, NbtCompression compressionMode = NbtCompression.None)
    : IEquatable<NbtReader>
{
    // The reader owns the decompression stream, so it can buffer ahead of the tag; reading a compressed stream a few
    // bytes at a time is very slow otherwise.
    public Stream BaseStream { get; } = compressionMode switch
    {
        NbtCompression.GZip => new BufferedStream(new GZipStream(input, CompressionMode.Decompress)),
        NbtCompression.ZLib => new BufferedStream(new ZLibStream(input, CompressionMode.Decompress)),
        _ => input
    };

    /// <summary>
    /// The memory budget for network NBT, by vanilla's estimate of each tag's size: FriendlyByteBuf.readNbt allows
    /// 2 MiB, and <see cref="ReadNetworkTag"/> rejects what vanilla rejects.
    /// </summary>
    public const long NetworkQuota = NbtTagParser.NetworkQuota;

    /// <summary>
    /// Reads one tag from <paramref name="data"/>: its type, its name when <paramref name="readName"/> is set, and its
    /// payload. For NBT from a client use <see cref="ReadNetworkTag"/>, which also limits its size.
    /// </summary>
    /// <param name="data">The bytes to read; they may continue past the tag.</param>
    /// <param name="readName">Whether the root tag has a name.</param>
    /// <param name="bytesRead">How many bytes the tag took, including its type.</param>
    /// <returns>The tag, or null for an end tag or empty data.</returns>
    /// <exception cref="InvalidDataException">The data is not valid NBT or is nested too deeply.</exception>
    /// <exception cref="EndOfStreamException">The data ends before the tag does.</exception>
    public static INbtTag? ReadTag(ReadOnlySpan<byte> data, bool readName, out int bytesRead) =>
        ReadTag(data, readName, NbtTagParser.Unlimited, out bytesRead);

    /// <summary>
    /// Reads one network NBT tag from <paramref name="data"/> the way vanilla reads it from a packet: its type, no
    /// name, then its payload, rejecting tags bigger than <see cref="NetworkQuota"/>.
    /// </summary>
    /// <param name="data">The packet bytes from the tag on; they may continue past the tag.</param>
    /// <param name="bytesRead">How many bytes the tag took, including its type.</param>
    /// <returns>The tag, or null for an end tag (an absent optional tag) or empty data.</returns>
    /// <exception cref="InvalidDataException">
    /// The data is not valid NBT, is nested too deeply or is too big.
    /// </exception>
    /// <exception cref="EndOfStreamException">The data ends before the tag does.</exception>
    public static INbtTag? ReadNetworkTag(ReadOnlySpan<byte> data, out int bytesRead) =>
        ReadTag(data, false, NbtTagParser.NetworkQuota, out bytesRead);

    /// <summary>Reads the next tag, or returns null for an end tag or at the end of the stream.</summary>
    public INbtTag? ReadNextTag(bool readName = true)
    {
        if (this.BaseStream is MemoryStream memory && memory.TryGetBuffer(out var buffer))
        {
            var start = (int)Math.Min(memory.Position, buffer.Count);
            var tag = ReadTag(buffer.AsSpan(start), readName, out var bytesRead);

            memory.Position += bytesRead;
            return tag;
        }

        var parser = new NbtTagParser<StreamNbtInput>(new StreamNbtInput(this.BaseStream), NbtTagParser.Unlimited);
        try
        {
            return parser.ReadRoot(readName);
        }
        finally
        {
            parser.Input.Dispose();
        }
    }

    private static INbtTag? ReadTag(ReadOnlySpan<byte> data, bool readName, long quota, out int bytesRead)
    {
        var parser = new NbtTagParser<SpanNbtInput>(new SpanNbtInput(data), quota);
        var tag = parser.ReadRoot(readName);

        bytesRead = parser.Input.Consumed;
        return tag;
    }

    internal NbtCompound ReadRootCompound() =>
        this.ReadNextTag() as NbtCompound ?? throw new InvalidOperationException("Unable to read the root compound.");

    public bool TryReadNextTag(bool readName, [MaybeNullWhen(false)] out INbtTag tag)
    {
        tag = this.ReadNextTag(readName);
        return tag is not null;
    }

    public bool TryReadNextTag<T>(bool readName, [MaybeNullWhen(false)] out T tag) where T : INbtTag
    {
        if (this.ReadNextTag(readName) is T matchedTag)
        {
            tag = matchedTag;
            return true;
        }

        tag = default;
        return false;
    }

    public bool TryReadNextTag([MaybeNullWhen(false)] out INbtTag tag) => this.TryReadNextTag(true, out tag);

    public bool TryReadNextTag<T>([MaybeNullWhen(false)] out T tag) where T : INbtTag =>
        this.TryReadNextTag(true, out tag);

    // Readers are equal when they read the same stream.
    public bool Equals(NbtReader other) => ReferenceEquals(this.BaseStream, other.BaseStream);

    public override bool Equals(object? obj) => obj is NbtReader other && this.Equals(other);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this.BaseStream);

    public static bool operator ==(NbtReader left, NbtReader right) => left.Equals(right);

    public static bool operator !=(NbtReader left, NbtReader right) => !left.Equals(right);
}
