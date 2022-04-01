namespace TapeArchive.Tests;

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public sealed class TapeArchiveTests
{
    [Fact]
    public async Task ReadAsync_WithUstarFormat_ShouldSuccess()
    {
        await using var reader = this.GetTestVector("ustar.tar");
        await using var subject = new TapeArchive(reader, true);
        var assertions = new Func<ArchiveItem, Task>[]
        {
            Assert0,
            Assert1,
            Assert2,
            Assert3,
            Assert4,
            Assert5,
        };

        var i = 0;

        await foreach (var item in subject.ReadAsync())
        {
            await assertions[i++](item);
        }

        Task Assert0(ArchiveItem i)
        {
            var u = Assert.IsType<UstarItem>(i);

            Assert.Equal(PrePosixType.Directory, i.Type);
            Assert.Equal("./", i.Name.ToString());
            Assert.Equal(493, i.Mode); // 755
            Assert.Equal(1000, i.UserId);
            Assert.Equal(1000, i.GroupId);
            Assert.Equal(new DateTime(2022, 3, 20, 20, 55, 14, DateTimeKind.Utc), i.ModificationTime);
            Assert.Equal("ultimaweapon", u.UserName);
            Assert.Equal("ultimaweapon", u.GroupName);
            Assert.Equal(0, i.Size);

            return Task.CompletedTask;
        }

        Task Assert1(ArchiveItem i)
        {
            var u = Assert.IsType<UstarItem>(i);

            Assert.Equal(PrePosixType.Directory, i.Type);
            Assert.Equal("./foo/", i.Name.ToString());
            Assert.Equal(493, i.Mode); // 755
            Assert.Equal(1000, i.UserId);
            Assert.Equal(1000, i.GroupId);
            Assert.Equal(new DateTime(2022, 03, 17, 20, 52, 53, DateTimeKind.Utc), i.ModificationTime);
            Assert.Equal("ultimaweapon", u.UserName);
            Assert.Equal("ultimaweapon", u.GroupName);
            Assert.Equal(0, i.Size);

            return Task.CompletedTask;
        }

        Task Assert2(ArchiveItem i)
        {
            var u = Assert.IsType<UstarItem>(i);

            Assert.Equal(PrePosixType.Directory, i.Type);
            Assert.Equal("./Foo/", i.Name.ToString());
            Assert.Equal(493, i.Mode); // 755
            Assert.Equal(1000, i.UserId);
            Assert.Equal(1000, i.GroupId);
            Assert.Equal(new DateTime(2022, 3, 17, 20, 53, 26, DateTimeKind.Utc), i.ModificationTime);
            Assert.Equal("ultimaweapon", u.UserName);
            Assert.Equal("ultimaweapon", u.GroupName);
            Assert.Equal(0, i.Size);

            return Task.CompletedTask;
        }

        async Task Assert3(ArchiveItem i)
        {
            using var reader = new StreamReader(i.Content, encoding: Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var u = Assert.IsType<UstarItem>(i);

            Assert.Equal(PrePosixType.RegularFile, i.Type);
            Assert.Equal("./Foo/file", i.Name.ToString());
            Assert.Equal(420, i.Mode); // 644
            Assert.Equal(1000, i.UserId);
            Assert.Equal(1000, i.GroupId);
            Assert.Equal(new DateTime(2022, 3, 17, 20, 53, 26, DateTimeKind.Utc), i.ModificationTime);
            Assert.Equal("ultimaweapon", u.UserName);
            Assert.Equal("ultimaweapon", u.GroupName);
            Assert.Equal(14, i.Size);
            Assert.Equal("Hello, world!\n", await reader.ReadToEndAsync());
        }

        async Task Assert4(ArchiveItem i)
        {
            using var reader = new StreamReader(i.Content, encoding: Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var u = Assert.IsType<UstarItem>(i);

            Assert.Equal(PrePosixType.RegularFile, i.Type);
            Assert.Equal("./file", i.Name.ToString());
            Assert.Equal(420, i.Mode); // 644
            Assert.Equal(1000, i.UserId);
            Assert.Equal(1000, i.GroupId);
            Assert.Equal(new DateTime(2022, 03, 17, 20, 54, 11, DateTimeKind.Utc), i.ModificationTime);
            Assert.Equal("ultimaweapon", u.UserName);
            Assert.Equal("ultimaweapon", u.GroupName);
            Assert.Equal(7, i.Size);
            Assert.Equal("Hello!\n", await reader.ReadToEndAsync());
        }

        Task Assert5(ArchiveItem i)
        {
            var u = Assert.IsType<UstarItem>(i);

            Assert.Equal(PrePosixType.RegularFile, i.Type);
            Assert.Equal("./empty", i.Name.ToString());
            Assert.Equal(420, i.Mode); // 644
            Assert.Equal(1000, i.UserId);
            Assert.Equal(1000, i.GroupId);
            Assert.Equal(new DateTime(2022, 3, 20, 20, 55, 14, DateTimeKind.Utc), i.ModificationTime);
            Assert.Equal("ultimaweapon", u.UserName);
            Assert.Equal("ultimaweapon", u.GroupName);
            Assert.Equal(0, i.Size);

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ReadAsync_LongPathWithUstar_ShouldSuccess()
    {
        await using var reader = this.GetTestVector("ustar-prefix.tar");
        await using var subject = new TapeArchive(reader, true);
        var expected = new[]
        {
            "./",
            "./this/",
            "./this/is/",
            "./this/is/very/",
            "./this/is/very/long/",
            "./this/is/very/long/path/",
            "./this/is/very/long/path/it/",
            "./this/is/very/long/path/it/is/",
            "./this/is/very/long/path/it/is/really/",
            "./this/is/very/long/path/it/is/really/long/",
            "./this/is/very/long/path/it/is/really/long/so/",
            "./this/is/very/long/path/it/is/really/long/so/long/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/not/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/not/long/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/not/long/enought/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/not/long/enought/we/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/not/long/enought/we/need/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/not/long/enought/we/need/more/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/not/long/enought/we/need/more/long/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/not/long/enought/we/need/more/long/path/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/not/long/enought/we/need/more/long/path/more/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/not/long/enought/we/need/more/long/path/more/and/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/not/long/enought/we/need/more/long/path/more/and/more/",
            "./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/not/long/enought/we/need/more/long/path/more/and/more/finally",
        };

        var i = 0;

        await foreach (var item in subject.ReadAsync())
        {
            Assert.Equal(expected[i++], item.Name.ToString());
        }
    }

    [Fact]
    public async Task ReadAsync_WithGnuFormat_ShouldSuccess()
    {
        await using var reader = this.GetTestVector("gnu.tar");
        await using var subject = new TapeArchive(reader, true);
        var assertions = new Func<ArchiveItem, Task>[]
        {
            Assert0,
            Assert1,
            Assert2,
            Assert3,
            Assert4,
            Assert5,
        };

        var i = 0;

        await foreach (var item in subject.ReadAsync())
        {
            await assertions[i++](item);
        }

        Task Assert0(ArchiveItem i)
        {
            var p = Assert.IsType<PrePosixItem>(i);

            Assert.Equal(PrePosixType.Directory, i.Type);
            Assert.Equal("./", i.Name.ToString());
            Assert.Equal(493, i.Mode); // 755
            Assert.Equal(1000, i.UserId);
            Assert.Equal(1000, i.GroupId);
            Assert.Equal(new DateTime(2022, 3, 20, 20, 55, 14, DateTimeKind.Utc), i.ModificationTime);
            Assert.Equal("ultimaweapon", p.UserName);
            Assert.Equal("ultimaweapon", p.GroupName);
            Assert.Equal(0, i.Size);

            return Task.CompletedTask;
        }

        Task Assert1(ArchiveItem i)
        {
            var p = Assert.IsType<PrePosixItem>(i);

            Assert.Equal(PrePosixType.Directory, i.Type);
            Assert.Equal("./foo/", i.Name.ToString());
            Assert.Equal(493, i.Mode); // 755
            Assert.Equal(1000, i.UserId);
            Assert.Equal(1000, i.GroupId);
            Assert.Equal(new DateTime(2022, 03, 17, 20, 52, 53, DateTimeKind.Utc), i.ModificationTime);
            Assert.Equal("ultimaweapon", p.UserName);
            Assert.Equal("ultimaweapon", p.GroupName);
            Assert.Equal(0, i.Size);

            return Task.CompletedTask;
        }

        Task Assert2(ArchiveItem i)
        {
            var p = Assert.IsType<PrePosixItem>(i);

            Assert.Equal(PrePosixType.Directory, i.Type);
            Assert.Equal("./Foo/", i.Name.ToString());
            Assert.Equal(493, i.Mode); // 755
            Assert.Equal(1000, i.UserId);
            Assert.Equal(1000, i.GroupId);
            Assert.Equal(new DateTime(2022, 3, 17, 20, 53, 26, DateTimeKind.Utc), i.ModificationTime);
            Assert.Equal("ultimaweapon", p.UserName);
            Assert.Equal("ultimaweapon", p.GroupName);
            Assert.Equal(0, i.Size);

            return Task.CompletedTask;
        }

        async Task Assert3(ArchiveItem i)
        {
            using var reader = new StreamReader(i.Content, encoding: Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var p = Assert.IsType<PrePosixItem>(i);

            Assert.Equal(PrePosixType.RegularFile, i.Type);
            Assert.Equal("./Foo/file", i.Name.ToString());
            Assert.Equal(420, i.Mode); // 644
            Assert.Equal(1000, i.UserId);
            Assert.Equal(1000, i.GroupId);
            Assert.Equal(new DateTime(2022, 3, 17, 20, 53, 26, DateTimeKind.Utc), i.ModificationTime);
            Assert.Equal("ultimaweapon", p.UserName);
            Assert.Equal("ultimaweapon", p.GroupName);
            Assert.Equal(14, i.Size);
            Assert.Equal("Hello, world!\n", await reader.ReadToEndAsync());
        }

        async Task Assert4(ArchiveItem i)
        {
            using var reader = new StreamReader(i.Content, encoding: Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var p = Assert.IsType<PrePosixItem>(i);

            Assert.Equal(PrePosixType.RegularFile, i.Type);
            Assert.Equal("./file", i.Name.ToString());
            Assert.Equal(420, i.Mode); // 644
            Assert.Equal(1000, i.UserId);
            Assert.Equal(1000, i.GroupId);
            Assert.Equal(new DateTime(2022, 03, 17, 20, 54, 11, DateTimeKind.Utc), i.ModificationTime);
            Assert.Equal("ultimaweapon", p.UserName);
            Assert.Equal("ultimaweapon", p.GroupName);
            Assert.Equal(7, i.Size);
            Assert.Equal("Hello!\n", await reader.ReadToEndAsync());
        }

        Task Assert5(ArchiveItem i)
        {
            var p = Assert.IsType<PrePosixItem>(i);

            Assert.Equal(PrePosixType.RegularFile, i.Type);
            Assert.Equal("./empty", i.Name.ToString());
            Assert.Equal(420, i.Mode); // 644
            Assert.Equal(1000, i.UserId);
            Assert.Equal(1000, i.GroupId);
            Assert.Equal(new DateTime(2022, 3, 20, 20, 55, 14, DateTimeKind.Utc), i.ModificationTime);
            Assert.Equal("ultimaweapon", p.UserName);
            Assert.Equal("ultimaweapon", p.GroupName);
            Assert.Equal(0, i.Size);

            return Task.CompletedTask;
        }
    }

    private Stream GetTestVector(string name) => File.OpenRead(Path.Join("test-vectors", name));
}
