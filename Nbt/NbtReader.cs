using Obsidian.Nbt.Utilities;

namespace Obsidian.Nbt;

public ref partial struct NbtReader
{
    private const int DefaultBufferSize = 256;

    private readonly Stream stream;
    private readonly ArrayPool<byte> arrayPool;
    private byte[] buffer;
    private ReadOnlySpan<byte> span;

    public NbtReader(Stream stream, ArrayPool<byte>? arrayPool = null)
    {
        this.stream = stream;
        this.arrayPool = arrayPool ?? FakeArrayPool.Instance;
        buffer = this.arrayPool.Rent(DefaultBufferSize);
        span = [];
    }

    public NbtReader(ReadOnlyMemory<byte> memory)
    {
        stream = Stream.Null;
        arrayPool = FakeArrayPool.Instance;
        buffer = [];
        span = memory.Span;
    }

    public NbtReader(ReadOnlySpan<byte> span)
    {
        stream = Stream.Null;
        arrayPool = FakeArrayPool.Instance;
        buffer = [];
        this.span = span;
    }

    private bool TryFetch(int size)
    {
        if (span.Length >= size)
            return true;

        Fetch(size);

        return span.Length >= size;
    }

    private void Fetch(int size)
    {
        Debug.Assert(size > span.Length, "Fetch should only be called when span doesn't have required size.");

        if (size > buffer.Length - span.Length)
        {
            // Grow buffer
            var newBuffer = arrayPool.Rent(size + span.Length);
            if (!span.IsEmpty)
                span.CopyTo(newBuffer);
            arrayPool.Return(buffer);
            buffer = newBuffer;

            // Caution: 'span' stil points to the old buffer,
            // but it will get overwritten before anything
            // other than its size gets used
        }

        // Read from stream
        ref byte destinationRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), (nint)(uint)span.Length);
        Span<byte> destination = MemoryMarshal.CreateSpan(ref destinationRef, buffer.Length - span.Length);
        int bytesRead = stream.Read(destination);
        span = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(buffer), span.Length + bytesRead);
    }

    private ref byte GetRef(int size)
    {
        ref byte reference = ref MemoryMarshal.GetReference(span);
        span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref reference, size), span.Length - size);
        return ref reference;
    }

    public readonly void Dispose()
    {
        if (buffer != Array.Empty<byte>())
        {
            arrayPool.Return(buffer);
        }
    }
}
