namespace TapeArchive;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents an archive item of the early draft of IEEE Std 1003.1-1988 ("POSIX.1") TAR.
/// </summary>
public class PrePosixItem : ArchiveItem
{
    private string? userName;
    private string? groupName;

    public PrePosixItem(ItemType type, ItemName name)
        : base(type, name)
    {
        this.userName = string.Empty;
        this.groupName = string.Empty;
    }

    protected internal PrePosixItem()
    {
    }

    public override bool IsRegularFile
    {
        get => this.Type == PrePosixType.RegularFile || base.IsRegularFile;
    }

    public override bool IsDirectory
    {
        get => this.Type == PrePosixType.Directory || base.IsDirectory;
    }

    public string UserName
    {
        get => LoadBackingField(this.userName);
        set => this.userName = value;
    }

    public string GroupName
    {
        get => LoadBackingField(this.groupName);
        set => this.groupName = value;
    }

    public override void WriteHeaders(Span<byte> output)
    {
        base.WriteHeaders(output);

        this.WriteMagic(output);
        this.WriteVersion(output);
        this.WriteUserName(output);
        this.WriteGroupName(output);
    }

    protected internal override async ValueTask ParseHeadersAsync(HeaderCollection headers, CancellationToken cancellationToken = default)
    {
        await base.ParseHeadersAsync(headers, cancellationToken);

        this.userName = await this.ParseUserNameAsync(headers, cancellationToken);
        this.groupName = await this.ParseGroupNameAsync(headers, cancellationToken);
    }

    protected internal override PrePosixItem CreateParent(ItemName name)
    {
        if (!name.IsDirectory)
        {
            throw new ArgumentException("The value is not a directory.", nameof(name));
        }

        return new(PrePosixType.Directory, name)
        {
            Mode = this.Mode,
            UserId = this.UserId,
            GroupId = this.GroupId,
            ModificationTime = this.ModificationTime,
            UserName = this.UserName,
            GroupName = this.GroupName,
        };
    }

    protected override bool IsNumericLeading(byte value)
    {
        return value == ' ';
    }

    protected override int? CheckMediumNumericEnding(ReadOnlySpan<byte> value)
    {
        return value[^1] == ' ' || value[^1] == 0 ? 1 : null;
    }

    protected override int? CheckLargeNumericEnding(ReadOnlySpan<byte> value)
    {
        return value[^1] == ' ' || value[^1] == 0 ? 1 : null;
    }

    protected override ValueTask<ItemType> ParseTypeAsync(HeaderCollection headers, CancellationToken cancellationToken = default)
    {
        var linkflag = headers[0].Span[156];

        if (linkflag == 0 || linkflag == '1')
        {
            return base.ParseTypeAsync(headers, cancellationToken);
        }
        else if (linkflag == '2')
        {
            throw new ArchiveException("Symbolic link is not supported.");
        }
        else if (linkflag == '3')
        {
            throw new ArchiveException("Character device node is not supported.");
        }
        else if (linkflag == '4')
        {
            throw new ArchiveException("Block device node is not supported.");
        }
        else if (linkflag == '5')
        {
            return new(PrePosixType.Directory);
        }
        else if (linkflag == '6')
        {
            throw new ArchiveException("FIFO node is not supported.");
        }
        else if (linkflag == '7')
        {
            throw new ArchiveException("Reserved is not supported.");
        }
        else if (linkflag >= 'A' && linkflag <= 'Z')
        {
            throw new ArchiveException("Custom extensions is not supported.");
        }
        else
        {
            // Treat all remaining type as a regular file.
            return new(new ItemType(linkflag));
        }
    }

    protected virtual ValueTask<string> ParseUserNameAsync(HeaderCollection headers, CancellationToken cancellationToken = default)
    {
        string value;

        try
        {
            value = ParseNullTerminatedAscii(headers[0][265..297].Span);
        }
        catch (FormatException ex)
        {
            throw new ArchiveException("Invalid uname.", ex);
        }

        return new(value);
    }

    protected virtual ValueTask<string> ParseGroupNameAsync(HeaderCollection headers, CancellationToken cancellationToken = default)
    {
        string value;

        try
        {
            value = ParseNullTerminatedAscii(headers[0][297..329].Span);
        }
        catch (FormatException ex)
        {
            throw new ArchiveException("Invalid gname.", ex);
        }

        return new(value);
    }

    protected override void WriteMode(Span<byte> output)
    {
        if (!this.WriteOctal(output[100..107], this.Mode))
        {
            throw new ArchiveException("Mode is not valid.");
        }

        output[107] = 0;
    }

    protected override void WriteUserId(Span<byte> output)
    {
        if (!this.WriteOctal(output[108..115], this.UserId))
        {
            throw new ArchiveException("UserId is not valid.");
        }

        output[115] = 0;
    }

    protected override void WriteGroupId(Span<byte> output)
    {
        if (!this.WriteOctal(output[116..123], this.GroupId))
        {
            throw new ArchiveException("GroupId is not valid.");
        }

        output[123] = 0;
    }

    protected override void WriteSize(Span<byte> output)
    {
        base.WriteSize(output);

        output[135] = 0;
    }

    protected override void WriteModificationTime(Span<byte> output)
    {
        base.WriteModificationTime(output);

        output[147] = 0;
    }

    protected virtual void WriteMagic(Span<byte> output)
    {
        output[257] = (byte)'u';
        output[258] = (byte)'s';
        output[259] = (byte)'t';
        output[260] = (byte)'a';
        output[261] = (byte)'r';
        output[262] = (byte)' ';
    }

    protected virtual void WriteVersion(Span<byte> output)
    {
        output[263] = (byte)' ';
        output[264] = 0;
    }

    protected virtual void WriteUserName(Span<byte> output)
    {
        var uname = output[265..296];
        var length = Encoding.ASCII.GetBytes(this.UserName, uname);

        if (length == uname.Length)
        {
            throw new ArchiveException("UserName too long.");
        }

        uname[length] = 0;
    }

    protected virtual void WriteGroupName(Span<byte> output)
    {
        var gname = output[297..328];
        var length = Encoding.ASCII.GetBytes(this.GroupName, gname);

        if (length == gname.Length)
        {
            throw new ArchiveException("GroupName too long.");
        }

        gname[length] = 0;
    }
}
