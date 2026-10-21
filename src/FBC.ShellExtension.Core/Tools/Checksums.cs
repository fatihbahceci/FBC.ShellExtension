using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace FBC.ShellExtension.Core.Tools;

public enum ChecksumAlgorithm
{
    Md5,
    Sha1,
    Sha256,
    Sha512,
}

/// <summary>The four checksums of one file, lower-case hex.</summary>
public sealed record FileChecksums(string Md5, string Sha1, string Sha256, string Sha512)
{
    public string Get(ChecksumAlgorithm algorithm) => algorithm switch
    {
        ChecksumAlgorithm.Md5 => Md5,
        ChecksumAlgorithm.Sha1 => Sha1,
        ChecksumAlgorithm.Sha256 => Sha256,
        _ => Sha512,
    };
}

/// <summary>Hashes streams in one pass and reads checksums the way people paste them (ADR-0017).</summary>
public static class Checksums
{
    internal const int BufferSize = 1024 * 1024;

    public static string Name(ChecksumAlgorithm algorithm) => algorithm switch
    {
        ChecksumAlgorithm.Md5 => "MD5",
        ChecksumAlgorithm.Sha1 => "SHA-1",
        ChecksumAlgorithm.Sha256 => "SHA-256",
        _ => "SHA-512",
    };

    /// <summary>The algorithm whose hex digest has this many characters; null for any other length.</summary>
    public static ChecksumAlgorithm? ForHexLength(int length) => length switch
    {
        32 => ChecksumAlgorithm.Md5,
        40 => ChecksumAlgorithm.Sha1,
        64 => ChecksumAlgorithm.Sha256,
        128 => ChecksumAlgorithm.Sha512,
        _ => null,
    };

    /// <summary>All four in one read of the stream; <paramref name="progress"/> gets the bytes read so far.</summary>
    public static FileChecksums ComputeAll(Stream stream, Action<long>? progress, CancellationToken cancellationToken)
    {
        using IncrementalHash md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        using IncrementalHash sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        using IncrementalHash sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using IncrementalHash sha512 = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
        Read(stream, progress, cancellationToken, chunk =>
        {
            md5.AppendData(chunk);
            sha1.AppendData(chunk);
            sha256.AppendData(chunk);
            sha512.AppendData(chunk);
        });

        return new FileChecksums(Hex(md5), Hex(sha1), Hex(sha256), Hex(sha512));
    }

    public static string Compute(Stream stream, ChecksumAlgorithm algorithm, Action<long>? progress, CancellationToken cancellationToken)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashName(algorithm));
        Read(stream, progress, cancellationToken, chunk => hash.AppendData(chunk));
        return Hex(hash);
    }

    /// <summary>
    /// A checksum as copied from a web page or a tool: surrounding text such as "SHA256:" or a
    /// file name is dropped, spaces inside are not allowed. Returns lower-case hex, or null when
    /// the text holds no digest of a known length.
    /// </summary>
    public static string? FindHex(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        string? found = null;
        foreach (string word in text.Split([' ', '\t', '\r', '\n', ':', '=', '(', ')', '*', ',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (ForHexLength(word.Length) is not null && word.All(char.IsAsciiHexDigit))
            {
                // Two different digests: which one was meant cannot be told.
                if (found is not null && !string.Equals(found, word, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                found = word;
            }
        }

        return found?.ToLowerInvariant();
    }

    internal static HashAlgorithmName HashName(ChecksumAlgorithm algorithm) => algorithm switch
    {
        ChecksumAlgorithm.Md5 => HashAlgorithmName.MD5,
        ChecksumAlgorithm.Sha1 => HashAlgorithmName.SHA1,
        ChecksumAlgorithm.Sha256 => HashAlgorithmName.SHA256,
        _ => HashAlgorithmName.SHA512,
    };

    internal static string Hex(IncrementalHash hash) => Convert.ToHexStringLower(hash.GetHashAndReset());

    private static void Read(Stream stream, Action<long>? progress, CancellationToken cancellationToken, Action<ReadOnlySpan<byte>> append)
    {
        byte[] buffer = new byte[BufferSize];
        long done = 0;
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            append(buffer.AsSpan(0, read));
            done += read;
            progress?.Invoke(done);
        }
    }
}

/// <param name="Hash">Lower-case hex.</param>
/// <param name="FileName">As written in the file, relative to its folder; empty for a bare digest, which is for the file the checksum file is named after.</param>
public sealed record ChecksumEntry(string Hash, string FileName, ChecksumAlgorithm Algorithm);

/// <summary>
/// Checksum lists as sha256sum and its kin write them (<c>&lt;hex&gt; *name</c> or
/// <c>&lt;hex&gt;  name</c>), and the BSD form (<c>SHA256 (name) = &lt;hex&gt;</c>).
/// </summary>
public static partial class ChecksumFile
{
    private static readonly string[] s_extensions = [".md5", ".sha1", ".sha256", ".sha512"];
    private static readonly string[] s_names = ["MD5SUMS", "SHA1SUMS", "SHA256SUMS", "SHA512SUMS"];

    /// <summary>True for a file that, by its name, lists checksums of other files.</summary>
    public static bool IsChecksumFileName(string fileName) =>
        s_extensions.Any(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        || s_names.Any(name => string.Equals(fileName, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>The names a checksum file next to <paramref name="fileName"/> may have, most trusted first.</summary>
    public static IEnumerable<string> SidecarNames(string fileName) =>
        new[] { ".sha256", ".sha512", ".sha1", ".md5" }.Select(extension => fileName + extension);

    /// <summary>One line for <paramref name="fileName"/>, in the binary-mode form sha256sum writes.</summary>
    public static string Line(string hash, string fileName) => $"{hash} *{fileName}";

    /// <summary>
    /// Reads the forms in use: GNU (<c>&lt;hex&gt; *name</c>, <c>&lt;hex&gt;  name</c>, and the
    /// <c>\</c>-escaped line it writes for a name holding a backslash), BSD
    /// (<c>SHA256 (name) = &lt;hex&gt;</c>), a bare digest (the whole checksum file of many
    /// downloads, and of FBCTools' own zip), and certutil -hashfile's output. A bare digest has an
    /// empty name: it is for the file the checksum file is named after. Blank lines and comments
    /// are skipped; any other line is counted as unreadable, so a list is never half checked unnoticed.
    /// </summary>
    public static ChecksumList Parse(string text)
    {
        var entries = new List<ChecksumEntry>();
        int unreadable = 0;
        string? certutilName = null;
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] is '#' or ';' || line.StartsWith("CertUtil:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (CertUtilHeader().Match(line) is { Success: true } header)
            {
                certutilName = Path.GetFileName(header.Groups[1].Value.Replace('\\', '/'));
                continue;
            }

            ChecksumEntry? entry = ParseBare(line, certutilName ?? "") ?? ParseGnu(line) ?? ParseBsd(line);
            certutilName = null;
            if (entry is null)
            {
                unreadable++;
            }
            else
            {
                entries.Add(entry);
            }
        }

        return new ChecksumList(entries, unreadable);
    }

    /// <summary>A digest alone on its line; older certutil writes it as byte pairs separated by spaces.</summary>
    private static ChecksumEntry? ParseBare(string line, string name)
    {
        string[] words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1 && !words.All(word => word.Length == 2))
        {
            return null;
        }

        string hash = string.Concat(words);
        return Checksums.ForHexLength(hash.Length) is { } algorithm && hash.All(char.IsAsciiHexDigit)
            ? new ChecksumEntry(hash.ToLowerInvariant(), name, algorithm)
            : null;
    }

    private static ChecksumEntry? ParseGnu(string line)
    {
        bool escaped = line.StartsWith('\\');
        if (escaped)
        {
            line = line[1..];
        }

        int space = line.IndexOf(' ');
        if (space <= 0 || space + 1 >= line.Length)
        {
            return null;
        }

        string hash = line[..space];
        string name = line[(space + 1)..];
        if (name.Length > 0 && name[0] is ' ' or '*')
        {
            name = name[1..];
        }

        return Entry(hash, escaped ? Unescape(name) : name);
    }

    /// <summary>GNU tools write <c>\\</c> for a backslash and <c>\n</c> for a newline in an escaped line.</summary>
    private static string Unescape(string name)
    {
        var text = new System.Text.StringBuilder(name.Length);
        for (int i = 0; i < name.Length; i++)
        {
            if (name[i] == '\\' && i + 1 < name.Length)
            {
                i++;
                text.Append(name[i] switch { 'n' => '\n', 'r' => '\r', _ => name[i] });
            }
            else
            {
                text.Append(name[i]);
            }
        }

        return text.ToString();
    }

    private static ChecksumEntry? ParseBsd(string line)
    {
        int open = line.IndexOf(" (", StringComparison.Ordinal);
        int close = line.LastIndexOf(") = ", StringComparison.Ordinal);
        return open > 0 && close > open
            ? Entry(line[(close + 4)..].Trim(), line[(open + 2)..close])
            : null;
    }

    private static ChecksumEntry? Entry(string hash, string name) =>
        name.Length > 0
        && Checksums.ForHexLength(hash.Length) is { } algorithm
        && hash.All(char.IsAsciiHexDigit)
            ? new ChecksumEntry(hash.ToLowerInvariant(), name, algorithm)
            : null;

    [GeneratedRegex(@"^(?:MD5|SHA1|SHA256|SHA384|SHA512) hash of (.+):$", RegexOptions.IgnoreCase)]
    private static partial Regex CertUtilHeader();
}

/// <param name="Unreadable">Lines in neither form: what they meant to check cannot be.</param>
public sealed record ChecksumList(IReadOnlyList<ChecksumEntry> Entries, int Unreadable);
