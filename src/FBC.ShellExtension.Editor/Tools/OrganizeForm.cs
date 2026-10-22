using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Moves files into folders named after their date or extension, next to them (ADR-0028). The
/// list shows where each file goes before anything moves; Windows' own engine moves them.
/// </summary>
internal partial class OrganizeForm : Form
{
    private static readonly OrganizeDate[] s_dates = [OrganizeDate.Modified, OrganizeDate.Created, OrganizeDate.Taken];
    private static readonly DateFolders[] s_folders = [DateFolders.Year, DateFolders.YearMonth, DateFolders.YearMonthDay, DateFolders.YearThenMonth];

    private readonly IReadOnlyList<SelectedItem> _items;
    private readonly IReadOnlyList<string> _files;
    private readonly List<string> _unreadable;
    private Dictionary<string, DateTime>? _taken;
    private CancellationTokenSource? _reading;
    private IReadOnlyList<OrganizeMove> _plan = [];

    public OrganizeForm(IReadOnlyList<SelectedItem> items)
    {
        _items = items;
        InitializeComponent();
        _files = FileTools.OrganizeSources(items, out _unreadable);
        ApplyStrings();
        dateCombo.SelectedIndex = 0;
        foldersCombo.SelectedIndex = 1;
        byDateRadio.Checked = true;
    }

    private void ApplyStrings()
    {
        selectionLabel.Text = ToolWindows.Selection(_items);
        byDateRadio.Text = Strings.OrganizeByDate();
        byExtensionRadio.Text = Strings.OrganizeByExtension();
        dateLabel.Text = Strings.OrganizeDateLabel();
        dateCombo.Items.AddRange([Strings.OrganizeDateModified(), Strings.OrganizeDateCreated(), Strings.OrganizeDateTaken()]);
        foldersLabel.Text = Strings.OrganizeFoldersLabel();
        DateTime today = DateTime.Today;
        foldersCombo.Items.AddRange(
        [
            Strings.OrganizeFolderYear(FileTools.DateFolder(today, DateFolders.Year)),
            Strings.OrganizeFolderYearMonth(FileTools.DateFolder(today, DateFolders.YearMonth)),
            Strings.OrganizeFolderYearMonthDay(FileTools.DateFolder(today, DateFolders.YearMonthDay)),
            Strings.OrganizeFolderYearThenMonth(FileTools.DateFolder(today, DateFolders.YearThenMonth)),
        ]);
        fileColumn.Text = Strings.OrganizeFileColumn();
        folderColumn.Text = Strings.OrganizeFolderColumn();
        moveButton.Text = Strings.OrganizeMove();
        cancelButton.Text = Strings.EditorCancel();
    }

    private async void OnOptionsChanged(object? sender, EventArgs e)
    {
        // The radio that lost its tick says so too; one refresh is enough.
        if (sender is RadioButton { Checked: false } || dateCombo.SelectedIndex < 0 || foldersCombo.SelectedIndex < 0)
        {
            return;
        }

        await RefreshPlanAsync();
    }

    private async Task RefreshPlanAsync()
    {
        bool byDate = byDateRadio.Checked;
        datePanel.Enabled = byDate;
        OrganizeDate date = s_dates[dateCombo.SelectedIndex];
        if (byDate && date == OrganizeDate.Taken && _taken is null)
        {
            ShowPlan(null);
            if (_reading is not null)
            {
                // Already reading; that one shows the list when it is done.
                return;
            }

            _reading = new CancellationTokenSource();
            try
            {
                _taken = await ShellDates.TakenAsync(_files, _reading.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                _reading.Dispose();
                _reading = null;
            }

            // The options may have changed meanwhile: the list follows what they are now.
            if (!IsDisposed)
            {
                await RefreshPlanAsync();
            }

            return;
        }

        Dictionary<string, DateTime> taken = _taken ?? [];
        ShowPlan(FileTools.OrganizePlan(_files, byDate ? OrganizeBy.Date : OrganizeBy.Extension, s_folders[foldersCombo.SelectedIndex],
            file => date == OrganizeDate.Taken ? taken.GetValueOrDefault(file, DateTime.Now) : DateOf(file, date)));
    }

    private static DateTime DateOf(string file, OrganizeDate date)
    {
        try
        {
            return FileTools.FileDate(file, date);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Gone meanwhile: moving it will say so.
            return DateTime.Now;
        }
    }

    /// <param name="plan">Null while the dates are being read.</param>
    private void ShowPlan(IReadOnlyList<OrganizeMove>? plan)
    {
        _plan = plan ?? [];
        planList.BeginUpdate();
        planList.Items.Clear();
        planList.Items.AddRange([.. _plan.Select(move => new ListViewItem([Path.GetFileName(move.Source), move.Folder]))]);
        planList.EndUpdate();
        moveButton.Enabled = _plan.Count > 0;

        string status = plan is null ? Strings.OrganizeReadingDates()
            : _files.Count == 0 ? Strings.OrganizeNothing()
            : Strings.OrganizeSummary(_plan.Count, _plan.Select(move => move.Destination).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        statusLabel.Text = _unreadable.Count > 0 ? $"{status} {Strings.OrganizeUnreadable(_unreadable.Count)}" : status;
    }

    private void OnMove(object? sender, EventArgs e)
    {
        var errors = new List<string>(_unreadable);
        var made = new List<string>();
        var moves = new List<(string Source, string Folder)>();
        foreach (IGrouping<string, OrganizeMove> folder in _plan.GroupBy(move => move.Destination, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                made.AddRange(Missing(folder.Key));
                Directory.CreateDirectory(folder.Key);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A file of the same name is there, say: its files stay where they are.
                errors.AddRange(folder.Select(move => Strings.ToolItemFailed(move.Source, ex.Message)));
                continue;
            }

            moves.AddRange(folder.Select(move => (move.Source, folder.Key)));
        }

        if (moves.Count > 0)
        {
            errors.AddRange(ShellFileOperation.Move(this, moves));
        }

        // A cancel, or a skip in Windows' question, leaves a folder made here empty: it goes again.
        foreach (string folder in made.Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(folder => folder.Length))
        {
            try
            {
                FileTools.RemoveIfEmpty(folder);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // An empty folder left behind is no reason to say the files did not move.
            }
        }

        ToolWindows.ShowErrors(this, Text, Strings.OrganizeNotMoved(), errors);
        Close();
    }

    /// <summary><paramref name="folder"/> and those above it that do not exist yet, deepest first.</summary>
    private static IEnumerable<string> Missing(string folder)
    {
        for (string? path = folder; !string.IsNullOrEmpty(path) && !Directory.Exists(path); path = Path.GetDirectoryName(path))
        {
            yield return path;
        }
    }

    // Not a dialog: the window is the program, and Cancel ends it.
    private void OnClose(object? sender, EventArgs e) => Close();

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _reading?.Cancel();
        base.OnFormClosed(e);
    }
}
