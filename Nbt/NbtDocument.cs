using Obsidian.Nbt.Utilities;
using System.Collections;
using System.Collections.Generic;

namespace Obsidian.Nbt;

public sealed partial class NbtDocument
{
    public NbtCompound Root => new(this, 0);

    private readonly ReadOnlyMemory<byte> data;
    private Dictionary<int, string>? stringsCache;

    private NbtDocument(ReadOnlyMemory<byte> data)
    {
        this.data = data;
    }

    [SkipLocalsInit]
    internal bool TryGetProperty(ReadOnlySpan<char> propertyName, int index, out NbtProperty property)
    {
        if (!ModifiedUtf8.TryGetByteCount(propertyName, out int nameByteCount))
        {
            property = default;
            return false;
        }

        byte[]? rentedArray = null;
        Span<byte> utf8name = nameByteCount <= 1024 ? stackalloc byte[1024] : (rentedArray = ArrayPool<byte>.Shared.Rent(nameByteCount));
        utf8name = utf8name[..nameByteCount];
        ModifiedUtf8.GetBytesCommon(propertyName, utf8name);

        PropertiesEnumerator enumerator = new(this, index);
        while (enumerator.MoveNext())
        {
            if (enumerator.Current.NameEquals(utf8name))
            {
                if (rentedArray is not null)
                    ArrayPool<byte>.Shared.Return(rentedArray);
                property = enumerator.Current;
                return true;
            }
        }

        if (rentedArray is not null)
            ArrayPool<byte>.Shared.Return(rentedArray);
        property = default;
        return false;
    }

    internal bool TryGetProperty(ReadOnlySpan<byte> propertyName, int index, out NbtProperty property)
    {
        throw new NotImplementedException();
    }

    internal bool TryGetProperty(int index, out NbtProperty property, out int endIndex)
    {
        ref byte @ref = ref GetRef(index);
        NbtTag tag = (NbtTag)@ref;

        if (tag == NbtTag.End)
        {
            Unsafe.SkipInit(out property);
            Unsafe.SkipInit(out endIndex);
            return false;
        }

        @ref = ref Unsafe.Add(ref @ref, sizeof(byte));
        index++;

        int length = RefReader.ReadUShort(ref @ref);
        int valueIndex = index + sizeof(ushort) + length;
        property = new NbtProperty(this, index, valueIndex, tag);

        int? valueLength = null;
        if(tag == NbtTag.Compound)
        {
            var compound = property.GetCompound();

            var childLength = 0;
            foreach (var child in compound.EnumerateProperties())
            {
                childLength += child.GetIndex() - index;
            }

            if(childLength > 0)
                valueLength = childLength;
        }

        endIndex = valueIndex + (valueLength ?? GetValueLength(ref Unsafe.Add(ref @ref, sizeof(ushort) + length), tag));

        return true;
    }

    internal bool TryGetArrayItem(int index, NbtTag listType, out NbtProperty property, out int endIndex)
    {
        ref byte @ref = ref GetRef(index);

        int length = RefReader.ReadUShort(ref @ref);
        property = new NbtProperty(this, -1, index, listType);

        endIndex = GetValueLength(ref Unsafe.Add(ref @ref, sizeof(ushort) + length), listType);

        return true;
    }

    private int GetValueLength(ref byte @ref, NbtTag tag)
    {
        switch (tag)
        {
            case NbtTag.Byte:
                return sizeof(byte);

            case NbtTag.Short:
                return sizeof(short);

            case NbtTag.Int:
                return sizeof(int);

            case NbtTag.Long:
                return sizeof(long);

            case NbtTag.Float:
                return sizeof(float);

            case NbtTag.Double:
                return sizeof(double);

            case NbtTag.ByteArray:
                return sizeof(int) + RefReader.ReadInt(ref @ref) * sizeof(byte);

            case NbtTag.IntArray:
                return sizeof(int) + RefReader.ReadInt(ref @ref) * sizeof(int);

            case NbtTag.LongArray:
                return sizeof(int) + RefReader.ReadInt(ref @ref) * sizeof(long);

            case NbtTag.String:
                return sizeof(ushort) + RefReader.ReadUShort(ref @ref);
            default:
                throw new NotImplementedException();
        }
    }

    internal byte GetByte(int index)
    {
        return GetRef(index);
    }

    internal ushort GetUShort(int index)
    {
        return RefReader.ReadUShort(ref GetRef(index));
    }

    internal short GetShort(int index)
    {
        return RefReader.ReadShort(ref GetRef(index));
    }

    internal int GetInt(int index)
    {
        return RefReader.ReadInt(ref GetRef(index));
    }

    internal long GetLong(int index)
    {
        return RefReader.ReadLong(ref GetRef(index));
    }

    internal float GetFloat(int index)
    {
        return RefReader.ReadFloat(ref GetRef(index));
    }

    internal double GetDouble(int index)
    {
        return RefReader.ReadDouble(ref GetRef(index));
    }

    internal byte[] GetByteArray(int index)
    {
        int length = RefReader.ReadInt(ref GetRef(index));
        index += sizeof(int);
        byte[] array = GC.AllocateUninitializedArray<byte>(length);
        RefReader.ReadBytes(ref GetRef(index), ref MemoryMarshal.GetArrayDataReference(array), length);
        return array;
    }

    internal int[] GetIntArray(int index)
    {
        int length = RefReader.ReadInt(ref GetRef(index));
        index += sizeof(int);
        int[] array = GC.AllocateUninitializedArray<int>(length);
        RefReader.ReadInts(ref GetRef(index), ref Unsafe.As<int, byte>(ref MemoryMarshal.GetArrayDataReference(array)), length);
        return array;
    }

    internal long[] GetLongArray(int index)
    {
        int length = RefReader.ReadInt(ref GetRef(index));
        index += sizeof(int);
        long[] array = GC.AllocateUninitializedArray<long>(length);
        RefReader.ReadLongs(ref GetRef(index), ref Unsafe.As<long, byte>(ref MemoryMarshal.GetArrayDataReference(array)), length);
        return array;
    }

    internal NbtCompound GetCompound(int index)
    {
        return new NbtCompound(this, index);
    }

    internal NbtList GetList(int index)
    {
        ref byte @ref = ref GetRef(index);

        var tagType = (NbtTag)@ref;
        int length = RefReader.ReadInt(ref GetRef(index + 1));

        return new(this, index + 2, tagType, length);
    }

    internal string GetString(int index)
    {
        if (stringsCache is not null && stringsCache.TryGetValue(index, out string? cachedString))
        {
            return cachedString;
        }

        ref byte @ref = ref GetRef(index);
        int length = RefReader.ReadUShort(ref @ref);
        @ref = ref Unsafe.Add(ref @ref, sizeof(ushort));
        string @string = RefReader.ReadString(ref @ref, length);
        stringsCache ??= [];
        stringsCache.Add(index, @string);
        return @string;
    }

    internal ReadOnlyMemory<byte> GetUtf8StringRaw(int index)
    {
        int length = RefReader.ReadUShort(ref GetRef(index));
        return data.Slice(index + sizeof(ushort), length);
    }

    internal ReadOnlyMemory<byte> GetRawData(int index, int length)
    {
        return data.Slice(index, length);
    }

    private ref byte GetRef(int index)
    {
        return ref Unsafe.Add(ref MemoryMarshal.GetReference(data.Span), index);
    }

    public struct ArrayEnumerator<T> : IEnumerator<T> where T : unmanaged
    {
        public T Current => GetCurrent();
        object? IEnumerator.Current => Current;

        private readonly NbtDocument document;
        private readonly int end;
        private readonly int start;
        private int index;

        internal unsafe ArrayEnumerator(NbtDocument document, int start, int length)
        {
            this.document = document;
            this.start = start;
            end = start + length;
            index = start - sizeof(T);
        }

        public unsafe bool MoveNext()
        {
            return (index += sizeof(T)) < end;
        }

        public void Reset()
        {
            index = start;
        }

        public void Dispose()
        {
        }

        readonly private T GetCurrent()
        {
            if (typeof(T) == typeof(byte))
            {
                byte value = document.GetByte(index);
                return Unsafe.As<byte, T>(ref value);
            }
            else if (typeof(T) == typeof(short))
            {
                short value = document.GetShort(index);
                return Unsafe.As<short, T>(ref value);
            }
            else if (typeof(T) == typeof(int))
            {
                int value = document.GetInt(index);
                return Unsafe.As<int, T>(ref value);
            }
            else if (typeof(T) == typeof(long))
            {
                long value = document.GetLong(index);
                return Unsafe.As<long, T>(ref value);
            }
            else if (typeof(T) == typeof(float))
            {
                float value = document.GetInt(index);
                return Unsafe.As<float, T>(ref value);
            }
            else if (typeof(T) == typeof(double))
            {
                double value = document.GetInt(index);
                return Unsafe.As<double, T>(ref value);
            }
            return default;
        }
    }

    public struct StringsEnumerator : IEnumerator<string>
    {
        public string Current => document.GetString(currentStringIndex);
        object? IEnumerator.Current => Current;

        private readonly NbtDocument document;
        private readonly int start;
        private readonly int count;
        private int index;
        private int visited;
        private int currentStringIndex;

        internal unsafe StringsEnumerator(NbtDocument document, int start, int count)
        {
            this.document = document;
            this.start = start;
            this.count = count;

            currentStringIndex = start;
            index = start;
            visited = 0;
        }

        public unsafe bool MoveNext()
        {
            if (visited++ >= count)
                return false;

            currentStringIndex = index;
            ushort length = document.GetUShort(index);
            index += sizeof(ushort) + length;

            return true;
        }

        public void Reset()
        {
            index = start;
            visited = 0;
        }

        public void Dispose()
        {
        }
    }

    public struct PropertiesEnumerator : IEnumerator<NbtProperty>
    {
        public NbtProperty Current => property;
        object IEnumerator.Current => Current;

        private readonly NbtDocument document;
        private readonly int start;
        private int index;

        private NbtProperty property;

        internal PropertiesEnumerator(NbtDocument document, int start)
        {
            this.document = document;
            this.start = start;
            index = start;
            property = default;
        }

        public bool MoveNext()
        {
            return document.TryGetProperty(index, out property, out index);
        }

        public void Reset()
        {
            index = start;
        }

        public IEnumerator<NbtProperty> GetEnumerator()
        {
            return this;
        }

        public void Dispose()
        {
        }
    }

    public struct ArrayEnumerator : IEnumerator<NbtProperty>
    {
        public NbtProperty Current => property;
        object IEnumerator.Current => Current;

        private readonly NbtDocument document;
        private readonly int start;
        private readonly NbtTag listType;
        private int index;

        private NbtProperty property;

        internal ArrayEnumerator(NbtDocument document, int start, NbtTag listType)
        {
            this.document = document;
            this.start = start;
            this.listType = listType;
            index = start;
            property = default;
        }

        public bool MoveNext()
        {
            return document.TryGetArrayItem(index, listType, out property, out index);
        }

        public void Reset()
        {
            index = start;
        }

        public IEnumerator<NbtProperty> GetEnumerator()
        {
            return this;
        }

        public void Dispose()
        {
        }
    }
}
