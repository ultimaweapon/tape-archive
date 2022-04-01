namespace TapeArchive;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

internal sealed class ArchiveReader : IAsyncDisposable, IDisposable
{
    private readonly Stream source;
    private readonly bool leaveOpen;
    private readonly IMemoryOwner<byte> buffer;
    private int position;

    public ArchiveReader(Stream source, bool leaveOpen)
    {
        this.source = source;
        this.leaveOpen = leaveOpen;
        this.buffer = MemoryPool<byte>.Shared.Rent(512);
    }

    public ReadOnlyMemory<byte> Buffer => this.buffer.Memory[..512];

    public bool CanTimeout => this.source.CanTimeout;

    public int ReadTimeout => this.source.ReadTimeout;

    public void Dispose()
    {
        if (!this.leaveOpen)
        {
            this.source.Dispose();
        }

        this.buffer.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (!this.leaveOpen)
        {
            await this.source.DisposeAsync();
        }

        this.buffer.Dispose();
    }

    public async ValueTask NextAsync(CancellationToken cancellationToken = default)
    {
        var buffer = this.buffer.Memory;
        var total = 0;

        while (total < 512)
        {
            var read = await this.source.ReadAsync(buffer[total..512], cancellationToken);

            if (read == 0)
            {
                throw new IOException("End of stream has been reached before TAR EOF.");
            }

            total += read;
        }

        this.position = 0;
    }

    public async ValueTask<int> ReadAsync(Memory<byte> output, CancellationToken cancellationToken = default)
    {
        if (this.position == 512)
        {
            await this.NextAsync(cancellationToken);
        }

        var start = this.position;
        var end = Math.Min(start + output.Length, 512);

        this.buffer.Memory[start..end].CopyTo(output);
        this.position = end;

        return end - start;
    }

    public async ValueTask AdvanceAsync(long count, CancellationToken cancellationToken = default)
    {
        var advanced = 0L;

        while (advanced < count)
        {
            if (this.position == 512)
            {
                await this.NextAsync(cancellationToken);
            }

            var remaining = 512 - this.position;

            this.position += remaining;
            advanced += remaining;
        }
    }
}
