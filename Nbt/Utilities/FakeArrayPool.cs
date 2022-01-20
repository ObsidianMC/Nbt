namespace Obsidian.Nbt.Utilities;

internal sealed class FakeArrayPool : ArrayPool<byte>
{
    public static FakeArrayPool Instance => instance ??= new();
    private static FakeArrayPool? instance;

    public override byte[] Rent(int minimumLength)
    {
        return GC.AllocateUninitializedArray<byte>(minimumLength);
    }

    public override void Return(byte[] array, bool clearArray = false)
    {
    }
}
