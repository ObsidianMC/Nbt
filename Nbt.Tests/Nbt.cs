using Obsidian.Nbt;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Nbt.Tests;
public class Nbt(ITestOutputHelper output)
{
    private readonly ITestOutputHelper output = output;

    [Fact]
    public void HelloWorldTest()
    {
        using var fs = Assembly.GetExecutingAssembly().GetManifestResourceStream("Nbt.Tests.Assets.hello_world.nbt")!;

        var reader = new NbtReader(fs, NbtCompression.None);

        var main = (NbtCompound)reader.ReadNextTag()!;

        output.WriteLine(main.ToString());

        Assert.Equal("hello world", main.Name);

        if (!main.TryGetTag("name", out var tag))
            Assert.Fail("Failed to find name tag.");

        var nameTag = (NbtTag<string>)tag;

        Assert.Equal("Bananrama", nameTag.Value);
    }

    [Fact]
    public void BigTest()
    {
        using var fs = Assembly.GetExecutingAssembly().GetManifestResourceStream("Nbt.Tests.Assets.bigtest.nbt")!;

        var reader = new NbtReader(fs, NbtCompression.GZip);

        var main = (NbtCompound)reader.ReadNextTag()!;

        //Writing out the string to read ourselves
        output.WriteLine(main.ToString());

        //Begin reading

        Assert.Equal("Level", main.Name);

        Assert.Equal(NbtTagType.Compound, main.Type);

        Assert.Equal(11, main.Count);

        var longTest = main.GetLong("longTest");
        Assert.Equal(long.MaxValue, longTest);

        var shortTest = main.GetShort("shortTest");
        Assert.Equal(short.MaxValue, shortTest);

        var stringTest = main.GetString("stringTest");
        Assert.Equal("HELLO WORLD THIS IS A TEST STRING ÅÄÖ!", stringTest);

        var floatTest = main.GetFloat("floatTest");
        Assert.Equal(0.49823147058486938, floatTest);

        var intTest = main.GetInt("intTest");
        Assert.Equal(int.MaxValue, intTest);

        var byteTest = main.GetByte("byteTest");
        Assert.Equal(127, byteTest);

        var doubleTest = main.GetDouble("doubleTest");
        Assert.Equal(0.49312871321823148, doubleTest);

        if (!main.TryGetTag("byteArrayTest (the first 1000 values of (n*n*255+n*7)%100, starting with n=0 (0, 62, 34, 16, 8, ...))",
            out var array))
            Assert.Fail("Failed to find byte array tag");

        var byteArrayTest = (NbtArray<byte>)array;
        Assert.Equal(1000, byteArrayTest.Count);

        for (int n = 0; n < byteArrayTest.Count; n++)
            Assert.Equal((n * n * 255 + n * 7) % 100, byteArrayTest[n]);

        #region nested compounds
        if (!main.TryGetTag("nested compound test", out INbtTag? compound))
            Assert.Fail("Failed to find nested compound test tag");

        var nestedCompound = (NbtCompound)compound;

        Assert.Equal(2, nestedCompound.Count);

        if(!nestedCompound.TryGetTag("ham", out INbtTag? hamCompound))
            Assert.Fail("Failed to find ham tag");

        var ham = (NbtCompound)hamCompound;

        Assert.Equal(2, ham.Count);

        Assert.Equal("Hampus", ham.GetString("name"));
        Assert.Equal(0.75, ham.GetFloat("value"));

        if(!nestedCompound.TryGetTag("egg", out INbtTag? eggCompound))
            Assert.Fail("Failed to find egg tag");

        var egg = (NbtCompound)eggCompound;

        Assert.Equal(2, egg.Count);
        Assert.Equal("Eggbert", egg.GetString("name"));
        Assert.Equal(0.5, egg.GetFloat("value"));
        #endregion nested compounds

        #region lists
        if(!main.TryGetTag("listTest (long)", out var longList))
            Assert.Fail("Failed to find listtest (long) tag");

        var listLongTest = (NbtList)longList;

        Assert.Equal(5, listLongTest.Count);

        var count = 11;

        foreach (var tag in listLongTest)
        {
            if (tag is NbtTag<long> item)
                Assert.Equal(count++, item.Value);
        }

        if (!main.TryGetTag("listTest (compound)", out var compoundList))
            Assert.Fail("Failed to find listtest (compound) tag.");

        var listCompoundTest = (NbtList)compoundList;

        Assert.Equal(2, listCompoundTest.Count);

        var compound1 = (NbtCompound)listCompoundTest[0];
        Assert.Equal("Compound tag #0", compound1.GetString("name"));
        Assert.Equal(1264099775885, compound1.GetLong("created-on"));


        var compound2 = (NbtCompound)listCompoundTest[1];
        Assert.Equal("Compound tag #1", compound2.GetString("name"));
        Assert.Equal(1264099775885, compound2.GetLong("created-on"));
        #endregion lists
    }
}
