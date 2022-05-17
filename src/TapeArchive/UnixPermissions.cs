namespace TapeArchive;

public static class UnixPermissions
{
    /// <summary>
    /// Flag to indicated the owner has a read permission.
    /// </summary>
    /// <remarks>
    /// This represents the value of S_IRUSR, which is 0400 in octal number.
    /// </remarks>
    public const int OwnerRead = 0x100;

    /// <summary>
    /// Flag to indicated the owner has a write permission.
    /// </summary>
    /// <remarks>
    /// This represents the value of S_IWUSR, which is 0200 in octal number.
    /// </remarks>
    public const int OwnerWrite = 0x80;

    /// <summary>
    /// Flag to indicated the owner has an execute permission.
    /// </summary>
    /// <remarks>
    /// This represents the value of S_IXUSR, which is 0100 in octal number.
    /// </remarks>
    public const int OwnerExecute = 0x40;

    /// <summary>
    /// Flag to indicated the group has a read permission.
    /// </summary>
    /// <remarks>
    /// This represents the value of S_IRGRP, which is 0040 in octal number.
    /// </remarks>
    public const int GroupRead = 0x20;

    /// <summary>
    /// Flag to indicated the group has a write permission.
    /// </summary>
    /// <remarks>
    /// This represents the value of S_IWGRP, which is 0020 in octal number.
    /// </remarks>
    public const int GroupWrite = 0x10;

    /// <summary>
    /// Flag to indicated the group has an execute permission.
    /// </summary>
    /// <remarks>
    /// This represents the value of S_IXGRP, which is 0010 in octal number.
    /// </remarks>
    public const int GroupExecute = 0x8;

    /// <summary>
    /// Flag to indicated the other has a read permission.
    /// </summary>
    /// <remarks>
    /// This represents the value of S_IROTH, which is 0004 in octal number.
    /// </remarks>
    public const int OtherRead = 0x4;

    /// <summary>
    /// Flag to indicated the other has a write permission.
    /// </summary>
    /// <remarks>
    /// This represents the value of S_IWOTH, which is 0002 in octal number.
    /// </remarks>
    public const int OtherWrite = 0x2;

    /// <summary>
    /// Flag to indicated the other has an execute permission.
    /// </summary>
    /// <remarks>
    /// This represents the value of S_IXOTH, which is 0001 in octal number.
    /// </remarks>
    public const int OtherExecute = 0x1;
}
