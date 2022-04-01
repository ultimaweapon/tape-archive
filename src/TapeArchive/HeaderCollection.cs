namespace TapeArchive;

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class HeaderCollection : IReadOnlyList<ReadOnlyMemory<byte>>, IDisposable
{
    private readonly ArchiveReader reader;
    private readonly List<IMemoryOwner<byte>> headers;
    private bool disposed;

    internal HeaderCollection(ArchiveReader reader)
    {
        this.reader = reader;
        this.headers = new(1);

        this.Add(reader.Buffer);
    }

    public int Count => this.headers.Count;

    public ReadOnlyMemory<byte> this[int index] => this.headers[index].Memory[..512];

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        foreach (var header in this.headers)
        {
            header.Dispose();
        }

        this.disposed = true;
    }

    public IEnumerator<ReadOnlyMemory<byte>> GetEnumerator()
    {
        foreach (var header in this.headers)
        {
            yield return header.Memory[..512];
        }
    }

    public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        await this.reader.NextAsync(cancellationToken);

        this.Add(this.reader.Buffer);
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private void Add(ReadOnlyMemory<byte> source)
    {
        var header = MemoryPool<byte>.Shared.Rent(512);

        try
        {
            source.CopyTo(header.Memory);

            this.headers.Add(header);
        }
        catch
        {
            header.Dispose();
            throw;
        }
    }
}
