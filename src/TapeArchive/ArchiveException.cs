namespace TapeArchive;

using System;

/// <summary>
/// The exception that is thrown when archive is malformed.
/// </summary>
public class ArchiveException : Exception
{
    public ArchiveException()
    {
    }

    public ArchiveException(string? message)
        : base(message)
    {
    }

    public ArchiveException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
