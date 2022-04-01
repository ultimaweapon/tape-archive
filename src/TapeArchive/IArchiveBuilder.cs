namespace TapeArchive;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IArchiveBuilder : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Write a specified item to the archive.
    /// </summary>
    /// <param name="item">
    /// Item to write.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous write operation.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="item"/> is not valid.
    /// </exception>
    /// <remarks>
    /// This method automatically create a sub-directory for <paramref name="item"/>. That mean is most cases you don't need to create an
    /// <see cref="ArchiveItem"/> with <see cref="ItemType.Directory"/> type.
    /// </remarks>
    ValueTask WriteItemAsync(ArchiveItem item, CancellationToken cancellationToken = default);
}
