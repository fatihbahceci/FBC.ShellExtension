using System.IO.Enumeration;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>A file or folder Advanced delete found; a folder's size is not counted.</summary>
public sealed record FoundItem(string Path, bool IsFolder, long Length, DateTime Modified);

/// <summary>What Advanced delete found, and the folders it could not look into.</summary>
public sealed record FindResult(IReadOnlyList<FoundItem> Items, IReadOnlyList<string> Unreadable);

/// <summary>The search of Advanced delete (ADR-0028); the editor sends what it found to the Recycle Bin.</summary>
public static partial class FileTools
{
    private static readonly char[] s_patternSeparators = [';', ','];

    /// <summary>
    /// The patterns in <paramref name="text"/>, separated by <c>;</c> or <c>,</c>:
    /// <c>*.tmp; ~*; Thumbs.db</c>. Null, with the reason, when there is none or one names a folder.
    /// </summary>
    public static IReadOnlyList<string>? ParsePatterns(string text, out string? problem)
    {
        string[] patterns = text.Split(s_patternSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        problem = patterns.Length == 0
            ? Strings.DeletePatternsEmpty()
            : patterns.FirstOrDefault(pattern => !IsValidPattern(pattern)) is { } invalid
                ? Strings.DeletePatternInvalid(invalid)
                : null;
        return problem is null ? patterns : null;
    }

    /// <summary>
    /// A name with <c>*</c> and <c>?</c> in it. Only <c>*.</c>, "no extension" to <c>dir</c>,
    /// may end in a dot: a name cannot, so any other such pattern would never match.
    /// </summary>
    private static bool IsValidPattern(string pattern) =>
        pattern == "*." || WindowsPath.IsValidName(pattern.Replace('*', 'x').Replace('?', 'x'));

    /// <summary>Whether <paramref name="name"/> matches one of the patterns, as <c>dir</c> matches: case aside, <c>*.*</c> is everything.</summary>
    public static bool MatchesAny(string name, IReadOnlyList<string> patterns) =>
        patterns.Any(pattern => FileSystemName.MatchesWin32Expression(FileSystemName.TranslateWin32Expression(pattern), name, ignoreCase: true));

    /// <summary>
    /// The files and folders under <paramref name="roots"/> whose name matches. A matching folder
    /// is found whole, not looked into, and nothing in it is found on its own. Hidden and system
    /// items are found too (Thumbs.db, desktop.ini); links are neither found nor entered. The
    /// roots themselves are never found; a root inside another is searched all the same.
    /// </summary>
    /// <exception cref="OperationCanceledException">Cancelled.</exception>
    public static FindResult FindMatches(IReadOnlyList<string> roots, IReadOnlyList<string> patterns, bool files, bool folders, bool subfolders, CancellationToken cancellationToken)
    {
        var found = new List<FoundItem>();
        var unreadable = new List<string>();

        // Each folder once, however often it is reached: a root may lie inside another.
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<string>(roots);
        while (pending.TryPop(out string? folder))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!visited.Add(Path.TrimEndingDirectorySeparator(folder)))
            {
                continue;
            }

            FileSystemInfo[] entries;
            try
            {
                entries = new DirectoryInfo(folder).GetFileSystemInfos("*", s_everything);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                unreadable.Add(folder);
                continue;
            }

            foreach (FileSystemInfo entry in entries.Where(entry => !IsLink(entry)))
            {
                bool isFolder = entry is DirectoryInfo;
                if ((isFolder ? folders : files) && MatchesAny(entry.Name, patterns))
                {
                    found.Add(new FoundItem(entry.FullName, isFolder, isFolder ? 0 : ((FileInfo)entry).Length, entry.LastWriteTime));
                }
                else if (isFolder && subfolders)
                {
                    pending.Push(entry.FullName);
                }
            }
        }

        // Sending a found folder to the Recycle Bin takes what is in it; a root inside it found some of that again.
        List<FoundItem> folderItems = [.. found.Where(item => item.IsFolder)];
        return new FindResult(
            [.. found.Where(item => !folderItems.Any(folder => folder != item && WindowsPath.IsSameOrInside(item.Path, folder.Path)))
                .DistinctBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase)],
            unreadable);
    }
}
