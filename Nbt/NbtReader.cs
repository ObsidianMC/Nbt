using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace Obsidian.Nbt;

public readonly partial struct NbtReader(Stream input, NbtCompression compressionMode = NbtCompression.None)
{
    public Stream BaseStream { get; } = compressionMode switch
    {
        NbtCompression.GZip => new GZipStream(input, CompressionMode.Decompress),
        NbtCompression.ZLib => new ZLibStream(input, CompressionMode.Decompress),
        _ => input
    };

   
    public INbtTag? ReadNextTag(bool readName = true)
    {
        var firstType = this.ReadTagType();
        if (firstType == NbtTagType.End)
            return null;

        string tagName = readName ? this.ReadString() : string.Empty;

        return firstType switch
        {
            NbtTagType.List => ReadListTag(tagName),
            NbtTagType.Compound => ReadCompoundTag(tagName),
            NbtTagType.ByteArray => ReadByteArray(tagName),
            NbtTagType.IntArray => ReadIntArray(tagName),
            NbtTagType.LongArray => ReadLongArray(tagName),
            _ => GetCurrentTag(firstType, tagName)
        };
    }

    internal NbtCompound ReadRootCompound()
    {
        var tagType = this.ReadTagType();
        if (tagType != NbtTagType.Compound)
            throw new InvalidOperationException("Unable to read the root compound.");

        return this.ReadCompoundTag(this.ReadString());
    }

    public bool TryReadNextTag(bool readName, [MaybeNullWhen(false)] out INbtTag tag)
    {
        var nextTag = this.ReadNextTag(readName);

        if (nextTag != null)
        {
            tag = nextTag;
            return true;
        }

        tag = default;
        return false;
    }

    public bool TryReadNextTag<T>(bool readName, [MaybeNullWhen(false)] out T tag) where T : INbtTag
    {
        if (this.TryReadNextTag(readName, out INbtTag newTag) && newTag is T matchedTag)
        {
            tag = matchedTag;
            return true;
        }

        tag = default;
        return false;
    }

    public bool TryReadNextTag([MaybeNullWhen(false)] out INbtTag tag)
    {
        var nextTag = this.ReadNextTag();

        if (nextTag != null)
        {
            tag = nextTag;
            return true;
        }

        tag = default;
        return false;
    }

    public bool TryReadNextTag<T>([MaybeNullWhen(false)] out T tag) where T : INbtTag
    {
        if (this.TryReadNextTag(out INbtTag? newTag) && newTag is T matchedTag)
        {
            tag = matchedTag;
            return true;
        }

        tag = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private INbtTag GetCurrentTag(NbtTagType type) => this.GetCurrentTag(type, this.ReadString());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private INbtTag GetCurrentTag(NbtTagType type, string name) => type switch
        {
            NbtTagType.Byte => new NbtTag<byte>(name, this.ReadByte()),
            NbtTagType.Short => new NbtTag<short>(name, this.ReadInt16()),
            NbtTagType.Int => new NbtTag<int>(name, this.ReadInt32()),
            NbtTagType.Long => new NbtTag<long>(name, this.ReadInt64()),
            NbtTagType.Float => new NbtTag<float>(name, this.ReadSingle()),
            NbtTagType.Double => new NbtTag<double>(name, this.ReadDouble()),
            NbtTagType.String => new NbtTag<string>(name, this.ReadString()),
            NbtTagType.Compound => this.ReadCompoundTag(name),
            NbtTagType.List => this.ReadListTag(name),
            NbtTagType.ByteArray => this.ReadByteArray(name),
        NbtTagType.IntArray => this.ReadIntArray(name),
        NbtTagType.LongArray => this.ReadLongArray(name),
            _ => throw new InvalidOperationException($"Unknown tag type: {type}")
        };

    private INbtTag ReadArray<T>(string name, Func<T> readElement) where T : struct
    {
        int length = ReadInt32();
        if (length < 0)
            throw new UnreachableException("Array length should never be below 0.");

        var array = new T[length];
        for (int i = 0; i < length; i++)
        {
            array[i] = readElement();
        }

        return new NbtArray<T>(name, array);
    }

    private NbtArray<byte> ReadByteArray(string name)
    {
        var length = ReadInt32();
        if (length < 0)
            throw new UnreachableException("Array length should never be below 0.");

        var array = new byte[length];
        this.BaseStream.ReadExactly(array);

        return new NbtArray<byte>(name, array);
    }

    private NbtArray<int> ReadIntArray(string name)
    {
        var length = ReadInt32();
        if (length < 0)
            throw new UnreachableException("Array length should never be below 0.");

        var array = GC.AllocateUninitializedArray<int>(length);
        for (var i = 0; i < length; i++)
            array[i] = this.ReadInt32();

        return new NbtArray<int>(name, array);
    }

    private NbtArray<long> ReadLongArray(string name)
    {
        var length = ReadInt32();
        if (length < 0)
            throw new UnreachableException("Array length should never be below 0.");

        var array = GC.AllocateUninitializedArray<long>(length);
        for (var i = 0; i < length; i++)
            array[i] = this.ReadInt64();

        return new NbtArray<long>(name, array);
    }

    private NbtList ReadListTag(string name)
    {
        var listType = this.ReadTagType();

        var length = this.ReadInt32();

        if (length <= 0)
            return new NbtList(listType, name);

        var list = new NbtList(listType, name);
        for (var i = 0; i < length; i++)
            list.Add(this.GetCurrentTag(listType, string.Empty));

        return list;
    }

    private NbtCompound ReadCompoundTag(string name)
    {
        var compound = new NbtCompound(name);

        NbtTagType type;
        while ((type = this.ReadTagType()) != NbtTagType.End)
        {
            var tag = this.GetCurrentTag(type);

            compound.Add(tag);
        }

        return compound;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private NbtTagType ReadTagType()
    {
        var type = this.BaseStream.ReadByte();

        return type switch
        {
            <= 0 => NbtTagType.End,
            > (byte)NbtTagType.LongArray => throw new ArgumentOutOfRangeException(
                $"Tag is out of range: {(NbtTagType)type}"),
            _ => (NbtTagType)type
        };
    }

}
