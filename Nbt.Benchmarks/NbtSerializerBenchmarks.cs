using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Obsidian.Nbt.Serializer;
using Microsoft.VSDiagnostics;

namespace Obsidian.Nbt.Benchmarks;

[CPUUsageDiagnoser]
public class NbtSerializerBenchmarks
{
    private PlayerData _value = null!;
    private NbtCompound _compound = null!;
    private byte[] _uncompressedPayload = null!;
    private byte[] _gzipPayload = null!;
    private NbtSerializerOptions _plainOptions = null!;
    private NbtSerializerOptions _gzipOptions = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _value = PlayerData.Create();
        _plainOptions = new NbtSerializerOptions
        {
            RootName = "Player"
        };
        _gzipOptions = new NbtSerializerOptions
        {
            RootName = "Player",
            Compression = NbtCompression.GZip
        };
        _compound = NbtSerializer.Serialize(_value, _plainOptions);
        using var plainStream = new MemoryStream();
        NbtSerializer.Serialize(plainStream, _value, _plainOptions);
        _uncompressedPayload = plainStream.ToArray();
        using var gzipStream = new MemoryStream();
        NbtSerializer.Serialize(gzipStream, _value, _gzipOptions);
        _gzipPayload = gzipStream.ToArray();
    }

    [Benchmark(Baseline = true)]
    public NbtCompound SerializeToCompound() => NbtSerializer.Serialize(_value, _plainOptions);

    [Benchmark]
    public byte[] SerializeToStream()
    {
        using var stream = new MemoryStream();
        NbtSerializer.Serialize(stream, _value, _plainOptions);
        return stream.ToArray();
    }

    [Benchmark]
    public byte[] SerializeToStreamGZip()
    {
        using var stream = new MemoryStream();
        NbtSerializer.Serialize(stream, _value, _gzipOptions);
        return stream.ToArray();
    }

    [Benchmark]
    public async Task<byte[]> SerializeAsyncToStream()
    {
        await using var stream = new MemoryStream();
        await NbtSerializer.SerializeAsync(stream, _value, _plainOptions);
        return stream.ToArray();
    }

    [Benchmark]
    public PlayerData? DeserializeFromCompound() => NbtSerializer.Deserialize<PlayerData>(_compound, _plainOptions);

    [Benchmark]
    public PlayerData? DeserializeFromStream() => NbtSerializer.Deserialize<PlayerData>(new MemoryStream(_uncompressedPayload), _plainOptions);

    [Benchmark]
    public PlayerData? DeserializeFromStreamGZip() => NbtSerializer.Deserialize<PlayerData>(new MemoryStream(_gzipPayload), _gzipOptions);

    [Benchmark]
    public Task<PlayerData?> DeserializeAsyncFromStream() => NbtSerializer.DeserializeAsync<PlayerData>(new MemoryStream(_uncompressedPayload), _plainOptions).AsTask();

    public sealed class PlayerData
    {
        public string Name { get; set; } = string.Empty;
        public int Health { get; set; }
        public bool IsAdmin { get; set; }
        public PositionData Position { get; set; } = new();
        public List<int> Scores { get; set; } = [];
        public byte[] Data { get; set; } = [];
        public InventoryItem[] Inventory { get; set; } = [];

        public static PlayerData Create()
        {
            var scores = new List<int>(256);
            for (var i = 0; i < 256; i++)
                scores.Add(i * 3);
            var data = new byte[4096];
            for (var i = 0; i < data.Length; i++)
                data[i] = (byte)(i % byte.MaxValue);
            var inventory = new InventoryItem[64];
            for (var i = 0; i < inventory.Length; i++)
            {
                inventory[i] = new InventoryItem
                {
                    Slot = i,
                    Count = (byte)((i % 64) + 1),
                    Id = $"minecraft:item_{i}",
                    CustomModelData = i * 17
                };
            }

            return new PlayerData
            {
                Name = "BenchmarkPlayer",
                Health = 20,
                IsAdmin = true,
                Position = new PositionData
                {
                    X = 1024.5f,
                    Y = 64,
                    Z = -256.25f
                },
                Scores = scores,
                Data = data,
                Inventory = inventory
            };
        }
    }

    public sealed class PositionData
    {
        public float X { get; set; }
        public int Y { get; set; }
        public float Z { get; set; }
    }

    public sealed class InventoryItem
    {
        public int Slot { get; set; }
        public byte Count { get; set; }
        public string Id { get; set; } = string.Empty;
        public int CustomModelData { get; set; }
    }
}