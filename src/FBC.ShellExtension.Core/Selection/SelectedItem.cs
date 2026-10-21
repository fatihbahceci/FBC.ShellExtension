using System.Text.Json.Serialization;
using FBC.ShellExtension.Core.Configuration;

namespace FBC.ShellExtension.Core.Selection;

[JsonConverter(typeof(CamelCaseEnumConverter<ItemKind>))]
public enum ItemKind
{
    File,
    Folder,

    /// <summary>The empty area of a folder window; the item's path is that folder.</summary>
    Background,

    /// <summary>A drive root such as <c>C:\</c>.</summary>
    Drive,
}

/// <summary>
/// One item the menu was opened on. The path parts follow Windows rules on every OS, so the
/// placeholder values are the same in tests on Linux as in Explorer.
/// </summary>
public sealed record SelectedItem(string Path, ItemKind Kind)
{
    /// <summary>
    /// The kind Explorer's selection gives a path: a drive root (<c>C:\</c>) is a drive, checked
    /// first and without touching the disk; otherwise <paramref name="isDirectory"/> decides.
    /// </summary>
    public static ItemKind Classify(string path, Func<string, bool> isDirectory)
    {
        if (path.Length == 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && path[2] == '\\')
        {
            return ItemKind.Drive;
        }

        return isDirectory(path) ? ItemKind.Folder : ItemKind.File;
    }

    /// <summary>
    /// The folder the user is looking at: the parent of a file or folder, the folder itself
    /// for <see cref="ItemKind.Background"/>, the root itself for a drive or share root.
    /// </summary>
    [JsonIgnore]
    public string Directory => Kind switch
    {
        ItemKind.Background => Path,
        _ when WindowsPath.IsRoot(Path) => Path,
        _ => WindowsPath.GetDirectoryName(Path),
    };

    /// <summary>The last path segment; for a drive, its letter.</summary>
    [JsonIgnore]
    public string Name => Kind == ItemKind.Drive
        ? Path[..1]
        : WindowsPath.GetFileName(Path);

    /// <summary>The extension with its dot (<c>.txt</c>); empty for anything but a file.</summary>
    [JsonIgnore]
    public string Extension => Kind == ItemKind.File
        ? WindowsPath.GetExtension(Name)
        : "";

    [JsonIgnore]
    public string NameWithoutExtension => Name[..^Extension.Length];

    /// <summary>The full path without the extension, like cmd's <c>%~dpn1</c>.</summary>
    [JsonIgnore]
    public string PathWithoutExtension => Path[..^Extension.Length];
}
