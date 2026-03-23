namespace Obsidian.Nbt.Serializer;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class NbtIgnoreAttribute : Attribute
{
}
