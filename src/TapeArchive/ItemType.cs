namespace TapeArchive;

public enum ItemType : byte
{
    /// <summary>
    /// A regular file.
    /// </summary>
    /// <remarks>
    /// The content of item with this type is the actual file data.
    /// </remarks>
    RegularFile = 0,

    /// <summary>
    /// A directory.
    /// </summary>
    /// <remarks>
    /// The item of this type has no content.
    /// </remarks>
    Directory = 1,
}
