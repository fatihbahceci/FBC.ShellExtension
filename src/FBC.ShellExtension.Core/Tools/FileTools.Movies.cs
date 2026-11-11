using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>The files Rename from IMDb works on (ADR-0046); the names are <see cref="MovieNames"/>'.</summary>
public static partial class FileTools
{
    // Neither desktop.ini nor a hidden folder; a link is not entered, its target is another tree.
    private static readonly EnumerationOptions s_movieFolders = new()
    {
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
        IgnoreInaccessible = false,
    };

    /// <summary>
    /// The selected files, whatever their extension, and the videos and subtitles in the selected
    /// folders, without the hidden and system ones; in their subfolders too when asked. Each once.
    /// </summary>
    /// <param name="errors">A line for each folder that could not be read.</param>
    public static IReadOnlyList<SelectedItem> MovieSources(IReadOnlyList<SelectedItem> items, bool subfolders, out List<string> errors,
        CancellationToken cancellation = default)
    {
        errors = [];
        var files = new List<SelectedItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (SelectedItem item in items)
        {
            if (item.Kind == ItemKind.File)
            {
                if (seen.Add(item.Path))
                {
                    files.Add(item);
                }

                continue;
            }

            var folders = new Stack<string>([item.Path]);
            while (folders.TryPop(out string? folder))
            {
                cancellation.ThrowIfCancellationRequested();
                try
                {
                    var directory = new DirectoryInfo(folder);
                    files.AddRange(directory.EnumerateFiles("*", s_visibleFiles)
                        .Where(file => MovieNames.IsMovieFile(file.Name))
                        .Select(file => file.FullName)
                        .Order(StringComparer.OrdinalIgnoreCase)
                        .Where(seen.Add)
                        .Select(path => new SelectedItem(path, ItemKind.File)));
                    if (subfolders)
                    {
                        // Popped in name order.
                        foreach (DirectoryInfo child in directory.EnumerateDirectories("*", s_movieFolders).OrderByDescending(child => child.Name, StringComparer.OrdinalIgnoreCase))
                        {
                            folders.Push(child.FullName);
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    errors.Add(Strings.ToolItemFailed(folder, ex.Message));
                }
            }
        }

        return files;
    }
}
