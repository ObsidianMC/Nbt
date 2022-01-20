namespace Nbt.Tests.Writing;

public sealed class Primitives
{
    private void Compare(MemoryStream stream, params byte[] expectedData)
    {
        byte[] streamData = stream.ToArray();
        Assert.Equal(expectedData, streamData);
    }

    [Fact]
    public void WritingEnd()
    {
        var stream = new MemoryStream();
        using (var writer = new NbtWriter(stream))
        {
            writer.WriteEnd();
        }
        Compare(stream, 0);
    }

    [Fact]
    public void WritingByte()
    {
        var stream = new MemoryStream();
        using (var writer = new NbtWriter(stream))
        {
            writer.WriteByte(byte.MinValue);
            writer.WriteByte(byte.MaxValue);
        }
        Compare(stream, byte.MinValue, byte.MaxValue);
    }

    [Fact]
    public void WritíngBool()
    {
        var stream = new MemoryStream();
        using (var writer = new NbtWriter(stream))
        {
            writer.WriteBool(false);
            writer.WriteBool(true);
        }
        Compare(stream, 0, 1);
    }

    [Fact]
    public void WritingShort()
    {
        var stream = new MemoryStream();
        using (var writer = new NbtWriter(stream))
        {
            writer.WriteShort(0);
            writer.WriteShort(short.MinValue);
            writer.WriteShort(short.MaxValue);
        }
        Compare(stream,
            0, 0,
            128, 0,
            127, 255);
    }

    [Fact]
    public void WritingInt()
    {
        var stream = new MemoryStream();
        using (var writer = new NbtWriter(stream))
        {
            writer.WriteInt(0);
            writer.WriteInt(1);
            writer.WriteInt(int.MaxValue);
        }
        Compare(stream,
            0, 0, 0, 0,
            0, 0, 0, 1,
            127, 255, 255, 255);
    }

    [Fact]
    public void WritingLong()
    {
        var stream = new MemoryStream();
        using (var writer = new NbtWriter(stream))
        {
            writer.WriteLong(0);
            writer.WriteLong(1);
            writer.WriteLong(long.MaxValue);
        }
        Compare(stream,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 1,
            127, 255, 255, 255, 255, 255, 255, 255);
    }

    [Fact]
    public void WritingNullString()
    {
        var stream = new MemoryStream();
        using (var writer = new NbtWriter(stream))
        {
            string? output = null;
            writer.WriteString(output);
        }
        Compare(stream, 0, 0);
    }

    [Fact]
    public void WritingEmptyString()
    {
        var stream = new MemoryStream();
        using (var writer = new NbtWriter(stream))
        {
            writer.WriteString(string.Empty);
        }
        Compare(stream, 0, 0);
    }

    [Fact]
    public void WritingString()
    {
        var stream = new MemoryStream();
        string output = "\0\u0001\u007F\u0080\u07FF\u0800\uFFFF";
        using (var writer = new NbtWriter(stream))
        {
            writer.WriteString(output);
        }
        Compare(stream,
            // Data generated with https://docs.oracle.com/javase/8/docs/api/java/io/DataOutputStream.html
            0b00000000, 0b00001110, 0b11000000, 0b10000000,
            0b00000001, 0b01111111, 0b11000010, 0b10000000,
            0b11011111, 0b10111111, 0b11100000, 0b10100000,
            0b10000000, 0b11101111, 0b10111111, 0b10111111);
    }
}
