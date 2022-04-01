namespace TapeArchive;

public static class PrePosixType
{
    public static readonly ItemType RegularFile = new((byte)'0');

    public static readonly ItemType Directory = new((byte)'5');
}
