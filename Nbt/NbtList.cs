namespace Obsidian.Nbt;

public readonly struct NbtList
{
    private readonly NbtDocument parent;

    private readonly int index;

    public NbtTag ChildType { get; }

    public int Count { get; }
    public string Name => this.parent.TryGetProperty(this.index, out var property, out _) ? property.Name : string.Empty;

    internal NbtList(NbtDocument document, int index, NbtTag childType, int count)
    {
        this.parent = document;
        this.index = index;
        this.ChildType = childType;
        this.Count = count;
    }

    public NbtDocument.ArrayEnumerator EnumerateArray()
    {
        return new NbtDocument.ArrayEnumerator(parent, index, this.ChildType);
    }
}