using Obsidian.Nbt;
using System.Buffers;

namespace Examples;

internal class NbtReaderExample : IExample
{
    public void Run()
    {
        // NbtReader can accept Streams, ReadOnlyMemory<byte> and ReadOnlySpan<byte>
        // When using streams, it is recommended to also set an array pool
        Stream dataStream = GetNbtBytes();
        var reader = new NbtReader(dataStream, ArrayPool<byte>.Shared);

        // We are expecting valid data
        // If data were malformed, exception will get thrown
        // For safety, you can use reader.TryReadXXXX methods instead or wrap reading code in a try-catch block
        NbtTag tag;
        while ((tag = reader.ReadTag()) != NbtTag.End)
        {
            switch (tag)
            {
                case NbtTag.Int:
                    {
                        string name = reader.ReadString();
                        int value = reader.ReadInt();

                        Console.WriteLine($"Integer '{name}': {value}");
                    }
                    break;

                case NbtTag.String:
                    {
                        string name = reader.ReadString();
                        string value = reader.ReadString();

                        Console.WriteLine($"String '{name}': {value}");
                    }
                    break;

                case NbtTag.IntArray:
                    {
                        string name = reader.ReadString();
                        int length = reader.ReadInt();

                        Console.Write($"Integer array '{name}':");
                        for (int i = 0; i < length; i++)
                        {
                            Console.Write($" {reader.ReadInt()}");
                        }
                        Console.WriteLine();
                    }
                    break;

                default:
                    throw new Exception("Unexpected input!");
            }

            reader.Dispose(); // After reading, NbtReader must get properly disposed!
        }
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
