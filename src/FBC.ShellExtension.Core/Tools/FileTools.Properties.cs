using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>What Attributes changes: the attributes to set and the ones to clear; the others stay as they are.</summary>
public readonly record struct AttributeChange(FileAttributes Set, FileAttributes Clear)
{
    public bool IsEmpty => Set == 0 && Clear == 0;
}

/// <summary>What Change time writes, in local time; a null one stays as it is.</summary>
public sealed record TimeChange(DateTime? Created, DateTime? Modified, DateTime? Accessed)
{
    public bool IsEmpty => Created is null && Modified is null && Accessed is null;
}

/// <summary>
/// The work of the Attributes and Change time windows (ADR-0028): the editor shows the window,
/// what is written is decided here, where it can be tested.
/// </summary>
public static partial class FileTools
{
    /// <summary>
    /// Read-only and system on a folder mark it as customized (desktop.ini, ADR-0027), not as
    /// protected; Explorer, too, sets them only on the files in a folder.
    /// </summary>
    public const FileAttributes FileOnlyAttributes = FileAttributes.ReadOnly | FileAttributes.System;

    /// <summary>The attributes the Attributes window shows, in its order.</summary>
    public static IReadOnlyList<FileAttributes> EditableAttributes { get; } =
        [FileAttributes.ReadOnly, FileAttributes.Hidden, FileAttributes.System, FileAttributes.Archive];

    /// <summary>
    /// Whether all of <paramref name="current"/> have <paramref name="attribute"/> (true), none
    /// (false) or some (null). A file-only attribute counts only the files; with none among them
    /// nothing is known, so null.
    /// </summary>
    public static bool? AttributeState(IEnumerable<FileAttributes> current, FileAttributes attribute)
    {
        List<FileAttributes> counted = [.. current.Where(attributes =>
            (attribute & FileOnlyAttributes) == 0 || !attributes.HasFlag(FileAttributes.Directory))];
        int with = counted.Count(attributes => attributes.HasFlag(attribute));
        return counted.Count == 0 || (with > 0 && with < counted.Count) ? null : with > 0;
    }

    /// <summary><paramref name="current"/> after <paramref name="change"/>; a folder keeps its read-only and system marks.</summary>
    public static FileAttributes ChangedAttributes(FileAttributes current, AttributeChange change)
    {
        FileAttributes set = change.Set;
        FileAttributes clear = change.Clear;
        if (current.HasFlag(FileAttributes.Directory))
        {
            set &= ~FileOnlyAttributes;
            clear &= ~FileOnlyAttributes;
        }

        return (current | set) & ~clear;
    }

    /// <summary>Changes the attributes of the items, and with <paramref name="inside"/> of everything in the folders among them.</summary>
    /// <returns>A line for each item that could not be changed, and each folder that could not be read.</returns>
    /// <exception cref="OperationCanceledException">Cancelled; what was changed stays changed.</exception>
    public static IReadOnlyList<string> ChangeAttributes(IReadOnlyList<SelectedItem> items, AttributeChange change, bool inside, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        foreach (FileSystemInfo entry in ItemsAndInside(items, inside, errors, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // Not entry.Attributes: for an item gone meanwhile it reads -1 instead of failing.
                FileAttributes current = File.GetAttributes(entry.FullName);
                FileAttributes changed = ChangedAttributes(current, change);
                if (changed != current)
                {
                    File.SetAttributes(entry.FullName, changed);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add(Strings.ToolItemFailed(entry.FullName, ex.Message));
            }
        }

        return errors;
    }

    /// <summary>Sets the times of the items, and with <paramref name="inside"/> of everything in the folders among them.</summary>
    /// <returns>A line for each item that could not be changed, and each folder that could not be read.</returns>
    /// <exception cref="OperationCanceledException">Cancelled; what was changed stays changed.</exception>
    public static IReadOnlyList<string> ChangeTimes(IReadOnlyList<SelectedItem> items, TimeChange change, bool inside, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        foreach (FileSystemInfo entry in ItemsAndInside(items, inside, errors, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // A read-only file takes new times: they are attributes, not its content.
                if (change.Created is { } created)
                {
                    entry.CreationTime = created;
                }

                if (change.Modified is { } modified)
                {
                    entry.LastWriteTime = modified;
                }

                if (change.Accessed is { } accessed)
                {
                    entry.LastAccessTime = accessed;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                errors.Add(Strings.ToolItemFailed(entry.FullName, ex.Message));
            }
        }

        return errors;
    }

    /// <summary>
    /// The items, then, with <paramref name="inside"/>, everything under the folders among them.
    /// A link inside is neither entered nor changed: what it leads to is somewhere else.
    /// </summary>
    private static IEnumerable<FileSystemInfo> ItemsAndInside(IReadOnlyList<SelectedItem> items, bool inside, List<string> errors, CancellationToken cancellationToken)
    {
        foreach (SelectedItem item in items)
        {
            if (item.Kind == ItemKind.File)
            {
                yield return new FileInfo(item.Path);
                continue;
            }

            var root = new DirectoryInfo(item.Path);
            yield return root;
            if (!inside || IsLink(root))
            {
                continue;
            }

            var pending = new Stack<DirectoryInfo>([root]);
            while (pending.TryPop(out DirectoryInfo? folder))
            {
                cancellationToken.ThrowIfCancellationRequested();
                FileSystemInfo[] entries;
                try
                {
                    entries = folder.GetFileSystemInfos("*", s_everything);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    errors.Add(Strings.ToolItemFailed(folder.FullName, ex.Message));
                    continue;
                }

                foreach (FileSystemInfo entry in entries.Where(entry => !IsLink(entry)))
                {
                    yield return entry;
                    if (entry is DirectoryInfo subfolder)
                    {
                        pending.Push(subfolder);
                    }
                }
            }
        }
    }
}
