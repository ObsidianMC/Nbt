namespace Nbt.Tests.Encoding;

public sealed class GettingString
{
    [Fact]
    public void EmptyBytes()
    {
        bool success = ModifiedUtf8.TryGetString(Array.Empty<byte>(), out string? result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void BorderChars()
    {
        byte[] bytes =
        {
            // Data generated with https://docs.oracle.com/javase/8/docs/api/java/io/DataOutputStream.html
            /*0b00000000, 0b00001110,*/ 0b11000000, 0b10000000, // Ignore length prefix
            0b00000001, 0b01111111, 0b11000010, 0b10000000,
            0b11011111, 0b10111111, 0b11100000, 0b10100000,
            0b10000000, 0b11101111, 0b10111111, 0b10111111
        };
        string expectedResult = "\0\u0001\u007F\u0080\u07FF\u0800\uFFFF";

        bool success = ModifiedUtf8.TryGetString(bytes, out string? result);

        Assert.True(success);
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(8), InlineData(32), InlineData(33)] // Values chosen to test vectorization
    public void AsciiString(int size)
    {
        byte[] bytes = Enumerable.Repeat((byte)'A', size).ToArray();
        var expectedResult = new string('A', size);

        bool success = ModifiedUtf8.TryGetString(bytes, out string? result);

        Assert.True(success);
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void TooManyBytes()
    {
        var bytes = new byte[ushort.MaxValue + 1];
        Array.Fill(bytes, (byte)'A');

        bool success = ModifiedUtf8.TryGetString(bytes, out string? result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(new byte[] { 0b1000_0000 })]
    [InlineData(new byte[] { 0b1100_0000 })]
    [InlineData(new byte[] { 0b1110_0000, 0b1000_0000 })]
    [InlineData(new byte[] { 0b1110_0000, 0b1100_0000 })]
    [InlineData(new byte[] { 0b1110_0000, 0b100_0000, 0b1100_0000 })]
    public void CorruptBytes(byte[] bytes)
    {
        bool success = ModifiedUtf8.TryGetString(bytes, out string? result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void DirectVariantThrowsFormatExceptionOnFailure()
    {
        byte[] invalidBytes = { 0b1100_0000, /* Expects another byte starting with bits 10 */ };

        Assert.Throws<FormatException>(() => ModifiedUtf8.GetString(invalidBytes));
    }
}
