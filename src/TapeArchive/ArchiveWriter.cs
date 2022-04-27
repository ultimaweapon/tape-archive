namespace TapeArchive;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

internal sealed class ArchiveWriter : Stream
{
    private readonly Stream output;
    private readonly bool leaveOpen;
    private readonly IMemoryOwner<byte> buffer;
    private int buffered;

    public ArchiveWriter(Stream output, bool leaveOpen)
    {
        this.output = output;
        this.leaveOpen = leaveOpen;
        this.buffer = MemoryPool<byte>.Shared.Rent(512);

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

    private int Available => 512 - this.buffered;

    public override async ValueTask DisposeAsync()
    {
        if (!this.leaveOpen)
        {
            await this.output.DisposeAsync();
        }

        this.buffer.Dispose();
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        throw new NotSupportedException();
    }

    public async ValueTask<bool> CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (this.buffered != 0)
        {
            return false;
        }

        this.buffer.Memory.Span.Fill(0);

        await this.output.WriteAsync(this.buffer.Memory[..512], cancellationToken);
        await this.output.WriteAsync(this.buffer.Memory[..512], cancellationToken);

        return true;
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
            await this.output.WriteAsync(this.buffer.Memory[..512], cancellationToken);

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
            var destination = this.buffer.Memory.Span[..512];
            var amount = Math.Min(buffer.Length - written, this.Available);

            buffer.Slice(written, amount).CopyTo(destination[this.buffered..]);

            this.buffered += amount;
            written += amount;

            // Flush.
            if (this.buffered == 512)
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
            var destination = this.buffer.Memory[..512];
            var amount = Math.Min(buffer.Length - written, this.Available);

            buffer.Slice(written, amount).CopyTo(destination[this.buffered..]);

            this.buffered += amount;
            written += amount;

            // Flush.
            if (this.buffered == 512)
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
        var buffer = this.buffer.Memory.Span[..512];

        buffer[this.buffered++] = value;

        if (this.buffered == 512)
        {
            this.output.Write(buffer);
            this.buffered = 0;

            buffer.Fill(0);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!this.leaveOpen)
            {
                this.output.Dispose();
            }

            this.buffer.Dispose();
        }
    }
}
