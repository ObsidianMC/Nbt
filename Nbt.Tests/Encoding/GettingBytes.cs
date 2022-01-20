using System.Buffers;

namespace Nbt.Tests.Encoding;

public sealed class GettingBytes
{
    [Fact]
    public void EmptyString()
    {
        bool success = ModifiedUtf8.TryGetBytes(string.Empty, out byte[]? bytes);

        Assert.True(success);
        Assert.NotNull(bytes);
        Assert.Empty(bytes);
    }

    [Fact]
    public void BorderChars()
    {
        string borderChars = "\0\u0001\u007F\u0080\u07FF\u0800\uFFFF";
        byte[] expectedResult =
        {
            // Data generated with https://docs.oracle.com/javase/8/docs/api/java/io/DataOutputStream.html
            /*0b00000000, 0b00001110,*/ 0b11000000, 0b10000000, // Ignore length prefix
            0b00000001, 0b01111111, 0b11000010, 0b10000000,
            0b11011111, 0b10111111, 0b11100000, 0b10100000,
            0b10000000, 0b11101111, 0b10111111, 0b10111111
        };

        bool success = ModifiedUtf8.TryGetBytes(borderChars, out byte[]? bytes);

        Assert.True(success);
        Assert.NotNull(bytes);
        Assert.Equal(expectedResult, bytes);
    }

    [Theory]
    [InlineData(8), InlineData(32), InlineData(33)] // Values chosen to test vectorization
    public void AsciiString(int size)
    {
        var vectorSizedAscii = new string('A', size);
        byte[] expectedResult = Enumerable.Repeat((byte)'A', size).ToArray();

        bool success = ModifiedUtf8.TryGetBytes(vectorSizedAscii, out byte[]? bytes);

        Assert.True(success);
        Assert.NotNull(bytes);
        Assert.Equal(expectedResult, bytes);
    }

    [Fact]
    public void StringTooLong()
    {
        var tooLongString = new string('A', ushort.MaxValue + 1);

        bool success = ModifiedUtf8.TryGetBytes(tooLongString, out byte[]? bytes);

        Assert.False(success);
        Assert.Null(bytes);
    }

    [Fact]
    public void TooManyBytes()
    {
        var tooManyBytesString = new string('\0', ushort.MaxValue - 1);

        bool success = ModifiedUtf8.TryGetBytes(tooManyBytesString, out byte[]? bytes);

        Assert.False(success);
        Assert.Null(bytes);
    }

    [Fact]
    public void BufferWriter()
    {
        string testString = "ABCD";
        byte[] expectedResult = { (byte)'A', (byte)'B', (byte)'C', (byte)'D' };

        var bufferWriter = new ArrayBufferWriter<byte>();

        bool success = ModifiedUtf8.TryGetBytes(testString, bufferWriter);

        Assert.True(success);
        Assert.Equal(expectedResult, bufferWriter.WrittenMemory.ToArray());
    }

    [Fact]
    public void InvalidBufferWriterThrowsException()
    {
        string testString = "ABCD";
        var bufferWriter = new InvalidBufferWriter();

        Assert.Throws<Exception>(() => ModifiedUtf8.TryGetBytes(testString, bufferWriter));
    }

    private sealed class InvalidBufferWriter : IBufferWriter<byte>
    {
        public void Advance(int count)
        {
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            return Memory<byte>.Empty;
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            return Span<byte>.Empty;
        }
    }
}
