# TapeArchive
[![Nuget](https://img.shields.io/nuget/v/TapeArchive)](https://www.nuget.org/packages/TapeArchive)

## Usage

Before using this library you need to understand some basic of TAR structure. **DO NOT SKIP THIS** otherwise you won't known how to properly working with this library.

TAR is acronym for **T**ape **Ar**chive. It was designed for writing and reading to/from a tape drive. That mean its structure was designed for sequential access, not random access. If you want to seek to a specific file in a TAR what most library actually do is keep reading and discard all data until it reach that file. And TAR does not support compression by itself. The compression you see like file.tar.gz is just a TAR that compressed witgh GZIP later.

TAR format have a lot of variants:

1. Original TAR that shipped with AT&T UNIX Version 7
2. Pre-POSIX (AKA. POSIX.1-1988 draft)
3. POSIX.1-1988 (AKA. ustar)
4. pax (AKA. POSIX.1-2001)
5. GNU
6. Solaris
7. AIX
8. macOS

Usually most reader will be able to extract any variants. This library currently support up to ustar. But just as I said before that most reader will be able to extract any variants, including this library. So you should not have any problem when reading. For writing try to stick with original or ustar. Usually original variant will be able to suite you need in most cases.

### Reading

```csharp
using TapeArchive;

await using var reader = new TapeArchive(stream, true);

await foreach (var item in reader.ReadAsync())
{
    // Do something with item.
}
```

### Writing

```csharp
using System;
using TapeArchive;

await using var builder = new ArchiveBuilder(stream, true);

await builder.WriteItemAsync(new(ItemType.RegularFile, new("./file1"))
{
    Content = content,
    Size = size,
    ModificationTime = DateTime.Now,
});

await builder.CompleteAsync();
```

## Breaking changes

### 1.0 to 2.0

Disposing of `IArchiveBuilder` is changed. In 1.0 it will complete the archive. For 2.0 it will abort the archive if archive is not completed with
`CompleteAsync`. The aborted archive is a broken TAR and cannot be read by any TAR readers.

## Development

### Prerequisites

- .NET 6 SDK

### Build

```sh
dotnet build src/TapeArchive.sln
```

### Run tests

```sh
dotnet test src/TapeArchive.sln
```

## License

MIT
