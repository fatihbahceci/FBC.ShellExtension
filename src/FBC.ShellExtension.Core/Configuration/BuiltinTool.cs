using System.Text.Json.Serialization;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Configuration;

/// <summary>
/// A tool FBCTools carries itself. A command entry names one in <c>tool</c> instead of a program
/// in <c>command</c>; the runner does the work in its own process (ADR-0015, ADR-0017).
/// </summary>
[JsonConverter(typeof(CamelCaseEnumConverter<BuiltinTool>))]
public enum BuiltinTool
{
    /// <summary>The full path of every item, one per line.</summary>
    CopyPath,

    /// <summary>The name of every item, one per line.</summary>
    CopyName,

    /// <summary>Like <see cref="CopyPath"/>, with a mapped network drive written as <c>\\server\share</c>.</summary>
    CopyUncPath,

    /// <summary>The text of the selected files.</summary>
    CopyContent,

    /// <summary>MD5, SHA-1, SHA-256 and SHA-512 of every file, shown in the runner's window.</summary>
    Checksum,

    /// <summary>Checks files against a checksum file, a <c>.sha256</c> next to them or the clipboard.</summary>
    VerifyChecksum,

    /// <summary>Cuts a file into <c>.001</c>, <c>.002</c> … parts and writes its SHA-256 next to them.</summary>
    Split,

    /// <summary>Puts <c>.001</c>, <c>.002</c> … parts back together and checks the result.</summary>
    Join,

    /// <summary>The number of lines of every file.</summary>
    CountLines,

    /// <summary>Every item as a <c>file:///</c> address, one per line.</summary>
    CopyInternetPath,

    /// <summary>Adds the clipboard's text at the end of every selected file.</summary>
    AppendClipboard,

    /// <summary>Copies every file next to itself as "name - Copy".</summary>
    Duplicate,

    /// <summary>How much a folder holds, with its files and subfolders.</summary>
    FolderSize,

    /// <summary>Moves the selected items into a new folder whose name it asks for.</summary>
    PackToFolder,

    /// <summary>Makes a folder, whose name it asks for, in the folder the user is looking at.</summary>
    NewFolder,

    /// <summary>Starts the selected program with arguments it asks for.</summary>
    RunWithParameters,

    /// <summary>Moves what a folder holds up into its parent and removes the folder.</summary>
    UnpackFolder,

    /// <summary>Removes the empty folders under the selected ones, after a confirmation.</summary>
    DeleteEmptyFolders,

    /// <summary>Deletes without the Recycle Bin, after a confirmation.</summary>
    DeletePermanently,

    /// <summary>The "Title" property of every file (a song's, a document's), one per line.</summary>
    CopyTitle,

    /// <summary>Sends a markdown file and its images to a Wiki.js 2 page (ADR-0020).</summary>
    WikiImport,
}

/// <summary>What the runner and the config rules need to know about each tool.</summary>
public static class BuiltinTools
{
    /// <summary>The config version that introduced the tool (ADR-0015, ADR-0017, ADR-0018).</summary>
    public static int SinceVersion(BuiltinTool tool) => tool switch
    {
        BuiltinTool.CopyPath or BuiltinTool.CopyName or BuiltinTool.CopyUncPath or BuiltinTool.CopyContent => 3,
        BuiltinTool.Checksum or BuiltinTool.VerifyChecksum or BuiltinTool.Split or BuiltinTool.Join or BuiltinTool.CountLines => 4,
        BuiltinTool.CopyInternetPath or BuiltinTool.AppendClipboard or BuiltinTool.Duplicate or BuiltinTool.FolderSize => 5,
        BuiltinTool.WikiImport => 7,
        _ => 6,
    };

    /// <summary>
    /// Runs in the runner's progress window, one item after another, with a cancel button; the
    /// window stays open after success, since what the tool found or made is the point. The
    /// clipboard tools run without a window and speak only when something failed.
    /// </summary>
    public static bool UsesWindow(BuiltinTool tool) => tool is BuiltinTool.Checksum or BuiltinTool.VerifyChecksum
        or BuiltinTool.Split or BuiltinTool.Join or BuiltinTool.CountLines or BuiltinTool.Duplicate or BuiltinTool.FolderSize
        or BuiltinTool.DeletePermanently or BuiltinTool.DeleteEmptyFolders;

    /// <summary>The kinds of item the tool works on; the template's match rules offer it only for those.</summary>
    public static bool Accepts(BuiltinTool tool, ItemKind kind) => tool switch
    {
        BuiltinTool.CopyPath or BuiltinTool.CopyName or BuiltinTool.CopyUncPath or BuiltinTool.CopyInternetPath
            or BuiltinTool.NewFolder => true,
        BuiltinTool.FolderSize or BuiltinTool.DeleteEmptyFolders => kind is ItemKind.Folder or ItemKind.Background or ItemKind.Drive,
        BuiltinTool.UnpackFolder => kind == ItemKind.Folder,
        BuiltinTool.PackToFolder or BuiltinTool.DeletePermanently => kind is ItemKind.File or ItemKind.Folder,
        _ => kind == ItemKind.File,
    };
}
