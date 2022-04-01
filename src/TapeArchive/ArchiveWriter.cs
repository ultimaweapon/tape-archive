namespace TapeArchive;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

internal sealed class ArchiveWriter : Stream
{
    public const int RecordSize = 512;

    private readonly Stream output;
    private readonly bool leaveOpen;
    private readonly IMemoryOwner<byte> buffer;
    private int buffered;
    private bool disposed;

    public ArchiveWriter(Stream output, bool leaveOpen)
    {
        this.output = output;
        this.leaveOpen = leaveOpen;
        this.buffer = MemoryPool<byte>.Shared.Rent(RecordSize);

        try
        {
            this.buffer.Memory.Span.Fill(0);
        }
        catch
        {
            this.buffer.Dispose();
            throw;
        }
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanTimeout => this.output.CanTimeout;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int WriteTimeout
    {
        get => this.output.WriteTimeout;
        set => this.output.WriteTimeout = value;
    }

    private int Available => RecordSize - this.buffered;

    public override async ValueTask DisposeAsync()
    {
        if (!this.disposed)
        {
            if (this.buffered == 0)
            {
                // Write terminations only when we exit cleanly.
                this.buffer.Memory.Span.Fill(0);

                await this.output.WriteAsync(this.buffer.Memory[..RecordSize]);
                await this.output.WriteAsync(this.buffer.Memory[..RecordSize]);
            }

            // Dispose members.
            if (!this.leaveOpen)
            {
                await this.output.DisposeAsync();
            }

            this.buffer.Dispose();
            this.disposed = true;
        }

        await base.DisposeAsync();
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        throw new NotSupportedException();
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        throw new NotSupportedException();
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
        // Do nothing.
    }

    public new async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        if (this.buffered > 0)
        {
            await this.output.WriteAsync(this.buffer.Memory[..RecordSize], cancellationToken);

            this.buffer.Memory.Span.Fill(0);
            this.buffered = 0;
        }
    }

    public override int Read(Span<byte> buffer)
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public override int ReadByte()
    {
        throw new NotSupportedException();
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
        var written = 0;

        while (written < buffer.Length)
        {
            // Copy remaining.
            var destination = this.buffer.Memory.Span[..RecordSize];
            var amount = Math.Min(buffer.Length - written, this.Available);

            buffer.Slice(written, amount).CopyTo(destination[this.buffered..]);

            this.buffered += amount;
            written += amount;

            // Flush.
            if (this.buffered == RecordSize)
            {
                this.output.Write(destination);
                this.buffered = 0;

                destination.Fill(0);
            }
        }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);

        this.Write(new ReadOnlySpan<byte>(buffer, offset, count));
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var written = 0;

        while (written < buffer.Length)
        {
            // Copy remaining.
            var destination = this.buffer.Memory[..RecordSize];
            var amount = Math.Min(buffer.Length - written, this.Available);

            buffer.Slice(written, amount).CopyTo(destination[this.buffered..]);

            this.buffered += amount;
            written += amount;

            // Flush.
            if (this.buffered == RecordSize)
            {
                await this.output.WriteAsync(destination);
                this.buffered = 0;

                destination.Span.Fill(0);
            }
        }
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);

        await this.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
    }

    public override void WriteByte(byte value)
    {
        var buffer = this.buffer.Memory.Span[..RecordSize];

        buffer[this.buffered++] = value;

        if (this.buffered == RecordSize)
        {
            this.output.Write(buffer);
            this.buffered = 0;

            buffer.Fill(0);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                if (this.buffered == 0)
                {
                    // Write terminations only when we exit cleanly.
                    this.buffer.Memory.Span.Fill(0);

                    this.output.Write(this.buffer.Memory.Span[..RecordSize]);
                    this.output.Write(this.buffer.Memory.Span[..RecordSize]);
                }

                // Dispose members.
                if (!this.leaveOpen)
                {
                    this.output.Dispose();
                }

                this.buffer.Dispose();
            }

            this.disposed = true;
        }

        base.Dispose(disposing);
    }
}
