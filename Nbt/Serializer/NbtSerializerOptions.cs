namespace Obsidian.Nbt.Serializer;

public sealed class NbtSerializerOptions
{
    public NbtCompression Compression { get; set; } = NbtCompression.None;

    public string? RootName { get; set; }

    public bool IncludeFields { get; set; }

    public bool PropertyNameCaseInsensitive { get; set; }

    public Func<string, string>? NameConverter { get; set; }
}