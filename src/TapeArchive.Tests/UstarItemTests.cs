namespace TapeArchive;

using System;
using System.Text;
using Xunit;

public sealed class UstarItemTests
{
    [Theory]
    [InlineData("./this/is/very/long/path/it/is/really/long/so/long/super/long/exceptional/long/still/not/long/enought/")]
    [InlineData("./1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890/")] // Last component not fit in name.
    public void WriteName_WithPrefix_ShouldProduceCorrectHeader(string path)
    {
        // Arrange.
        var subject = new UstarItem(PrePosixType.Directory, new(path));
        var header = new byte[512 * subject.GetHeaderBlocksForWriting()];

        for (var i = 0; i < header.Length; i++)
        {
            // We want to fill the header with non-zero to test if it do null-termination correctly.
            header[i] = 0xFF;
        }

        // Act.
        subject.WriteHeaders(header);

        // Assert.
        Assert.Equal(path, $"{DecodeString(header.AsSpan(345..500))}/{DecodeString(header.AsSpan(..100))}");

        static string DecodeString(Span<byte> bin)
        {
            var i = bin.IndexOf((byte)0);

            return Encoding.ASCII.GetString(i == -1 ? bin : bin[..i]);
        }
    }
}
