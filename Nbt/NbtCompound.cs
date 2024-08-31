using Obsidian.Nbt.Utilities;
using System.Collections.Generic;

namespace Obsidian.Nbt;

public readonly struct NbtCompound
{
    private readonly NbtDocument parent;

    private readonly int index;

    public string Name => this.parent.TryGetProperty(this.index, out var property, out _) ? property.Name : string.Empty;

    public int Count()
    {
        var result = 0;
        using var enumerator = this.EnumerateProperties();

        while (enumerator.MoveNext())
            result++;

        return result;
    }

    // Ctor for compounds
    internal NbtCompound(NbtDocument document, int index)
    {
        parent = document;
        this.index = index;
    }

    public NbtProperty this[string? propertyName]
    {
        get => GetProperty(propertyName);
    }

    public NbtProperty this[ReadOnlySpan<char> propertyName]
    {
        get => GetProperty(propertyName);
    }

    public NbtProperty this[ReadOnlySpan<byte> propertyName]
    {
        get => GetProperty(propertyName);
    }

    public NbtProperty GetProperty(string? propertyName)
    {
        return GetProperty(propertyName.AsSpan());
    }

    public bool TryGetProperty(string? propertyName, out NbtProperty property)
    {
        ValidateInstance();
        return TryGetProperty(propertyName.AsSpan(), out property);
    }

    public NbtProperty GetProperty(ReadOnlySpan<char> propertyName)
    {
        ValidateInstance();
        if (parent.TryGetProperty(propertyName, index, out NbtProperty property))
        {
            return property;
        }

        throw new KeyNotFoundException();
    }

    public bool TryGetProperty(ReadOnlySpan<char> propertyName, out NbtProperty property)
    {
        ValidateInstance();
        return parent.TryGetProperty(propertyName, index, out property);
    }

    public NbtProperty GetProperty(ReadOnlySpan<byte> propertyName)
    {
        ValidateInstance();
        if (parent.TryGetProperty(propertyName, index, out NbtProperty property))
        {
            return property;
        }

        throw new KeyNotFoundException();
    }

    public bool TryGetProperty(ReadOnlySpan<byte> propertyName, out NbtProperty property)
    {
        ValidateInstance();
        return parent.TryGetProperty(propertyName, index, out property);
    }

    public NbtDocument.PropertiesEnumerator EnumerateProperties()
    {
        return new NbtDocument.PropertiesEnumerator(parent, index);
    }

    private void ValidateInstance()
    {
        if (parent is null)
        {
            ThrowHelper.ThrowInvalidOperationException_InvalidInstace();
        }
    }
}
