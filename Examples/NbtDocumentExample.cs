using Obsidian.Nbt;
using System.Buffers;

namespace Examples;

internal class NbtDocumentExample : IExample
{
    public void Run()
    {
        Stream dataStream = GetNbtBytes();

        // Create an instance using NbtDocument.Parse
        // byte[] and ReadOnlyMemory<byte> are also supported
        NbtDocument document = NbtDocument.Parse(dataStream);

        NbtCompound root = document.Root; // Root represents the implicit NbtCompound surrounding all NBT data

        int myNumber = root["my_number"].GetInt();
        string myName = root.GetProperty("my_name").GetString();
        int[] myArray = root.TryGetProperty("my_array", out NbtProperty property) ? property.GetIntArray() : [];

        foreach (NbtProperty nbtProperty in root.EnumerateProperties())
        {
            Console.WriteLine($"Found property: {nbtProperty.Name} of type {nbtProperty.Tag}");
        }
        Console.WriteLine();

        Console.WriteLine($"My number: {myNumber}");
        Console.WriteLine($"My name: {myName}");
        Console.Write("My array:");
        foreach (int number in myArray)
            Console.Write($" {number}");
        Console.WriteLine();
    }

    private Stream GetNbtBytes()
    {
        var stream = new MemoryStream();
        var writer = new NbtWriter(stream, ArrayPool<byte>.Shared);

        writer.WriteInt("my_number", 5);
        writer.WriteString("my_name", "Jun");
        writer.WriteIntArray("my_array", new[] { 1, 2, 3 });
        writer.WriteEnd();
        writer.Dispose();

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
