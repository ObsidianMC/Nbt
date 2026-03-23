namespace Obsidian.Nbt.Serializer;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class NbtPropertyNameAttribute : Attribute
{
    public NbtPropertyNameAttribute()
    {
    }

    public NbtPropertyNameAttribute(string name)
    {
        this.Name = name;
    }

    public string? Name { get; set; }
}
