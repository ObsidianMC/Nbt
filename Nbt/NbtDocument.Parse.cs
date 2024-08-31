﻿using System.IO.Compression;
using System.Threading;

namespace Obsidian.Nbt;

public sealed partial class NbtDocument
{
    public static NbtDocument Parse(byte[] nbtBytes)
        => Parse(nbtBytes.AsMemory());

    public static NbtDocument Parse(Stream nbtDataStream, NbtCompression compression = NbtCompression.None)
    {
        var nbtStream = compression switch
        {
            NbtCompression.GZip => new GZipStream(nbtDataStream, CompressionMode.Decompress),
            NbtCompression.ZLib => new ZLibStream(nbtDataStream, CompressionMode.Decompress),
            _ => nbtDataStream
        };

        int length = 512;
        try
        {
            length = (int)nbtStream.Length;
        }
        catch
        {
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
        int readBytes;
        while ((readBytes = nbtStream.Read(buffer)) < buffer.Length)
        {
            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
            buffer.AsSpan(0, readBytes).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = newBuffer;
        }

        return Parse(buffer.AsMemory(0, readBytes));
    }

    public static async ValueTask<NbtDocument> ParseAsync(Stream nbtDataStream, CancellationToken cancellationToken = default)
    {
        int length = 512;
        try
        {
            length = (int)nbtDataStream.Length;
        }
        catch
        {
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
        int readBytes;
        while ((readBytes = await nbtDataStream.ReadAsync(buffer, cancellationToken)) < buffer.Length)
        {
            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
            buffer.AsSpan(0, readBytes).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = newBuffer;
        }

        return Parse(buffer.AsMemory(0, readBytes));
    }

    public static NbtDocument Parse(ReadOnlyMemory<byte> nbtBytes)
    {
        return new NbtDocument(nbtBytes);
    }
}