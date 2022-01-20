namespace Examples;

internal interface IExample
{
    public string Name => GetType().Name;
    public string? Description => $"Typical usage of {Name}";

    public void Run();
}
