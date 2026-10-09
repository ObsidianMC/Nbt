using Obsidian.Nbt;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Xunit;

namespace Nbt.Tests;

/// <summary>
/// Network NBT as vanilla writes and reads it (FriendlyByteBuf.writeNbt/readNbt since 1.20.2): the root tag's type
/// then its payload, with no root name.
/// </summary>
public class NbtNetworkTests
{
    [Fact]
    public void WriterProducesVanillaNetworkBytes()
    {
        using var writer = new RawNbtWriter(true);

        writer.WriteListStart("lists", NbtTagType.List, 1);
        writer.WriteListStart("", NbtTagType.Int, 1);
        writer.WriteInt(7);
        writer.EndList();
        writer.EndList();

        writer.WriteListStart("arrays", NbtTagType.IntArray, 1);
        writer.WriteArray(null, [1]);
        writer.EndList();

        writer.WriteString("s", "é\0");
        writer.EndCompound();
        writer.TryFinish();

        byte[] expected =
        [
            0x0A, // Root compound, no name.
            0x09, 0x00, 0x05, .. "lists"u8, 0x09, 0x00, 0x00, 0x00, 0x01,
            0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x07, // The inner list: no type or name of its own.
            0x09, 0x00, 0x06, .. "arrays"u8, 0x0B, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // The int array, payload only.
            0x08, 0x00, 0x01, .. "s"u8, 0x00, 0x04, 0xC3, 0xA9, 0xC0, 0x80, // Modified UTF-8: \0 is C0 80.
            0x00
        ];

        Assert.Equal(expected, writer.Data.ToArray());

        var root = Assert.IsType<NbtCompound>(NbtReader.ReadTag(expected, false, out var bytesRead));
        Assert.Equal(expected.Length, bytesRead);
        Assert.Equal("é\0", root.GetString("s"));

        var inner = Assert.IsType<NbtList>(Assert.IsType<NbtList>(root["lists"])[0]);
        Assert.Equal(7, Assert.IsType<NbtTag<int>>(inner[0]).Value);
        Assert.Equal([1], Assert.IsType<NbtArray<int>>(Assert.IsType<NbtList>(root["arrays"])[0]).GetArray());
    }

    [Fact]
    public void RoundTripsEveryTagTypeThroughStreamsAndSpans()
    {
        var original = new NbtCompound
        {
            new NbtTag<byte>("byte", 0xFE),
            new NbtTag<short>("short", -2),
            new NbtTag<int>("int", int.MinValue),
            new NbtTag<long>("long", long.MaxValue),
            new NbtTag<float>("float", 0.5f),
            new NbtTag<double>("double", -0.25),
            new NbtTag<string>("long string", new string('x', 40_000)), // Past a signed 16-bit length.
            new NbtArray<byte>("bytes", [1, 2, 3]),
            new NbtArray<int>("ints", [1, -1, int.MaxValue]),
            new NbtArray<long>("longs", [long.MinValue, 2]),
            new NbtList(NbtTagType.End, "empty"),
            new NbtList(NbtTagType.Compound, "compounds") { new NbtCompound { new NbtTag<int>("a", 1) } }
        };

        var payload = WriteNetwork(original);

        // Obsidian's packet reader wraps a copy of the packet in a MemoryStream that doesn't expose its buffer, with
        // more packet data after the tag.
        var stream = new MemoryStream([.. payload, 0xAB]);
        var fromStream = Assert.IsType<NbtCompound>(new NbtReader(stream).ReadNextTag(false));
        Assert.Equal(payload.Length, stream.Position);

        var fromSpan = Assert.IsType<NbtCompound>(NbtReader.ReadTag(payload, false, out _));

        Assert.Equal(payload, WriteNetwork(fromStream));
        Assert.Equal(payload, WriteNetwork(fromSpan));
        Assert.Equal(40_000, fromSpan.GetString("long string")!.Length);
    }

    [Fact]
    public void ReadsVanillaHeterogeneousListsAndRepeatedKeys()
    {
        // Since 1.21.5 vanilla writes a list of mixed types as compounds that wrap each element under an empty key.
        byte[] mixedList =
        [
            0x0A,
            0x09, 0x00, 0x01, .. "m"u8, 0x0A, 0x00, 0x00, 0x00, 0x02,
            0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, // {"": 1}
            0x08, 0x00, 0x00, 0x00, 0x01, .. "a"u8, 0x00, // {"": "a"}
            0x00
        ];

        var root = Assert.IsType<NbtCompound>(NbtReader.ReadTag(mixedList, false, out _));
        var wrapped = Assert.IsType<NbtCompound>(Assert.IsType<NbtList>(root["m"])[1]);
        Assert.Equal("a", wrapped.GetString(""));

        // Written back, the wrappers keep their empty keys, so the client reads the same list.
        Assert.Equal(mixedList, WriteNetwork(root));

        // Like vanilla, a repeated key keeps the last value.
        byte[] repeatedKey =
        [
            0x0A,
            0x03, 0x00, 0x01, .. "k"u8, 0x00, 0x00, 0x00, 0x01,
            0x03, 0x00, 0x01, .. "k"u8, 0x00, 0x00, 0x00, 0x02,
            0x00
        ];

        Assert.Equal(2, Assert.IsType<NbtCompound>(NbtReader.ReadTag(repeatedKey, false, out _)).GetInt("k"));
    }

    public static TheoryData<string, byte[], Type> MalformedPayloads => new()
    {
        { "nested 513 deep", NestedLists(513), typeof(InvalidDataException) },
        { "list longer than the data", [0x09, 0x01, 0x7F, 0xFF, 0xFF, 0xFF], typeof(EndOfStreamException) },
        { "array longer than the data", [0x0C, 0x7F, 0xFF, 0xFF, 0xFF, 0x00], typeof(EndOfStreamException) },
        { "byte array longer than the data", [0x07, 0x7F, 0xFF, 0xFF, 0xFF, 0x00], typeof(EndOfStreamException) },
        { "negative list length", [0x09, 0x01, 0xFF, 0xFF, 0xFF, 0xFF], typeof(InvalidDataException) },
        { "non-empty list without a type", [0x09, 0x00, 0x00, 0x00, 0x00, 0x01], typeof(InvalidDataException) },
        { "compound cut short", [0x0A, 0x01, 0x00, 0x01, .. "b"u8, 0x05], typeof(EndOfStreamException) },
        { "unknown tag type", [0x0A, 0x0D, 0x00, 0x00], typeof(InvalidDataException) }
    };

    [Theory]
    [MemberData(nameof(MalformedPayloads))]
    public void RejectsMalformedPayloads(string description, byte[] payload, Type exceptionType)
    {
        Assert.NotNull(description);
        Assert.Throws(exceptionType, () => NbtReader.ReadTag(payload, false, out _));
        Assert.Throws(exceptionType, () => new NbtReader(new MemoryStream(payload, false)).ReadNextTag(false));

        // A decompression stream can't tell how much data is left, so forged lengths must fail without allocating
        // what they declare.
        var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, true))
            gzip.Write(payload);

        compressed.Position = 0;
        Assert.Throws(exceptionType, () => new NbtReader(compressed, NbtCompression.GZip).ReadNextTag(false));
    }

    public static TheoryData<string, byte[], bool> QuotaBoundaries => new()
    {
        // Vanilla charges a byte array 24 bytes plus its length, against a 2 MiB budget.
        { "largest byte array", ByteArray(2_097_128), true },
        { "byte array a byte too big", ByteArray(2_097_129), false },

        // A list of empty compounds costs 36, plus 4 and 48 per compound: a one-byte compound can't expand forever.
        { "most empty compounds", EmptyCompounds(40_329), true },
        { "one empty compound too many", EmptyCompounds(40_330), false }
    };

    [Theory]
    [MemberData(nameof(QuotaBoundaries))]
    public void NetworkReadsAllowWhatVanillaAllows(string description, byte[] payload, bool accepted)
    {
        Assert.NotNull(description);

        if (accepted)
        {
            Assert.NotNull(NbtReader.ReadNetworkTag(payload, out var bytesRead));
            Assert.Equal(payload.Length, bytesRead);
        }
        else
        {
            Assert.Throws<InvalidDataException>(() => NbtReader.ReadNetworkTag(payload, out _));
        }

        // Files have no budget.
        Assert.NotNull(NbtReader.ReadTag(payload, false, out _));
    }

    [Fact]
    public void AcceptsNestingAsDeepAsVanilla()
    {
        Assert.IsType<NbtList>(NbtReader.ReadTag(NestedLists(512), false, out _));
    }

    [Fact]
    public void EndTagReadsAsNoTag()
    {
        Assert.Null(NbtReader.ReadTag([0x00, 0x0A], false, out var bytesRead));
        Assert.Equal(1, bytesRead);
    }

    [Fact]
    public void NonNetworkRootHasEmptyName()
    {
        using var writer = new RawNbtWriter(false);
        writer.EndCompound();

        Assert.Equal([0x0A, 0x00, 0x00, 0x00], writer.Data.ToArray());
    }

    [Fact]
    public void WriterRejectsInvalidStructure()
    {
        using var writer = new RawNbtWriter(true);

        writer.WriteListStart("list", NbtTagType.Int, 1);
        Assert.Throws<InvalidOperationException>(() => writer.WriteString("x"));
        Assert.Throws<InvalidOperationException>(() => writer.EndList());
        writer.WriteInt(1);
        Assert.Throws<InvalidOperationException>(() => writer.WriteInt(2));
        writer.EndList();

        Assert.Throws<ArgumentException>(() => writer.WriteInt("list", 1));
        Assert.Throws<InvalidOperationException>(() => writer.TryFinish());
    }

    // A root list holding a list holding a list..., `depth` lists in all, the innermost empty.
    private static byte[] NestedLists(int depth)
    {
        var bytes = new List<byte> { 0x09 };

        for (var i = 1; i < depth; i++)
            bytes.AddRange([0x09, 0x00, 0x00, 0x00, 0x01]);

        bytes.AddRange([0x00, 0x00, 0x00, 0x00, 0x00]);

        return [.. bytes];
    }

    // A root byte array of `length` zeros.
    private static byte[] ByteArray(int length)
    {
        var payload = new byte[1 + sizeof(int) + length];
        payload[0] = 0x07;
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(1), length);

        return payload;
    }

    // A root list of `count` empty compounds.
    private static byte[] EmptyCompounds(int count)
    {
        var payload = new byte[1 + 1 + sizeof(int) + count];
        payload[0] = 0x09;
        payload[1] = 0x0A;
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(2), count);

        return payload;
    }

    private static byte[] WriteNetwork(NbtCompound compound)
    {
        using var writer = new RawNbtWriter(true);

        foreach (var (_, tag) in compound)
            writer.WriteTag(tag);

        writer.EndCompound();
        writer.TryFinish();

        return writer.Data.ToArray();
    }
}
