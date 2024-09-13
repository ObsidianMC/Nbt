namespace Obsidian.Nbt.Exceptions;
public sealed class TagNotFoundException : NbtException
{
    public TagNotFoundException(string? message) : base(message)
    {
    }

    public TagNotFoundException(string? message, Exception? innerException) : base(message, innerException) { }
}

