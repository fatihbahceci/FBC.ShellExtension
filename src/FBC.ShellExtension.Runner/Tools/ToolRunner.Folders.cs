using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;
using FBC.ShellExtension.Runner.UI;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// The tools that ask for a folder or an icon with Windows' own pickers, and copy and move with
/// Windows' own engine (ADR-0026). Like the other organize tools they speak only when something
/// failed; Windows' own dialogs report the problems of single files.
/// </summary>
internal static partial class ToolRunner
{
    // One per picker: Windows remembers the last folder chosen under each.
    private static readonly Guid s_copyPicker = new("E0D39EFD-C08D-47B8-BD78-949A767B4981");
    private static readonly Guid s_movePicker = new("CFB34505-C811-4736-AE7F-40D4D079DA48");
    private static readonly Guid s_linkPicker = new("9A14929C-D613-43C2-9897-4FCF9EEAD091");

    private static int RunCopyOrMove(RunJob job, bool move)
    {
        BuiltinTool tool = move ? BuiltinTool.MoveToFolder : BuiltinTool.CopyToFolder;
        List<SelectedItem> items = Accepted(job, tool, out List<string> errors);
        if (items.Count == 0)
        {
            return Finish(job, "", errors);
        }

        string title = items.Count == 1
            ? (move ? Strings.PickMoveTitleOne(TextPrompt.ShortName(items[0].Name)) : Strings.PickCopyTitleOne(TextPrompt.ShortName(items[0].Name)))
            : (move ? Strings.PickMoveTitle(items.Count) : Strings.PickCopyTitle(items.Count));
        try
        {
            if (FolderPicker.Pick(title, move ? Strings.PickMoveOk() : Strings.PickCopyOk(), move ? s_movePicker : s_copyPicker, items[0].Directory) is not { } folder)
            {
                return 0;
            }

            errors.AddRange(ShellFileOperation.Run(move, [.. items.Select(item => item.Path)], folder));
        }
        catch (ToolFailedException ex)
        {
            errors.Add(ex.Message);
        }

        return Finish(job, "", errors);
    }

    /// <summary>A junction for each folder, a symbolic link for each file, in a folder the user picks.</summary>
    private static int RunCreateLink(RunJob job)
    {
        List<SelectedItem> items = Accepted(job, BuiltinTool.CreateLink, out List<string> errors);
        if (items.Count == 0)
        {
            return Finish(job, "", errors);
        }

        string title = items.Count == 1 ? Strings.PickLinkTitleOne(TextPrompt.ShortName(items[0].Name)) : Strings.PickLinkTitle(items.Count);
        string? folder;
        try
        {
            folder = FolderPicker.Pick(title, Strings.PickLinkOk(), s_linkPicker, items[0].Directory);
        }
        catch (ToolFailedException ex)
        {
            errors.Add(ex.Message);
            return Finish(job, "", errors);
        }

        if (folder is null)
        {
            return 0;
        }

        foreach (SelectedItem item in items)
        {
            if (FileTools.LinkProblem(item, folder) is { } problem)
            {
                errors.Add(problem);
                continue;
            }

            string link = Path.Combine(folder, FileTools.FreeItemName(folder, item.Name, item.Kind == ItemKind.File));
            try
            {
                if (item.Kind == ItemKind.Folder)
                {
                    Links.CreateJunction(link, item.Path);
                }
                else
                {
                    Links.CreateFileLink(link, item.Path);
                }
            }
            catch (ToolFailedException ex)
            {
                errors.Add(ex.Message);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add(Strings.ToolLinkFailed(item.Name, ex.Message));
            }
        }

        return Finish(job, Strings.ToolLinksFailed(), errors);
    }

    /// <summary>What the folders hold goes into a new folder next to them; the ones left empty go.</summary>
    private static int RunJoinFolders(RunJob job)
    {
        string title = Strings.RunnerWindowTitle(job.Label);
        List<SelectedItem> folders = Accepted(job, BuiltinTool.JoinFolders, out List<string> errors);
        if (FileTools.JoinProblem(folders) is { } problem)
        {
            errors.Add(problem);
            return Finish(job, "", errors);
        }

        string parent = folders[0].Directory;
        string suggested = FileTools.FreeName(parent, Strings.PromptNewFolderName());
        if (TextPrompt.Ask(new TextPromptOptions(title, Strings.PromptJoinQuestion(folders.Count), suggested,
            Validate: name => FileTools.NameProblem(parent, name))) is not { } chosen)
        {
            return 0;
        }

        try
        {
            // Listed before the new folder exists, which then holds nothing of the old ones yet.
            IReadOnlyList<string> sources = FileTools.JoinSources(folders);
            string target = FileTools.NewFolder(parent, chosen);
            if (sources.Count > 0)
            {
                errors.AddRange(ShellFileOperation.Run(move: true, sources, target));
            }

            // A skip in Windows' question, or a cancel, leaves something behind: that folder stays.
            // One that cannot go (a console open in it) does not keep the others.
            var kept = new List<string>();
            foreach (SelectedItem folder in folders)
            {
                try
                {
                    if (!FileTools.RemoveIfEmpty(folder.Path))
                    {
                        kept.Add(folder.Name);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    errors.Add(Strings.ToolItemFailed(folder.Path, ex.Message));
                }
            }

            if (kept.Count > 0)
            {
                errors.Add($"{Strings.ToolJoinNotRemoved()}\n{string.Join("\n", kept)}");
            }
        }
        catch (Exception ex) when (ex is ToolFailedException or IOException or UnauthorizedAccessException)
        {
            errors.Add(ex.Message);
        }

        return Finish(job, "", errors);
    }

    /// <summary>One icon for every selected folder, chosen in Windows' icon picker.</summary>
    private static int RunChangeFolderIcon(RunJob job)
    {
        List<SelectedItem> folders = IconFolders(job, BuiltinTool.ChangeFolderIcon, out List<string> errors);
        if (folders.Count == 0)
        {
            return Finish(job, Strings.ToolIconsFailed(), errors);
        }

        (string File, int Index) start = FolderIcons.Current(folders[0].Path) ?? (FolderIcons.DefaultIconFile, 0);
        try
        {
            if (FolderIcons.Pick(start.File, start.Index) is not { } icon)
            {
                return 0;
            }

            // The picker hands back what it was given, %SystemRoot% and all; desktop.ini keeps that form.
            if (!File.Exists(Environment.ExpandEnvironmentVariables(icon.File)))
            {
                errors.Add(Strings.ToolIconFileMissing(icon.File));
                return Finish(job, "", errors);
            }

            foreach (SelectedItem folder in folders)
            {
                try
                {
                    FolderIcons.Set(folder.Path, icon.File, icon.Index);
                }
                catch (ToolFailedException ex)
                {
                    errors.Add(ex.Message);
                }
            }
        }
        catch (ToolFailedException ex)
        {
            errors.Add(ex.Message);
        }

        return Finish(job, Strings.ToolIconsFailed(), errors);
    }

    private static int RunRestoreFolderIcon(RunJob job)
    {
        List<SelectedItem> folders = IconFolders(job, BuiltinTool.RestoreFolderIcon, out List<string> errors);
        foreach (SelectedItem folder in folders)
        {
            try
            {
                FolderIcons.Restore(folder.Path);
            }
            catch (ToolFailedException ex)
            {
                errors.Add(ex.Message);
            }
        }

        return Finish(job, Strings.ToolIconsFailed(), errors);
    }

    /// <summary>
    /// The folders whose icon the tools may change; a line for each they leave alone. A drive
    /// root has no folder icon. Documents, Pictures and the like keep Windows' own icon: a
    /// restore would take it away.
    /// </summary>
    private static List<SelectedItem> IconFolders(RunJob job, BuiltinTool tool, out List<string> errors)
    {
        List<SelectedItem> accepted = Accepted(job, tool, out errors);
        var folders = new List<SelectedItem>();
        foreach (SelectedItem folder in accepted)
        {
            if (FileTools.IconProblem(folder) is { } problem)
            {
                errors.Add(problem);
            }
            else if (FolderIcons.IsWindowsFolder(folder.Path))
            {
                errors.Add(Strings.ToolIconSpecialFolder(folder.Name));
            }
            else
            {
                folders.Add(folder);
            }
        }

        return folders;
    }
}
