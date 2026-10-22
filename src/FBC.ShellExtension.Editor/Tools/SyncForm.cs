using System.Globalization;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Synchronize folders, one way (ADR-0033): what is new or changed in the source goes into the
/// destination. The comparison is listed first; files newer in the destination are not ticked,
/// and what is only there goes to the Recycle Bin only when the box asks for it.
/// </summary>
internal partial class SyncForm : Form
{
    private IReadOnlyList<SyncItem> _plan = [];
    private string _comparedSource = "";
    private string _comparedDestination = "";
    private int _leftAlone;
    private bool _filling;

    public SyncForm(IReadOnlyList<SelectedItem> items)
    {
        InitializeComponent();
        ApplyStrings();
        (string source, string destination) = FileTools.SyncFolders(items);
        sourceBox.Text = source;
        destinationBox.Text = destination;
    }

    private void ApplyStrings()
    {
        sourceLabel.Text = Strings.SyncFrom();
        destinationLabel.Text = Strings.SyncTo();
        browseButton.Text = Strings.SyncBrowse();
        swapButton.Text = Strings.SyncSwap();
        extrasCheck.Text = Strings.SyncExtras();
        hintLabel.Text = Strings.SyncHint();
        compareButton.Text = Strings.SyncCompare();
        pathColumn.Text = Strings.DeletePathColumn();
        actionColumn.Text = Strings.SyncActionColumn();
        sizeColumn.Text = Strings.DeleteSizeColumn();
        modifiedColumn.Text = Strings.DeleteModifiedColumn();
        syncButton.Text = Strings.SyncButton();
        closeButton.Text = Strings.ToolWindowClose();
    }

    private void OnBrowse(object? sender, EventArgs e)
    {
        folderDialog.InitialDirectory = Directory.Exists(destinationBox.Text) ? destinationBox.Text : Path.GetDirectoryName(sourceBox.Text) ?? "";
        if (folderDialog.ShowDialog(this) == DialogResult.OK)
        {
            destinationBox.Text = folderDialog.SelectedPath;
        }
    }

    private void OnSwap(object? sender, EventArgs e) => (sourceBox.Text, destinationBox.Text) = (destinationBox.Text, sourceBox.Text);

    /// <summary>Another folder: the list no longer says what Synchronize would do.</summary>
    private void OnFoldersChanged(object? sender, EventArgs e)
    {
        // Not Directory.Exists: a typed network path would be asked about on every keystroke.
        swapButton.Enabled = destinationBox.Text.Trim().Length > 0;
        _plan = [];
        planList.Items.Clear();
        syncButton.Enabled = false;
        statusLabel.Text = "";
    }

    private async void OnCompare(object? sender, EventArgs e)
    {
        string source = sourceBox.Text;
        string destination = destinationBox.Text.Trim();
        // Then where both really lead: a mapped drive, a subst drive or a junction may hide that one is inside the other.
        string? problem = FileTools.SyncProblem(source, destination)
            ?? (FinalPath.Of(source) is { } realSource && FinalPath.Of(destination) is { } realDestination && FileTools.SyncInside(realSource, realDestination)
                ? Strings.SyncInside()
                : null);
        if (problem is not null)
        {
            MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            destinationBox.Focus();
            return;
        }

        Comparison? comparison = await ToolWindows.WithProgressAsync(this, Strings.SyncComparing(), cancellation =>
        {
            IReadOnlyList<SyncItem> plan = FileTools.SyncPlan(source, destination, out List<string> errors, cancellation);
            return new Comparison(plan, errors);
        });

        if (comparison is null)
        {
            return;
        }

        _plan = comparison.Plan;
        _comparedSource = source;
        _comparedDestination = destination;
        _leftAlone = comparison.Errors.Count;
        _filling = true;
        planList.BeginUpdate();
        planList.Items.Clear();
        planList.Items.AddRange([.. _plan.Select(item => new ListViewItem(
        [
            item.Path,
            ActionText(item),
            item.IsFolder ? Strings.DeleteFolderSize() : SizeText.Format(item.Length),
            item.Modified.ToString("g", CultureInfo.CurrentCulture),
        ])
        {
            Checked = item.Action is SyncAction.New or SyncAction.Changed || (item.Action == SyncAction.Extra && extrasCheck.Checked),
            Tag = item,
        })]);
        planList.EndUpdate();
        _filling = false;
        ShowStatus();
        ToolWindows.ShowErrors(this, Text, Strings.SyncNotCompared(), comparison.Errors);
    }

    private sealed record Comparison(IReadOnlyList<SyncItem> Plan, List<string> Errors);

    private static string ActionText(SyncItem item) => item.Action switch
    {
        SyncAction.New => Strings.SyncNew(),
        SyncAction.Changed => Strings.SyncChanged(),
        SyncAction.NewerThere => Strings.SyncNewerThere(),
        SyncAction.SizeDiffers => Strings.SyncSizeDiffers(),
        _ => Strings.SyncExtra(),
    };

    private void OnExtrasChanged(object? sender, EventArgs e)
    {
        _filling = true;
        foreach (ListViewItem row in planList.Items)
        {
            if (((SyncItem)row.Tag!).Action == SyncAction.Extra)
            {
                row.Checked = extrasCheck.Checked;
            }
        }

        _filling = false;
        ShowStatus();
    }

    // In the list Enter would compare again, and tick every row back as it came.
    private void OnListEnter(object? sender, EventArgs e) => AcceptButton = null;

    private void OnListLeave(object? sender, EventArgs e) => AcceptButton = compareButton;

    private void OnItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (!_filling)
        {
            ShowStatus();
        }
    }

    private void ShowStatus()
    {
        List<SyncItem> ticked = [.. planList.CheckedItems.Cast<ListViewItem>().Select(row => (SyncItem)row.Tag!)];
        int copies = ticked.Count(item => item.Action != SyncAction.Extra);
        int extras = ticked.Count - copies;
        string status = _plan.Count > 0
            ? Strings.SyncSummary(
                _plan.Count(item => item.Action == SyncAction.New),
                _plan.Count(item => item.Action is SyncAction.Changed or SyncAction.SizeDiffers),
                _plan.Count(item => item.Action == SyncAction.NewerThere),
                _plan.Count(item => item.Action == SyncAction.Extra))
            : _leftAlone == 0 ? Strings.SyncSame() : Strings.SyncNothingToCopy();
        statusLabel.Text = _leftAlone > 0 ? $"{status} {Strings.SyncLeftAlone(_leftAlone)}" : status;
        syncButton.Enabled = copies + extras > 0;
    }

    /// <summary>
    /// The files to be replaced go to the Recycle Bin first, so that the copy asks nothing but
    /// what Windows must (a file that would lose its encryption) and a replaced file can come back;
    /// what is only in the destination goes last, and not after a cancel.
    /// </summary>
    private void OnSync(object? sender, EventArgs e)
    {
        List<SyncItem> ticked = [.. planList.CheckedItems.Cast<ListViewItem>().Select(row => (SyncItem)row.Tag!)];
        List<(string Source, string Folder)> copies = [.. ticked.Where(item => item.Action != SyncAction.Extra).Select(item =>
            (Path.Combine(_comparedSource, item.Path), Path.GetDirectoryName(Path.Combine(_comparedDestination, item.Path)) ?? _comparedDestination))];
        List<string> replaced = [.. ticked.Where(item => item.Action is SyncAction.Changed or SyncAction.NewerThere or SyncAction.SizeDiffers)
            .Select(item => Path.Combine(_comparedDestination, item.Path))];
        List<string> extras = [.. ticked.Where(item => item.Action == SyncAction.Extra).Select(item => Path.Combine(_comparedDestination, item.Path))];
        if (MessageBox.Show(this, Strings.SyncConfirm(copies.Count, replaced.Count, extras.Count), Text, MessageBoxButtons.YesNo,
            MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        var errors = new List<string>();
        bool aborted = false;
        if (replaced.Count > 0)
        {
            errors.AddRange(ShellFileOperation.Recycle(this, replaced, out aborted));
        }

        if (!aborted && copies.Count > 0)
        {
            errors.AddRange(ShellFileOperation.Copy(this, copies, out aborted));
        }

        if (!aborted && extras.Count > 0)
        {
            errors.AddRange(ShellFileOperation.Recycle(this, extras));
        }

        ToolWindows.ShowErrors(this, Text, Strings.SyncNotDone(), errors);
        if (aborted)
        {
            // Stopped half way: compared again, the list says what is left.
            OnFoldersChanged(this, EventArgs.Empty);
            statusLabel.Text = Strings.SyncStopped();
            return;
        }

        Close();
    }

    private void OnClose(object? sender, EventArgs e) => Close();
}
