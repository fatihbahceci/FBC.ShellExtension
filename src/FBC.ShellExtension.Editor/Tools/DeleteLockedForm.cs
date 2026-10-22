using System.ComponentModel;
using System.Diagnostics;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Unlock and delete (ADR-0031): the programs that use the selected items, from Restart
/// Manager; closed one by one on request, as the user would close them, then the items go to
/// the Recycle Bin. Or Windows deletes them at the next restart.
/// </summary>
internal partial class DeleteLockedForm : Form
{
    // A program asked to close may first ask about saving; then it is the user's to end.
    private static readonly TimeSpan s_closeWait = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_endWait = TimeSpan.FromSeconds(5);

    private readonly IReadOnlyList<SelectedItem> _items;
    private RestartManager? _manager;
    private List<Holder> _holders = [];
    private string _notes = "";

    public DeleteLockedForm(IReadOnlyList<SelectedItem> items)
    {
        _items = items;
        InitializeComponent();
        ApplyStrings();
    }

    private void ApplyStrings()
    {
        selectionLabel.Text = ToolWindows.Selection(_items);
        statusLabel.Text = Strings.LockedSearching();
        programColumn.Text = Strings.LockedProgramColumn();
        idColumn.Text = Strings.LockedIdColumn();
        kindColumn.Text = Strings.LockedKindColumn();
        hintLabel.Text = Strings.LockedHint();
        deleteButton.Text = Strings.LockedCloseAndDelete();
        restartButton.Text = Strings.LockedAtRestart();
        cancelButton.Text = Strings.EditorCancel();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Search? search = await ToolWindows.WithProgressAsync(this, Strings.LockedSearching(), cancellation =>
        {
            IReadOnlyList<string> files = FileTools.LockCheckFiles(_items, out bool cut, out List<string> unreadable, cancellation);
            RestartManager? manager = null;
            try
            {
                manager = files.Count > 0 ? RestartManager.ForFiles(files) : null;
                return new Search(manager, manager?.Holders() ?? [], cut, unreadable.Count, null);
            }
            catch (Win32Exception ex)
            {
                manager?.Dispose();
                return new Search(null, [], cut, unreadable.Count, ex.Message);
            }
        });

        if (search is null)
        {
            Close();
            return;
        }

        _manager = search.Manager;
        _holders = search.Holders;
        _notes = string.Join(" ", new[]
        {
            search.Failure is { } failure ? Strings.LockedSearchFailed(failure) : null,
            search.Cut ? Strings.LockedTooMany(FileTools.MaxLockedFiles) : null,
            search.Unreadable > 0 ? Strings.ReplaceUnreadable(search.Unreadable) : null,
        }.OfType<string>());
        ShowHolders();
    }

    private sealed record Search(RestartManager? Manager, List<Holder> Holders, bool Cut, int Unreadable, string? Failure);

    private void ShowHolders()
    {
        holderList.BeginUpdate();
        holderList.Items.Clear();
        holderList.Items.AddRange([.. _holders.Select(holder => new ListViewItem(
        [
            holder.Name,
            holder.ProcessId.ToString(System.Globalization.CultureInfo.CurrentCulture),
            KindText(holder),
        ]))]);
        holderList.EndUpdate();

        string status = _holders.Count == 0 ? Strings.LockedNone() : Strings.LockedHolders();
        statusLabel.Text = _notes.Length > 0 ? $"{status} {_notes}" : status;

        // Closing some of them frees nothing while Explorer or a service still holds a file.
        deleteButton.Text = _holders.Count == 0 ? Strings.LockedDelete() : Strings.LockedCloseAndDelete();
        deleteButton.Enabled = _holders.All(holder => holder.Closable);
        restartButton.Enabled = true;
    }

    private static string KindText(Holder holder) => holder.Kind switch
    {
        HolderKind.Service => Strings.LockedKindService(),
        HolderKind.Explorer => Strings.LockedKindExplorer(),
        HolderKind.Critical => Strings.LockedKindCritical(),
        HolderKind.Console => Strings.LockedKindConsole(),
        _ => Strings.LockedKindProgram(),
    };

    private async void OnDelete(object? sender, EventArgs e)
    {
        Enabled = false;
        statusLabel.Text = Strings.LockedClosing();
        List<string> notClosed;
        try
        {
            notClosed = await CloseAsync(_holders);
        }
        finally
        {
            Enabled = true;
        }

        try
        {
            // Up to 10,000 files to ask about: not on the window's thread.
            RestartManager? manager = _manager;
            _holders = manager is null ? [] : await Task.Run(manager.Holders);
        }
        catch (Win32Exception ex)
        {
            _notes = Strings.LockedSearchFailed(ex.Message);
        }

        if (_holders.Count > 0)
        {
            ShowHolders();
            ToolWindows.ShowErrors(this, Text, Strings.LockedStillUsed(), [.. notClosed.Count > 0 ? notClosed : _holders.Select(holder => holder.Name)]);
            return;
        }

        List<string> errors = ShellFileOperation.Recycle(this, [.. _items.Select(item => item.Path)]);
        ToolWindows.ShowErrors(this, Text, Strings.DeleteNotDeleted(), errors);
        Close();
    }

    /// <summary>Each program asked to close as a user would; one that does not, ended if the user says so.</summary>
    /// <returns>The names of the programs still running.</returns>
    private async Task<List<string>> CloseAsync(IReadOnlyList<Holder> holders)
    {
        var notClosed = new List<string>();
        foreach (Holder holder in holders)
        {
            Process process;
            try
            {
                process = Process.GetProcessById(holder.ProcessId);

                // A handle held from here on: the checks, the wait and an end all act on this very
                // process, not on whatever gets its id should it exit meanwhile. Opening it fails
                // for an elevated program or another user's: not ours to close.
                _ = process.SafeHandle;

                // The id may already be another program's: the start time tells.
                if (process.StartTime.ToFileTime() != holder.StartTime)
                {
                    process.Dispose();
                    continue;
                }
            }
            catch (ArgumentException)
            {
                // Gone already.
                continue;
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                notClosed.Add(holder.Name);
                continue;
            }

            using (process)
            {
                bool closed = process.CloseMainWindow() && await ExitedAsync(process, s_closeWait);
                if (!closed && MessageBox.Show(this, Strings.LockedEndQuestion(holder.Name), Text, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    try
                    {
                        process.Kill();
                        closed = await ExitedAsync(process, s_endWait);
                    }
                    catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
                    {
                        closed = process.HasExited;
                    }
                }

                if (!closed)
                {
                    notClosed.Add(holder.Name);
                }
            }
        }

        return notClosed;
    }

    private static async Task<bool> ExitedAsync(Process process, TimeSpan wait)
    {
        using var timeout = new CancellationTokenSource(wait);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            return true;
        }
        catch (Exception ex) when (ex is OperationCanceledException or Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    private async void OnRestart(object? sender, EventArgs e)
    {
        // A second click would ask UAC twice; Close them and delete would race the marking.
        bool? marked;
        Enabled = false;
        try
        {
            marked = await RebootDeletion.RequestAsync(this, Text, [.. _items.Select(item => item.Path)]);
        }
        finally
        {
            Enabled = true;
        }

        if (marked == true)
        {
            MessageBox.Show(this, Strings.LockedScheduled(_items.Count), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
    }

    // Not a dialog: the window is the program, and Cancel ends it.
    private void OnClose(object? sender, EventArgs e) => Close();

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _manager?.Dispose();
        base.OnFormClosed(e);
    }
}
