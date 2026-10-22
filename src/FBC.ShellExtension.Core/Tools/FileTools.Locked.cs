using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>An item to delete at the next restart; a link is removed at once instead (ADR-0034).</summary>
public sealed record RebootEntry(string Path, bool IsLink, bool IsFolder);

/// <summary>
/// Unlock and delete (ADR-0031): which files to ask Restart Manager about, and in what order
/// Windows can delete the items at the next restart. The editor does the asking and the closing.
/// </summary>
public static partial class FileTools
{
    /// <summary>The most files inside the selected folders asked about; a larger tree is cut there.</summary>
    public const int MaxLockedFiles = 10_000;

    /// <summary>
    /// The selected files and the files inside the selected folders, links not entered, at most
    /// <see cref="MaxLockedFiles"/>. Restart Manager knows files only: a folder that is some
    /// program's current folder is not found this way.
    /// </summary>
    /// <param name="cut">More files were there than were taken.</param>
    /// <param name="errors">A line for each folder that could not be read.</param>
    /// <exception cref="OperationCanceledException">Cancelled.</exception>
    public static IReadOnlyList<string> LockCheckFiles(IReadOnlyList<SelectedItem> items, out bool cut, out List<string> errors, CancellationToken cancellationToken)
    {
        errors = [];
        var files = new List<string>();
        cut = false;
        foreach (FileSystemInfo entry in ItemsAndInside(items, inside: true, errors, cancellationToken))
        {
            if (entry is not FileInfo)
            {
                continue;
            }

            if (files.Count == MaxLockedFiles)
            {
                cut = true;
                break;
            }

            files.Add(entry.FullName);
        }

        return files;
    }

    /// <summary>
    /// What to delete at the next restart, in an order Windows can carry out: what is in a folder
    /// before the folder, as Windows removes a folder at restart only if it is empty. A link is
    /// not entered and is flagged: whether the boot-time deletion takes a link or what it leads
    /// to is not documented, so the editor removes links at once.
    /// </summary>
    /// <param name="errors">A line for each path that is not there and each folder that could not be read.</param>
    public static IReadOnlyList<RebootEntry> RebootDeletionOrder(IReadOnlyList<string> paths, out List<string> errors)
    {
        errors = [];
        var order = new List<RebootEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in paths)
        {
            var root = new DirectoryInfo(path);
            if (!root.Exists)
            {
                var file = new FileInfo(path);
                if (!file.Exists)
                {
                    errors.Add(Strings.ToolItemFailed(path, Strings.LockedNotFound()));
                }
                else if (seen.Add(file.FullName))
                {
                    order.Add(new RebootEntry(file.FullName, IsLink(file), IsFolder: false));
                }

                continue;
            }

            AddFolderLast(root, order, seen, errors);
        }

        return order;
    }

    /// <summary>The folder's content, deepest first, then the folder itself.</summary>
    private static void AddFolderLast(DirectoryInfo root, List<RebootEntry> order, HashSet<string> seen, List<string> errors)
    {
        // Each folder is pushed twice: the first time it is opened, the second time it is added.
        var pending = new Stack<(DirectoryInfo Folder, bool Opened)>([(root, false)]);
        while (pending.TryPop(out (DirectoryInfo Folder, bool Opened) next))
        {
            bool link = IsLink(next.Folder);
            if (next.Opened || link)
            {
                if (seen.Add(next.Folder.FullName))
                {
                    order.Add(new RebootEntry(next.Folder.FullName, link, IsFolder: true));
                }

                continue;
            }

            pending.Push((next.Folder, true));
            FileSystemInfo[] entries;
            try
            {
                entries = next.Folder.GetFileSystemInfos("*", s_everything);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add(Strings.ToolItemFailed(next.Folder.FullName, ex.Message));
                continue;
            }

            foreach (FileSystemInfo entry in entries)
            {
                if (entry is DirectoryInfo folder)
                {
                    pending.Push((folder, false));
                }
                else if (seen.Add(entry.FullName))
                {
                    order.Add(new RebootEntry(entry.FullName, IsLink(entry), IsFolder: false));
                }
            }
        }
    }
}
