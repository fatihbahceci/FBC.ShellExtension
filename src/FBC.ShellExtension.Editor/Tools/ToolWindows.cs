using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// The windows of the built-in tools that need more than a question (ADR-0028). The runner hands
/// the job over; a window works on the items its tool takes, and like the runner's tools it
/// speaks only when something went wrong.
/// </summary>
internal static class ToolWindows
{
    private const int MaxErrorLines = 15;
    private const int MaxShownNames = 3;

    public static void Run(string jobPath)
    {
        RunJob job;
        try
        {
            job = JobFile.ReadAndDelete(jobPath);
        }
        catch (JobFileException ex)
        {
            MessageBox.Show(ex.Message, ProductInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string title = Strings.RunnerWindowTitle(job.Label);
        if (job.Tool is not { } tool || !BuiltinTools.UsesEditorWindow(tool))
        {
            MessageBox.Show(Strings.JobInvalid(job.Tool?.ToString() ?? ""), title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        List<SelectedItem> items = [.. job.Items.Where(item => BuiltinTools.Accepts(tool, item.Kind))];
        List<string> skipped = [.. job.Items.Where(item => !BuiltinTools.Accepts(tool, item.Kind)).Select(item => Strings.ToolWrongKind(item.Path))];
        if (items.Count > 0)
        {
            using Form window = tool switch
            {
                BuiltinTool.Attributes => new AttributesForm(items),
                BuiltinTool.ChangeTime => new ChangeTimeForm(items),
                BuiltinTool.OrganizeIntoFolders => new OrganizeForm(items),
                BuiltinTool.AdvancedRename => new AdvancedRenameForm(items),
                BuiltinTool.FindReplace => new FindReplaceForm(items),
                BuiltinTool.DeleteLocked => new DeleteLockedForm(items),
                BuiltinTool.Encrypt => new EncryptForm(items),
                BuiltinTool.Decrypt => new DecryptForm(items),
                BuiltinTool.SyncFolders => new SyncForm(items),
                _ => new AdvancedDeleteForm(items),
            };
            window.Text = title;
            Application.Run(window);
        }

        ShowErrors(null, title, Strings.ToolWindowNotChanged(), skipped);

        // Explorer keeps an old DLL until it restarts (G-0101), as the runner says too.
        if (job.ShellExtVersion is { } shellExt && shellExt != ProductInfo.InformationalVersion)
        {
            MessageBox.Show(Strings.RunnerVersionMismatch(shellExt, ProductInfo.InformationalVersion), title,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    /// <summary>"Selected: report.pdf", or "5 items selected: a, b, c, …".</summary>
    public static string Selection(IReadOnlyList<SelectedItem> items)
    {
        if (items.Count == 1)
        {
            return Strings.ToolWindowSelectedOne(items[0].Path);
        }

        IEnumerable<string> names = items.Take(MaxShownNames).Select(item => item.Name);
        return Strings.ToolWindowSelected(items.Count, string.Join(", ", items.Count > MaxShownNames ? names.Append("…") : names));
    }

    /// <summary>The lines under <paramref name="heading"/>, in an error box; nothing when there are none.</summary>
    public static void ShowErrors(IWin32Window? owner, string title, string heading, IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        IEnumerable<string> shown = errors.Take(MaxErrorLines);
        string lines = string.Join("\n", errors.Count > MaxErrorLines ? shown.Append(Strings.ConfirmListMore(errors.Count - MaxErrorLines)) : shown);
        MessageBox.Show(owner, $"{heading}\n\n{lines}", title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>Runs <paramref name="work"/> off the UI thread, with the window disabled and a progress window up.</summary>
    /// <returns>What the work returned; null when it was cancelled.</returns>
    public static async Task<T?> WithProgressAsync<T>(Form owner, string message, Func<CancellationToken, T> work)
        where T : class
    {
        using var progress = new ProgressForm(message);
        progress.Show(owner);
        owner.Enabled = false;
        try
        {
            CancellationToken cancellation = progress.Cancellation;
            return await Task.Run(() => work(cancellation), cancellation);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            owner.Enabled = true;
            progress.Close();
        }
    }
}
