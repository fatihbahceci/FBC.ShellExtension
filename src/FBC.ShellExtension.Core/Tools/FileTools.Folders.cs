using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tools;

/// <summary>
/// The checks and the plain file system work of the folder-picker tools (ADR-0026). Copying and
/// moving are Windows' own (the runner's IFileOperation), and so are the pickers, the links and
/// the folder icons; what can be decided without them is here, where it can be tested.
/// </summary>
public static partial class FileTools
{
    /// <summary>
    /// <paramref name="name"/>, or with " (2)", " (3)" … when it is taken: before the extension
    /// for a file (<c>report (2).pdf</c>), at the end for a folder.
    /// </summary>
    public static string FreeItemName(string folder, string name, bool isFile)
    {
        string extension = isFile ? WindowsPath.GetExtension(name) : "";
        if (extension.Length == name.Length)
        {
            // ".gitignore" is all extension: the number goes at the end.
            extension = "";
        }

        string stem = name[..^extension.Length];
        return Enumerable.Range(1, int.MaxValue - 1)
            .Select(number => number == 1 ? name : string.Create(CultureInfo.InvariantCulture, $"{stem} ({number}){extension}"))
            .First(candidate => NameProblem(folder, candidate) is null || !WindowsPath.IsValidName(candidate));
    }

    /// <summary>
    /// Why the folders cannot be joined; null when they can. They must be two or more, side by
    /// side in one folder (the new one goes next to them), and none may be a link: its contents
    /// live elsewhere, and moving them out would empty another folder.
    /// </summary>
    public static string? JoinProblem(IReadOnlyList<SelectedItem> folders)
    {
        if (folders.Count < 2)
        {
            return Strings.ToolJoinTwoOrMore();
        }

        string parent = folders[0].Directory;
        if (folders.Any(folder => !string.Equals(folder.Directory, parent, StringComparison.OrdinalIgnoreCase)))
        {
            return Strings.ToolJoinDifferentFolders();
        }

        return folders.FirstOrDefault(folder => IsLink(new DirectoryInfo(folder.Path))) is { } link
            ? Strings.ToolJoinLink(link.Name)
            : null;
    }

    /// <summary>Everything the folders hold, hidden and system items included: what goes into the joined folder.</summary>
    public static IReadOnlyList<string> JoinSources(IReadOnlyList<SelectedItem> folders) =>
        [.. folders.SelectMany(folder => new DirectoryInfo(folder.Path).EnumerateFileSystemInfos("*", s_everything).Select(entry => entry.FullName))];

    /// <summary>Removes <paramref name="folder"/> if nothing is left in it; a customized folder's read-only mark does not stop it.</summary>
    /// <returns>Whether it was removed.</returns>
    public static bool RemoveIfEmpty(string folder)
    {
        var info = new DirectoryInfo(folder);
        if (!info.Exists || info.EnumerateFileSystemInfos("*", s_everything).Any())
        {
            return false;
        }

        info.Attributes &= ~FileAttributes.ReadOnly;
        info.Delete();
        return true;
    }

    /// <summary>
    /// Why no link to <paramref name="item"/> can be made in <paramref name="linkFolder"/>; null
    /// when it can. A link to a folder inside that folder would be a loop, which Explorer and
    /// every program that walks the tree would follow without end.
    /// </summary>
    public static string? LinkProblem(SelectedItem item, string linkFolder) =>
        item.Kind == ItemKind.Folder && WindowsPath.IsSameOrInside(linkFolder, item.Path)
            ? Strings.ToolLinkLoop(item.Name)
            : null;

    /// <summary>
    /// Why the icon tools leave <paramref name="folder"/> alone; null when they can work on it. A
    /// drive's icon comes from the registry, not from a desktop.ini in its root, and the empty
    /// area of a drive's window arrives as that root.
    /// </summary>
    public static string? IconProblem(SelectedItem folder) =>
        folder.Kind == ItemKind.Drive || WindowsPath.IsRoot(folder.Path)
            ? Strings.ToolIconDrive(folder.Path.TrimEnd('\\'))
            : null;

    /// <summary>The largest reparse buffer Windows takes (MAXIMUM_REPARSE_DATA_BUFFER_SIZE).</summary>
    private const int MaxReparseBuffer = 16 * 1024;

    /// <summary>
    /// The FSCTL_SET_REPARSE_POINT input that turns an empty folder into a junction to
    /// <paramref name="target"/>, byte for byte as <c>mklink /J</c> writes it: the tag
    /// (0xA0000003), the data length, then the substitute name (<c>\??\C:\target</c>) and the
    /// print name (<c>C:\target</c>), each followed by a NUL that the lengths leave out. Windows
    /// takes the names only in this order.
    /// </summary>
    /// <param name="target">A full local path, <c>C:\folder</c> or <c>\\?\C:\folder</c>.</param>
    /// <exception cref="ToolFailedException">The target is a network path, or too long for a junction.</exception>
    public static byte[] MountPointData(string target)
    {
        string path = target.StartsWith(@"\\?\", StringComparison.Ordinal) ? target[4..] : target;
        if (path.Length < 3 || !char.IsAsciiLetter(path[0]) || path[1] != ':' || path[2] != '\\')
        {
            throw new ToolFailedException(Strings.ToolLinkNotLocal(WindowsPath.GetFileName(path)));
        }

        // A drive root keeps its backslash: \??\C: would name the volume, not its root folder.
        string print = path.Length > 3 ? path.TrimEnd('\\') : path;
        string substitute = @"\??\" + print;
        int substituteBytes = substitute.Length * 2;
        int printBytes = print.Length * 2;
        int dataLength = substituteBytes + printBytes + 12;
        if (dataLength + 8 > MaxReparseBuffer)
        {
            throw new ToolFailedException(Strings.ToolLinkFailed(WindowsPath.GetFileName(print), Strings.ToolLinkTooLong()));
        }

        var buffer = new byte[dataLength + 8];
        var data = buffer.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(data, 0xA0000003);
        BinaryPrimitives.WriteUInt16LittleEndian(data[4..], (ushort)dataLength);
        BinaryPrimitives.WriteUInt16LittleEndian(data[8..], 0);
        BinaryPrimitives.WriteUInt16LittleEndian(data[10..], (ushort)substituteBytes);
        BinaryPrimitives.WriteUInt16LittleEndian(data[12..], (ushort)(substituteBytes + 2));
        BinaryPrimitives.WriteUInt16LittleEndian(data[14..], (ushort)printBytes);
        Encoding.Unicode.GetBytes(substitute, data[16..]);
        Encoding.Unicode.GetBytes(print, data[(16 + substituteBytes + 2)..]);
        return buffer;
    }

    private static readonly string[] s_iconKeys = ["IconResource", "IconFile", "IconIndex"];

    /// <summary>
    /// Whether a <c>desktop.ini</c> holds anything once its icon lines are gone: another key in
    /// any section (a localized name, an info tip, Explorer's view state). Such a folder stays
    /// marked as customized; an empty one is not.
    /// </summary>
    public static bool DesktopIniHasOtherSettings(string text) =>
        text.Split('\n')
            .Select(line => line.Trim().TrimStart('\uFEFF'))
            .Where(line => line.Length > 0 && line[0] is not ';' and not '[' && line.Contains('='))
            .Any(line => !s_iconKeys.Contains(line[..line.IndexOf('=')].Trim(), StringComparer.OrdinalIgnoreCase));
}
