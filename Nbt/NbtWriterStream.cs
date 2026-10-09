using Obsidian.Nbt.Interfaces;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace Obsidian.Nbt;

/// <summary>
/// Writes binary NBT to a stream, optionally compressed. Output is buffered: call <see cref="TryFinish"/> (or
/// dispose the writer, which also disposes the stream) to write it out.
/// </summary>
/// <remarks>
/// Copies of this struct share one underlying writer, so it can be passed by value to helpers that write part of
/// the document.
/// </remarks>
public readonly partial struct NbtWriterStream : INbtWriter, IEquatable<NbtWriterStream>
{
    private const int BufferSize = 16 * 1024;

    private readonly RawNbtWriter writer;

    /// <summary>Creates a writer with nothing written; the first tag written becomes the root.</summary>
    public NbtWriterStream(Stream outstream, NbtCompression compressionMode = NbtCompression.None)
        : this(outstream, compressionMode, false)
    {
    }

    /// <summary>Starts a root compound with the given name.</summary>
    public NbtWriterStream(Stream outstream, string name) : this(outstream, NbtCompression.None, false) =>
        this.writer.WriteCompoundStart(name);

    /// <summary>
    /// Starts a root compound. Network NBT has no root name; otherwise the root is named with an empty string.
    /// </summary>
    public NbtWriterStream(Stream outstream, bool networked) : this(outstream, NbtCompression.None, networked) =>
        this.writer.WriteCompoundStart();

    /// <summary>Starts a root compound with the given name, compressing the output.</summary>
    public NbtWriterStream(Stream outstream, NbtCompression compressionMode, string name)
        : this(outstream, compressionMode, false) => this.writer.WriteCompoundStart(name);

    private NbtWriterStream(Stream outstream, NbtCompression compressionMode, bool networked)
    {
        this.BaseStream = compressionMode switch
        {
            NbtCompression.GZip => new GZipStream(outstream, CompressionMode.Compress),
            NbtCompression.ZLib => new ZLibStream(outstream, CompressionMode.Compress),
            _ => outstream
        };

        this.writer = new RawNbtWriter(this.BaseStream, networked, BufferSize);
    }

    public Stream BaseStream { get; }

    /// <inheritdoc cref="RawNbtWriter.RootType"/>
    public NbtTagType? RootType => this.writer.RootType;

    public bool Networked => this.writer.Networked;

    public void WriteCompoundStart(string name = "") => this.writer.WriteCompoundStart(name);

    /// <inheritdoc cref="RawNbtWriter.WriteListStart"/>
    public void WriteListStart(string name, NbtTagType listType, int length, bool writeName = true) =>
        this.writer.WriteListStart(name, listType, length, writeName);

    public void EndList() => this.writer.EndList();

    public void EndCompound() => this.writer.EndCompound();

    /// <inheritdoc cref="RawNbtWriter.WriteTag"/>
    public void WriteTag(INbtTag tag) => this.writer.WriteTag(tag);

    /// <inheritdoc cref="RawNbtWriter.WriteListTag"/>
    public void WriteListTag(INbtTag tag) => this.writer.WriteListTag(tag);

    public void WriteArray(string? name, ReadOnlySpan<int> values) => this.writer.WriteArray(name, values);

    public void WriteArray(string? name, ReadOnlySpan<long> values) => this.writer.WriteArray(name, values);

    public void WriteArray(string? name, ReadOnlySpan<byte> values) => this.writer.WriteArray(name, values);

    /// <summary>Writes a raw tag type byte, without any checks.</summary>
    public void Write(NbtTagType tagType) => this.writer.Write(tagType);

    /// <summary>Checks that every tag is closed, then writes out the buffered output and flushes the stream.</summary>
    public void TryFinish() => this.writer.TryFinish();

    /// <inheritdoc cref="TryFinish"/>
    public Task TryFinishAsync() => this.writer.TryFinishAsync();

    /// <summary>Writes out any buffered output, then disposes the stream.</summary>
    public void Dispose()
    {
        try
        {
            this.writer.FlushToSink();
        }
        finally
        {
            this.writer.Dispose();
            this.BaseStream.Dispose();
        }
    }

    /// <inheritdoc cref="Dispose"/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await this.writer.FlushToSinkAsync();
        }
        finally
        {
            this.writer.Dispose();
            await this.BaseStream.DisposeAsync();
        }
    }

    // Writers are equal when they write to the same stream.
    public bool Equals(NbtWriterStream other) => ReferenceEquals(this.BaseStream, other.BaseStream);

    public override bool Equals(object? obj) => obj is NbtWriterStream other && this.Equals(other);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this.BaseStream);

    public static bool operator ==(NbtWriterStream left, NbtWriterStream right) => left.Equals(right);

    public static bool operator !=(NbtWriterStream left, NbtWriterStream right) => !left.Equals(right);
}
