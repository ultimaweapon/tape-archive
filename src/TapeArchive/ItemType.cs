namespace TapeArchive;

public readonly struct ItemType
{
    /// <summary>
    /// A regular file or directory.
    /// </summary>
    public static readonly ItemType RegularFile = new(0);

    public ItemType(byte value)
    {
        this.Value = value;
    }

    public byte Value { get; }

    public override string ToString()
    {
        return this.Value.ToString();
    }
}
