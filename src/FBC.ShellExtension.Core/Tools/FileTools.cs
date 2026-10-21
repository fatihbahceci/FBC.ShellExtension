using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>Where a windowed tool reports while it works on one item; called from a worker thread.</summary>
public interface IToolReport
{
    /// <summary>A line for the runner's log.</summary>
    void Output(string line);

    void Progress(long done, long total);
}

/// <summary>The item did not succeed; the message says why, in the user's terms.</summary>
public sealed class ToolFailedException(string message) : Exception(message);

/// <summary>
/// The tools that run in the runner's progress window, one item at a time (ADR-0017). They work
/// on real files through System.IO, so the tests run them in a temporary folder. Files are read
/// shared, as Notepad does; parts and joined files are created new and never overwrite.
/// </summary>
public static partial class FileTools
{
    private const string SidecarExtension = ".sha256";

    /// <summary>
    /// The items a tool runs on. Join takes each set of parts once, by its first part, whichever
    /// part was selected; every other tool takes the items as they are.
    /// </summary>
    public static IReadOnlyList<SelectedItem> Targets(BuiltinTool tool, IReadOnlyList<SelectedItem> items)
    {
        if (tool != BuiltinTool.Join)
        {
            return items;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var targets = new List<SelectedItem>();
        foreach (SelectedItem item in items)
        {
            SelectedItem target = PartNumber(item.Path) is { } part ? item with { Path = PartPath(part.Base, 1, part.Width) } : item;
            if (seen.Add(target.Path))
            {
                targets.Add(target);
            }
        }

        return targets;
    }

    /// <summary>Runs <paramref name="tool"/> on one item; <paramref name="partSize"/> is for split.</summary>
    /// <exception cref="ToolFailedException">The item failed for a reason the tool knows.</exception>
    /// <exception cref="IOException">Reading or writing failed.</exception>
    /// <exception cref="UnauthorizedAccessException">Windows refused access.</exception>
    /// <exception cref="OperationCanceledException">The user cancelled; whatever the tool had written is removed.</exception>
    public static void Run(BuiltinTool tool, SelectedItem item, ToolInput input, IToolReport report, CancellationToken cancellationToken)
    {
        if (!BuiltinTools.Accepts(tool, item.Kind))
        {
            throw new ToolFailedException(WrongKind(tool, item));
        }

        switch (tool)
        {
            case BuiltinTool.Checksum:
                Checksum(item.Path, report, cancellationToken);
                break;
            case BuiltinTool.VerifyChecksum:
                Verify(item.Path, input, report, cancellationToken);
                break;
            case BuiltinTool.Split:
                Split(item.Path, input.PartSize, report, cancellationToken);
                break;
            case BuiltinTool.Join:
                Join(item.Path, input.TextEncoding, report, cancellationToken);
                break;
            case BuiltinTool.CountLines:
                CountLines(item.Path, report, cancellationToken);
                break;
            case BuiltinTool.Duplicate:
                Duplicate(item.Path, input.Copier ?? StreamCopier.Instance, report, cancellationToken);
                break;
            case BuiltinTool.FolderSize:
                FolderSize(item.Path, report, cancellationToken);
                break;
            case BuiltinTool.DeletePermanently:
                DeletePermanently(item.Path, cancellationToken);
                break;
            case BuiltinTool.DeleteEmptyFolders:
                DeleteEmptyFolders(item.Path, input.Confirm, report, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(tool), tool, null);
        }
    }

    // ---- checksum and verify --------------------------------------------------------------

    private static void Checksum(string path, IToolReport report, CancellationToken cancellationToken)
    {
        using FileStream stream = OpenRead(path);
        long total = stream.Length;
        FileChecksums checksums = Checksums.ComputeAll(stream, done => report.Progress(done, total), cancellationToken);
        foreach (ChecksumAlgorithm algorithm in Enum.GetValues<ChecksumAlgorithm>())
        {
            report.Output($"{Checksums.Name(algorithm),-8} {checksums.Get(algorithm)}");
        }
    }

    /// <summary>
    /// A checksum file is checked line by line. Any other file is checked against a checksum
    /// file next to it (<c>name.sha256</c> …), or else against a checksum on the clipboard.
    /// </summary>
    private static void Verify(string path, ToolInput input, IToolReport report, CancellationToken cancellationToken)
    {
        string name = Path.GetFileName(path);
        if (ChecksumFile.IsChecksumFileName(name))
        {
            VerifyList(path, input.TextEncoding, report, cancellationToken);
            return;
        }

        string folder = Path.GetDirectoryName(path) ?? "";
        foreach (string sidecarName in ChecksumFile.SidecarNames(name))
        {
            string sidecar = Path.Combine(folder, sidecarName);
            if (File.Exists(sidecar) && EntryFor(ReadText(sidecar, input.TextEncoding), name) is { } entry)
            {
                Compare(path, entry.Algorithm, entry.Hash, sidecarName, report, cancellationToken);
                return;
            }
        }

        if (Checksums.FindHex(input.Clipboard) is { } expected)
        {
            Compare(path, Checksums.ForHexLength(expected.Length)!.Value, expected, Strings.ToolVerifyClipboard(), report, cancellationToken);
            return;
        }

        throw new ToolFailedException(Strings.ToolVerifyNothing());
    }

    private static void Compare(string path, ChecksumAlgorithm algorithm, string expected, string source, IToolReport report, CancellationToken cancellationToken)
    {
        string actual = Hash(path, algorithm, report, cancellationToken);
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new ToolFailedException(Strings.ToolVerifyMismatch(Checksums.Name(algorithm), source, expected, actual));
        }

        report.Output(Strings.ToolVerifyMatches(Checksums.Name(algorithm), source));
    }

    private static void VerifyList(string path, Encoding ansi, IToolReport report, CancellationToken cancellationToken)
    {
        ChecksumList list = ChecksumFile.Parse(ReadText(path, ansi));
        IReadOnlyList<ChecksumEntry> entries = list.Entries;
        if (entries.Count == 0)
        {
            throw new ToolFailedException(Strings.ToolVerifyListEmpty(Path.GetFileName(path)));
        }

        string folder = Path.GetDirectoryName(path) ?? "";
        int bad = 0;
        foreach (ChecksumEntry listed in entries)
        {
            // A bare digest is for the file this one is named after: abc.zip.sha256 → abc.zip.
            ChecksumEntry entry = listed.FileName.Length > 0 ? listed : listed with { FileName = Path.GetFileNameWithoutExtension(path) };

            // A name may hold a subfolder, written with either slash.
            string target = Path.Combine(folder, entry.FileName.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(target))
            {
                report.Output(Strings.ToolVerifyEntryMissing(entry.FileName));
                bad++;
            }
            else if (!string.Equals(Hash(target, entry.Algorithm, report, cancellationToken), entry.Hash, StringComparison.OrdinalIgnoreCase))
            {
                report.Output(Strings.ToolVerifyEntryMismatch(entry.FileName));
                bad++;
            }
            else
            {
                report.Output(Strings.ToolVerifyEntryMatch(entry.FileName));
            }
        }

        if (list.Unreadable > 0)
        {
            report.Output(Strings.ToolVerifyListUnreadable(list.Unreadable));
        }

        if (bad > 0)
        {
            throw new ToolFailedException(Strings.ToolVerifyListFailed(bad, entries.Count));
        }

        if (list.Unreadable > 0)
        {
            throw new ToolFailedException(Strings.ToolVerifyListUnreadable(list.Unreadable));
        }

        report.Output(Strings.ToolVerifyListPassed(entries.Count));
    }

    /// <summary>The entry for <paramref name="fileName"/>, or the only entry of a one-line file (a bare digest too).</summary>
    private static ChecksumEntry? EntryFor(string text, string fileName)
    {
        IReadOnlyList<ChecksumEntry> entries = ChecksumFile.Parse(text).Entries;
        return entries.FirstOrDefault(entry => string.Equals(Path.GetFileName(entry.FileName.Replace('\\', '/')), fileName, StringComparison.OrdinalIgnoreCase))
            ?? (entries.Count == 1 ? entries[0] : null);
    }

    private static string Hash(string path, ChecksumAlgorithm algorithm, IToolReport report, CancellationToken cancellationToken)
    {
        using FileStream stream = OpenRead(path);
        long total = stream.Length;
        return Checksums.Compute(stream, algorithm, done => report.Progress(done, total), cancellationToken);
    }

    // ---- split and join -------------------------------------------------------------------

    /// <summary>
    /// <c>name.001</c>, <c>name.002</c> … <c>name.999</c>, <c>name.1000</c>, as 7-Zip writes them:
    /// the first part is always <c>.001</c>, which the template's join matches. The SHA-256 of the
    /// whole file goes to <c>name.sha256</c> for join to check.
    /// </summary>
    private static void Split(string path, long partSize, IToolReport report, CancellationToken cancellationToken)
    {
        if (partSize < SizeText.Minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(partSize), partSize, null);
        }

        using FileStream source = OpenRead(path);
        long total = source.Length;
        if (total <= partSize)
        {
            throw new ToolFailedException(Strings.ToolSplitTooSmall(SizeText.Format(partSize)));
        }

        int count = (int)((total + partSize - 1) / partSize);
        List<string> parts = [.. Enumerable.Range(1, count).Select(number => PartPath(path, number, PartWidth))];
        string sidecar = path + SidecarExtension;
        if (parts.Append(sidecar).FirstOrDefault(File.Exists) is { } existing)
        {
            throw new ToolFailedException(Strings.ToolAlreadyExists(Path.GetFileName(existing)));
        }

        var written = new List<string>();
        try
        {
            using IncrementalHash sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            byte[] buffer = new byte[Checksums.BufferSize];
            long done = 0;
            foreach (string part in parts)
            {
                // Listed for removal only once it is ours: one that appeared since the check is not.
                using FileStream target = new(part, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                written.Add(part);
                long left = Math.Min(partSize, total - done);
                while (left > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, left));
                    if (read == 0)
                    {
                        // The file shrank while it was being split.
                        throw new IOException(Strings.ToolFileChanged(Path.GetFileName(path)));
                    }

                    target.Write(buffer, 0, read);
                    sha256.AppendData(buffer, 0, read);
                    left -= read;
                    done += read;
                    report.Progress(done, total);
                }
            }

            // Opened for sharing writes: a file that grew while it was split was not split whole.
            if (source.ReadByte() >= 0)
            {
                throw new IOException(Strings.ToolFileChanged(Path.GetFileName(path)));
            }

            using FileStream checksum = new(sidecar, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            written.Add(sidecar);
            checksum.Write(Encoding.UTF8.GetBytes(ChecksumFile.Line(Checksums.Hex(sha256), Path.GetFileName(path)) + "\n"));
        }
        catch
        {
            DeleteQuietly(written);
            throw;
        }

        report.Output(Strings.ToolSplitDone(count, SizeText.Format(partSize), Path.GetFileName(parts[0]), Path.GetFileName(parts[^1])));
        report.Output(Strings.ToolSplitChecksum(Path.GetFileName(sidecar)));
    }

    /// <summary>
    /// Joins the parts that follow on from <paramref name="firstPart"/> into the name without the
    /// number, then checks it against <c>name.sha256</c> when there is one.
    /// </summary>
    private static void Join(string firstPart, Encoding ansi, IToolReport report, CancellationToken cancellationToken)
    {
        if (PartNumber(firstPart) is not { } first)
        {
            throw new ToolFailedException(Strings.ToolJoinNotAPart(Path.GetFileName(firstPart)));
        }

        var parts = new List<string>();
        for (int number = 1; File.Exists(PartPath(first.Base, number, first.Width)); number++)
        {
            parts.Add(PartPath(first.Base, number, first.Width));
        }

        if (parts.Count == 0)
        {
            throw new ToolFailedException(Strings.ToolJoinFirstMissing(Path.GetFileName(PartPath(first.Base, 1, first.Width))));
        }

        // A part missing in the middle would make a file that looks whole and is not.
        if (LaterPartExists(first.Base, parts.Count + 1))
        {
            throw new ToolFailedException(Strings.ToolJoinGap(Path.GetFileName(PartPath(first.Base, parts.Count + 1, first.Width))));
        }

        string target = first.Base;
        if (File.Exists(target) || Directory.Exists(target))
        {
            throw new ToolFailedException(Strings.ToolAlreadyExists(Path.GetFileName(target)));
        }

        long total = parts.Sum(part => new FileInfo(part).Length);
        string hash;
        bool created = false;
        try
        {
            using IncrementalHash sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using (FileStream output = new(target, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                created = true;
                byte[] buffer = new byte[Checksums.BufferSize];
                long done = 0;
                foreach (string part in parts)
                {
                    using FileStream input = OpenRead(part);
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        output.Write(buffer, 0, read);
                        sha256.AppendData(buffer, 0, read);
                        done += read;
                        report.Progress(done, total);
                    }
                }
            }

            hash = Checksums.Hex(sha256);
        }
        catch
        {
            // A target that appeared since the check is not ours to delete.
            if (created)
            {
                DeleteQuietly([target]);
            }

            throw;
        }

        report.Output(Strings.ToolJoinDone(parts.Count, Path.GetFileName(target), SizeText.Format(total)));
        string sidecar = target + SidecarExtension;
        if (!File.Exists(sidecar) || EntryFor(ReadText(sidecar, ansi), Path.GetFileName(target)) is not { Algorithm: ChecksumAlgorithm.Sha256 } entry)
        {
            report.Output(Strings.ToolJoinNotVerified());
            return;
        }

        if (!string.Equals(entry.Hash, hash, StringComparison.OrdinalIgnoreCase))
        {
            // Kept: it is the user's to look at, and the parts are still there.
            throw new ToolFailedException(Strings.ToolJoinMismatch(Path.GetFileName(sidecar)));
        }

        report.Output(Strings.ToolVerifyMatches(Checksums.Name(ChecksumAlgorithm.Sha256), Path.GetFileName(sidecar)));
    }

    private static bool LaterPartExists(string basePath, int missing)
    {
        string folder = Path.GetDirectoryName(basePath) ?? "";
        string prefix = Path.GetFileName(basePath) + ".";
        return Directory.EnumerateFiles(folder, prefix + "*")
            .Select(file => PartNumber(file))
            .Any(part => part is { } p && string.Equals(p.Base, basePath, StringComparison.OrdinalIgnoreCase) && p.Number > missing);
    }

    private const int PartWidth = 3;

    private readonly record struct Part(string Base, int Number, int Width);

    /// <summary>
    /// <c>C:\a\film.mp4.002</c> → base <c>C:\a\film.mp4</c>, number 2, width 3; null for any
    /// other name. Numbers are padded to three digits and grow past them (<c>.1000</c>), as 7-Zip
    /// writes them; a set padded wider, such as <c>.0001</c>, keeps its width.
    /// </summary>
    private static Part? PartNumber(string path)
    {
        Match match = PartPattern().Match(path);
        string digits = match.Groups[2].Value;
        return match.Success && int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int number) && number > 0
            ? new Part(match.Groups[1].Value, number, digits.StartsWith('0') ? digits.Length : PartWidth)
            : null;
    }

    private static string PartPath(string basePath, int number, int width) =>
        basePath + "." + number.ToString(new string('0', width), CultureInfo.InvariantCulture);

    [GeneratedRegex(@"^(.+[^\\/])\.(\d{3,9})$")]
    private static partial Regex PartPattern();

    // ---- count lines ----------------------------------------------------------------------

    /// <summary>
    /// Counts line ends: LF, CRLF or a lone CR, plus a last line that has none, as PowerShell's
    /// Get-Content does. UTF-16 (with its byte order mark) is counted in characters, anything
    /// else in bytes, so the file is never decoded as a whole.
    /// </summary>
    private static void CountLines(string path, IToolReport report, CancellationToken cancellationToken)
    {
        using FileStream stream = OpenRead(path);
        long total = stream.Length;
        long lines = LineCounter.Count(stream, done => report.Progress(done, total), cancellationToken);
        report.Output(Strings.ToolLineCount(lines.ToString("N0", CultureInfo.InvariantCulture)));
    }

    // ---- duplicate, folder size, append ---------------------------------------------------

    /// <summary>
    /// Copies the file next to itself as "name - Copy.ext", then "name - Copy (2).ext" …, the
    /// way Explorer names a copy pasted into the same folder. The copier decides what comes
    /// along: on Windows, CopyFileEx keeps what Explorer's copy keeps.
    /// </summary>
    private static void Duplicate(string path, IFileCopier copier, IToolReport report, CancellationToken cancellationToken)
    {
        string folder = Path.GetDirectoryName(path) ?? "";
        string name = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        if (name.Length == 0)
        {
            // ".gitignore" has no name before its extension.
            (name, extension) = (extension, "");
        }

        string target = Enumerable.Range(1, int.MaxValue - 1)
            .Select(number => Path.Combine(folder, number == 1
                ? $"{name} - Copy{extension}"
                : string.Create(CultureInfo.InvariantCulture, $"{name} - Copy ({number}){extension}")))
            .First(candidate => !File.Exists(candidate) && !Directory.Exists(candidate));

        copier.Copy(path, target, report.Progress, cancellationToken);
        report.Output(Strings.ToolDuplicateDone(Path.GetFileName(target)));
    }

    /// <summary>
    /// The size of every file under the folder, as Explorer's Properties adds it up, hidden and
    /// system files included. A junction or symbolic link to a folder is counted as a folder but
    /// not entered: it would count another place, or loop.
    /// </summary>
    private static void FolderSize(string path, IToolReport report, CancellationToken cancellationToken)
    {
        var options = new EnumerationOptions { AttributesToSkip = 0, IgnoreInaccessible = false, RecurseSubdirectories = false };
        long bytes = 0;
        long files = 0;
        long folders = 0;
        int unreadable = 0;
        var pending = new Stack<string>([path]);
        while (pending.TryPop(out string? folder))
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<FileSystemInfo> entries;
            try
            {
                entries = [.. new DirectoryInfo(folder).EnumerateFileSystemInfos("*", options)];
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                unreadable++;
                continue;
            }

            foreach (FileSystemInfo entry in entries)
            {
                if (entry is FileInfo file)
                {
                    files++;
                    bytes += file.Length;
                }
                else
                {
                    folders++;
                    if (!IsLink(entry))
                    {
                        pending.Push(entry.FullName);
                    }
                }
            }
        }

        report.Output(Strings.ToolFolderSizeResult(
            SizeText.Format(bytes),
            bytes.ToString("N0", CultureInfo.InvariantCulture),
            files.ToString("N0", CultureInfo.InvariantCulture),
            folders.ToString("N0", CultureInfo.InvariantCulture)));
        if (unreadable > 0)
        {
            report.Output(Strings.ToolFolderSizeUnreadable(unreadable));
        }
    }

    /// <summary>
    /// A junction or a symbolic link: entering it would count another place, or loop. Other
    /// reparse points are ordinary folders to the user: OneDrive's on-demand folders are some.
    /// </summary>
    private static bool IsLink(FileSystemInfo entry)
    {
        if (!entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            return false;
        }

        try
        {
            return entry.LinkTarget is not null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Unreadable as a link: not entered, rather than counted twice.
            return true;
        }
    }

    /// <summary>
    /// Adds <paramref name="text"/> at the end of a text file, on a line of its own, in the
    /// file's own encoding: its byte order mark, else UTF-8 when the file reads as UTF-8 (or is
    /// empty), else the Windows code page. Nothing else in the file changes.
    /// </summary>
    /// <exception cref="ToolFailedException">The file does not look like text.</exception>
    public static void AppendText(string path, string text, Encoding ansi)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        byte[] head = new byte[(int)Math.Min(stream.Length, AppendProbeBytes)];
        stream.ReadExactly(head);
        Encoding encoding = AppendEncoding(head, stream.Length, ansi)
            ?? throw new ToolFailedException(Strings.ToolNotText(path));
        if (ReferenceEquals(encoding, ansi))
        {
            // The Windows code page cannot hold every character; one it cannot is refused, not
            // written as "?".
            var strict = (Encoding)ansi.Clone();
            strict.EncoderFallback = EncoderFallback.ExceptionFallback;
            encoding = strict;
        }

        // The text starts a line of its own, unless the file is empty or already ends one.
        int unit = encoding is UnicodeEncoding ? 2 : 1;
        int mark = encoding is UnicodeEncoding ? 2 : head.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]) ? 3 : 0;
        string lineBreak = "";
        if (stream.Length >= mark + unit)
        {
            byte[] last = new byte[unit];
            stream.Seek(-unit, SeekOrigin.End);
            stream.ReadExactly(last);
            lineBreak = encoding.GetString(last) is "\n" or "\r" ? "" : "\r\n";
        }

        byte[] bytes;
        try
        {
            bytes = encoding.GetBytes(lineBreak + text);
        }
        catch (EncoderFallbackException)
        {
            throw new ToolFailedException(Strings.ToolAppendUnmappable(path));
        }

        stream.Seek(0, SeekOrigin.End);
        stream.Write(bytes);
    }

    private const int AppendProbeBytes = 1024 * 1024;

    private static Encoding? AppendEncoding(byte[] head, long length, Encoding ansi)
    {
        if (head.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xFF, 0xFE]))
        {
            return Encoding.Unicode;
        }

        if (head.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xFE, 0xFF]))
        {
            return Encoding.BigEndianUnicode;
        }

        if (head.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]))
        {
            // The mark decides, as it does for Notepad, even if a stray byte further on is not UTF-8.
            return s_utf8;
        }

        if (head.Contains((byte)0))
        {
            return null;
        }

        // A sample cut in the middle of a character would read as invalid; up to three bytes of it are dropped.
        ReadOnlySpan<byte> sample = head;
        for (int cut = 0; cut <= 3 && sample.Length > 0; cut++)
        {
            try
            {
                s_strictUtf8.GetCharCount(sample);
                return s_utf8;
            }
            catch (DecoderFallbackException) when (head.Length < length && cut < 3)
            {
                sample = sample[..^1];
            }
            catch (DecoderFallbackException)
            {
                break;
            }
        }

        return sample.Length == 0 ? s_utf8 : ansi;
    }

    private static readonly UTF8Encoding s_strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false);

    // ---- organize (ADR-0019) -------------------------------------------------------------

    /// <summary>
    /// Every entry, hidden and system ones too; a folder that cannot be listed throws rather than
    /// looking empty, which is what IgnoreInaccessible's default would make it.
    /// </summary>
    private static readonly EnumerationOptions s_everything = new() { AttributesToSkip = 0, IgnoreInaccessible = false };

    /// <summary>Why a tool cannot take this kind of item, in the user's terms.</summary>
    private static string WrongKind(BuiltinTool tool, SelectedItem item) =>
        BuiltinTools.Accepts(tool, ItemKind.File) ? Strings.ToolNotAFile(item.Path)
        : BuiltinTools.Accepts(tool, ItemKind.Folder) ? Strings.ToolNotAFolder(item.Path)
        : Strings.ToolWrongKind(item.Path);

    /// <summary>Why <paramref name="name"/> cannot be a new item in <paramref name="folder"/>; null when it can.</summary>
    public static string? NameProblem(string folder, string name)
    {
        if (!WindowsPath.IsValidName(name))
        {
            return Strings.ToolNameInvalid(name);
        }

        string path = Path.Combine(folder, name);
        return File.Exists(path) || Directory.Exists(path) ? Strings.ToolNameTaken(name) : null;
    }

    /// <summary><paramref name="name"/>, or "name (2)", "name (3)" … when it is taken, as Explorer numbers a new folder.</summary>
    public static string FreeName(string folder, string name) =>
        Enumerable.Range(1, int.MaxValue - 1)
            .Select(number => number == 1 ? name : string.Create(CultureInfo.InvariantCulture, $"{name} ({number})"))
            .First(candidate => NameProblem(folder, candidate) is null || !WindowsPath.IsValidName(candidate));

    /// <summary>Makes <paramref name="name"/> in <paramref name="folder"/>.</summary>
    /// <exception cref="ToolFailedException">The name is not valid, or taken.</exception>
    public static string NewFolder(string folder, string name)
    {
        if (NameProblem(folder, name) is { } problem)
        {
            throw new ToolFailedException(problem);
        }

        return Directory.CreateDirectory(Path.Combine(folder, name)).FullName;
    }

    /// <summary>
    /// Makes <paramref name="name"/> in <paramref name="folder"/> and moves the items into it.
    /// An item that cannot be moved stays where it was; the new folder goes again if it stayed empty.
    /// </summary>
    /// <returns>A line for every item that could not be moved.</returns>
    /// <exception cref="ToolFailedException">The name is not valid, or taken.</exception>
    public static IReadOnlyList<string> PackInto(IReadOnlyList<SelectedItem> items, string folder, string name)
    {
        string target = NewFolder(folder, name);
        var errors = new List<string>();
        foreach (SelectedItem item in items)
        {
            string destination = Path.Combine(target, Path.GetFileName(item.Path));
            try
            {
                if (item.Kind == ItemKind.Folder)
                {
                    Directory.Move(item.Path, destination);
                }
                else if (item.Kind == ItemKind.File)
                {
                    File.Move(item.Path, destination);
                }
                else
                {
                    errors.Add(Strings.ToolWrongKind(item.Path));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add(Strings.ToolItemFailed(item.Path, ex.Message));
            }
        }

        if (!Directory.EnumerateFileSystemEntries(target).Any())
        {
            Directory.Delete(target);
        }

        return errors;
    }

    /// <summary>
    /// Moves everything in <paramref name="folder"/> up into its parent and removes it. Nothing
    /// moves when a name is taken there. A child named like the folder itself is why the folder
    /// is first renamed aside.
    /// </summary>
    /// <exception cref="ToolFailedException">A name is taken, or the folder is a link.</exception>
    public static void Unpack(string folder) => Unpack(folder, Move);

    /// <param name="move">Moves one item; the tests pass one that fails, to see everything put back.</param>
    internal static void Unpack(string folder, Action<string, string> move)
    {
        var info = new DirectoryInfo(folder);
        if (IsLink(info))
        {
            // Its contents live elsewhere: moving them out would empty another folder.
            throw new ToolFailedException(Strings.ToolUnpackLink(info.Name));
        }

        string parent = info.Parent?.FullName ?? throw new ToolFailedException(Strings.ToolWrongKind(folder));
        string[] names = [.. info.EnumerateFileSystemInfos("*", s_everything).Select(entry => entry.Name)];
        string[] taken = [.. names.Where(name =>
            !string.Equals(name, info.Name, StringComparison.OrdinalIgnoreCase) && Path.Exists(Path.Combine(parent, name)))];
        if (taken.Length > 0)
        {
            throw new ToolFailedException(Strings.ToolUnpackTaken(info.Name, string.Join(", ", taken.Take(5)) + (taken.Length > 5 ? ", …" : "")));
        }

        string work = folder;
        if (names.Any(name => string.Equals(name, info.Name, StringComparison.OrdinalIgnoreCase)))
        {
            work = Path.Combine(parent, $"{info.Name}.unpacking-{Guid.NewGuid():N}");
            Directory.Move(folder, work);
        }

        var moved = new List<string>();
        try
        {
            foreach (string name in names)
            {
                move(Path.Combine(work, name), Path.Combine(parent, name));
                moved.Add(name);
            }

            // A customized folder carries the read-only attribute, which would stop its removal.
            var emptied = new DirectoryInfo(work);
            emptied.Attributes &= ~FileAttributes.ReadOnly;
            emptied.Delete();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // All or nothing: what moved goes back, and the folder gets its name back.
            for (int i = moved.Count - 1; i >= 0; i--)
            {
                TryMove(Path.Combine(parent, moved[i]), Path.Combine(work, moved[i]));
            }

            if (work != folder)
            {
                TryMove(work, folder);
            }

            throw new ToolFailedException(Strings.ToolUnpackUndone(info.Name, ex.Message));
        }

        static void TryMove(string source, string destination)
        {
            try
            {
                Move(source, destination);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Left where it is; the error says what happened to the rest.
            }
        }
    }

    private static void Move(string source, string destination)
    {
        if (Directory.Exists(source))
        {
            Directory.Move(source, destination);
        }
        else
        {
            File.Move(source, destination);
        }
    }

    /// <summary>
    /// The folders under <paramref name="root"/> that hold nothing, or only such folders; the
    /// deepest first, so they can be removed in this order. The root itself is not among them.
    /// A link, a file of any kind or a folder that cannot be read makes its folder not empty. So
    /// does a hidden or system folder, which is not entered: such empty folders are often part
    /// of a structure (a git repository's .git, Windows' own folders) that breaks without them.
    /// </summary>
    public static IReadOnlyList<string> FindEmptyFolders(string root, CancellationToken cancellationToken)
    {
        var empty = new List<string>();
        IsEmpty(new DirectoryInfo(root), isRoot: true);
        return empty;

        bool IsEmpty(DirectoryInfo folder, bool isRoot)
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<FileSystemInfo> entries;
            try
            {
                entries = [.. folder.EnumerateFileSystemInfos("*", s_everything)];
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }

            bool result = true;
            foreach (FileSystemInfo entry in entries)
            {
                if (entry is not DirectoryInfo child
                    || (child.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0
                    || IsLink(child)
                    || !IsEmpty(child, isRoot: false))
                {
                    result = false;
                }
            }

            if (result && !isRoot)
            {
                empty.Add(folder.FullName);
            }

            return result;
        }
    }

    /// <summary>Removes folders that must be empty, in the order given; one that is not any more stays.</summary>
    /// <returns>A line for every folder that could not be removed.</returns>
    public static IReadOnlyList<string> DeleteFolders(IEnumerable<string> folders)
    {
        var errors = new List<string>();
        foreach (string folder in folders)
        {
            try
            {
                var info = new DirectoryInfo(folder);
                info.Attributes &= ~FileAttributes.ReadOnly;
                info.Delete();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add(Strings.ToolItemFailed(folder, ex.Message));
            }
        }

        return errors;
    }

    /// <summary>
    /// Lists the empty folders under <paramref name="root"/> in the window, then removes them if
    /// <paramref name="confirm"/> agrees. Without a confirmation nothing is removed.
    /// </summary>
    private static void DeleteEmptyFolders(string root, Func<string, bool>? confirm, IToolReport report, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> empty = FindEmptyFolders(root, cancellationToken);
        if (empty.Count == 0)
        {
            report.Output(Strings.NoEmptyFolders(Path.GetFileName(root.TrimEnd('\\', '/')) is { Length: > 0 } name ? name : root));
            return;
        }

        foreach (string folder in empty)
        {
            report.Output(Path.GetRelativePath(root, folder));
        }

        string list = string.Join("\n", empty.Take(15).Select(folder => Path.GetRelativePath(root, folder)))
            + (empty.Count > 15 ? "\n" + Strings.ConfirmListMore(empty.Count - 15) : "");
        if (confirm?.Invoke(Strings.ConfirmDeleteEmpty(empty.Count, list)) != true)
        {
            report.Output(Strings.ToolNothingRemoved());
            return;
        }

        IReadOnlyList<string> errors = DeleteFolders(empty);
        if (errors.Count > 0)
        {
            throw new ToolFailedException(string.Join("\n", errors));
        }

        report.Output(Strings.ToolEmptyFoldersRemoved(empty.Count));
    }

    /// <summary>
    /// Deletes a file, or a folder with everything in it, without the Recycle Bin. Read-only
    /// items go too, as with Shift+Delete. A link is removed, never what it points to.
    /// </summary>
    private static void DeletePermanently(string path, CancellationToken cancellationToken)
    {
        if (File.Exists(path))
        {
            DeleteFile(new FileInfo(path));
            return;
        }

        DeleteTree(new DirectoryInfo(path));

        void DeleteTree(DirectoryInfo folder)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsLink(folder))
            {
                foreach (FileSystemInfo entry in folder.EnumerateFileSystemInfos("*", s_everything))
                {
                    if (entry is DirectoryInfo child)
                    {
                        DeleteTree(child);
                    }
                    else
                    {
                        DeleteFile((FileInfo)entry);
                    }
                }
            }

            folder.Attributes &= ~FileAttributes.ReadOnly;
            folder.Delete();
        }

        static void DeleteFile(FileInfo file)
        {
            file.Attributes &= ~FileAttributes.ReadOnly;
            file.Delete();
        }
    }

    // ---- files ----------------------------------------------------------------------------

    /// <summary>Shared the way Notepad opens a file, so a log another program is writing can be read.</summary>
    internal static FileStream OpenRead(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan);

    /// <summary>A checksum list, decoded as Notepad would guess it (ADR-0015): a list PowerShell 5.1 wrote is ANSI.</summary>
    private static string ReadText(string path, Encoding ansi)
    {
        using FileStream stream = OpenRead(path);
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return TextDecoder.Decode(bytes.ToArray(), ansi) ?? "";
    }

    private static void DeleteQuietly(IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The original failure is the one to report.
            }
        }
    }
}

/// <summary>What a windowed tool needs besides the item: asked for or read once, before the run.</summary>
/// <param name="PartSize">Split: bytes per part.</param>
/// <param name="Clipboard">Verify: the clipboard's text when the run started.</param>
/// <param name="Ansi">The Windows code page, for checksum lists that are neither UTF-8 nor marked; Latin-1 when not given.</param>
/// <param name="Copier">How duplicate copies a file; a plain stream copy when not given.</param>
/// <param name="Confirm">Delete empty folders: asks the user, from the worker thread; nothing is removed without it.</param>
public sealed record ToolInput(long PartSize = 0, string? Clipboard = null, Encoding? Ansi = null, IFileCopier? Copier = null,
    Func<string, bool>? Confirm = null)
{
    internal Encoding TextEncoding => Ansi ?? Encoding.Latin1;
}

/// <summary>Copies one file to a new name (ADR-0018).</summary>
public interface IFileCopier
{
    /// <summary>
    /// Copies <paramref name="source"/> to <paramref name="target"/>, which must not exist yet;
    /// a target left half-written by a failure or a cancel is removed.
    /// </summary>
    void Copy(string source, string target, Action<long, long> progress, CancellationToken cancellationToken);
}

/// <summary>
/// The bytes, the date and the read-only, hidden, system and archive attributes. Alternate data
/// streams (Windows' "downloaded from the internet" mark among them) and encryption are lost,
/// which is why the runner uses CopyFileEx instead; this one is for the tests.
/// </summary>
public sealed class StreamCopier : IFileCopier
{
    public static StreamCopier Instance { get; } = new();

    public void Copy(string source, string target, Action<long, long> progress, CancellationToken cancellationToken)
    {
        var info = new FileInfo(source);
        bool created = false;
        try
        {
            using (FileStream input = FileTools.OpenRead(source))
            using (FileStream output = new(target, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                created = true;
                long total = input.Length;
                byte[] buffer = new byte[Checksums.BufferSize];
                long done = 0;
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    output.Write(buffer, 0, read);
                    done += read;
                    progress(done, total);
                }
            }

            File.SetLastWriteTimeUtc(target, info.LastWriteTimeUtc);
            File.SetAttributes(target, info.Attributes & (FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive));
        }
        catch
        {
            if (created)
            {
                try
                {
                    File.Delete(target);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // The original failure is the one to report.
                }
            }

            throw;
        }
    }
}

/// <summary>Line ends in a stream, without decoding it (see <c>FileTools.CountLines</c>).</summary>
public static class LineCounter
{
    public static long Count(Stream stream, Action<long>? progress, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[Checksums.BufferSize];
        int read = ReadAtLeast(stream, buffer, 2);
        bool? bigEndian = read >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE ? false
            : read >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF ? true
            : null;

        long lines = 0;
        long done = 0;
        bool afterCr = false;
        bool lineOpen = false;
        bool utf8Bom = read >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF;
        int start = bigEndian is not null ? 2 : utf8Bom ? 3 : 0;
        byte pending = 0;
        bool hasPending = false;
        while (read > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int i = start; i < read; i++)
            {
                int unit;
                if (bigEndian is { } big)
                {
                    if (!hasPending)
                    {
                        pending = buffer[i];
                        hasPending = true;
                        continue;
                    }

                    unit = big ? (pending << 8) | buffer[i] : (buffer[i] << 8) | pending;
                    hasPending = false;
                }
                else
                {
                    unit = buffer[i];
                }

                if (unit == '\n')
                {
                    // CRLF is one line end; the CR already counted it.
                    if (!afterCr)
                    {
                        lines++;
                    }

                    afterCr = false;
                    lineOpen = false;
                }
                else if (unit == '\r')
                {
                    lines++;
                    afterCr = true;
                    lineOpen = false;
                }
                else
                {
                    afterCr = false;
                    lineOpen = true;
                }
            }

            done += read;
            progress?.Invoke(done);
            start = 0;
            read = stream.Read(buffer, 0, buffer.Length);
        }

        return lineOpen ? lines + 1 : lines;
    }

    private static int ReadAtLeast(Stream stream, byte[] buffer, int minimum)
    {
        int total = 0;
        int read;
        while (total < minimum && (read = stream.Read(buffer, total, buffer.Length - total)) > 0)
        {
            total += read;
        }

        return total;
    }
}
