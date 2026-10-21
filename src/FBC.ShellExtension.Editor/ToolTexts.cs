using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor;

/// <summary>What the editor shows for a built-in tool: its name to choose it by, and what it does.</summary>
internal static class ToolTexts
{
    public static string Name(BuiltinTool tool) => tool switch
    {
        BuiltinTool.CopyPath => Strings.ToolCopyPath(),
        BuiltinTool.CopyName => Strings.ToolCopyName(),
        BuiltinTool.CopyUncPath => Strings.ToolCopyUncPath(),
        BuiltinTool.CopyContent => Strings.ToolCopyContent(),
        BuiltinTool.Checksum => Strings.ToolChecksum(),
        BuiltinTool.VerifyChecksum => Strings.ToolVerifyChecksum(),
        BuiltinTool.Split => Strings.ToolSplit(),
        BuiltinTool.Join => Strings.ToolJoin(),
        BuiltinTool.CountLines => Strings.ToolCountLines(),
        BuiltinTool.CopyInternetPath => Strings.ToolCopyInternetPath(),
        BuiltinTool.AppendClipboard => Strings.ToolAppendClipboard(),
        BuiltinTool.Duplicate => Strings.ToolDuplicate(),
        BuiltinTool.FolderSize => Strings.ToolFolderSize(),
        BuiltinTool.PackToFolder => Strings.ToolPackToFolder(),
        BuiltinTool.NewFolder => Strings.ToolNewFolder(),
        BuiltinTool.RunWithParameters => Strings.ToolRunWithParameters(),
        BuiltinTool.UnpackFolder => Strings.ToolUnpackFolder(),
        BuiltinTool.DeleteEmptyFolders => Strings.ToolDeleteEmptyFolders(),
        BuiltinTool.DeletePermanently => Strings.ToolDeletePermanently(),
        BuiltinTool.CopyTitle => Strings.ToolCopyTitle(),
        BuiltinTool.WikiImport => Strings.ToolWikiImport(),
        _ => tool.ToString(),
    };

    public static string Note(BuiltinTool tool) => tool switch
    {
        BuiltinTool.CopyPath => Strings.ToolCopyPathNote(),
        BuiltinTool.CopyName => Strings.ToolCopyNameNote(),
        BuiltinTool.CopyUncPath => Strings.ToolCopyUncPathNote(),
        BuiltinTool.CopyContent => Strings.ToolCopyContentNote(ClipboardTools.MaxContentMegabytes),
        BuiltinTool.Checksum => Strings.ToolChecksumNote(),
        BuiltinTool.VerifyChecksum => Strings.ToolVerifyChecksumNote(),
        BuiltinTool.Split => Strings.ToolSplitNote(),
        BuiltinTool.Join => Strings.ToolJoinNote(),
        BuiltinTool.CountLines => Strings.ToolCountLinesNote(),
        BuiltinTool.CopyInternetPath => Strings.ToolCopyInternetPathNote(),
        BuiltinTool.AppendClipboard => Strings.ToolAppendClipboardNote(),
        BuiltinTool.Duplicate => Strings.ToolDuplicateNote(),
        BuiltinTool.FolderSize => Strings.ToolFolderSizeNote(),
        BuiltinTool.PackToFolder => Strings.ToolPackToFolderNote(),
        BuiltinTool.NewFolder => Strings.ToolNewFolderNote(),
        BuiltinTool.RunWithParameters => Strings.ToolRunWithParametersNote(),
        BuiltinTool.UnpackFolder => Strings.ToolUnpackFolderNote(),
        BuiltinTool.DeleteEmptyFolders => Strings.ToolDeleteEmptyFoldersNote(),
        BuiltinTool.DeletePermanently => Strings.ToolDeletePermanentlyNote(),
        BuiltinTool.CopyTitle => Strings.ToolCopyTitleNote(),
        BuiltinTool.WikiImport => Strings.ToolWikiImportNote(),
        _ => "",
    };
}
