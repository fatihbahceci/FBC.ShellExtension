using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>What Synchronize folders would do with an item.</summary>
public enum SyncAction
{
    /// <summary>Only in the source: copied.</summary>
    New,

    /// <summary>Newer in the source: copied over the destination's.</summary>
    Changed,

    /// <summary>Newer in the destination: copying over it would lose that change, so it is not ticked.</summary>
    NewerThere,

    /// <summary>The same time on both sides, a different size: which is right is not known, so it is not ticked.</summary>
    SizeDiffers,

    /// <summary>Only in the destination: sent to the Recycle Bin only when asked.</summary>
    Extra,
}

/// <summary>An item of the comparison; a folder that is only on one side is one item, whole.</summary>
/// <param name="Path">Relative to the source and the destination.</param>
public sealed record SyncItem(string Path, bool IsFolder, SyncAction Action, long Length, DateTime Modified);

/// <summary>Synchronize folders, one way (ADR-0033): the comparison. The editor copies with Windows' own engine.</summary>
public static partial class FileTools
{
    // FAT and exFAT keep times to two seconds; a copy there looks older than it is.
    private static readonly TimeSpan s_timeTolerance = TimeSpan.FromSeconds(2);

    // A volume root's own folders: Windows' bin and restore points are no one's to copy or delete.
    private static readonly string[] s_volumeFolders = ["$RECYCLE.BIN", "System Volume Information"];

    /// <summary>
    /// Why <paramref name="destination"/> cannot take <paramref name="source"/>'s content; null when
    /// it can. Only a full path: a relative one would be read against the editor's own folder. The
    /// paths are compared as written once ".." and "/" are resolved; the editor compares where they
    /// really lead too (a mapped drive, a junction), with <see cref="SyncInside"/>.
    /// </summary>
    public static string? SyncProblem(string source, string destination) =>
        destination.Trim().Length == 0 ? Strings.SyncNoDestination()
        : !Path.IsPathFullyQualified(destination) ? Strings.SyncNotFullPath(destination)
        : !Directory.Exists(destination) ? Strings.SyncDestinationMissing(destination)
        : SyncInside(Path.GetFullPath(source), Path.GetFullPath(destination)) ? Strings.SyncInside()
        : null;

    /// <summary>Whether one folder is the other or lies inside it, regardless of case and of a long-path prefix.</summary>
    public static bool SyncInside(string first, string second)
    {
        static string Plain(string path) =>
            path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase) ? @"\\" + path[8..]
            : path.StartsWith(@"\\?\", StringComparison.Ordinal) ? path[4..]
            : path;

        string a = Plain(first);
        string b = Plain(second);
        return WindowsPath.IsSameOrInside(a, b) || WindowsPath.IsSameOrInside(b, a);
    }

    /// <summary>
    /// What differs between the two folders, deepest folders after their parents, by path.
    /// Everything counts, hidden and system items too: the destination is meant to be a copy;
    /// only a volume root's bin and restore points do not. A link on either side is left alone
    /// and said so: copying into a junction would write somewhere else, and taking one for missing
    /// would copy or delete what is behind it.
    /// </summary>
    /// <param name="errors">A line for each folder that could not be read.</param>
    /// <exception cref="OperationCanceledException">Cancelled.</exception>
    public static IReadOnlyList<SyncItem> SyncPlan(string source, string destination, out List<string> errors, CancellationToken cancellationToken)
    {
        errors = [];
        var plan = new List<SyncItem>();
        var pending = new Stack<string>([""]);
        while (pending.TryPop(out string? relative))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Entries(source, relative, errors) is not { } here || Entries(destination, relative, errors) is not { } there)
            {
                continue;
            }

            foreach (FileSystemInfo entry in here.Values.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase))
            {
                string path = Path.Combine(relative, entry.Name);
                there.TryGetValue(entry.Name, out FileSystemInfo? other);
                if (IsLink(entry) || (other is not null && IsLink(other)))
                {
                    errors.Add(Strings.ToolItemFailed(path, Strings.SyncLinkLeft()));
                    continue;
                }

                if (entry is DirectoryInfo)
                {
                    if (other is DirectoryInfo)
                    {
                        pending.Push(path);
                    }
                    else if (other is null)
                    {
                        plan.Add(new SyncItem(path, IsFolder: true, SyncAction.New, 0, entry.LastWriteTime));
                    }
                    else
                    {
                        // A file there with the folder's name: one of them has to go first, which is the user's to do.
                        errors.Add(Strings.ToolItemFailed(Path.Combine(destination, path), Strings.SyncKindDiffers()));
                    }

                    continue;
                }

                var file = (FileInfo)entry;
                if (other is null)
                {
                    plan.Add(new SyncItem(path, IsFolder: false, SyncAction.New, file.Length, file.LastWriteTime));
                }
                else if (other is FileInfo copy)
                {
                    TimeSpan newer = file.LastWriteTimeUtc - copy.LastWriteTimeUtc;
                    SyncAction? action = newer > s_timeTolerance ? SyncAction.Changed
                        : newer < -s_timeTolerance ? SyncAction.NewerThere
                        : file.Length != copy.Length ? SyncAction.SizeDiffers
                        : null;
                    if (action is { } some)
                    {
                        plan.Add(new SyncItem(path, IsFolder: false, some, file.Length, file.LastWriteTime));
                    }
                }
                else
                {
                    errors.Add(Strings.ToolItemFailed(Path.Combine(destination, path), Strings.SyncKindDiffers()));
                }
            }

            foreach (FileSystemInfo entry in there.Values.Where(entry => !here.ContainsKey(entry.Name)).OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase))
            {
                string path = Path.Combine(relative, entry.Name);
                if (IsLink(entry))
                {
                    errors.Add(Strings.ToolItemFailed(path, Strings.SyncLinkLeft()));
                    continue;
                }

                plan.Add(new SyncItem(path, entry is DirectoryInfo, SyncAction.Extra, entry is FileInfo extra ? extra.Length : 0, entry.LastWriteTime));
            }
        }

        return [.. plan.OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>A folder's entries by name, Windows' way regardless of case; links among them. Null when it cannot be read.</summary>
    private static Dictionary<string, FileSystemInfo>? Entries(string root, string relative, List<string> errors)
    {
        string folder = Path.Combine(root, relative);
        bool volumeRoot = relative.Length == 0 && WindowsPath.IsRoot(root);
        try
        {
            var entries = new Dictionary<string, FileSystemInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (FileSystemInfo entry in new DirectoryInfo(folder).GetFileSystemInfos("*", s_everything))
            {
                if (!(volumeRoot && s_volumeFolders.Contains(entry.Name, StringComparer.OrdinalIgnoreCase)))
                {
                    entries.TryAdd(entry.Name, entry);
                }
            }

            return entries;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errors.Add(Strings.ToolItemFailed(folder, ex.Message));
            return null;
        }
    }

    /// <summary>The source folder and, when two folders are selected, the second as the destination.</summary>
    public static (string Source, string Destination) SyncFolders(IReadOnlyList<SelectedItem> items) =>
        (items[0].Path, items.Count > 1 ? items[1].Path : "");
}
