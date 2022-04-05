namespace TapeArchive;

using System;

public readonly struct ItemType : IEquatable<ItemType>
{
    /// <summary>
    /// A regular file or directory.
    /// </summary>
    public static readonly ItemType RegularFile = default;

    public ItemType(byte value)
    {
        this.Value = value;
    }

    public byte Value { get; }

    public static bool operator ==(ItemType left, ItemType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemType left, ItemType right)
    {
        return !(left == right);
    }

    public bool Equals(ItemType other)
    {
        return other.Value == this.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemType other && this.Equals(other);
    }

    public override int GetHashCode()
    {
        return this.Value.GetHashCode();
    }

    public override string ToString()
    {
        return this.Value.ToString();
    }
}
