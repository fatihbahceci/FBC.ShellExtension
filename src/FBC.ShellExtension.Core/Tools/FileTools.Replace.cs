using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>What Find and replace looks for and writes instead.</summary>
public sealed record ReplaceRules(string Find, string Replace, bool MatchCase, bool WholeWord, bool RegularExpression);

/// <summary>How many times a file holds what is looked for; <see cref="Note"/> says why one is left alone.</summary>
public sealed record ReplaceCount(string Path, int Matches, string? Note);

/// <summary>
/// Find and replace in the content of files (ADR-0029). A file is written back in its own
/// encoding, byte order mark and line endings; only the matches change. A file that is not text,
/// or that cannot be read back exactly as it was, is left alone.
/// </summary>
public static partial class FileTools
{
    /// <summary>A larger file is left alone: it is read whole, and is hardly a text file.</summary>
    public const int MaxReplaceMegabytes = 64;

    // A matched file and its writing are both in memory; the time limit stops a runaway expression.
    private static readonly TimeSpan s_replaceTimeout = TimeSpan.FromSeconds(5);

    // Hidden and system folders (.git) are not searched, nor such files.
    private static readonly EnumerationOptions s_visible = new() { AttributesToSkip = FileAttributes.Hidden | FileAttributes.System, IgnoreInaccessible = false };

    /// <summary>The search as a regular expression; null, with the reason, when it cannot be one.</summary>
    public static Regex? ReplacePattern(ReplaceRules rules, out string? problem)
    {
        problem = null;
        if (rules.Find.Length == 0)
        {
            problem = Strings.ReplaceFindEmpty();
            return null;
        }

        string pattern = rules.RegularExpression ? rules.Find : Regex.Escape(rules.Find);
        // Not \b: next to a search that starts or ends with no letter (".NET", "C++") it never holds.
        if (rules.WholeWord)
        {
            pattern = $@"(?<!\w)(?:{pattern})(?!\w)";
        }

        try
        {
            return new Regex(pattern, RegexOptions.CultureInvariant | (rules.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase), s_replaceTimeout);
        }
        catch (ArgumentException ex)
        {
            problem = Strings.ToolRegexInvalid(ex.Message);
            return null;
        }
    }

    /// <summary>What the matches are replaced with; plain text is taken as it is, a $ too.</summary>
    public static string ReplaceText(ReplaceRules rules) =>
        rules.RegularExpression ? rules.Replace : rules.Replace.Replace("$", "$$", StringComparison.Ordinal);

    /// <summary>
    /// The files to search: the selected files, and in the selected folders those whose name
    /// matches <paramref name="patterns"/>, with <paramref name="subfolders"/> under them too.
    /// Hidden and system files and folders are left out, and links are not entered.
    /// </summary>
    /// <param name="errors">A line for each folder that could not be read.</param>
    /// <exception cref="OperationCanceledException">Cancelled.</exception>
    public static IReadOnlyList<string> ReplaceSources(IReadOnlyList<SelectedItem> items, IReadOnlyList<string> patterns, bool subfolders,
        out List<string> errors, CancellationToken cancellationToken)
    {
        errors = [];
        var files = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (SelectedItem item in items)
        {
            if (item.Kind == ItemKind.File)
            {
                if (seen.Add(item.Path))
                {
                    files.Add(item.Path);
                }

                continue;
            }

            var pending = new Stack<DirectoryInfo>([new DirectoryInfo(item.Path)]);
            while (pending.TryPop(out DirectoryInfo? folder))
            {
                cancellationToken.ThrowIfCancellationRequested();
                FileSystemInfo[] entries;
                try
                {
                    entries = folder.GetFileSystemInfos("*", s_visible);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    errors.Add(Strings.ToolItemFailed(folder.FullName, ex.Message));
                    continue;
                }

                List<FileSystemInfo> visible = [.. entries.Where(entry => !IsLink(entry)).OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)];
                files.AddRange(visible.Where(entry => entry is FileInfo && MatchesAny(entry.Name, patterns)).Select(entry => entry.FullName).Where(seen.Add));

                // Pushed last first, so they come off in the order of their names.
                if (subfolders)
                {
                    foreach (DirectoryInfo subfolder in visible.OfType<DirectoryInfo>().Reverse())
                    {
                        pending.Push(subfolder);
                    }
                }
            }
        }

        return files;
    }

    /// <summary>How many times <paramref name="path"/> holds a match, or why it is left alone.</summary>
    public static ReplaceCount CountMatches(string path, Regex pattern, Encoding ansi)
    {
        try
        {
            if (ReadForReplace(path, ansi, out string? note) is not { } text)
            {
                return new ReplaceCount(path, 0, note);
            }

            return new ReplaceCount(path, pattern.Count(text.Text), null);
        }
        catch (RegexMatchTimeoutException)
        {
            return new ReplaceCount(path, 0, Strings.ToolRegexTimeout());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ReplaceCount(path, 0, ex.Message);
        }
    }

    /// <summary>
    /// Replaces every match in <paramref name="path"/>, written back in the file's own encoding
    /// through a new file that takes its place, so the file is never left half written.
    /// </summary>
    /// <param name="backup">Keeps the old file next to it, as <c>name.bak</c> (or <c>name (2).bak</c> …).</param>
    /// <returns>How many matches were replaced.</returns>
    /// <exception cref="ToolFailedException">The file is not text, cannot hold the replacement, or the search took too long; it is unchanged.</exception>
    /// <exception cref="IOException">Reading or writing failed; the file is unchanged.</exception>
    /// <exception cref="UnauthorizedAccessException">Windows refused (a read-only file); the file is unchanged.</exception>
    public static int ReplaceInFile(string path, Regex pattern, string replacement, Encoding ansi, bool backup)
    {
        if (ReadForReplace(path, ansi, out string? note) is not { } text)
        {
            throw new ToolFailedException(Strings.ToolItemFailed(path, note ?? ""));
        }

        int matches;
        string replaced;
        try
        {
            matches = pattern.Count(text.Text);
            replaced = pattern.Replace(text.Text, replacement);
        }
        catch (RegexMatchTimeoutException)
        {
            throw new ToolFailedException(Strings.ToolItemFailed(path, Strings.ToolRegexTimeout()));
        }

        if (matches == 0)
        {
            return 0;
        }

        byte[] body;
        try
        {
            body = text.Encoding.GetBytes(replaced);
        }
        catch (EncoderFallbackException)
        {
            throw new ToolFailedException(Strings.ToolItemFailed(path, Strings.ReplaceUnmappable()));
        }

        string folder = Path.GetDirectoryName(path) ?? "";
        string name = Path.GetFileName(path);
        string temporary = Path.Combine(folder, string.Create(CultureInfo.InvariantCulture, $"{name}.{Guid.NewGuid():N}.fbctools-tmp"));

        // Always a backup name: without one, a failed rename of the new file leaves the old one
        // deleted and the new one under its temporary name (ReplaceFile's documentation).
        string old = backup
            ? Path.Combine(folder, FreeItemName(folder, name + ".bak", isFile: true))
            : Path.Combine(folder, string.Create(CultureInfo.InvariantCulture, $"{name}.{Guid.NewGuid():N}.fbctools-old"));
        bool written = false;
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
            {
                stream.Write(text.Preamble);
                stream.Write(body);
            }

            // Replace keeps the file's attributes, creation time and permissions, as an editor's save does.
            File.Replace(temporary, path, old);
            written = true;
        }
        finally
        {
            if (!written)
            {
                PutBack(path, old, temporary);
            }
        }

        if (!backup)
        {
            DeleteQuietly([old]);
        }

        return matches;
    }

    /// <summary>
    /// After a failed replace, the file under its own name again: ReplaceFile can stop half way
    /// with the old file under the backup's name, or with only the new one left.
    /// </summary>
    private static void PutBack(string path, string old, string temporary)
    {
        try
        {
            if (!File.Exists(path))
            {
                File.Move(File.Exists(old) ? old : temporary, path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Left where it is, under a name that says what it is; the failure reported is the replace's.
            return;
        }

        DeleteQuietly([temporary]);
    }

    /// <summary>A file's text, the encoding to write it back with, and its byte order mark.</summary>
    private sealed record TextFile(string Text, Encoding Encoding, byte[] Preamble);

    /// <summary>
    /// The text, as Notepad tells the encoding: a byte order mark, else UTF-8 if it is valid,
    /// else the Windows code page. Null, with the reason, for a file too large, not text (a NUL
    /// without a byte order mark), or one whose bytes would not come back the same.
    /// </summary>
    private static TextFile? ReadForReplace(string path, Encoding ansi, out string? note)
    {
        note = null;
        var file = new FileInfo(path);

        // Written back, a link would become a file of its own and the file it shows would stay as it was.
        if (file.LinkTarget is not null)
        {
            note = Strings.ReplaceLink();
            return null;
        }

        if (file.Length > MaxReplaceMegabytes * 1024L * 1024L)
        {
            note = Strings.ReplaceTooLarge(MaxReplaceMegabytes);
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        (Encoding? encoding, int preamble) = bytes switch
        {
            [0xEF, 0xBB, 0xBF, ..] => ((Encoding?)s_strictUtf8, 3),
            [0xFF, 0xFE, 0x00, 0x00, ..] => (new UTF32Encoding(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true), 4),
            [0xFF, 0xFE, ..] => (new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true), 2),
            [0xFE, 0xFF, ..] => (new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true), 2),
            _ => (null, 0),
        };

        if (encoding is null && bytes.AsSpan().Contains((byte)0))
        {
            note = Strings.ReplaceNotText();
            return null;
        }

        try
        {
            if (encoding is not null)
            {
                return new TextFile(encoding.GetString(bytes, preamble, bytes.Length - preamble), encoding, bytes[..preamble]);
            }

            return new TextFile(s_strictUtf8.GetString(bytes), s_strictUtf8, []);
        }
        catch (DecoderFallbackException) when (encoding is null)
        {
            // Not UTF-8: the code page, which must give the same bytes back.
            var codePage = (Encoding)ansi.Clone();
            codePage.EncoderFallback = EncoderFallback.ExceptionFallback;
            codePage.DecoderFallback = DecoderFallback.ExceptionFallback;
            try
            {
                string text = codePage.GetString(bytes);
                if (codePage.GetBytes(text).AsSpan().SequenceEqual(bytes))
                {
                    return new TextFile(text, codePage, []);
                }
            }
            catch (Exception ex) when (ex is DecoderFallbackException or EncoderFallbackException)
            {
                // Falls through: not the code page either.
            }

            note = Strings.ReplaceNotText();
            return null;
        }
        catch (DecoderFallbackException)
        {
            note = Strings.ReplaceNotText();
            return null;
        }
    }
}
