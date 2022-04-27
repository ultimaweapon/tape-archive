namespace TapeArchive;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public sealed class ArchiveBuilder : IArchiveBuilder
{
    private readonly HashSet<ItemName> directories;
    private ArchiveWriter? writer;
    private bool disposed;

    public ArchiveBuilder(Stream output, bool leaveOpen)
    {
        this.directories = new();
        this.writer = new(output, leaveOpen);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await this.DisposeAsyncCore();
        this.Dispose(false);
        GC.SuppressFinalize(this);
    }

    public async ValueTask CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (this.writer == null)
        {
            throw new InvalidOperationException("The archive already completed.");
        }

        if (!await this.writer.CompleteAsync(cancellationToken))
        {
            throw new InvalidOperationException("The archive cannot be completed due to it is already malformed.");
        }

        await this.writer.DisposeAsync();
        this.writer = null;
    }

    public async ValueTask WriteItemAsync(ArchiveItem item, CancellationToken cancellationToken = default)
    {
        if (this.writer == null)
        {
            throw new InvalidOperationException("The archive already completed.");
        }

        // Create a parent if not exists.
        var name = item.Name;
        var parent = name.Parent;

        if (parent != null && !this.directories.Contains(parent))
        {
            await this.WriteItemAsync(item.CreateParent(parent), cancellationToken);
        }

        // Create header.
        var headerSize = 512 * item.GetHeaderBlocksForWriting();
        using var header = MemoryPool<byte>.Shared.Rent(headerSize);

        header.Memory.Span.Fill(0);

        try
        {
            item.WriteHeaders(header.Memory.Span);
            item.WriteChecksum(header.Memory.Span);
        }
        catch (ArchiveException ex)
        {
            throw new ArgumentException("Header data is not valid.", nameof(item), ex);
        }

        // Write item.
        await this.writer.WriteAsync(header.Memory[..headerSize], cancellationToken);
        await item.Content.CopyToAsync(this.writer);
        await this.writer.FlushAsync();

        if (name.IsDirectory)
        {
            this.directories.Add(name);
        }
    }

    private void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        if (disposing)
        {
            this.writer?.Dispose();
        }

        this.disposed = true;
    }

    private async ValueTask DisposeAsyncCore()
    {
        if (this.disposed)
        {
            return;
        }

        if (this.writer != null)
        {
            await this.writer.DisposeAsync();
        }
    }
}
