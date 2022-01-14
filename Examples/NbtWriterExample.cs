using Obsidian.Nbt;
using System.Buffers;

namespace Examples;

internal class NbtWriterExample : IExample
{
    public void Run()
    {
        // Prepare data for writing
        int number = 5;
        string name = "Jon";
        long[] array = { 1, 2, 3, 4, 5 };

        // Initialize writer and output stream
        // Passing in an arrray pool is optional, but recommended
        var stream = new MemoryStream();
        var writer = new NbtWriter(stream, ArrayPool<byte>.Shared);

        // Write data
        writer.WriteInt("my_number", number);
        writer.WriteString("my_name", name);
        writer.WriteLongArray("my_array", array);

        // Write a list of floats
        writer.WriteList(NbtTag.Float, length: 3);
        writer.WriteFloat(1f);
        writer.WriteFloat(2f);
        writer.WriteFloat(3f);

        // Write an empty list
        writer.WriteEmptyList("my_enemies");

        // Write a compound
        writer.WriteCompound("fridge");
        writer.WriteInt("eggs", 3);
        writer.WriteInt("cheese_slices", 12);
        writer.WriteEnd(); // Don't forget to close your compounds!

        writer.WriteEnd(); // End the root compound
        writer.Dispose(); // Writer must get propertly disposed when it's done with writing

        // Use written data
        byte[] buffer = stream.ToArray();
        Console.WriteLine($"Written bytes [Length = {buffer.Length}]:");
        for (int i = 0; i < buffer.Length; i++)
        {
            Console.Write($"{buffer[i]} ");
        }
    }
}
