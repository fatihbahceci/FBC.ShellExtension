using System.ComponentModel;
using System.Diagnostics;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;
using FBC.ShellExtension.Runner.UI;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// The tools that ask for a name, arguments or a confirmation and then act at once, without
/// the progress window (ADR-0019). Each speaks only when something failed.
/// </summary>
internal static partial class ToolRunner
{
    private const int MaxListLines = 15;

    private static int RunPack(RunJob job)
    {
        string title = Strings.RunnerWindowTitle(job.Label);
        List<SelectedItem> items = Accepted(job, BuiltinTool.PackToFolder, out List<string> errors);
        if (items.Count == 0)
        {
            return Finish(job, Strings.ToolPackFailed(), errors);
        }

        // A search or a library can show items from several folders: "next to them" would be no place.
        string folder = items[0].Directory;
        if (items.Any(item => !string.Equals(item.Directory, folder, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add(Strings.ToolPackDifferentFolders());
            return Finish(job, "", errors);
        }

        string suggested = FileTools.FreeName(folder, items.Count == 1 ? items[0].NameWithoutExtension : Strings.PromptNewFolderName());
        string question = items.Count == 1 ? Strings.PromptPackQuestionOne(TextPrompt.ShortName(items[0].Name)) : Strings.PromptPackQuestion(items.Count);
        if (TextPrompt.Ask(new TextPromptOptions(title, question, suggested, Validate: name => FileTools.NameProblem(folder, name))) is not { } name)
        {
            return 0;
        }

        try
        {
            errors.AddRange(FileTools.PackInto(items, folder, name));
        }
        catch (Exception ex) when (ex is ToolFailedException or IOException or UnauthorizedAccessException)
        {
            errors.Add(ex.Message);
        }

        return Finish(job, Strings.ToolPackFailed(), errors);
    }

    /// <summary>In the folder the user is looking at: the background's folder, or the selected item's parent.</summary>
    private static int RunNewFolder(RunJob job)
    {
        string title = Strings.RunnerWindowTitle(job.Label);
        string folder = job.Items[0].Directory;
        string shown = Path.GetFileName(folder.TrimEnd('\\')) is { Length: > 0 } name ? name : folder;
        string suggested = FileTools.FreeName(folder, Strings.PromptNewFolderName());
        if (TextPrompt.Ask(new TextPromptOptions(title, Strings.PromptNewFolderQuestion(TextPrompt.ShortName(shown)), suggested,
            Validate: candidate => FileTools.NameProblem(folder, candidate))) is not { } chosen)
        {
            return 0;
        }

        var errors = new List<string>();
        try
        {
            FileTools.NewFolder(folder, chosen);
        }
        catch (Exception ex) when (ex is ToolFailedException or IOException or UnauthorizedAccessException)
        {
            errors.Add(ex.Message);
        }

        return Finish(job, "", errors);
    }

    /// <summary>One program at a time, named in the question; it starts in its own folder.</summary>
    private static int RunWithParameters(RunJob job)
    {
        string title = Strings.RunnerWindowTitle(job.Label);
        List<SelectedItem> items = Accepted(job, BuiltinTool.RunWithParameters, out List<string> errors);
        if (items.Count != 1)
        {
            // Several programs would all get arguments typed for the one the question names.
            errors.Add(Strings.ToolRunOneProgram());
            return Finish(job, "", errors);
        }

        if (TextPrompt.Ask(new TextPromptOptions(title, Strings.PromptRunQuestion(TextPrompt.ShortName(items[0].Name)), "",
            Hint: Strings.PromptRunHint(), OkText: Strings.PromptRunOk())) is not { } arguments)
        {
            return 0;
        }

        foreach (SelectedItem item in items)
        {
            try
            {
                using Process? process = Process.Start(new ProcessStartInfo(item.Path)
                {
                    Arguments = arguments,
                    UseShellExecute = true,
                    WorkingDirectory = item.Directory,
                });
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
            {
                // The user said no to the elevation the program asks for.
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                errors.Add(Strings.ToolRunFailed(item.Path, ex.Message));
            }
        }

        return Finish(job, "", errors);
    }

    private static int RunUnpack(RunJob job)
    {
        List<SelectedItem> items = Accepted(job, BuiltinTool.UnpackFolder, out List<string> errors);
        foreach (SelectedItem item in items)
        {
            try
            {
                FileTools.Unpack(item.Path);
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

        return Finish(job, Strings.ToolUnpackFailed(), errors);
    }

    /// <summary>The items the tool takes; a line for each it does not.</summary>
    private static List<SelectedItem> Accepted(RunJob job, BuiltinTool tool, out List<string> errors)
    {
        errors = [.. job.Items.Where(item => !BuiltinTools.Accepts(tool, item.Kind)).Select(item => Strings.ToolWrongKind(item.Path))];
        return [.. job.Items.Where(item => BuiltinTools.Accepts(tool, item.Kind))];
    }

    /// <summary>
    /// No: the default button, so Enter does not delete. Also called from the progress window's
    /// worker thread, where it must still come to the front.
    /// </summary>
    private static bool Confirm(string text, string title) =>
        MessageBoxW(0, text, title, MB_YESNO | MB_ICONWARNING | MB_DEFBUTTON2 | MB_TOPMOST | MB_SETFOREGROUND) == IDYES;

    private static string ListLines(IEnumerable<string> lines)
    {
        List<string> all = [.. lines];
        IEnumerable<string> shown = all.Take(MaxListLines);
        return all.Count > MaxListLines
            ? string.Join("\n", shown.Append(Strings.ConfirmListMore(all.Count - MaxListLines)))
            : string.Join("\n", shown);
    }

    /// <summary>The errors under <paramref name="heading"/>, or the version note when all went well.</summary>
    private static int Finish(RunJob job, string heading, List<string> errors)
    {
        string title = Strings.RunnerWindowTitle(job.Label);
        if (errors.Count > 0)
        {
            string body = ErrorLines(errors);
            MessageBoxW(0, heading.Length > 0 ? $"{heading}\n\n{body}" : body, title, MB_ICONERROR);
            return 1;
        }

        if (Program.VersionMismatch(job) is { } mismatch)
        {
            MessageBoxW(0, mismatch, title, MB_ICONINFORMATION);
        }

        return 0;
    }
}
