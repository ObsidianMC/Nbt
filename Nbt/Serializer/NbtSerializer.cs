using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;

namespace Obsidian.Nbt.Serializer;

public static class NbtSerializer
{
    private const string RootValueName = "Value";

    private static readonly ConcurrentDictionary<Type, CachedTypeMetadata> TypeMetadataCache = new();
    private static readonly ConcurrentDictionary<Type, Type?> EnumerableElementTypeCache = new();

    public static NbtCompound Serialize<T>(T value, NbtSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        options ??= new();

        var rootName = GetRootName(typeof(T), options);

        if (value is NbtCompound compound)
        {
            if (string.IsNullOrWhiteSpace(compound.Name))
                compound.Name = rootName;

            return compound;
        }

        if (IsComplexType(value.GetType()))
            return SerializeCompound(value, value.GetType(), rootName, options);

        var root = new NbtCompound(rootName)
        {
            SerializeValue(RootValueName, value, typeof(T), options)
        };

        return root;
    }

    public static void Serialize<T>(Stream stream, T value, NbtSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        options ??= new();

        var compound = Serialize(value, options);
        var rootName = compound.Name ?? GetRootName(typeof(T), options);

        using var writer = new RawNbtWriter(rootName);

        foreach (var (_, child) in compound)
            writer.WriteTag(child);

        writer.EndCompound();
        writer.TryFinish();

        WritePayload(stream, writer.AsSpan(), options.Compression);
    }

    public static async ValueTask SerializeAsync<T>(Stream stream, T value, NbtSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        options ??= new();

        var compound = Serialize(value, options);
        var rootName = compound.Name ?? GetRootName(typeof(T), options);

        await using var writer = new RawNbtWriter(rootName);

        foreach (var (_, child) in compound)
            writer.WriteTag(child);

        writer.EndCompound();
        await writer.TryFinishAsync();

        await WritePayloadAsync(stream, writer.AsSpan().ToArray(), options.Compression, cancellationToken);
    }

    public static T? Deserialize<T>(NbtCompound compound, NbtSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(compound);

        options ??= new();

        return (T?)DeserializeRoot(compound, typeof(T), options);
    }

    public static T? Deserialize<T>(Stream stream, NbtSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        options ??= new();

        var compound = ReadCompound(stream, options.Compression);
        return Deserialize<T>(compound, options);
    }

    public static async ValueTask<T?> DeserializeAsync<T>(Stream stream, NbtSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        options ??= new();

        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        return Deserialize<T>(buffer, options);
    }

    private static NbtCompound ReadCompound(Stream stream, NbtCompression compression)
    {
        if (compression == NbtCompression.None)
            return ReadCompoundCore(stream);

        using var readerStream = CreateDecompressionStream(stream, compression);
        return ReadCompoundCore(readerStream);
    }

    private static NbtCompound ReadCompoundCore(Stream stream)
    {
        var reader = new NbtReader(stream, NbtCompression.None);
        return reader.ReadRootCompound();
    }

    private static void WritePayload(Stream stream, ReadOnlySpan<byte> payload, NbtCompression compression)
    {
        switch (compression)
        {
            case NbtCompression.None:
                stream.Write(payload);
                return;
            case NbtCompression.GZip:
                using (var compressionStream = new GZipStream(stream, CompressionMode.Compress, true))
                    compressionStream.Write(payload);
                return;
            case NbtCompression.ZLib:
                using (var compressionStream = new ZLibStream(stream, CompressionMode.Compress, true))
                    compressionStream.Write(payload);
                return;
            case NbtCompression.Brotli:
                using (var compressionStream = new BrotliStream(stream, CompressionMode.Compress, true))
                    compressionStream.Write(payload);
                return;
            case NbtCompression.Zstd:
                throw new NotSupportedException("Zstd compression is not supported by the serializer yet.");
            default:
                throw new ArgumentOutOfRangeException(nameof(compression));
        }
    }

    private static async ValueTask WritePayloadAsync(Stream stream, byte[] payload, NbtCompression compression, CancellationToken cancellationToken)
    {
        switch (compression)
        {
            case NbtCompression.None:
                await stream.WriteAsync(payload, cancellationToken);
                return;
            case NbtCompression.GZip:
                await using (var compressionStream = new GZipStream(stream, CompressionMode.Compress, true))
                    await compressionStream.WriteAsync(payload, cancellationToken);
                return;
            case NbtCompression.ZLib:
                await using (var compressionStream = new ZLibStream(stream, CompressionMode.Compress, true))
                    await compressionStream.WriteAsync(payload, cancellationToken);
                return;
            case NbtCompression.Brotli:
                await using (var compressionStream = new BrotliStream(stream, CompressionMode.Compress, true))
                    await compressionStream.WriteAsync(payload, cancellationToken);
                return;
            case NbtCompression.Zstd:
                throw new NotSupportedException("Zstd compression is not supported by the serializer yet.");
            default:
                throw new ArgumentOutOfRangeException(nameof(compression));
        }
    }

    private static Stream CreateDecompressionStream(Stream stream, NbtCompression compression) => compression switch
    {
        NbtCompression.GZip => new GZipStream(stream, CompressionMode.Decompress, true),
        NbtCompression.ZLib => new ZLibStream(stream, CompressionMode.Decompress, true),
        NbtCompression.Brotli => new BrotliStream(stream, CompressionMode.Decompress, true),
        NbtCompression.Zstd => throw new NotSupportedException("Zstd compression is not supported by the serializer yet."),
        _ => throw new ArgumentOutOfRangeException(nameof(compression))
    };

    private static object? DeserializeRoot(NbtCompound compound, Type targetType, NbtSerializerOptions options)
    {
        if (targetType.IsAssignableFrom(typeof(NbtCompound)))
            return compound;

        if (IsComplexType(targetType))
            return DeserializeCompound(compound, targetType, options);

        if (compound.TryGetTag(RootValueName, out var valueTag))
            return DeserializeTag(valueTag, targetType, options);

        throw new NotSupportedException($"Type '{targetType}' could not be deserialized from the root compound.");
    }

    private static NbtCompound SerializeCompound(object value, Type type, string? name, NbtSerializerOptions options)
    {
        var metadata = GetTypeMetadata(type);
        var capacity = metadata.SerializableProperties.Length;
        if (options.IncludeFields)
            capacity += metadata.SerializableFields.Length;

        var compound = new NbtCompound(name, capacity);

        foreach (var property in metadata.SerializableProperties)
        {
            var propertyValue = property.Info.GetValue(value);
            if (propertyValue is null)
                continue;

            compound.Add(SerializeValue(GetMemberName(property, options), propertyValue, property.Info.PropertyType, options));
        }

        if (!options.IncludeFields)
            return compound;

        foreach (var field in metadata.SerializableFields)
        {
            var fieldValue = field.Info.GetValue(value);
            if (fieldValue is null)
                continue;

            compound.Add(SerializeValue(GetMemberName(field, options), fieldValue, field.Info.FieldType, options));
        }

        return compound;
    }

    private static INbtTag SerializeValue(string? name, object value, Type declaredType, NbtSerializerOptions options)
    {
        if (value is INbtTag tag)
            return CloneTagWithName(tag, name);

        var valueType = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();

        if (valueType.IsEnum)
        {
            var underlyingType = Enum.GetUnderlyingType(valueType);
            var enumValue = Convert.ChangeType(value, underlyingType);
            return SerializeValue(name, enumValue!, underlyingType, options);
        }

        if (value is bool boolValue)
            return new NbtTag<bool>(name, boolValue);

        if (value is byte byteValue)
            return new NbtTag<byte>(name, byteValue);

        if (value is short shortValue)
            return new NbtTag<short>(name, shortValue);

        if (value is int intValue)
            return new NbtTag<int>(name, intValue);

        if (value is long longValue)
            return new NbtTag<long>(name, longValue);

        if (value is float floatValue)
            return new NbtTag<float>(name, floatValue);

        if (value is double doubleValue)
            return new NbtTag<double>(name, doubleValue);

        if (value is string stringValue)
            return new NbtTag<string>(name, stringValue);

        if (value is byte[] byteArray)
            return new NbtArray<byte>(name, [.. byteArray]);

        if (value is int[] intArray)
            return new NbtArray<int>(name, [.. intArray]);

        if (value is long[] longArray)
            return new NbtArray<long>(name, [.. longArray]);

        if (value is IEnumerable enumerable && value is not string)
            return SerializeList(name, enumerable, declaredType, value.GetType(), options);

        return SerializeCompound(value, valueType, name, options);
    }

    private static NbtList SerializeList(string? name, IEnumerable enumerable, Type declaredType, Type runtimeType, NbtSerializerOptions options)
    {
        var items = new List<object?>();
        Type? runtimeElementType = null;

        foreach (var item in enumerable)
        {
            items.Add(item);
            runtimeElementType ??= item?.GetType();
        }

        var elementType = ResolveElementType(declaredType, runtimeType, runtimeElementType);
        var list = new NbtList(GetTagType(elementType), name);

        foreach (var item in items)
        {
            if (item is null)
                throw new NotSupportedException("Null values inside collections are not supported.");

            list.Add(SerializeValue(null, item, elementType, options));
        }

        return list;
    }

    private static object? DeserializeTag(INbtTag tag, Type targetType, NbtSerializerOptions options)
    {
        if (targetType == typeof(object))
            return DeserializeToObject(tag, options);

        var effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (effectiveType.IsAssignableFrom(tag.GetType()))
            return tag;

        if (effectiveType.IsEnum)
        {
            var enumValue = DeserializeTag(tag, Enum.GetUnderlyingType(effectiveType), options);
            return enumValue is null ? null : Enum.ToObject(effectiveType, enumValue);
        }

        if (effectiveType == typeof(bool))
            return tag switch
            {
                NbtTag<bool> boolTag => boolTag.Value,
                NbtTag<byte> boolByteTag => boolByteTag.Value == 1,
                _ => throw new NotSupportedException($"Tag '{tag.Name}' cannot be converted to '{effectiveType}'.")
            };

        if (effectiveType == typeof(byte) && tag is NbtTag<byte> byteTag)
            return byteTag.Value;

        if (effectiveType == typeof(short) && tag is NbtTag<short> shortTag)
            return shortTag.Value;

        if (effectiveType == typeof(int) && tag is NbtTag<int> intTag)
            return intTag.Value;

        if (effectiveType == typeof(long) && tag is NbtTag<long> longTag)
            return longTag.Value;

        if (effectiveType == typeof(float) && tag is NbtTag<float> floatTag)
            return floatTag.Value;

        if (effectiveType == typeof(double) && tag is NbtTag<double> doubleTag)
            return doubleTag.Value;

        if (effectiveType == typeof(string) && tag is NbtTag<string> stringTag)
            return stringTag.Value;

        if (effectiveType == typeof(byte[]) && tag is NbtArray<byte> byteArray)
            return byteArray.GetArray().ToArray();

        if (effectiveType == typeof(int[]) && tag is NbtArray<int> intArray)
            return intArray.GetArray().ToArray();

        if (effectiveType == typeof(long[]) && tag is NbtArray<long> longArray)
            return longArray.GetArray().ToArray();

        if (TryGetEnumerableElementType(effectiveType, out var elementType) && tag is NbtList list)
            return DeserializeList(list, effectiveType, elementType, options);

        if (tag is NbtCompound compound)
            return DeserializeCompound(compound, effectiveType, options);

        throw new NotSupportedException($"Tag '{tag.Name}' cannot be converted to '{effectiveType}'.");
    }

    private static object? DeserializeCompound(NbtCompound compound, Type targetType, NbtSerializerOptions options)
    {
        if (targetType.IsAssignableFrom(typeof(NbtCompound)))
            return compound;

        var metadata = GetTypeMetadata(targetType);
        var instance = Activator.CreateInstance(targetType)
            ?? throw new NotSupportedException($"Type '{targetType}' must have a parameterless constructor.");

        var comparer = options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var tags = new Dictionary<string, INbtTag>(compound.Count, comparer);

        foreach (var (tagName, tagValue) in compound)
            tags.Add(tagName, tagValue);

        foreach (var property in metadata.DeserializableProperties)
        {
            if (!tags.TryGetValue(GetMemberName(property, options), out var tag))
                continue;

            property.Info.SetValue(instance, DeserializeTag(tag, property.Info.PropertyType, options));
        }

        if (!options.IncludeFields)
            return instance;

        foreach (var field in metadata.SerializableFields)
        {
            if (!tags.TryGetValue(GetMemberName(field, options), out var tag))
                continue;

            field.Info.SetValue(instance, DeserializeTag(tag, field.Info.FieldType, options));
        }

        return instance;
    }

    private static object DeserializeList(NbtList list, Type targetType, Type elementType, NbtSerializerOptions options)
    {
        if (targetType.IsArray)
        {
            var array = Array.CreateInstance(elementType, list.Count);

            for (int i = 0; i < list.Count; i++)
                array.SetValue(DeserializeTag(list[i], elementType, options), i);

            return array;
        }

        var listType = typeof(List<>).MakeGenericType(elementType);
        var buffer = (IList)Activator.CreateInstance(listType)!;

        foreach (var child in list)
            buffer.Add(DeserializeTag(child, elementType, options));

        if (targetType.IsAssignableFrom(listType))
            return buffer;

        var instance = Activator.CreateInstance(targetType)
            ?? throw new NotSupportedException($"Collection type '{targetType}' must have a parameterless constructor.");

        var addMethod = targetType.GetMethod("Add", [elementType])
            ?? throw new NotSupportedException($"Collection type '{targetType}' must expose an Add method.");

        foreach (var item in buffer)
            addMethod.Invoke(instance, [item]);

        return instance;
    }

    private static object? DeserializeToObject(INbtTag tag, NbtSerializerOptions options) => tag switch
    {
        NbtTag<byte> byteTag => byteTag.Value,
        NbtTag<short> shortTag => shortTag.Value,
        NbtTag<int> intTag => intTag.Value,
        NbtTag<long> longTag => longTag.Value,
        NbtTag<float> floatTag => floatTag.Value,
        NbtTag<double> doubleTag => doubleTag.Value,
        NbtTag<string> stringTag => stringTag.Value,
        NbtArray<byte> byteArray => byteArray.GetArray().ToArray(),
        NbtArray<int> intArray => intArray.GetArray().ToArray(),
        NbtArray<long> longArray => longArray.GetArray().ToArray(),
        NbtList list => DeserializeList(list, typeof(List<object>), typeof(object), options),
        NbtCompound compound => compound,
        _ => throw new NotSupportedException($"Tag '{tag.Name}' cannot be converted to an object.")
    };

    private static CachedPropertyMetadata[] GetSerializableProperties(Type type) =>
        GetTypeMetadata(type).SerializableProperties;

    private static CachedPropertyMetadata[] GetDeserializableProperties(Type type) =>
        GetTypeMetadata(type).DeserializableProperties;

    private static CachedFieldMetadata[] GetSerializableFields(Type type) =>
        GetTypeMetadata(type).SerializableFields;

    private static bool ShouldIgnore(MemberInfo member) =>
        member.IsDefined(typeof(NbtIgnoreAttribute), true);

    private static string GetMemberName(CachedPropertyMetadata property, NbtSerializerOptions options) =>
        GetMemberName(property.Info.Name, property.CustomName, options);

    private static string GetMemberName(CachedFieldMetadata field, NbtSerializerOptions options) =>
        GetMemberName(field.Info.Name, field.CustomName, options);

    private static string GetMemberName(string memberName, string? customName, NbtSerializerOptions options) =>
        customName ?? ConvertMemberName(memberName, options.NameConverter);

    private static string ConvertMemberName(string memberName, Func<string, string>? nameConverter) =>
        nameConverter is null ? memberName : nameConverter(memberName);

    private static string? GetCustomMemberName(MemberInfo member) =>
        member.GetCustomAttribute<NbtPropertyNameAttribute>(true)?.Name;

    private static string GetRootName(Type type, NbtSerializerOptions options) =>
        string.IsNullOrWhiteSpace(options.RootName) ? type.Name : options.RootName;

    private static bool IsComplexType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string) || typeof(INbtTag).IsAssignableFrom(type))
            return false;

        if (type.IsEnum || type.IsPrimitive)
            return false;

        if (type == typeof(decimal) || type == typeof(byte[]) || type == typeof(int[]) || type == typeof(long[]))
            return false;

        return !typeof(IEnumerable).IsAssignableFrom(type);
    }

    private static Type ResolveElementType(Type declaredType, Type runtimeType, Type? runtimeElementType)
    {
        if (TryGetEnumerableElementType(declaredType, out var declaredElementType) && declaredElementType != typeof(object))
            return declaredElementType;

        if (runtimeElementType is not null)
            return runtimeElementType;

        if (TryGetEnumerableElementType(runtimeType, out var actualElementType))
            return actualElementType;

        throw new NotSupportedException($"Unable to determine the element type for '{runtimeType}'.");
    }

    private static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        var cachedElementType = EnumerableElementTypeCache.GetOrAdd(type, static type => ResolveEnumerableElementType(type));
        if (cachedElementType is null)
        {
            elementType = default!;
            return false;
        }

        elementType = cachedElementType;
        return true;
    }

    private static CachedTypeMetadata GetTypeMetadata(Type type) =>
        TypeMetadataCache.GetOrAdd(type, static type => new CachedTypeMetadata
        {
            SerializableProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && x.GetIndexParameters().Length == 0 && !ShouldIgnore(x))
                .Select(static x => new CachedPropertyMetadata
                {
                    Info = x,
                    CustomName = GetCustomMemberName(x)
                })
                .ToArray(),
            DeserializableProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanWrite && x.GetIndexParameters().Length == 0 && !ShouldIgnore(x))
                .Select(static x => new CachedPropertyMetadata
                {
                    Info = x,
                    CustomName = GetCustomMemberName(x)
                })
                .ToArray(),
            SerializableFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => !x.IsInitOnly && !ShouldIgnore(x))
                .Select(static x => new CachedFieldMetadata
                {
                    Info = x,
                    CustomName = GetCustomMemberName(x)
                })
                .ToArray()
        });

    private static Type? ResolveEnumerableElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];

        foreach (var interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return interfaceType.GetGenericArguments()[0];
        }

        return null;
    }

    private static NbtTagType GetTagType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (typeof(INbtTag).IsAssignableFrom(type))
        {
            if (type == typeof(NbtCompound))
                return NbtTagType.Compound;

            if (type == typeof(NbtList))
                return NbtTagType.List;

            if (type == typeof(NbtArray<byte>))
                return NbtTagType.ByteArray;

            if (type == typeof(NbtArray<int>))
                return NbtTagType.IntArray;

            if (type == typeof(NbtArray<long>))
                return NbtTagType.LongArray;
        }

        if (type.IsEnum)
            return GetTagType(Enum.GetUnderlyingType(type));

        if (type == typeof(bool) || type == typeof(byte))
            return NbtTagType.Byte;

        if (type == typeof(short))
            return NbtTagType.Short;

        if (type == typeof(int))
            return NbtTagType.Int;

        if (type == typeof(long))
            return NbtTagType.Long;

        if (type == typeof(float))
            return NbtTagType.Float;

        if (type == typeof(double))
            return NbtTagType.Double;

        if (type == typeof(string))
            return NbtTagType.String;

        if (type == typeof(byte[]))
            return NbtTagType.ByteArray;

        if (type == typeof(int[]))
            return NbtTagType.IntArray;

        if (type == typeof(long[]))
            return NbtTagType.LongArray;

        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string) && TryGetEnumerableElementType(type, out var elementType))
            return GetTagType(elementType);

        return NbtTagType.Compound;
    }

    private static INbtTag CloneTagWithName(INbtTag tag, string? name) => tag switch
    {
        NbtTag<bool> boolTag => new NbtTag<bool>(name, boolTag.Value),
        NbtTag<byte> byteTag => new NbtTag<byte>(name, byteTag.Value),
        NbtTag<short> shortTag => new NbtTag<short>(name, shortTag.Value),
        NbtTag<int> intTag => new NbtTag<int>(name, intTag.Value),
        NbtTag<long> longTag => new NbtTag<long>(name, longTag.Value),
        NbtTag<float> floatTag => new NbtTag<float>(name, floatTag.Value),
        NbtTag<double> doubleTag => new NbtTag<double>(name, doubleTag.Value),
        NbtTag<string> stringTag => new NbtTag<string>(name, stringTag.Value),
        NbtArray<byte> byteArray => new NbtArray<byte>(name, [.. byteArray.GetArray()]),
        NbtArray<int> intArray => new NbtArray<int>(name, [.. intArray.GetArray()]),
        NbtArray<long> longArray => new NbtArray<long>(name, [.. longArray.GetArray()]),
        NbtList list => CloneList(list, name),
        NbtCompound compound => CloneCompound(compound, name),
        _ => throw new NotSupportedException($"Tag type '{tag.GetType()}' is not supported.")
    };

    private static NbtCompound CloneCompound(NbtCompound compound, string? name)
    {
        var clone = new NbtCompound(name);

        foreach (var (_, child) in compound)
            clone.Add(CloneTagWithName(child, child.Name));

        return clone;
    }

    private static NbtList CloneList(NbtList list, string? name)
    {
        var clone = new NbtList(list.ListType, name);

        foreach (var child in list)
            clone.Add(CloneTagWithName(child, null));

        return clone;
    }

    private sealed class CachedTypeMetadata
    {
        public required CachedPropertyMetadata[] SerializableProperties { get; init; }

        public required CachedPropertyMetadata[] DeserializableProperties { get; init; }

        public required CachedFieldMetadata[] SerializableFields { get; init; }
    }

    private sealed class CachedPropertyMetadata
    {
        public required PropertyInfo Info { get; init; }

        public required string? CustomName { get; init; }
    }

    private sealed class CachedFieldMetadata
    {
        public required FieldInfo Info { get; init; }

        public required string? CustomName { get; init; }
    }
}
