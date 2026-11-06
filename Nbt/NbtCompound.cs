using Obsidian.Nbt.Exceptions;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Obsidian.Nbt;

public sealed class NbtCompound : INbtTag, IEnumerable<KeyValuePair<string, INbtTag>>
{
    private readonly Dictionary<string, INbtTag> children;

    public int Count => this.children.Count;

    public NbtTagType Type => NbtTagType.Compound;

    public string? Name { get; set; }

    public INbtTag? Parent { get; set; }

    public INbtTag this[string name] { get => this.children[name]; set => this.Add(name, value); }

    public NbtCompound(string name = "", int capacity = 0)
    {
        if (this.Parent?.Type == NbtTagType.Compound && string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name), "Tags within a compound must be named.");

        this.children = capacity > 0
            ? new Dictionary<string, INbtTag>(capacity, StringComparer.Ordinal)
            : new Dictionary<string, INbtTag>(StringComparer.Ordinal);
        this.Name = name;
    }

    public NbtCompound(List<INbtTag> children) : this()
    {
        foreach (var child in children)
            this.Add(child.Name, child);
    }

    public NbtCompound(string name, List<INbtTag> children) : this(name, children.Count)
    {
        foreach (var child in children)
            this.Add(child.Name, child);
    }

    public bool Remove(string name) => this.children.Remove(name);

    public bool HasTag(string name) => this.children.ContainsKey(name);

    public bool TryGetTag(string name, [MaybeNullWhen(false)] out INbtTag tag) =>
        this.children.TryGetValue(name, out tag);

    public bool TryGetTag<T>(string name, [MaybeNullWhen(false)] out T tag) where T : INbtTag
    {
        if (this.TryGetTag(name, out var childTag) && childTag is T matchedTag)
        {
            tag = matchedTag;
            return true;
        }

        tag = default;
        return false;
    }

    /// <summary>
    /// Gets a tag's value as <typeparamref name="TValue"/>. Like vanilla's numeric tags, a number tag of any type reads
    /// as any number type, so data written by hand or converted from JSON (where a float's <c>1</c> is an int) reads as
    /// it does in vanilla.
    /// </summary>
    public bool TryGetTagValue<TValue>(string name, [MaybeNullWhen(false)] out TValue value)
    {
        if (this.TryGetTag(name, out var tag))
            return TryReadValue(tag, out value);

        value = default;
        return false;
    }

    public byte GetByte(string name) => this.GetTagValue<byte>(name);

    public short GetShort(string name) => this.GetTagValue<short>(name);

    public int GetInt(string name) => this.GetTagValue<int>(name);

    public long GetLong(string name) => this.GetTagValue<long>(name);

    public float GetFloat(string name) => this.GetTagValue<float>(name);

    public double GetDouble(string name) => this.GetTagValue<double>(name);

    public string? GetString(string name) => this.GetTagValue<string>(name);

    public bool GetBool(string name) =>
        this.TryGetTag<NbtTag<byte>>(name, out var tag) && tag.Value == 1;

    public int EnsureCapacity(int capacity) => this.children.EnsureCapacity(capacity);

    public void Clear() => this.children.Clear();

    public override string ToString()
    {
        var sb = new StringBuilder();
        var count = this.Count;

        sb.AppendLine($"TAG_Compound('{this.Name}'): {count} {(count > 1 ? "entries" : "entry")}")
            .AppendLine("{");

        foreach (var (_, tag) in this)
            sb.AppendLine(tag.PrettyString());

        sb.AppendLine("}");

        return sb.ToString();
    }

    public string PrettyString(int depth = 2, int addBraceDepth = 1)
    {
        var sb = new StringBuilder();
        var count = this.Count;

        var name = $"TAG_Compound('{this.Name}'): {count} {(count > 1 ? "entries" : "entry")}";

        sb.AppendLine(name.PadLeft(name.Length + depth))
            .AppendLine("{".PadLeft(depth + addBraceDepth));

        foreach (var (_, tag) in this)
        {
            var tagString = tag.PrettyString(depth + 1, addBraceDepth + 2);
            sb.AppendLine(tagString.PadLeft(tagString.Length + depth));
        }

        sb.AppendLine("}".PadLeft(depth + addBraceDepth));

        return sb.ToString();
    }

    public void Add(string name, INbtTag tag)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name), "Tags inside a compound must be named.");

        tag.Parent = this;

        this.children.Add(name, tag);
    }

    public void Add(INbtTag tag)
    {
        var name = tag.Name;
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Tags inside a compound must be named.");

        tag.Parent = this;

        this.children.Add(name, tag);
    }

    /// <summary>
    /// Stores a tag read from binary NBT. Unlike <see cref="Add(string, INbtTag)"/>, empty names are allowed and a
    /// repeated name replaces the earlier tag, as vanilla reads them: heterogeneous lists are saved and sent as
    /// compounds with an empty key.
    /// </summary>
    /// <returns>Whether the name was new to the compound.</returns>
    internal bool SetReadTag(string name, INbtTag tag)
    {
        tag.Parent = this;

        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(this.children, name, out var exists);
        slot = tag;

        return !exists;
    }

    /// <summary>
    /// Enumerates the entries by key. The enumerator is a struct, so <c>foreach</c> over a compound doesn't allocate.
    /// </summary>
    public Dictionary<string, INbtTag>.Enumerator GetEnumerator() => this.children.GetEnumerator();

    IEnumerator<KeyValuePair<string, INbtTag>> IEnumerable<KeyValuePair<string, INbtTag>>.GetEnumerator() =>
        this.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private T? GetTagValue<T>(string name) => this.TryGetTagValue<T>(name, out var value)
        ? value
        : throw new TagNotFoundException(name);

    private static bool TryReadValue<TValue>(INbtTag tag, [MaybeNullWhen(false)] out TValue value)
    {
        if (tag is NbtTag<TValue> exact)
        {
            value = exact.Value!;
            return true;
        }

        var converted = tag switch
        {
            NbtTag<byte> number => FromWhole<TValue>(number.Value),
            NbtTag<short> number => FromWhole<TValue>(number.Value),
            NbtTag<int> number => FromWhole<TValue>(number.Value),
            NbtTag<long> number => FromWhole<TValue>(number.Value),
            NbtTag<float> number => FromReal<TValue>(number.Value),
            NbtTag<double> number => FromReal<TValue>(number.Value),
            _ => null
        };

        if (converted is TValue result)
        {
            value = result;
            return true;
        }

        value = default;
        return false;
    }

    // Vanilla's integer tags keep the low bits of a narrower type (an int's 300 is a byte's 44).
    private static object? FromWhole<TValue>(long number) => System.Type.GetTypeCode(typeof(TValue)) switch
    {
        TypeCode.Byte => unchecked((byte)number),
        TypeCode.Int16 => unchecked((short)number),
        TypeCode.Int32 => unchecked((int)number),
        TypeCode.Int64 => number,
        TypeCode.Single => (float)number,
        TypeCode.Double => (double)number,
        _ => null
    };

    // Vanilla's float and double tags floor to an integer, as Mth.floor does, before keeping its low bits.
    private static object? FromReal<TValue>(double number)
    {
        if (typeof(TValue) == typeof(float))
            return (float)number;

        if (typeof(TValue) == typeof(double))
            return number;

        var floored = Math.Floor(number);

        return typeof(TValue) == typeof(long)
            ? (long)floored
            : FromWhole<TValue>((int)floored);
    }
}
