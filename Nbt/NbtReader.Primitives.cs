using Obsidian.Nbt.Utilities;

namespace Obsidian.Nbt;

public ref partial struct NbtReader
{
    public bool TryReadByte(out byte value)
    {
        if (TryFetch(sizeof(byte)))
        {
            value = GetRef(sizeof(byte));
            return true;
        }

        Unsafe.SkipInit(out value);
        return false;
    }

    public byte ReadByte()
    {
        if (TryFetch(sizeof(byte)))
        {
            return GetRef(sizeof(byte));
        }

        ThrowHelper.ThrowInvalidOperationException_NotEnoughData();
        return default;
    }

    public bool TryReadTag(out NbtTag value)
    {
        if (TryFetch(sizeof(byte)))
        {
            value = (NbtTag)GetRef(sizeof(byte));
            return true;
        }

        Unsafe.SkipInit(out value);
        return false;
    }

    public NbtTag ReadTag()
    {
        if (TryFetch(sizeof(byte)))
        {
            return (NbtTag)GetRef(sizeof(byte));
        }

        ThrowHelper.ThrowInvalidOperationException_NotEnoughData();
        return default;
    }

    public bool TryReadShort(out short value)
    {
        if (TryFetch(sizeof(short)))
        {
            value = RefReader.ReadShort(ref GetRef(sizeof(short)));
            return true;
        }

        Unsafe.SkipInit(out value);
        return false;
    }

    public short ReadShort()
    {
        if (TryFetch(sizeof(short)))
        {
            return RefReader.ReadShort(ref GetRef(sizeof(short)));
        }

        ThrowHelper.ThrowInvalidOperationException_NotEnoughData();
        return default;
    }

    public bool TryReadInt(out int value)
    {
        if (TryFetch(sizeof(int)))
        {
            value = RefReader.ReadInt(ref GetRef(sizeof(int)));
            return true;
        }

        Unsafe.SkipInit(out value);
        return false;
    }

    public int ReadInt()
    {
        if (TryFetch(sizeof(int)))
        {
            return RefReader.ReadInt(ref GetRef(sizeof(int)));
        }

        ThrowHelper.ThrowInvalidOperationException_NotEnoughData();
        return default;
    }

    public bool TryReadLong(out long value)
    {
        if (TryFetch(sizeof(long)))
        {
            value = RefReader.ReadLong(ref GetRef(sizeof(long)));
            return true;
        }

        Unsafe.SkipInit(out value);
        return false;
    }

    public long ReadLong()
    {
        if (TryFetch(sizeof(long)))
        {
            return RefReader.ReadLong(ref GetRef(sizeof(long)));
        }

        ThrowHelper.ThrowInvalidOperationException_NotEnoughData();
        return default;
    }

    public bool TryReadFloat(out float value)
    {
        if (TryFetch(sizeof(float)))
        {
            value = RefReader.ReadFloat(ref GetRef(sizeof(float)));
            return true;
        }

        Unsafe.SkipInit(out value);
        return false;
    }

    public float ReadFloat()
    {
        if (TryFetch(sizeof(float)))
        {
            return RefReader.ReadFloat(ref GetRef(sizeof(float)));
        }

        ThrowHelper.ThrowInvalidOperationException_NotEnoughData();
        return default;
    }

    public bool TryReadDouble(out double value)
    {
        if (TryFetch(sizeof(double)))
        {
            value = RefReader.ReadDouble(ref GetRef(sizeof(double)));
            return true;
        }

        Unsafe.SkipInit(out value);
        return false;
    }

    public double ReadDouble()
    {
        if (TryFetch(sizeof(double)))
        {
            return RefReader.ReadDouble(ref GetRef(sizeof(double)));
        }

        ThrowHelper.ThrowInvalidOperationException_NotEnoughData();
        return default;
    }

    private bool TryReadUShort(out ushort value)
    {
        if (TryFetch(sizeof(ushort)))
        {
            value = RefReader.ReadUShort(ref GetRef(sizeof(ushort)));
            return true;
        }

        Unsafe.SkipInit(out value);
        return false;
    }

    public bool TryReadString([NotNullWhen(true)] out string? value)
    {
        if (!TryReadUShort(out ushort length) || !TryFetch(length))
        {
            value = null;
            return false;
        }

        value = RefReader.ReadString(ref GetRef(length), length);
        return true;
    }

    public string ReadString()
    {
        if (!TryReadUShort(out ushort length) || !TryFetch(length))
        {
            ThrowHelper.ThrowInvalidOperationException_NotEnoughData();
            return null!;
        }

        return RefReader.ReadString(ref GetRef(length), length);
    }

    public bool TryInspectString(out ReadOnlySpan<byte> value)
    {
        if (!TryReadUShort(out ushort length) || !TryFetch(length))
        {
            value = ReadOnlySpan<byte>.Empty;
            return false;
        }

        value = MemoryMarshal.CreateReadOnlySpan(ref GetRef(length), length);
        return true;
    }
}
