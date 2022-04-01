namespace TapeArchive;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed class ItemName
{
    private static readonly IReadOnlySet<char> InvalidFileSystemChars = new HashSet<char>(Path.GetInvalidFileNameChars());

    private readonly List<string> parts;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemName"/> class from a TAR path.
    /// </summary>
    /// <param name="value">
    /// A TAR path.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="value"/> is not a valid TAR path.
    /// </exception>
    public ItemName(string value)
    {
        // Do a roughly check first.
        if (!value.StartsWith("./", StringComparison.Ordinal))
        {
            throw new ArgumentException("The value is start with other than './'.", nameof(value));
        }

        // Normalize path.
        var parts = new List<string>();
        int i, start;

        value = value[2..];

        for (start = 0, i = 0; i < value.Length; i++)
        {
            var ch = value[i];

            if (ch == 0)
            {
                throw new ArgumentException("The value contains NUL character.", nameof(value));
            }
            else if (ch == '/')
            {
                if (i == start)
                {
                    // Consecutive of "/".
                    throw new ArgumentException("The value contains two consecutive path separator.", nameof(value));
                }

                AddPart();
            }
        }

        if (i != start)
        {
            // Final part (e.g. "bar" in "./foo/bar").
            AddPart();
        }

        this.parts = parts;
        this.IsDirectory = parts.Count == 0 || value[^1] == '/';

        void AddPart()
        {
            var part = value[start..i];

            if (part == "." || part == "..")
            {
                throw new ArgumentException("The value contains forbidden folder name.", nameof(value));
            }

            parts.Add(part);
            start = i + 1;
        }
    }

    private ItemName(List<string> parts, bool directory)
    {
        this.parts = parts;
        this.IsDirectory = directory;
    }

    public bool IsRoot => this.parts.Count == 0;

    public bool IsDirectory { get; }

    public ItemName? Parent => this.parts.Count == 0 ? null : new(this.parts.SkipLast(1).ToList(), true);

    /// <summary>
    /// Convert a path in current running file system to <see cref="ItemName"/>.
    /// </summary>
    /// <param name="path">
    /// File system path to convert, must not ended with a path separator and must be relative path without current directory (e.g. not "./abc").
    /// </param>
    /// <param name="isDirectory">
    /// <c>true</c> if <paramref name="path"/> represents a directory; otherwise <c>false</c>.
    /// </param>
    /// <returns>
    /// An <see cref="ItemName"/> for <paramref name="path"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="path"/> is not valid.
    /// </exception>
    /// <remarks>
    /// Specify empty string for <paramref name="path"/> and <c>true</c> for <paramref name="isDirectory"/> to create an <see cref="ItemName"/> that represents
    /// a root directory ("./").
    /// </remarks>
    public static ItemName FromFileSystem(string path, bool isDirectory)
    {
        if (path.Length > 0 && (path[^1] == Path.DirectorySeparatorChar || path[^1] == Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("The value is ended with a path separator.", nameof(path));
        }

        var parts = new List<string>();
        var start = 0;
        var i = 0;

        for (; i < path.Length; i++)
        {
            var ch = path[i];

            if (ch == 0)
            {
                throw new ArgumentException("The value contains NUL character.", nameof(path));
            }
            else if (ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar)
            {
                if (i == start)
                {
                    // Consecutive of path separator or the value begin with it.
                    if (i == 0)
                    {
                        throw new ArgumentException("The value start with a path separator.", nameof(path));
                    }
                    else
                    {
                        throw new ArgumentException("The value contains consecutive path separators.", nameof(path));
                    }
                }

                WriteResult();
            }
        }

        if (i != start)
        {
            WriteResult();
        }

        if (parts.Count == 0 && !isDirectory)
        {
            throw new ArgumentException($"The value represents a root directory but {nameof(isDirectory)} is false.", nameof(path));
        }

        return new(parts, isDirectory);

        void WriteResult()
        {
            var part = path[start..i];

            if (part == "." || part == "..")
            {
                throw new ArgumentException($"'{part}' is not a valid file name.", nameof(path));
            }

            parts.Add(part);
            start = i + 1;
        }
    }

    /// <summary>
    /// Build a file system path for this name.
    /// </summary>
    /// <param name="prefix">
    /// A prefix path. Use empty string to specify no prefix.
    /// </param>
    /// <returns>
    /// A file system path for the current running system.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// This name contains invalid character for current running system.
    /// </exception>
    /// <remarks>
    /// The return value will be an empty string if <paramref name="prefix"/> is empty and <see cref="IsRoot"/> is <c>true</c>.
    ///
    /// This method also make sure the resulting path is safe to use with in a file system (e.g. no directory traversal attack).
    /// </remarks>
    public string ToFileSystemPath(string prefix = "")
    {
        var args = new string[this.parts.Count + 1];
        var i = 0;

        args[i++] = prefix;

        foreach (var part in this.parts)
        {
            if (part.Any(InvalidFileSystemChars.Contains))
            {
                throw new InvalidOperationException($"'{part}' does not supported by current running system.");
            }

            args[i++] = part;
        }

        return Path.Join(args);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ItemName other || this.parts.Count != other.parts.Count)
        {
            return false;
        }

        for (var i = 0; i < this.parts.Count; i++)
        {
            if (this.parts[i] != other.parts[i])
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        return this.parts.Sum(p => p.GetHashCode());
    }

    /// <summary>
    /// Gets the value of this name.
    /// </summary>
    /// <returns>
    /// Value of this name, always non-empty and contains no NUL characters.
    /// </returns>
    public override string ToString()
    {
        if (this.IsDirectory && this.parts.Count > 0)
        {
            return $"./{string.Join('/', this.parts)}/";
        }
        else
        {
            return "./" + string.Join('/', this.parts);
        }
    }
}
