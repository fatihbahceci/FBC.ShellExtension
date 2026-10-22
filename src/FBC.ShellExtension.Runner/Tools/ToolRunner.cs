using System.ComponentModel;
using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;
using FBC.ShellExtension.Runner.Execution;
using FBC.ShellExtension.Runner.UI;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// Runs a built-in tool inside the runner, without another process. The clipboard tools show
/// no window, only a message when nothing could be copied (ADR-0015); the others run in the
/// progress window, one item after another (ADR-0017).
/// </summary>
internal static partial class ToolRunner
{
    private const int MaxErrorLines = 15;

    public static int Run(RunJob job, BuiltinTool tool) => tool switch
    {
        BuiltinTool.WikiImport => WikiTool.Run(job),
        _ when BuiltinTools.UsesEditorWindow(tool) => EditorWindow.Run(job),
        _ when BuiltinTools.UsesWindow(tool) => RunInWindow(job, tool),
        BuiltinTool.AppendClipboard => RunAppend(job),
        BuiltinTool.PackToFolder => RunPack(job),
        BuiltinTool.NewFolder => RunNewFolder(job),
        BuiltinTool.RunWithParameters => RunWithParameters(job),
        BuiltinTool.UnpackFolder => RunUnpack(job),
        BuiltinTool.CopyToFolder => RunCopyOrMove(job, move: false),
        BuiltinTool.MoveToFolder => RunCopyOrMove(job, move: true),
        BuiltinTool.CreateLink => RunCreateLink(job),
        BuiltinTool.JoinFolders => RunJoinFolders(job),
        BuiltinTool.ChangeFolderIcon => RunChangeFolderIcon(job),
        BuiltinTool.RestoreFolderIcon => RunRestoreFolderIcon(job),
        _ => RunToClipboard(job, tool),
    };

    /// <summary>Adds the clipboard's text to every selected file; speaks only when something failed (ADR-0018).</summary>
    private static int RunAppend(RunJob job)
    {
        string title = Strings.RunnerWindowTitle(job.Label);
        string? text = Clipboard.GetText(Clipboard.TextChars, out bool cut);
        if (cut || string.IsNullOrEmpty(text))
        {
            MessageBoxW(0, cut ? Strings.ToolClipboardTooLarge(Clipboard.TextChars / (1024 * 1024)) : Strings.ToolClipboardEmpty(), title, MB_ICONERROR);
            return 1;
        }

        var errors = new List<string>();
        foreach (SelectedItem item in job.Items)
        {
            if (item.Kind != ItemKind.File)
            {
                errors.Add(Strings.ToolNotAFile(item.Path));
                continue;
            }

            try
            {
                FileTools.AppendText(item.Path, text, CodePages.Ansi);
            }
            catch (ToolFailedException ex)
            {
                errors.Add(ex.Message);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add(Strings.ToolItemFailed(item.Path, ex.Message));
            }
        }

        if (errors.Count > 0)
        {
            MessageBoxW(0, $"{Strings.ToolAppendFailed()}\n\n{ErrorLines(errors)}", title, MB_ICONERROR);
            return 1;
        }

        if (Program.VersionMismatch(job) is { } mismatch)
        {
            MessageBoxW(0, mismatch, title, MB_ICONINFORMATION);
        }

        return 0;
    }

    private static int RunInWindow(RunJob job, BuiltinTool tool)
    {
        IReadOnlyList<SelectedItem> targets = FileTools.Targets(tool, job.Items);
        var input = new ToolInput(Ansi: CodePages.Ansi, Copier: new WindowsCopier());
        if (tool == BuiltinTool.Split)
        {
            // Asked once for the whole selection, before anything is written.
            if (SplitPrompt.Ask(targets, Strings.RunnerWindowTitle(job.Label)) is not { } partSize)
            {
                return 0;
            }

            input = input with { PartSize = partSize };
        }
        else if (tool == BuiltinTool.DeletePermanently)
        {
            if (!Confirm(Strings.ConfirmDeletePermanently(targets.Count, ListLines(targets.Select(item => item.Path))), Strings.RunnerWindowTitle(job.Label)))
            {
                return 0;
            }
        }
        else if (tool == BuiltinTool.DeleteEmptyFolders)
        {
            // Asked on the worker thread once the scan has found something.
            string title = Strings.RunnerWindowTitle(job.Label);
            input = input with { Confirm = text => Confirm(text, title) };
        }
        else if (tool == BuiltinTool.VerifyChecksum)
        {
            // Read now: while the files are hashed the user may copy something else.
            // A checksum is short: what is past the limit is not one.
            input = input with { Clipboard = Clipboard.GetText(Clipboard.ChecksumChars, out _) };
        }

        // The window shows each item and its lines, and stays open: the result is the point.
        job.Command = new CommandDefinition
        {
            ShowOutput = true,
            CloseOnSuccess = false,
            MultiSelect = MultiSelectMode.PerFileSequential,
        };
        IReadOnlyList<PlannedProcess> plan = [.. targets.Select(item => new PlannedProcess(tool.ToString(), "", item.Directory, [item]))];
        var window = new RunnerWindow(job, plan, Program.VersionMismatch(job));
        return window.Run(new ToolJobRunner(tool, input));
    }

    private static int RunToClipboard(RunJob job, BuiltinTool tool)
    {
        string title = Strings.RunnerWindowTitle(job.Label);
        ToolResult result = ClipboardTools.Run(tool, job.Items, new WindowsToolHost());
        if (result.Text is null)
        {
            MessageBoxW(0, $"{Strings.ToolNothingCopied()}\n\n{ErrorLines(result.Errors)}", title, MB_ICONERROR);
            return 1;
        }

        try
        {
            Clipboard.SetText(result.Text);
        }
        catch (ClipboardBusyException)
        {
            MessageBoxW(0, Strings.ToolClipboardBusy(), title, MB_ICONERROR);
            return 1;
        }
        catch (Win32Exception ex)
        {
            MessageBoxW(0, Strings.ToolClipboardFailed(ex.Message), title, MB_ICONERROR);
            return 1;
        }

        // Explorer keeps an old DLL until it restarts (G-0101); say so even when all went well.
        if (Program.VersionMismatch(job) is { } mismatch)
        {
            MessageBoxW(0, mismatch, title, MB_ICONINFORMATION);
        }

        return 0;
    }

    private static string ErrorLines(IReadOnlyList<string> errors)
    {
        IEnumerable<string> shown = errors.Take(MaxErrorLines);
        return errors.Count > MaxErrorLines
            ? string.Join("\n", shown.Append("…"))
            : string.Join("\n", shown);
    }
}
