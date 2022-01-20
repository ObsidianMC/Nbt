namespace Nbt.Tests.Encoding;

public sealed class CountingBytes
{
    [Fact]
    public void EmptyString()
    {
        bool success = ModifiedUtf8.TryGetByteCount(string.Empty, out int byteCount);

        Assert.True(success);
        Assert.Equal(0, byteCount);
    }

    [Theory]
    [InlineData('\u0001'), InlineData('\u007F')]
    public void OneByteCharacter(char character)
    {
        var text = new string(character, count: 1);
        bool success = ModifiedUtf8.TryGetByteCount(text, out int byteCount);

        Assert.True(success);
        Assert.Equal(1, byteCount);
    }

    [Theory]
    [InlineData('\u0000'), InlineData('\u0080'), InlineData('\u07FF')]
    public void TwoByteCharacters(char character)
    {
        var text = new string(character, count: 1);
        bool success = ModifiedUtf8.TryGetByteCount(text, out int byteCount);

        Assert.True(success);
        Assert.Equal(2, byteCount);
    }

    [Theory]
    [InlineData('\u0800'), InlineData('\uFFFF')]
    public void ThreeByteCharacters(char character)
    {
        var text = new string(character, count: 1);
        bool success = ModifiedUtf8.TryGetByteCount(text, out int byteCount);

        Assert.True(success);
        Assert.Equal(3, byteCount);
    }

    [Fact]
    public void VariableCharString()
    {
        string variableString = new string('A', 20) + new string('\u0080', 20) + new string('\u0800', 20);
        bool success = ModifiedUtf8.TryGetByteCount(variableString, out int byteCount);

        Assert.True(success);
        Assert.Equal(20 + 40 + 60, byteCount);
    }

    [Fact]
    public void StringMaxLength()
    {
        var maxLengthString = new string('A', ushort.MaxValue);
        bool success = ModifiedUtf8.TryGetByteCount(maxLengthString, out _);

        Assert.True(success);
    }

    [Fact]
    public void StringTooLong()
    {
        var tooLongString = new string('A', ushort.MaxValue + 1);
        bool success = ModifiedUtf8.TryGetByteCount(tooLongString, out _);

        Assert.False(success);
    }

    [Fact]
    public void TooManyBytes()
    {
        var tooManyBytesString = new string('\u0800', ushort.MaxValue);
        bool success = ModifiedUtf8.TryGetByteCount(tooManyBytesString, out _);

        Assert.False(success);
    }
}
