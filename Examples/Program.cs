using Examples;

var examples = new IExample[]
{
    new NbtWriterExample(),
    new NbtReaderExample(),
    new NbtDocumentExample(),
    new BigTestExample()
};

for (int i = 0; i < examples.Length; i++)
{
    Console.WriteLine($"[{i}] {examples[i].Name}");
}

int selection = int.TryParse(Console.ReadLine() ?? "0", out int s) ? s : 0;
IExample example = (uint)selection < (uint)examples.Length ? examples[selection] : examples[0];

Console.WriteLine($"\n{example.Description}\n");
example.Run();
