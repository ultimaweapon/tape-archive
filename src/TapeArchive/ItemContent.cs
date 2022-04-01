namespace TapeArchive;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

internal sealed class ItemContent : Stream
{
    private readonly ArchiveReader reader;
    private readonly long length;
    private long consumed;
    private bool disposed;

    public ItemContent(ArchiveReader reader, long length)
    {
        this.reader = reader;
        this.length = length;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanTimeout => this.reader.CanTimeout;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int ReadTimeout
    {
        get => this.reader.ReadTimeout;
        set => throw new InvalidOperationException();
    }

    private long Remaining => this.length - this.consumed;

    public override async ValueTask DisposeAsync()
    {
        if (!this.disposed)
        {
            await this.reader.AdvanceAsync(this.Remaining);
            this.disposed = true;
        }

        await base.DisposeAsync();
    }

    public override void Flush()
    {
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = this.reader.ReadAsync(new(buffer, offset, (int)Math.Min(count, this.Remaining))).AsTask().Result;

        this.consumed += read;

        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var count = buffer.Length;
        var read = await this.reader.ReadAsync(buffer[..(int)Math.Min(count, this.Remaining)], cancellationToken);

        this.consumed += read;

        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var read = await this.reader.ReadAsync(new(buffer, offset, (int)Math.Min(count, this.Remaining)), cancellationToken);

        this.consumed += read;

        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public override void WriteByte(byte value)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                this.reader.AdvanceAsync(this.Remaining).AsTask().Wait();
            }

            this.disposed = true;
        }

        base.Dispose(disposing);
    }
}
