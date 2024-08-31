using System.Reflection;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Obsidian.Tests;

public class Nbt
{
    private bool isSetup;
    private readonly ITestOutputHelper output;

    public Nbt(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void BigTest()
    {
        var fs = Assembly.GetExecutingAssembly().GetManifestResourceStream("Nbt.Tests.Assets.bigtest.nbt");

        var document = NbtDocument.Parse(fs!, NbtCompression.GZip);

        var root = document.Root;

        //Writing out the string to read ourselves
        output.WriteLine(root.ToString());

        //Begin reading

        Assert.Equal("Level", root.Name);

        Assert.Equal(11, root.Count());

        var longTest = root.GetProperty("longTest").GetLong();
        Assert.Equal(long.MaxValue, longTest);

        var shortTest = root.GetProperty("shortTest").GetShort();
        Assert.Equal(short.MaxValue, shortTest);

        var stringTest = root.GetProperty("stringTest").GetString();
        Assert.Equal("HELLO WORLD THIS IS A TEST STRING ÅÄÖ!", stringTest);

        var floatTest = root.GetProperty("floatTest").GetFloat();
        Assert.Equal(0.49823147058486938, floatTest);

        var intTest = root.GetProperty("intTest").GetInt();
        Assert.Equal(int.MaxValue, intTest);

        var byteTest = root.GetProperty("byteTest").GetByte();
        Assert.Equal(127, byteTest);

        var doubleTest = root.GetProperty("doubleTest").GetDouble();
        Assert.Equal(0.49312871321823148, doubleTest);

        //var byteArrayTest = main.GetArr("byteArrayTest (the first 1000 values of (n*n*255+n*7)%100, starting with n=0 (0, 62, 34, 16, 8, ...))");//TODO add getting an array from a compound
        /*Assert.Equal(1000, byteArrayTest.Value.Length);

        for (int n = 0; n < 1000; n++)
            Assert.Equal((n * n * 255 + n * 7) % 100, byteArrayTest.Value[n]);*/

        #region nested compounds

        root.TryGetProperty("nested compound test", out var compound);

        var nestedCompound = compound.GetCompound();
        Assert.Equal(2, nestedCompound.Count());

        nestedCompound.TryGetProperty("ham", out var hamCompound);
        var ham = hamCompound.GetCompound();

        Assert.Equal(2, ham.Count());

        Assert.Equal("Hampus", ham.GetProperty("name").GetString());
        Assert.Equal(0.75, ham.GetProperty("value").GetFloat());

        nestedCompound.TryGetProperty("egg", out var eggCompound);
        var egg = eggCompound.GetCompound();

        Assert.Equal(2, egg.Count());
        Assert.Equal("Eggbert", egg.GetProperty("name").GetString());
        Assert.Equal(0.5, egg.GetProperty("value").GetFloat());
        #endregion nested compounds

        #region lists
        root.TryGetProperty("listTest (long)", out var longList);
        var listLongTest = longList.GetLongArray();

        Assert.Equal(5, listLongTest.Length);

        var count = 11;

        foreach (var tag in listLongTest)
        {
            Assert.Equal(count++, tag);
        }

        root.TryGetProperty("listTest (compound)", out var compoundList);
        var listCompoundTest = compoundList.GetList();

        Assert.Equal(2, listCompoundTest.Count);

        var enu = listCompoundTest.EnumerateArray();

        enu.MoveNext();//This isn't ideal

        var compound1 = enu.Current.GetCompound();
        Assert.Equal("Compound tag #0", compound1.GetProperty("name").GetString());
        Assert.Equal(1264099775885, compound1.GetProperty("created-on").GetLong());

        enu.MoveNext();

        var compound2 = enu.Current.GetCompound();
        Assert.Equal("Compound tag #1", compound2.GetProperty("name").GetString());
        Assert.Equal(1264099775885, compound2.GetProperty("created-on").GetLong());
        #endregion lists
    }

    //[Fact]
    //public async Task ReadSlot()
    //{
    //    await SetupAsync();

    //    await using var stream = new MinecraftStream();

    //    var itemMeta = new ItemMetaBuilder()
    //        .WithName("test")
    //        .WithDurability(1)
    //        .Build();

    //    var material = Material.Bedrock;

    //    var dataSlot = new ItemStack(material, 0, itemMeta)
    //    {
    //        Present = true
    //    };

    //    await stream.WriteSlotAsync(dataSlot);

    //    stream.Position = 0;

    //    var slot = await stream.ReadSlotAsync();

    //    Assert.True(slot.Present);
    //    Assert.Equal(0, slot.Count);
    //    Assert.Equal(material, slot.Type);

    //    Assert.Equal("test", slot.ItemMeta.Name.Text);
    //    Assert.Equal(1, slot.ItemMeta.Durability);
    //}

    private async Task SetupAsync()
    {
        if (isSetup)
            return;
        isSetup = true;
    }
}
