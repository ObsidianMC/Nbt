using System.IO;
using BenchmarkDotNet.Attributes;

namespace Obsidian.Nbt.Benchmarks;

/// <summary>
/// Measures raw NBT reading and writing: network NBT (an unnamed root compound, as packets carry it) and
/// GZip-compressed files, for a chunk-sized compound and a small item-sized compound.
/// </summary>
[MemoryDiagnoser]
public class NbtIoBenchmarks
{
    private NbtCompound chunk = null!;
    private NbtCompound item = null!;
    private byte[] chunkNetworkPayload = null!;
    private byte[] itemNetworkPayload = null!;
    private byte[] chunkGZipPayload = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        this.chunk = CreateChunk();
        this.item = CreateItem();
        this.chunkNetworkPayload = WriteNetwork(this.chunk);
        this.itemNetworkPayload = WriteNetwork(this.item);
        this.chunkGZipPayload = WriteGZip(this.chunk);
    }

    [Benchmark]
    public int WriteChunkNetwork() => WriteNetwork(this.chunk).Length;

    [Benchmark]
    public int WriteItemNetwork() => WriteNetwork(this.item).Length;

    [Benchmark]
    public INbtTag? ReadChunkNetwork() => new NbtReader(new MemoryStream(this.chunkNetworkPayload)).ReadNextTag(false);

    [Benchmark]
    public INbtTag? ReadItemNetwork() => new NbtReader(new MemoryStream(this.itemNetworkPayload)).ReadNextTag(false);

    [Benchmark]
    public INbtTag? ReadChunkNetworkSpan() => NbtReader.ReadTag(this.chunkNetworkPayload, false, out _);

    [Benchmark]
    public INbtTag? ReadItemNetworkSpan() => NbtReader.ReadTag(this.itemNetworkPayload, false, out _);

    [Benchmark]
    public int WriteChunkGZip() => WriteGZip(this.chunk).Length;

    [Benchmark]
    public INbtTag? ReadChunkGZip() =>
        new NbtReader(new MemoryStream(this.chunkGZipPayload), NbtCompression.GZip).ReadNextTag();

    private static byte[] WriteNetwork(NbtCompound compound)
    {
        using var writer = new RawNbtWriter(true);

        foreach (var (_, tag) in compound)
            writer.WriteTag(tag);

        writer.EndCompound();
        writer.TryFinish();

        return writer.Data.ToArray();
    }

    private static byte[] WriteGZip(NbtCompound compound)
    {
        var stream = new MemoryStream();

        using (var writer = new NbtWriterStream(stream, NbtCompression.GZip, ""))
        {
            foreach (var (_, tag) in compound)
                writer.WriteTag(tag);

            writer.EndCompound();
            writer.TryFinish();
        }

        return stream.ToArray();
    }

    // Shaped like a saved chunk: 24 sections with block state palettes and packed data, heightmaps and block entities.
    private static NbtCompound CreateChunk()
    {
        var sections = new NbtList(NbtTagType.Compound, "sections");

        for (var y = -4; y < 20; y++)
        {
            var palette = new NbtList(NbtTagType.Compound, "palette");

            for (var i = 0; i < 12; i++)
            {
                var properties = new NbtCompound("Properties")
                {
                    new NbtTag<string>("facing", "north"),
                    new NbtTag<string>("half", "bottom")
                };

                palette.Add(new NbtCompound
                {
                    new NbtTag<string>("Name", $"minecraft:block_{i}"),
                    properties
                });
            }

            var data = new long[256];
            for (var i = 0; i < data.Length; i++)
                data[i] = i * 0x0123456789ABL;

            var blockStates = new NbtCompound("block_states")
            {
                palette,
                new NbtArray<long>("data", data)
            };

            var biomePalette = new NbtList(NbtTagType.String, "palette")
            {
                new NbtTag<string>("", "minecraft:plains"),
                new NbtTag<string>("", "minecraft:river")
            };

            var biomes = new NbtCompound("biomes")
            {
                biomePalette,
                new NbtArray<long>("data", [0x1234L])
            };

            sections.Add(new NbtCompound
            {
                new NbtTag<byte>("Y", (byte)y),
                blockStates,
                biomes,
                new NbtArray<byte>("BlockLight", new byte[2048]),
                new NbtArray<byte>("SkyLight", new byte[2048])
            });
        }

        var heightmaps = new NbtCompound("Heightmaps")
        {
            new NbtArray<long>("MOTION_BLOCKING", new long[37]),
            new NbtArray<long>("WORLD_SURFACE", new long[37])
        };

        var blockEntities = new NbtList(NbtTagType.Compound, "block_entities");
        for (var i = 0; i < 8; i++)
        {
            blockEntities.Add(new NbtCompound
            {
                new NbtTag<string>("id", "minecraft:chest"),
                new NbtTag<int>("x", i),
                new NbtTag<int>("y", 64),
                new NbtTag<int>("z", -i),
                new NbtList(NbtTagType.Compound, "Items")
            });
        }

        return new NbtCompound("")
        {
            new NbtTag<int>("DataVersion", 4325),
            new NbtTag<int>("xPos", 12),
            new NbtTag<int>("zPos", -7),
            new NbtTag<string>("Status", "minecraft:full"),
            new NbtTag<long>("LastUpdate", 123456789L),
            sections,
            heightmaps,
            blockEntities
        };
    }

    // Shaped like an item's custom data or a text component: a few short named values.
    private static NbtCompound CreateItem()
    {
        var lore = new NbtList(NbtTagType.String, "lore")
        {
            new NbtTag<string>("", "A sword of legend"),
            new NbtTag<string>("", "Forged in the nether")
        };

        return new NbtCompound("")
        {
            new NbtTag<string>("id", "minecraft:diamond_sword"),
            new NbtTag<int>("count", 1),
            new NbtTag<int>("damage", 12),
            new NbtTag<string>("name", "Excalibur"),
            lore,
            new NbtTag<byte>("unbreakable", 1)
        };
    }
}
