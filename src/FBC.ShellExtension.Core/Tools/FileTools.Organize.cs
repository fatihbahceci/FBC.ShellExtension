using System.Globalization;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>What Organize into folders names the folders after.</summary>
public enum OrganizeBy
{
    Date,
    Extension,
}

/// <summary>Which of a file's dates Organize into folders goes by.</summary>
public enum OrganizeDate
{
    Modified,
    Created,

    /// <summary>When a photo was taken or a video recorded, from the file's own properties; the modified date when it has none.</summary>
    Taken,
}

/// <summary>How a date becomes a folder.</summary>
public enum DateFolders
{
    /// <summary><c>2026</c></summary>
    Year,

    /// <summary><c>2026-10</c></summary>
    YearMonth,

    /// <summary><c>2026-10-22</c></summary>
    YearMonthDay,

    /// <summary><c>2026\10</c>: a folder for the year, one for the month inside it.</summary>
    YearThenMonth,
}

/// <summary>A file and the folder, next to it, it goes into; <see cref="Folder"/> may hold a subfolder.</summary>
public sealed record OrganizeMove(string Source, string Folder)
{
    /// <summary>The full path of the folder.</summary>
    public string Destination => Path.Combine(Path.GetDirectoryName(Source) ?? "", Folder);
}

/// <summary>The plan of Organize into folders (ADR-0028); the editor moves the files with Windows' own engine.</summary>
public static partial class FileTools
{
    // A selected folder gives its files, not desktop.ini, Thumbs.db and the like.
    private static readonly EnumerationOptions s_visibleFiles = new() { AttributesToSkip = FileAttributes.Hidden | FileAttributes.System, IgnoreInaccessible = false };

    /// <summary>
    /// The files to organize: the selected files, and the files directly in the selected folders,
    /// without the hidden and system ones. Each once.
    /// </summary>
    /// <param name="errors">A line for each folder that could not be read.</param>
    public static IReadOnlyList<string> OrganizeSources(IReadOnlyList<SelectedItem> items, out List<string> errors)
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

            try
            {
                files.AddRange(new DirectoryInfo(item.Path).EnumerateFiles("*", s_visibleFiles)
                    .Select(file => file.FullName)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .Where(seen.Add));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add(Strings.ToolItemFailed(item.Path, ex.Message));
            }
        }

        return files;
    }

    /// <summary>The folder a date goes into; <see cref="DateFolders.YearThenMonth"/> gives two levels.</summary>
    public static string DateFolder(DateTime date, DateFolders folders) => folders switch
    {
        DateFolders.YearMonth => date.ToString("yyyy-MM", CultureInfo.InvariantCulture),
        DateFolders.YearMonthDay => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateFolders.YearThenMonth => Path.Combine(date.ToString("yyyy", CultureInfo.InvariantCulture), date.ToString("MM", CultureInfo.InvariantCulture)),
        _ => date.ToString("yyyy", CultureInfo.InvariantCulture),
    };

    /// <summary>
    /// The folder a file goes into by its extension: <c>PDF</c> for <c>report.pdf</c>. A file
    /// without one, or all extension (<c>.gitignore</c>), goes into "No extension".
    /// </summary>
    public static string ExtensionFolder(string fileName)
    {
        string extension = WindowsPath.GetExtension(fileName);
        if (extension.Length <= 1 || extension.Length == fileName.Length)
        {
            return Strings.OrganizeNoExtension();
        }

        string folder = extension[1..].ToUpperInvariant();

        // A file may end in .nul or .con; a folder may not be called that.
        return WindowsPath.IsValidName(folder) ? folder : $"{folder}_";
    }

    /// <summary>Where each file goes, in the order given.</summary>
    /// <param name="dateOf">The date of a file; asked only when <paramref name="by"/> is <see cref="OrganizeBy.Date"/>.</param>
    public static IReadOnlyList<OrganizeMove> OrganizePlan(IReadOnlyList<string> files, OrganizeBy by, DateFolders folders, Func<string, DateTime> dateOf) =>
        [.. files.Select(file => new OrganizeMove(file, by == OrganizeBy.Date
            ? DateFolder(dateOf(file), folders)
            : ExtensionFolder(Path.GetFileName(file))))];

    /// <summary>The file system's own date for <paramref name="date"/>; <see cref="OrganizeDate.Taken"/> is the editor's to read, and falls back to this.</summary>
    /// <exception cref="FileNotFoundException">The file is gone: Windows would give 1601 for it, not fail.</exception>
    public static DateTime FileDate(string path, OrganizeDate date)
    {
        var file = new FileInfo(path);
        if (!file.Exists)
        {
            throw new FileNotFoundException(null, path);
        }

        return date == OrganizeDate.Created ? file.CreationTime : file.LastWriteTime;
    }
}
