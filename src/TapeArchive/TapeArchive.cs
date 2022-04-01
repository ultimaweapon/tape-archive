namespace TapeArchive;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public class TapeArchive : ITapeArchive
{
    private readonly ArchiveReader reader;
    private bool disposed;

    public TapeArchive(Stream data, bool leaveOpen)
    {
        this.reader = new(data, leaveOpen);
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

    public async IAsyncEnumerable<ArchiveItem> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (; ;)
        {
            // Load item.
            var item = await this.NextAsync(cancellationToken);

            if (item == null)
            {
                item = await this.NextAsync(cancellationToken);

                if (item == null)
                {
                    // Two consecutive blank records.
                    yield break;
                }
            }

            // Get item content.
            ItemContent? content;

            if (item.Size == 0)
            {
                content = null;
            }
            else
            {
                await this.reader.NextAsync(cancellationToken);

                content = new ItemContent(this.reader, item.Size);
            }

            // Yield item.
            try
            {
                item.Content = content ?? Stream.Null;

                yield return item;
            }
            finally
            {
                if (content != null)
                {
                    await content.DisposeAsync();
                }
            }
        }
    }

    protected virtual ArchiveItem? InspectHeader(ReadOnlySpan<byte> header)
    {
        // Old-Style Format.
        var name = header[..100];

        // "ustar" format.
        var magic = header[257..263];
        var version = header[263..265];

        if (name[0] == 0)
        {
            // Cheap way to check if record is empty.
            return null;
        }
        else if (magic[0] == 'u' && magic[1] == 's' && magic[2] == 't' && magic[3] == 'a' && magic[4] == 'r')
        {
            if (magic[5] == 0)
            {
                // POSIX ustar Archives.
                if (version[0] != '0' || version[1] != '0')
                {
                    throw new ArchiveException("Unknown ustar version.");
                }

                return new UstarItem();
            }
            else if (magic[5] == ' ')
            {
                // Pre-POSIX Archives.
                if (version[0] != ' ' || version[1] != 0)
                {
                    throw new ArchiveException("Unknown Pre-POSIX version.");
                }

                return new PrePosixItem();
            }
            else
            {
                throw new ArchiveException("Unknown ustar variant.");
            }
        }
        else
        {
            return new ArchiveItem();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        if (disposing)
        {
            this.reader.Dispose();
        }

        this.disposed = true;
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!this.disposed)
        {
            await this.reader.DisposeAsync();
        }
    }

    private async ValueTask<ArchiveItem?> NextAsync(CancellationToken cancellationToken)
    {
        await this.reader.NextAsync(cancellationToken);

        var item = this.InspectHeader(this.reader.Buffer.Span);

        if (item != null)
        {
            using var headers = new HeaderCollection(this.reader);

            item.ParseChecksum(headers);

            await item.ParseHeadersAsync(headers, cancellationToken);
        }

        return item;
    }
}
