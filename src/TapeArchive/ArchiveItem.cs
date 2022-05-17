namespace TapeArchive;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents an archive item of the original TAR that was shipped with Version 7 AT&amp;T UNIX.
/// </summary>
public class ArchiveItem
{
    private static readonly long UnixEpochTicks = DateTime.UnixEpoch.Ticks;

    private ItemType? type;
    private ItemName? name;
    private int? mode;
    private int? userId;
    private int? groupId;
    private long? size;
    private DateTime? modificationTime;
    private int? checksum;
    private Stream? content;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveItem"/> class.
    /// </summary>
    /// <param name="type">
    /// Type of item.
    /// </param>
    /// <param name="name">
    /// Name of item.
    /// </param>
    public ArchiveItem(ItemType type, ItemName name)
    {
        this.type = type;
        this.name = name;
        this.mode = name.IsDirectory ? 0x1ED : 0x1A4; // 755 for direcotry and 644 for other.
        this.userId = 0;
        this.groupId = 0;
        this.size = 0;
        this.modificationTime = DateTime.Now;
        this.content = Stream.Null;
    }

    protected internal ArchiveItem()
    {
    }

    protected delegate int? TrailChecker(ReadOnlySpan<byte> field);

    public ItemType Type
    {
        get => LoadBackingField(this.type);
    }

    public virtual bool IsRegularFile
    {
        get => this.Type == ItemType.RegularFile && !this.Name.IsDirectory;
    }

    public virtual bool IsDirectory
    {
        get => this.Type == ItemType.RegularFile && this.Name.IsDirectory;
    }

    public ItemName Name
    {
        get => LoadBackingField(this.name);
        set => this.name = value;
    }

    public int Mode
    {
        get => LoadBackingField(this.mode);
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.mode = value;
        }
    }

    public int UserId
    {
        get => LoadBackingField(this.userId);
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.userId = value;
        }
    }

    public int GroupId
    {
        get => LoadBackingField(this.groupId);
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.groupId = value;
        }
    }

    /// <summary>
    /// Gets or sets content size of this item.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// You are trying to set a negative value.
    /// </exception>
    /// <remarks>
    /// When writing archive you are responsible to make sure this value have the same size as <see cref="Content"/> otherwise the output archive will be
    /// corrupted.
    /// </remarks>
    public long Size
    {
        get => LoadBackingField(this.size);
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.size = value;
        }
    }

    public DateTime ModificationTime
    {
        get => LoadBackingField(this.modificationTime);
        set
        {
            if (value < DateTime.UnixEpoch)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.modificationTime = value;
        }
    }

    public int Checksum => LoadBackingField(this.checksum);

    /// <summary>
    /// Gets or sets a content of this item.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Type"/> to see what content is available when reading. When writing you are responsible to make sure the size of content you are setting
    /// to this property is the same as <see cref="Size"/> otherwise the output archive will be corrupted.
    ///
    /// Do not dispose this property when reading archive.
    /// </remarks>
    public Stream Content
    {
        get => LoadBackingField(this.content);
        set => this.content = value;
    }

    protected virtual byte PreferredNumericLeading => (byte)' ';

    /// <summary>
    /// Calculate number of records required for header.
    /// </summary>
    /// <returns>
    /// Number of records for header.
    /// </returns>
    public virtual int GetHeaderBlocksForWriting()
    {
        return 1;
    }

    /// <summary>
    /// Write header to the specified output.
    /// </summary>
    /// <param name="output">
    /// Destination to write to.
    /// </param>
    /// <exception cref="ArchiveException">
    /// The current item has invalid data for header.
    /// </exception>
    public virtual void WriteHeaders(Span<byte> output)
    {
        this.WriteType(output);
        this.WriteName(output);
        this.WriteMode(output);
        this.WriteUserId(output);
        this.WriteGroupId(output);
        this.WriteSize(output);
        this.WriteModificationTime(output);
    }

    public override string ToString()
    {
        if (this.name != null)
        {
            return this.name.ToString();
        }
        else
        {
            return string.Empty;
        }
    }

    internal void ParseChecksum(HeaderCollection headers)
    {
        // Load check sum.
        var header = headers[0].Span;
        int checksum;

        try
        {
            checksum = (int)ParseOctal(header[148..156], this.IsNumericLeading, v => v[^2] == 0 && v[^1] == ' ' ? 2 : null);
        }
        catch (FormatException ex)
        {
            throw new ArchiveException("Invalid checksum.", ex);
        }

        // Do checksum.
        if (checksum != CalculateChecksum(header, false) && checksum != CalculateChecksum(header, true))
        {
            throw new ArchiveException("Checksum mismatched.");
        }

        this.checksum = checksum;
    }

    internal void WriteChecksum(Span<byte> output)
    {
        this.checksum = CalculateChecksum(output[..512], false);
        this.WriteOctal(output[148..154], this.checksum.Value);

        output[154] = 0;
        output[155] = (byte)' ';
    }

    protected internal virtual async ValueTask ParseHeadersAsync(HeaderCollection headers, CancellationToken cancellationToken = default)
    {
        this.type = await this.ParseTypeAsync(headers, cancellationToken);
        this.name = await this.ParseNameAsync(headers, cancellationToken);
        this.size = await this.ParseSizeAsync(headers, cancellationToken);
        this.mode = await this.ParseModeAsync(headers, cancellationToken);
        this.userId = await this.ParseUserIdAsync(headers, cancellationToken);
        this.groupId = await this.ParseGroupIdAsync(headers, cancellationToken);
        this.modificationTime = await this.ParseModificationTimeAsync(headers, cancellationToken);
    }

    protected internal virtual ArchiveItem CreateParent(ItemName name, ParentProperties? props)
    {
        if (!name.IsDirectory)
        {
            throw new ArgumentException("The value is not a directory.", nameof(name));
        }

        return new(ItemType.RegularFile, name)
        {
            Mode = props?.Mode ?? GetParentMode(this.Mode),
            UserId = props?.UserId ?? this.UserId,
            GroupId = props?.GroupId ?? this.GroupId,
            ModificationTime = props?.ModificationTime ?? this.ModificationTime,
        };
    }

    /// <summary>
    /// Parse NULL-terminated ASCII.
    /// </summary>
    /// <param name="value">
    /// The value to parse.
    /// </param>
    /// <returns>
    /// A string decoded from <paramref name="value"/>.
    /// </returns>
    /// <exception cref="FormatException">
    /// <paramref name="value"/> is not a NULL-terminated ASCII.
    /// </exception>
    protected static string ParseNullTerminatedAscii(ReadOnlySpan<byte> value)
    {
        var end = value.IndexOf((byte)0);

        if (end == -1)
        {
            throw new FormatException("The value is not a NULL-terminated ASCII.");
        }

        return Encoding.ASCII.GetString(value[..end]);
    }

    /// <summary>
    /// Parse a non-negative octal number that represent in ASCII.
    /// </summary>
    /// <param name="value">
    /// The value to parse.
    /// </param>
    /// <param name="skipLeading">
    /// A delegate to check if the leading bytes should be skiped.
    /// </param>
    /// <param name="checkEnding">
    /// A delegate to check if parsing value end in expected values.
    /// </param>
    /// <returns>
    /// A <see cref="long"/> that represents by <paramref name="value"/>.
    /// </returns>
    /// <exception cref="FormatException">
    /// <paramref name="value"/> has invalid format.
    /// </exception>
    /// <exception cref="OverflowException">
    /// <paramref name="value"/> is larger than <see cref="long"/>.
    /// </exception>
    /// <remarks>
    /// The return value can fit in an <see cref="int"/> if <paramref name="value"/> does not longer than 10 digits. The only cases for
    /// <see cref="OverflowException"/> is when <paramref name="value"/> longer than 21 digits.
    /// </remarks>
    protected static long ParseOctal(ReadOnlySpan<byte> value, Func<byte, bool> skipLeading, TrailChecker checkEnding)
    {
        // Find a window to parse.
        var trim = checkEnding(value);
        int begin, end;

        if (trim == null)
        {
            throw new FormatException($"The value is not ended with expected value.");
        }

        for (begin = 0, end = value.Length - trim.Value; begin < end; begin++)
        {
            var ch = value[begin];

            if (ch == '0' || skipLeading(ch))
            {
                continue;
            }

            break;
        }

        if (begin == end)
        {
            return 0;
        }

        // Parse.
        var result = 0UL;
        var parsed = 0;

        for (var i = end - 1; i >= begin; i--, parsed++)
        {
            ulong bits = value[i] switch
            {
                (byte)'0' => 0,
                (byte)'1' => 1,
                (byte)'2' => 2,
                (byte)'3' => 3,
                (byte)'4' => 4,
                (byte)'5' => 5,
                (byte)'6' => 6,
                (byte)'7' => 7,
                _ => throw new FormatException("The value is not a valid octal number."),
            };

            // The maximum value that can fit in a "long" is 777777777777777777777.
            if (parsed == 21)
            {
                throw new OverflowException("The value is too large.");
            }

            result |= bits << (3 * parsed);
        }

        return (long)result;
    }

    protected static int GetParentMode(int child)
    {
        var result = child;

        // Owner.
        if ((result & UnixPermissions.OwnerRead) != 0)
        {
            result |= UnixPermissions.OwnerExecute;
        }

        // Group.
        if ((result & UnixPermissions.GroupRead) != 0)
        {
            result |= UnixPermissions.GroupExecute;
        }

        // Other.
        if ((result & UnixPermissions.OtherRead) != 0)
        {
            result |= UnixPermissions.OtherExecute;
        }

        return result;
    }

    protected static DateTime ConvertUnixTime(long time)
    {
        return new(UnixEpochTicks + (TimeSpan.TicksPerSecond * time), DateTimeKind.Utc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static T LoadBackingField<T>(T? value, [CallerMemberName] string property = "") where T : class
    {
        return value ?? throw new InvalidOperationException($"{property} has no value.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static T LoadBackingField<T>(T? value, [CallerMemberName] string property = "") where T : struct
    {
        return value ?? throw new InvalidOperationException($"{property} has no value.");
    }

    protected virtual bool IsNumericLeading(byte value)
    {
        return value == ' ' || value == '0';
    }

    /// <summary>
    /// An implementation of <see cref="TrailChecker"/> for "mode", "uid" and "gid".
    /// </summary>
    /// <param name="value">
    /// The value of the field.
    /// </param>
    /// <returns>
    /// A number of byte to trim from <paramref name="value"/> before parsing or <c>null</c> if <paramref name="value"/> is not ended with valid values.
    /// </returns>
    protected virtual int? CheckMediumNumericEnding(ReadOnlySpan<byte> value)
    {
        return value[^2] == ' ' && value[^1] == 0 ? 2 : null;
    }

    /// <summary>
    /// An implementation of <see cref="TrailChecker"/> for "size" and "mtime".
    /// </summary>
    /// <param name="value">
    /// The value of the field.
    /// </param>
    /// <returns>
    /// A number of byte to trim from <paramref name="value"/> before parsing or <c>null</c> if <paramref name="value"/> is not ended with valid values.
    /// </returns>
    protected virtual int? CheckLargeNumericEnding(ReadOnlySpan<byte> value)
    {
        return value[^1] == ' ' ? 1 : null;
    }

    protected bool WriteOctal(Span<byte> output, long value)
    {
        // We need at least one byte.
        var i = output.Length - 1;

        if (i < 0)
        {
            return false;
        }

        // Do conversion.
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        else if (value == 0)
        {
            output[i--] = (byte)'0';
        }
        else
        {
            while (value > 0)
            {
                if (i < 0)
                {
                    return false;
                }

                output[i--] = (value & 0x7) switch
                {
                    0 => (byte)'0',
                    1 => (byte)'1',
                    2 => (byte)'2',
                    3 => (byte)'3',
                    4 => (byte)'4',
                    5 => (byte)'5',
                    6 => (byte)'6',
                    _ => (byte)'7',
                };

                value >>= 3;
            }
        }

        // Fill leading.
        var lead = this.PreferredNumericLeading;

        while (i >= 0)
        {
            output[i--] = lead;
        }

        return true;
    }

    protected virtual ValueTask<ItemType> ParseTypeAsync(HeaderCollection headers, CancellationToken cancellationToken = default)
    {
        var header = headers[0].Span;
        var linkflag = header[156];

        if (linkflag == 0)
        {
            return new(ItemType.RegularFile);
        }
        else if (linkflag == '1')
        {
            throw new ArchiveException("Hard link is not supported.");
        }
        else
        {
            throw new ArchiveException($"Unknown linkflag {linkflag}.");
        }
    }

    protected virtual ValueTask<ItemName> ParseNameAsync(HeaderCollection headers, CancellationToken cancellationToken = default)
    {
        // Load value.
        string value;

        try
        {
            value = ParseNullTerminatedAscii(headers[0][..100].Span);
        }
        catch (FormatException ex)
        {
            throw new ArchiveException("Invalid name.", ex);
        }

        // Construct domain object.
        ItemName name;

        try
        {
            name = new(value);
        }
        catch (ArgumentException ex)
        {
            throw new ArchiveException("Invalid name.", ex);
        }

        return new(name);
    }

    protected virtual ValueTask<int> ParseModeAsync(HeaderCollection headers, CancellationToken cancellationToken = default)
    {
        int value;

        try
        {
            value = (int)ParseOctal(headers[0][100..108].Span, this.IsNumericLeading, this.CheckMediumNumericEnding);
        }
        catch (FormatException ex)
        {
            throw new ArchiveException("Invalid mode.", ex);
        }

        return new(value);
    }

    protected virtual ValueTask<int> ParseUserIdAsync(HeaderCollection headers, CancellationToken cancellationToken = default)
    {
        int value;

        try
        {
            value = (int)ParseOctal(headers[0][108..116].Span, this.IsNumericLeading, this.CheckMediumNumericEnding);
        }
        catch (FormatException ex)
        {
            throw new ArchiveException("Invalid uid.", ex);
        }

        return new(value);
    }

    protected virtual ValueTask<int> ParseGroupIdAsync(HeaderCollection headers, CancellationToken cancellationToken = default)
    {
        int value;

        try
        {
            value = (int)ParseOctal(headers[0][116..124].Span, this.IsNumericLeading, this.CheckMediumNumericEnding);
        }
        catch (FormatException ex)
        {
            throw new ArchiveException("Invalid gid.", ex);
        }

        return new(value);
    }

    protected virtual ValueTask<long> ParseSizeAsync(HeaderCollection headers, CancellationToken cancellationToken = default)
    {
        long value;

        try
        {
            value = ParseOctal(headers[0][124..136].Span, this.IsNumericLeading, this.CheckLargeNumericEnding);
        }
        catch (FormatException ex)
        {
            throw new ArchiveException("Invalid size.", ex);
        }

        return new(value);
    }

    protected virtual ValueTask<DateTime> ParseModificationTimeAsync(HeaderCollection headers, CancellationToken cancellationToken = default)
    {
        long value;

        try
        {
            value = ParseOctal(headers[0][136..148].Span, this.IsNumericLeading, this.CheckLargeNumericEnding);
        }
        catch (FormatException ex)
        {
            throw new ArchiveException("Invalid mtime.", ex);
        }

        return new(ConvertUnixTime(value));
    }

    protected virtual void WriteType(Span<byte> output)
    {
        output[156] = this.Type.Value;
    }

    protected virtual void WriteName(Span<byte> output)
    {
        var length = Encoding.ASCII.GetBytes(this.Name.ToString(), output[..100]);

        if (length == 100)
        {
            throw new ArchiveException("Name is not valid.");
        }

        output[length] = 0;
    }

    protected virtual void WriteMode(Span<byte> output)
    {
        if (!this.WriteOctal(output[100..106], this.Mode))
        {
            throw new ArchiveException("Mode is not valid.");
        }

        output[106] = (byte)' ';
        output[107] = 0;
    }

    protected virtual void WriteUserId(Span<byte> output)
    {
        if (!this.WriteOctal(output[108..114], this.UserId))
        {
            throw new ArchiveException("UserId is not valid.");
        }

        output[114] = (byte)' ';
        output[115] = 0;
    }

    protected virtual void WriteGroupId(Span<byte> output)
    {
        if (!this.WriteOctal(output[116..122], this.GroupId))
        {
            throw new ArchiveException("GroupId is not valid.");
        }

        output[122] = (byte)' ';
        output[123] = 0;
    }

    protected virtual void WriteSize(Span<byte> output)
    {
        if (!this.WriteOctal(output[124..135], this.Size))
        {
            throw new ArchiveException("Size is not valid.");
        }

        output[135] = (byte)' ';
    }

    protected virtual void WriteModificationTime(Span<byte> output)
    {
        if (!this.WriteOctal(output[136..147], ((DateTimeOffset)this.ModificationTime).ToUnixTimeSeconds()))
        {
            throw new ArchiveException("ModificationTime is not valid.");
        }

        output[147] = (byte)' ';
    }

    private static int CalculateChecksum(ReadOnlySpan<byte> header, bool nonStandard)
    {
        // The maximum value of checksum is 130,560.
        Span<byte> buffer = stackalloc byte[512];

        header.CopyTo(buffer);
        buffer[148..156].Fill((byte)' ');

        if (nonStandard)
        {
            var checksum = 0;

            for (var i = 0; i < 512; i++)
            {
                checksum += (sbyte)buffer[i];
            }

            return checksum;
        }
        else
        {
            var checksum = 0U;

            for (var i = 0; i < 512; i++)
            {
                checksum += buffer[i];
            }

            return (int)checksum;
        }
    }
}
