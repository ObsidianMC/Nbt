using Obsidian.Nbt.Utilities;

namespace Obsidian.Nbt;

[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
public partial struct NbtWriter : IAsyncDisposable, IDisposable
{
    private const int DefaultBufferSize = 256;

    private readonly ArrayPool<byte> arrayPool;
    private readonly Stream stream;
    private readonly int bufferSize;
    private byte[] buffer;
    private int index;

    public NbtWriter(Stream outputStream, int bufferSize = DefaultBufferSize) : this(outputStream, FakeArrayPool.Instance, bufferSize)
    {
    }

    public NbtWriter(Stream outputStream, ArrayPool<byte> arrayPool, int bufferSize = DefaultBufferSize)
    {
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(arrayPool);
        ThrowHelper.ThrowOutOfRangeException_IfNegative(bufferSize);
        this.bufferSize = Math.Min(bufferSize, DefaultBufferSize);

        stream = outputStream;
        this.arrayPool = arrayPool;
        buffer = arrayPool.Rent(this.bufferSize);
        index = 0;
    }

    internal NbtWriter(byte[] buffer)
    {
        this.buffer = buffer;
        bufferSize = buffer.Length;
        stream = null!;
        arrayPool = null!;
        index = 0;
    }

    internal void Skip(int advanceBy)
    {
        index += advanceBy;
    }

    private Span<byte> GetSpan(int size)
    {
        if (size + index < bufferSize)
        {
            return UnsafeGetSpanAndAdvance(size);
        }

        Flush();

        if (size >= bufferSize)
        {
            arrayPool.Return(buffer);
            buffer = arrayPool.Rent(size);
        }

        return UnsafeGetSpanAndAdvance(size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<byte> UnsafeGetSpanAndAdvance(int size)
    {
        Span<byte> span = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), (nint)(uint)index), size);
        index += size;
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteByteDirect(ref byte value)
    {
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), index) = value;
        index++;
    }

    private ref byte GetRef(int requestedSize)
    {
        if (requestedSize + index < bufferSize)
        {
            return ref UnsafeGetRefAndAdvance(requestedSize);
        }

        Flush();

        if (requestedSize >= bufferSize)
        {
            arrayPool.Return(buffer);
            buffer = arrayPool.Rent(requestedSize);
        }

        return ref UnsafeGetRefAndAdvance(requestedSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref byte UnsafeGetRefAndAdvance(int advanceBy)
    {
        ref byte reference = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), index);
        index += advanceBy;
        return ref reference;
    }

    private void Flush()
    {
        if (index == 0)
            return;

        ReadOnlySpan<byte> bufferedData = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(buffer), index);
        stream.Write(bufferedData);
        index = 0;
    }

    private async ValueTask FlushAsync()
    {
        if (index == 0)
            return;

        await stream.WriteAsync(buffer.AsMemory(0, index));
        index = 0;
    }

    public void Dispose()
    {
        Flush();
        arrayPool.Return(buffer);
    }

    public async ValueTask DisposeAsync()
    {
        await FlushAsync();
        arrayPool.Return(buffer);
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"DataPending = {index > 0}";
}
