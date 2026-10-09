using System.IO;

namespace Obsidian.Nbt;

/// <summary>
/// A source of NBT bytes for <see cref="NbtTagParser"/>. Implementations are structs so the parser is specialized for
/// each one and the calls are inlined.
/// </summary>
internal interface INbtInput
{
    /// <summary>The number of bytes left, or <see cref="long.MaxValue"/> when the input can't tell.</summary>
    long Remaining { get; }

    /// <summary>Reads one byte, or returns false at the end of the input.</summary>
    bool TryReadByte(out byte value);

    /// <summary>Reads the next <paramref name="count"/> bytes. The span is only valid until the next read.</summary>
    ReadOnlySpan<byte> Read(int count);

    /// <summary>Fills <paramref name="destination"/> with the next bytes.</summary>
    void ReadExactly(Span<byte> destination);
}

/// <summary>Reads NBT straight from memory, without copying.</summary>
internal ref struct SpanNbtInput(ReadOnlySpan<byte> data) : INbtInput
{
    private readonly ReadOnlySpan<byte> data = data;

    /// <summary>The number of bytes read so far.</summary>
    public int Consumed { get; private set; }

    public readonly long Remaining => this.data.Length - this.Consumed;

    public bool TryReadByte(out byte value)
    {
        if (this.Consumed >= this.data.Length)
        {
            value = 0;
            return false;
        }

        value = this.data[this.Consumed++];
        return true;
    }

    public ReadOnlySpan<byte> Read(int count)
    {
        if ((uint)count > (uint)(this.data.Length - this.Consumed))
            throw new EndOfStreamException();

        var bytes = this.data.Slice(this.Consumed, count);
        this.Consumed += count;

        return bytes;
    }

    public void ReadExactly(Span<byte> destination) => this.Read(destination.Length).CopyTo(destination);
}

/// <summary>
/// Reads NBT from a stream. It never reads past the bytes it needs, so the stream is left exactly after the tag, as
/// callers that mix tag reads with their own reads and seeks expect.
/// </summary>
internal struct StreamNbtInput : INbtInput, IDisposable
{
    private const int InitialScratchSize = 256;

    // Each thread keeps its scratch buffer between reads, which is cheaper than renting one for every tag. An input
    // takes the buffer while it reads, so a reader nested inside another would just get a new one.
    [ThreadStatic]
    private static byte[]? cachedScratch;

    private readonly Stream stream;
    private byte[] scratch;
    private long remaining;

    public StreamNbtInput(Stream stream)
    {
        this.stream = stream;
        this.scratch = cachedScratch ?? new byte[InitialScratchSize];
        cachedScratch = null;
        this.remaining = stream.CanSeek ? stream.Length - stream.Position : long.MaxValue;
    }

    public readonly long Remaining => this.remaining;

    public bool TryReadByte(out byte value)
    {
        var read = this.stream.ReadByte();
        if (read < 0)
        {
            value = 0;
            return false;
        }

        this.Consume(1);
        value = (byte)read;
        return true;
    }

    public ReadOnlySpan<byte> Read(int count)
    {
        // Only strings and primitives are read through here, and strings are at most 65535 bytes.
        if (count > this.scratch.Length)
            this.scratch = new byte[Math.Clamp(this.scratch.Length * 2, count, ushort.MaxValue)];

        var bytes = this.scratch.AsSpan(0, count);
        this.ReadExactly(bytes);

        return bytes;
    }

    public void ReadExactly(Span<byte> destination)
    {
        this.stream.ReadExactly(destination);
        this.Consume(destination.Length);
    }

    public void Dispose()
    {
        cachedScratch = this.scratch;
        this.scratch = [];
    }

    private void Consume(int count)
    {
        if (this.remaining != long.MaxValue)
            this.remaining -= count;
    }
}
