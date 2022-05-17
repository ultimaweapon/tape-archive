namespace TapeArchive;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IArchiveBuilder : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Complete the archive.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous complete operation.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The archive already completed or the last call to <see cref="WriteItemAsync(ArchiveItem, ParentProperties?, CancellationToken)"/> is completed with
    /// error.
    /// </exception>
    ValueTask CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Write a specified item to the archive.
    /// </summary>
    /// <param name="item">
    /// Item to write.
    /// </param>
    /// <param name="parentProps">
    /// Properties of the parent. Specify <c>null</c> to use default values.
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
    /// <exception cref="InvalidOperationException">
    /// The archive already completed.
    /// </exception>
    /// <remarks>
    /// This method automatically create a sub-directory for <paramref name="item"/> with properties supplied in <paramref name="parentProps"/>. That mean is
    /// most cases you don't need to create an <see cref="ArchiveItem"/> to represent a parent.
    ///
    /// When <paramref name="parentProps"/> is <c>null</c>, all properties will be inherited from <paramref name="item"/> with <see cref="ArchiveItem.Mode"/> as
    /// a special case. The execution flag will be added to <see cref="ArchiveItem.Mode"/> if the entity has a read flag (e.g. if <see cref="ArchiveItem.Mode"/>
    /// for <paramref name="item"/> is 0600 the value for parent will be 0700).
    /// </remarks>
    ValueTask WriteItemAsync(ArchiveItem item, ParentProperties? parentProps, CancellationToken cancellationToken = default);
}
