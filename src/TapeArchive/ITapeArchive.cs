namespace TapeArchive;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

/// <summary>
/// Represents a TAR file.
/// </summary>
/// <remarks>
/// This interface for reading a TAR file only. For writing, use <see cref="IArchiveBuilder"/>.
/// </remarks>
public interface ITapeArchive : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Read all items in this archive.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A list of items in this archive.
    /// </returns>
    /// <exception cref="ArchiveException">
    /// The archive is not a valid TAR.
    /// </exception>
    /// <exception cref="IOException">
    /// Error occurred on the underlying I/O.
    /// </exception>
    IAsyncEnumerable<ArchiveItem> ReadAsync(CancellationToken cancellationToken = default);
}
