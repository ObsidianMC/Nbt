namespace Obsidian.Nbt;

public partial struct NbtWriter
{
    public void WriteCompound()
    {
        WriteByte(NbtTag.Compound);
    }

    public void WriteCompound(string? name)
    {
        WriteCompound(name.AsSpan());
    }

    public void WriteCompound(ReadOnlySpan<char> name)
    {
        WriteByte(NbtTag.Compound);
        WriteString(name);
    }

    public void WriteCompound(ReadOnlySpan<byte> encodedName)
    {
        WriteByte(NbtTag.Compound);
        WriteString(encodedName);
    }

    public void WriteEnd()
    {
        WriteByte(NbtTag.End);
    }
}
