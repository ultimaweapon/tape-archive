namespace TapeArchive.Tests;

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public sealed class ArchiveBuilderTests
{
    [Fact]
    public async Task WriteItemAsync_NoInvocations_ArchiveShouldHaveTwoRecords()
    {
        await using var output = new MemoryStream();
        await using var subject = new ArchiveBuilder(output, true);

        await subject.DisposeAsync();

        Assert.Equal(1024, output.Length);
    }

    [Fact]
    public async Task WriteItemAsync_WithMultipleItems_ShouldProduceValidArchive()
    {
        // Arrange.
        await using var content1 = new MemoryStream();
        await using var content2 = new MemoryStream();

        await using (var writer = new StreamWriter(content1, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write("Hello, world!\n");
        }

        await using (var writer = new StreamWriter(content2, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write("Hello!\n");
        }

        content1.Seek(0, SeekOrigin.Begin);
        content2.Seek(0, SeekOrigin.Begin);

        // Act.
        await using var output = new MemoryStream();
        await using var subject = new ArchiveBuilder(output, true);

        await subject.WriteItemAsync(new UstarItem(ItemType.Directory, new("./"))
        {
            UserId = 1000,
            GroupId = 1000,
            ModificationTime = new(2022, 3, 20, 20, 55, 14, DateTimeKind.Utc),
            UserName = "ultimaweapon",
            GroupName = "ultimaweapon",
        });

        await subject.WriteItemAsync(new UstarItem(ItemType.Directory, new("./foo/"))
        {
            UserId = 1000,
            GroupId = 1000,
            ModificationTime = new(2022, 03, 17, 20, 52, 53, DateTimeKind.Utc),
            UserName = "ultimaweapon",
            GroupName = "ultimaweapon",
        });

        await subject.WriteItemAsync(new UstarItem(ItemType.Directory, new("./Foo/"))
        {
            UserId = 1000,
            GroupId = 1000,
            ModificationTime = new(2022, 3, 17, 20, 53, 26, DateTimeKind.Utc),
            UserName = "ultimaweapon",
            GroupName = "ultimaweapon",
        });

        await subject.WriteItemAsync(new UstarItem(ItemType.RegularFile, new("./Foo/file"))
        {
            UserId = 1000,
            GroupId = 1000,
            UserName = "ultimaweapon",
            GroupName = "ultimaweapon",
            ModificationTime = new(2022, 3, 17, 20, 53, 26, DateTimeKind.Utc),
            Content = content1,
            Size = content1.Length,
        });

        await subject.WriteItemAsync(new UstarItem(ItemType.RegularFile, new("./file"))
        {
            UserId = 1000,
            GroupId = 1000,
            UserName = "ultimaweapon",
            GroupName = "ultimaweapon",
            ModificationTime = new(2022, 03, 17, 20, 54, 11, DateTimeKind.Utc),
            Content = content2,
            Size = content2.Length,
        });

        await subject.WriteItemAsync(new UstarItem(ItemType.RegularFile, new("./empty"))
        {
            UserId = 1000,
            GroupId = 1000,
            UserName = "ultimaweapon",
            ModificationTime = new(2022, 3, 20, 20, 55, 14, DateTimeKind.Utc),
            GroupName = "ultimaweapon",
        });

        await subject.DisposeAsync();

        // Assert.
        await using var references = new MemoryStream();

        await using (var file = File.OpenRead(Path.Join("test-vectors", "ustar.tar")))
        {
            await file.CopyToAsync(references);
        }

        var expected = references.ToArray();
        var actual = output.ToArray();

        Assert.Equal(expected.Length, actual.Length);

        for (var i = 0; i < actual.Length; i++)
        {
            var local = i % 512;

            if (local >= 148 && local < 156)
            {
                // Skip checksum.
                continue;
            }
            else if (local >= 328 && local < 344)
            {
                // Skip devmajor and devminor.
                continue;
            }

            if (actual[i] != expected[i])
            {
                throw new Exception($"expected[{i}] != actual[{i}]");
            }
        }
    }
}
