namespace TapeArchive;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public sealed class ArchiveReader : IAsyncDisposable, IDisposable
{
    public const int RecordSize = 512;

    private readonly Stream source;
    private readonly bool leaveOpen;
    private readonly IMemoryOwner<byte> buffer;
    private int position;

    internal ArchiveReader(Stream source, bool leaveOpen)
    {
        this.source = source;
        this.leaveOpen = leaveOpen;
        this.buffer = MemoryPool<byte>.Shared.Rent(RecordSize);
    }

    public ReadOnlyMemory<byte> Buffer => this.buffer.Memory[..RecordSize];

    internal bool CanTimeout => this.source.CanTimeout;

    internal int ReadTimeout => this.source.ReadTimeout;

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

        while (total < RecordSize)
        {
            var read = await this.source.ReadAsync(buffer[total..RecordSize], cancellationToken);

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
        if (this.position == RecordSize)
        {
            await this.NextAsync(cancellationToken);
        }

        var start = this.position;
        var end = Math.Min(start + output.Length, RecordSize);

        this.buffer.Memory[start..end].CopyTo(output);
        this.position = end;

        return end - start;
    }

    public async ValueTask AdvanceAsync(long count, CancellationToken cancellationToken = default)
    {
        var advanced = 0L;

        while (advanced < count)
        {
            if (this.position == RecordSize)
            {
                await this.NextAsync(cancellationToken);
            }

            var remaining = RecordSize - this.position;

            this.position += remaining;
            advanced += remaining;
        }
    }
}
