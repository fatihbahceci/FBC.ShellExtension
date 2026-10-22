using System.Globalization;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Finds what matches name patterns under the selected folders and sends what stays ticked to
/// the Recycle Bin, with Windows' own engine (ADR-0028). Nothing goes before the list is shown
/// and the user has said yes.
/// </summary>
internal partial class AdvancedDeleteForm : Form
{
    private static readonly string[] s_presets = ["*.tmp; *.temp; ~*", "*.bak; *.old", "Thumbs.db; ehthumbs.db; .DS_Store"];

    private readonly IReadOnlyList<SelectedItem> _items;
    private readonly IReadOnlyList<string> _roots;
    private int _unreadable;
    private bool _filling;

    public AdvancedDeleteForm(IReadOnlyList<SelectedItem> items)
    {
        _items = items;
        _roots = [.. items.Select(item => item.Path)];
        InitializeComponent();
        ApplyStrings();
        patternsCombo.Items.AddRange(s_presets);
    }

    private void ApplyStrings()
    {
        selectionLabel.Text = ToolWindows.Selection(_items);
        patternsLabel.Text = Strings.DeletePatternsLabel();
        findButton.Text = Strings.DeleteFind();
        filesCheck.Text = Strings.DeleteFiles();
        foldersCheck.Text = Strings.DeleteFolders();
        subfoldersCheck.Text = Strings.DeleteSubfolders();
        hintLabel.Text = Strings.DeletePatternsHint();
        pathColumn.Text = Strings.DeletePathColumn();
        sizeColumn.Text = Strings.DeleteSizeColumn();
        modifiedColumn.Text = Strings.DeleteModifiedColumn();
        recycleButton.Text = Strings.DeleteRecycle();
        closeButton.Text = Strings.ToolWindowClose();
    }

    private async void OnFind(object? sender, EventArgs e)
    {
        if (FileTools.ParsePatterns(patternsCombo.Text, out string? problem) is not { } patterns)
        {
            MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            patternsCombo.Focus();
            return;
        }

        bool files = filesCheck.Checked;
        bool folders = foldersCheck.Checked;
        bool subfolders = subfoldersCheck.Checked;
        FindResult? result = await ToolWindows.WithProgressAsync(this, Strings.DeleteSearching(),
            cancellation => FileTools.FindMatches(_roots, patterns, files, folders, subfolders, cancellation));
        if (result is null)
        {
            return;
        }

        _unreadable = result.Unreadable.Count;
        _filling = true;
        foundList.BeginUpdate();
        foundList.Items.Clear();
        foundList.Items.AddRange([.. result.Items.Select(item => new ListViewItem(
        [
            DisplayPath(item.Path),
            item.IsFolder ? Strings.DeleteFolderSize() : SizeText.Format(item.Length),
            item.Modified.ToString("g", CultureInfo.CurrentCulture),
        ])
        {
            Checked = true,
            Tag = item,
        })]);
        foundList.EndUpdate();
        _filling = false;
        ShowStatus();
    }

    /// <summary>
    /// The path under the selected folder it was found in, which the selection line names; with
    /// several folders, from that folder's name on, so that A\x.tmp and B\x.tmp differ.
    /// </summary>
    private string DisplayPath(string path) =>
        _roots.FirstOrDefault(root => IsInside(path, root)) is not { } root ? path
        : _roots.Count == 1 ? Path.GetRelativePath(root, path)
        : Path.GetRelativePath(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(root)) ?? root, path);

    // A drive root keeps its separator: C:\ is not C:\\.
    private static bool IsInside(string path, string folder) =>
        path.StartsWith(Path.EndsInDirectorySeparator(folder) ? folder : folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    // In the list Enter would find again, and tick every row the user unticked.
    private void OnListEnter(object? sender, EventArgs e) => AcceptButton = null;

    private void OnListLeave(object? sender, EventArgs e) => AcceptButton = findButton;

    private void OnItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (!_filling)
        {
            ShowStatus();
        }
    }

    private void ShowStatus()
    {
        List<FoundItem> found = [.. foundList.Items.Cast<ListViewItem>().Select(row => (FoundItem)row.Tag!)];
        List<FoundItem> ticked = [.. foundList.CheckedItems.Cast<ListViewItem>().Select(row => (FoundItem)row.Tag!)];
        string status = found.Count == 0
            ? Strings.DeleteNothingFound()
            : Strings.DeleteFound(found.Count(item => !item.IsFolder), SizeText.Format(found.Where(item => !item.IsFolder).Sum(item => item.Length)), found.Count(item => item.IsFolder));
        statusLabel.Text = _unreadable > 0 ? $"{status} {Strings.DeleteUnreadable(_unreadable)}" : status;
        recycleButton.Enabled = ticked.Count > 0;
    }

    private void OnRecycle(object? sender, EventArgs e)
    {
        List<ListViewItem> rows = [.. foundList.CheckedItems.Cast<ListViewItem>()];
        if (rows.Count == 0
            || MessageBox.Show(this, Strings.DeleteConfirm(rows.Count), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        List<string> errors = ShellFileOperation.Recycle(this, [.. rows.Select(row => ((FoundItem)row.Tag!).Path)]);

        // What went stays off the list; what Windows kept (a skip, a cancel) stays on it.
        foundList.BeginUpdate();
        foreach (ListViewItem row in rows.Where(row => !Path.Exists(((FoundItem)row.Tag!).Path)))
        {
            foundList.Items.Remove(row);
        }

        foundList.EndUpdate();
        ShowStatus();
        ToolWindows.ShowErrors(this, Text, Strings.DeleteNotDeleted(), errors);
    }

    private void OnClose(object? sender, EventArgs e) => Close();
}
