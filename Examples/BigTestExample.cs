using Obsidian.Nbt;
using System.Diagnostics;
using System.Reflection;

namespace Examples;
public sealed class BigTestExample : IExample
{
    public void Run()
    {
        var fs = Assembly.GetExecutingAssembly().GetManifestResourceStream("Examples.Assets.bigtest.nbt");

        var document = NbtDocument.Parse(fs!, NbtCompression.GZip);

        var root = document.Root;

        //Writing out the string to read ourselves
        Console.WriteLine(root.Name);

        //Begin reading

        Debug.Assert("Level" == root.Name);
        var elementCount = root.Count();

        Debug.Assert(11 == elementCount);

        var longTest = root.GetProperty("longTest").GetLong();
        Debug.Assert(long.MaxValue == longTest);

        var shortTest = root.GetProperty("shortTest").GetShort();
        Debug.Assert(short.MaxValue == shortTest);

        var stringTest = root.GetProperty("stringTest").GetString();
        Debug.Assert("HELLO WORLD THIS IS A TEST STRING ÅÄÖ!" == stringTest);

        var floatTest = root.GetProperty("floatTest").GetFloat();
        Debug.Assert(0.49823147058486938 == floatTest);

        var intTest = root.GetProperty("intTest").GetInt();
        Debug.Assert(int.MaxValue == intTest);

        var doubleTest = root.GetProperty("doubleTest").GetDouble();
        Debug.Assert(0.49312871321823148 == doubleTest);

        var byteTest = root.GetProperty("byteTest").GetByte();
        Debug.Assert(127 == byteTest);

        //var byteArrayTest = main.GetArr("byteArrayTest (the first 1000 values of (n*n*255+n*7)%100, starting with n=0 (0, 62, 34, 16, 8, ...))");//TODO add getting an array from a compound
        /*Assert.Equal(1000, byteArrayTest.Value.Length);

        for (int n = 0; n < 1000; n++)
            Assert.Equal((n * n * 255 + n * 7) % 100, byteArrayTest.Value[n]);*/

        #region nested compounds

        root.TryGetProperty("nested compound test", out var compound);

        var nestedCompound = compound.GetCompound();
        Debug.Assert(2 == nestedCompound.Count());

        nestedCompound.TryGetProperty("ham", out var hamCompound);
        var ham = hamCompound.GetCompound();

        Debug.Assert(2 == ham.Count());

        Debug.Assert("Hampus" == ham.GetProperty("name").GetString());
        Debug.Assert(0.75 == ham.GetProperty("value").GetFloat());

        nestedCompound.TryGetProperty("egg", out var eggCompound);
        var egg = eggCompound.GetCompound();

        Debug.Assert(2 == egg.Count());
        Debug.Assert("Eggbert" == egg.GetProperty("name").GetString());
        Debug.Assert(0.5 == egg.GetProperty("value").GetFloat());
        #endregion nested compounds

        #region lists
        root.TryGetProperty("listTest (long)", out var longList);
        var listLongTest = longList.GetLongArray();

        Debug.Assert(5 == listLongTest.Length);

        var count = 11;

        foreach (var tag in listLongTest)
        {
            Debug.Assert(count++ == tag);
        }

        root.TryGetProperty("listTest (compound)", out var compoundList);
        var listCompoundTest = compoundList.GetList();

        Debug.Assert(2 == listCompoundTest.Count);

        var enu = listCompoundTest.EnumerateArray();

        enu.MoveNext();//This isn't ideal

        var compound1 = enu.Current.GetCompound();
        Debug.Assert("Compound tag #0" == compound1.GetProperty("name").GetString());
        Debug.Assert(1264099775885 == compound1.GetProperty("created-on").GetLong());

        enu.MoveNext();

        var compound2 = enu.Current.GetCompound();
        Debug.Assert("Compound tag #1" == compound2.GetProperty("name").GetString());
        Debug.Assert(1264099775885 == compound2.GetProperty("created-on").GetLong());
        #endregion lists
    }
}
