using Obsidian.Nbt.Utilities;

namespace Obsidian.Nbt;

public readonly struct NbtProperty
{
    public string Name => parent.GetString(nameIndex);
    public ReadOnlySpan<byte> Utf8Name => parent.GetUtf8StringRaw(nameIndex).Span;
    public NbtTag Tag => tag;

    // Indexes points BEFORE value length (if value has a length prefix)
    private readonly int nameIndex;
    private readonly int valueIndex;
    private readonly NbtDocument parent;
    private readonly NbtTag tag;

    internal NbtProperty(NbtDocument parent, int nameIndex, int valueIndex, NbtTag tag)
    {
        this.parent = parent;
        this.nameIndex = nameIndex;
        this.valueIndex = valueIndex;
        this.tag = tag;
    }

    public byte GetByte()
    {
        ValidateInstance();
        if (tag != NbtTag.Byte)
        {
            ThrowHelper.ThrowInvalidOperationException_IncorrectTagType();
        }

        return parent.GetByte(valueIndex);
    }

    public short GetShort()
    {
        ValidateInstance();
        if (tag != NbtTag.Short)
        {
            ThrowHelper.ThrowInvalidOperationException_IncorrectTagType();
        }

        return parent.GetShort(valueIndex);
    }

    public int GetInt()
    {
        ValidateInstance();
        if (tag != NbtTag.Int)
        {
            ThrowHelper.ThrowInvalidOperationException_IncorrectTagType();
        }

        return parent.GetInt(valueIndex);
    }

    public long GetLong()
    {
        ValidateInstance();
        if (tag != NbtTag.Long)
        {
            ThrowHelper.ThrowInvalidOperationException_IncorrectTagType();
        }

        return parent.GetLong(valueIndex);
    }

    public float GetFloat()
    {
        ValidateInstance();
        if (tag != NbtTag.Float)
        {
            ThrowHelper.ThrowInvalidOperationException_IncorrectTagType();
        }

        return parent.GetFloat(valueIndex);
    }

    public double GetDouble()
    {
        ValidateInstance();
        if (tag != NbtTag.Double)
        {
            ThrowHelper.ThrowInvalidOperationException_IncorrectTagType();
        }

        return parent.GetDouble(valueIndex);
    }

    public byte[] GetByteArray()
    {
        ValidateInstance();
        if (tag != NbtTag.ByteArray)
        {
            ThrowHelper.ThrowInvalidOperationException_IncorrectTagType();
        }

        return parent.GetByteArray(valueIndex);
    }

    public int[] GetIntArray()
    {
        ValidateInstance();
        if (tag != NbtTag.IntArray)
        {
            ThrowHelper.ThrowInvalidOperationException_IncorrectTagType();
        }

        return parent.GetIntArray(valueIndex);
    }

    public long[] GetLongArray()
    {
        ValidateInstance();
        if (tag != NbtTag.LongArray)
        {
            ThrowHelper.ThrowInvalidOperationException_IncorrectTagType();
        }

        return parent.GetLongArray(valueIndex);
    }

    public string GetString()
    {
        ValidateInstance();
        if (tag != NbtTag.String)
        {
            ThrowHelper.ThrowInvalidOperationException_IncorrectTagType();
        }
        return parent.GetString(valueIndex);
    }

    public NbtCompound GetCompound()
    {
        ValidateInstance();
        if (tag != NbtTag.Compound)
        {
            ThrowHelper.ThrowInvalidOperationException_IncorrectTagType();
        }
        return parent.GetCompound(valueIndex);
    }

    public bool NameEquals(ReadOnlySpan<byte> encodedName)
    {
        return MemoryExtensions.SequenceEqual(Utf8Name, encodedName);
    }

    private void ValidateInstance()
    {
        if (parent is null)
        {
            ThrowHelper.ThrowInvalidOperationException_InvalidInstace();
        }
    }
}
