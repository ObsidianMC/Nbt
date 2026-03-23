using Obsidian.Nbt;
using Obsidian.Nbt.Serializer;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Nbt.Tests;

public class NbtSerializerTests
{
    [Fact]
    public void SerializeAndDeserializeCompoundRoundTrips()
    {
        var value = new PlayerData
        {
            Name = "Lost",
            Health = 20,
            IsAdmin = true,
            Position = new PositionData
            {
                X = 1.5f,
                Y = 64,
                Z = -12.25f
            },
            Scores = [1, 2, 3],
            Data = [4, 5, 6]
        };

        var compound = NbtSerializer.Serialize(value, new NbtSerializerOptions { RootName = "Player" });
        var roundTrip = NbtSerializer.Deserialize<PlayerData>(compound)!;

        Assert.Equal("Player", compound.Name);
        Assert.Equal(value.Name, roundTrip.Name);
        Assert.Equal(value.Health, roundTrip.Health);
        Assert.Equal(value.IsAdmin, roundTrip.IsAdmin);
        Assert.Equal(value.Position.X, roundTrip.Position.X);
        Assert.Equal(value.Position.Y, roundTrip.Position.Y);
        Assert.Equal(value.Position.Z, roundTrip.Position.Z);
        Assert.Equal(value.Scores, roundTrip.Scores);
        Assert.Equal(value.Data, roundTrip.Data);
    }

    [Fact]
    public async Task SerializeAndDeserializeStreamRoundTripsAsync()
    {
        var options = new NbtSerializerOptions
        {
            RootName = "Player",
            Compression = NbtCompression.GZip
        };

        var value = new PlayerData
        {
            Name = "Async",
            Health = 18,
            IsAdmin = true,
            Position = new PositionData
            {
                X = 20,
                Y = 80,
                Z = -4
            },
            Scores = [2, 4, 6],
            Data = [1, 3, 5]
        };

        await using var stream = new MemoryStream();

        await NbtSerializer.SerializeAsync(stream, value, options);

        stream.Position = 0;

        var roundTrip = await NbtSerializer.DeserializeAsync<PlayerData>(stream, options);

        Assert.NotNull(roundTrip);
        Assert.Equal(value.Name, roundTrip.Name);
        Assert.Equal(value.Health, roundTrip.Health);
        Assert.Equal(value.IsAdmin, roundTrip.IsAdmin);
        Assert.Equal(value.Position.X, roundTrip.Position.X);
        Assert.Equal(value.Position.Y, roundTrip.Position.Y);
        Assert.Equal(value.Position.Z, roundTrip.Position.Z);
        Assert.Equal(value.Scores, roundTrip.Scores);
        Assert.Equal(value.Data, roundTrip.Data);
    }

    [Fact]
    public void SerializeAndDeserializeStreamRoundTrips()
    {
        var value = new PlayerData
        {
            Name = "Streamed",
            Health = 10,
            Position = new PositionData
            {
                X = 10,
                Y = 70,
                Z = 5
            },
            Scores = [7, 8],
            Data = [9, 10, 11]
        };

        using var stream = new MemoryStream();

        NbtSerializer.Serialize(stream, value, new NbtSerializerOptions { RootName = "Player" });

        stream.Position = 0;

        var roundTrip = NbtSerializer.Deserialize<PlayerData>(stream)!;

        Assert.Equal(value.Name, roundTrip.Name);
        Assert.Equal(value.Health, roundTrip.Health);
        Assert.Equal(value.IsAdmin, roundTrip.IsAdmin);
        Assert.Equal(value.Position.X, roundTrip.Position.X);
        Assert.Equal(value.Position.Y, roundTrip.Position.Y);
        Assert.Equal(value.Position.Z, roundTrip.Position.Z);
        Assert.Equal(value.Scores, roundTrip.Scores);
        Assert.Equal(value.Data, roundTrip.Data);
    }

    [Fact]
    public void DeserializeCanMatchPropertyNamesCaseInsensitively()
    {
        var compound = new NbtCompound("Player");

        compound.Add(new NbtTag<string>("name", "Casey"));
        compound.Add(new NbtTag<int>("health", 5));
        compound.Add(new NbtTag<byte>("isadmin", 1));

        var position = new NbtCompound("position");
        position.Add(new NbtTag<float>("x", 2));
        position.Add(new NbtTag<int>("y", 80));
        position.Add(new NbtTag<float>("z", 3));

        compound.Add(position);

        var result = NbtSerializer.Deserialize<PlayerData>(compound, new NbtSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;

        Assert.Equal("Casey", result.Name);
        Assert.Equal(5, result.Health);
        Assert.True(result.IsAdmin);
        Assert.Equal(2, result.Position.X);
        Assert.Equal(80, result.Position.Y);
        Assert.Equal(3, result.Position.Z);
    }

    [Fact]
    public void SerializeAndDeserializeUsePropertyNamingPolicy()
    {
        var value = new PlayerData
        {
            Name = "Policy",
            Health = 15,
            IsAdmin = true,
            Position = new PositionData
            {
                X = 4,
                Y = 90,
                Z = 8
            }
        };

        var options = new NbtSerializerOptions
        {
            NameConverter = JsonNamingPolicy.CamelCase.ConvertName
        };

        var compound = NbtSerializer.Serialize(value, options);

        Assert.True(compound.TryGetTag("name", out _));
        Assert.True(compound.TryGetTag("health", out _));
        Assert.True(compound.TryGetTag("isAdmin", out _));
        Assert.True(compound.TryGetTag<NbtCompound>("position", out var position));
        Assert.True(position.TryGetTag("x", out _));

        var roundTrip = NbtSerializer.Deserialize<PlayerData>(compound, options)!;

        Assert.Equal(value.Name, roundTrip.Name);
        Assert.Equal(value.Health, roundTrip.Health);
        Assert.Equal(value.IsAdmin, roundTrip.IsAdmin);
        Assert.Equal(value.Position.X, roundTrip.Position.X);
        Assert.Equal(value.Position.Y, roundTrip.Position.Y);
        Assert.Equal(value.Position.Z, roundTrip.Position.Z);
    }

    [Fact]
    public void IgnoreAttributeSkipsPropertyDuringSerializationAndDeserialization()
    {
        var value = new IgnoredPlayerData
        {
            Name = "Hidden",
            Secret = "do-not-write"
        };

        var compound = NbtSerializer.Serialize(value);

        Assert.True(compound.TryGetTag("Name", out _));
        Assert.False(compound.TryGetTag("Secret", out _));

        var incoming = new NbtCompound("IgnoredPlayerData");
        incoming.Add(new NbtTag<string>("Name", "Visible"));
        incoming.Add(new NbtTag<string>("Secret", "still-hidden"));

        var result = NbtSerializer.Deserialize<IgnoredPlayerData>(incoming)!;

        Assert.Equal("Visible", result.Name);
        Assert.Null(result.Secret);
    }

    [Fact]
    public void PropertyAttributeOverridesNameDuringSerializationAndDeserialization()
    {
        var options = new NbtSerializerOptions
        {
            NameConverter = JsonNamingPolicy.CamelCase.ConvertName
        };

        var value = new RenamedPlayerData
        {
            DisplayName = "Renamed",
            Health = 12
        };

        var compound = NbtSerializer.Serialize(value, options);

        Assert.True(compound.TryGetTag("display_name", out _));
        Assert.True(compound.TryGetTag("health", out _));
        Assert.False(compound.TryGetTag("displayName", out _));

        var incoming = new NbtCompound("RenamedPlayerData");
        incoming.Add(new NbtTag<string>("display_name", "Incoming"));
        incoming.Add(new NbtTag<int>("health", 9));

        var result = NbtSerializer.Deserialize<RenamedPlayerData>(incoming, options)!;

        Assert.Equal("Incoming", result.DisplayName);
        Assert.Equal(9, result.Health);
    }

    [Fact]
    public void SerializeAndDeserializeStreamRoundTripsListsOfCompounds()
    {
        var value = new InventoryPlayerData
        {
            Name = "Inventory",
            Items =
            [
                new InventoryItemData { Slot = 0, Count = 32, Id = "minecraft:stone" },
                new InventoryItemData { Slot = 1, Count = 16, Id = "minecraft:dirt" }
            ]
        };

        using var stream = new MemoryStream();

        NbtSerializer.Serialize(stream, value, new NbtSerializerOptions { RootName = "Player" });

        stream.Position = 0;

        var roundTrip = NbtSerializer.Deserialize<InventoryPlayerData>(stream)!;

        Assert.Equal(value.Name, roundTrip.Name);
        Assert.Equal(value.Items.Count, roundTrip.Items.Count);
        Assert.Collection(roundTrip.Items,
            item =>
            {
                Assert.Equal(0, item.Slot);
                Assert.Equal(32, item.Count);
                Assert.Equal("minecraft:stone", item.Id);
            },
            item =>
            {
                Assert.Equal(1, item.Slot);
                Assert.Equal(16, item.Count);
                Assert.Equal("minecraft:dirt", item.Id);
            });
    }

    private sealed class PlayerData
    {
        public string Name { get; set; } = string.Empty;

        public int Health { get; set; }

        public bool IsAdmin { get; set; }

        public PositionData Position { get; set; } = new();

        public List<int> Scores { get; set; } = [];

        public byte[] Data { get; set; } = [];
    }

    private sealed class PositionData
    {
        public float X { get; set; }

        public int Y { get; set; }

        public float Z { get; set; }
    }

    private sealed class IgnoredPlayerData
    {
        public string Name { get; set; } = string.Empty;

        [NbtIgnore]
        public string? Secret { get; set; }
    }

    private sealed class RenamedPlayerData
    {
        [NbtPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        public int Health { get; set; }
    }

    private sealed class InventoryPlayerData
    {
        public string Name { get; set; } = string.Empty;

        public List<InventoryItemData> Items { get; set; } = [];
    }

    private sealed class InventoryItemData
    {
        public int Slot { get; set; }

        public byte Count { get; set; }

        public string Id { get; set; } = string.Empty;
    }
}
