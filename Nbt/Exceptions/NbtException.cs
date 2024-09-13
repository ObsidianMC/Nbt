namespace Obsidian.Nbt.Exceptions;
public class NbtException : Exception
{
    /// <summary>
    /// The tag type that caused this exception.
    /// </summary>
    public NbtTagType? TagType { get; init; }

    public NbtException()
    {
    }

    public NbtException(string? message) : base(message) { }
    public NbtException(string? message, Exception? innerException) : base(message, innerException) { }
}
