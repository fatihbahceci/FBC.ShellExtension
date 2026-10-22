using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Renames the selected items by rules, with every new name shown before anything changes
/// (ADR-0029). All or nothing: while one name has a problem, none is renamed. Windows' own
/// engine renames, so Explorer's Undo takes it back. No default button: Enter in a number box
/// commits the number, it does not rename with one the list has not shown yet.
/// </summary>
internal partial class AdvancedRenameForm : Form
{
    private static readonly NameCase[] s_cases = [NameCase.Unchanged, NameCase.Lower, NameCase.Upper, NameCase.Title];

    private readonly IReadOnlyList<SelectedItem> _items;
    private readonly Dictionary<string, DateTime> _modified = new(StringComparer.OrdinalIgnoreCase);

    // The names in each folder, read once for the preview: a keystroke asks about every item.
    private readonly Dictionary<string, HashSet<string>> _names = new(StringComparer.OrdinalIgnoreCase);
    private RenamePreview _preview;
    private bool _ready;

    public AdvancedRenameForm(IReadOnlyList<SelectedItem> items)
    {
        _items = FileTools.RenameOrder(items);
        _preview = new RenamePreview([], null);
        InitializeComponent();
        ApplyStrings();
        caseCombo.SelectedIndex = 0;
        _ready = true;
        RefreshPreview();
    }

    private void ApplyStrings()
    {
        selectionLabel.Text = ToolWindows.Selection(_items);
        findLabel.Text = Strings.SearchFindLabel();
        replaceLabel.Text = Strings.SearchReplaceLabel();
        matchCaseCheck.Text = Strings.SearchMatchCase();
        regexCheck.Text = Strings.SearchRegularExpression();
        nameLabel.Text = Strings.RenameNameLabel();
        extensionLabel.Text = Strings.RenameExtensionLabel();
        tokensLabel.Text = Strings.RenameTokens();
        numbersLabel.Text = Strings.RenameNumbersLabel();
        startLabel.Text = Strings.RenameStart();
        stepLabel.Text = Strings.RenameStep();
        digitsLabel.Text = Strings.RenameDigits();
        caseLabel.Text = Strings.RenameCaseLabel();
        caseCombo.Items.AddRange([Strings.RenameCaseUnchanged(), Strings.RenameCaseLower(), Strings.RenameCaseUpper(), Strings.RenameCaseTitle()]);
        oldColumn.Text = Strings.RenameOldColumn();
        newColumn.Text = Strings.RenameNewColumn();
        problemColumn.Text = Strings.RenameProblemColumn();
        renameButton.Text = Strings.RenameButton();
        cancelButton.Text = Strings.EditorCancel();
    }

    private RenameRules Rules() => new(findBox.Text, replaceBox.Text, matchCaseCheck.Checked, regexCheck.Checked,
        nameBox.Text, extensionBox.Text, s_cases[Math.Max(0, caseCombo.SelectedIndex)],
        (int)startBox.Value, (int)stepBox.Value, (int)digitsBox.Value);

    // The designer sets the templates' text too; the list is built once everything is there,
    // and once the typing stops for a moment.
    private void OnRulesChanged(object? sender, EventArgs e)
    {
        if (_ready)
        {
            refreshTimer.Stop();
            refreshTimer.Start();
            renameButton.Enabled = false;
        }
    }

    private void OnRefreshTimer(object? sender, EventArgs e)
    {
        refreshTimer.Stop();
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        _preview = FileTools.PreviewRename(_items, Rules(), Exists, Modified);
        previewList.BeginUpdate();
        previewList.Items.Clear();
        previewList.Items.AddRange([.. _preview.Rows.Select(row => new ListViewItem([row.Item.Name, row.NewName, row.Problem ?? ""])
        {
            ForeColor = row.Problem is not null ? Color.Firebrick : row.Changes ? SystemColors.WindowText : SystemColors.GrayText,
        })]);
        previewList.EndUpdate();

        int problems = _preview.Rows.Count(row => row.Problem is not null);
        statusLabel.Text = _preview.Problem
            ?? (problems > 0 ? Strings.RenameProblems(problems) : Strings.RenameSummary(_preview.Rows.Count(row => row.Changes), _preview.Rows.Count));
        statusLabel.ForeColor = _preview.Problem is not null || problems > 0 ? Color.Firebrick : SystemColors.ControlText;
        renameButton.Enabled = _preview.CanRename;
    }

    private bool Exists(string path)
    {
        string folder = Path.GetDirectoryName(path) ?? "";
        if (!_names.TryGetValue(folder, out HashSet<string>? names))
        {
            try
            {
                names = new HashSet<string>(Directory.EnumerateFileSystemEntries(folder, "*", new EnumerationOptions { AttributesToSkip = 0 })
                    .Select(entry => Path.GetFileName(entry)), StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Unreadable: nothing known to be in the way; Windows asks if something is.
                names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            _names[folder] = names;
        }

        return names.Contains(Path.GetFileName(path));
    }

    /// <summary>Read once per item, and only when a template asks for <c>{date}</c>.</summary>
    private DateTime Modified(string path)
    {
        if (!_modified.TryGetValue(path, out DateTime date))
        {
            try
            {
                date = File.Exists(path) ? File.GetLastWriteTime(path) : Directory.GetLastWriteTime(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Gone meanwhile: renaming it will say so.
                date = DateTime.Now;
            }

            _modified[path] = date;
        }

        return date;
    }

    private void OnRename(object? sender, EventArgs e)
    {
        // Built again from the disk: something may have changed since the folders were read.
        refreshTimer.Stop();
        _names.Clear();
        RefreshPreview();
        if (!_preview.CanRename)
        {
            return;
        }

        List<string> errors = ShellFileOperation.Rename(this, [.. FileTools.RenameSequence(_preview).Select(row => (row.Item.Path, row.NewName))]);
        ToolWindows.ShowErrors(this, Text, Strings.RenameNotRenamed(), errors);
        Close();
    }

    // Not a dialog: the window is the program, and Cancel ends it.
    private void OnClose(object? sender, EventArgs e) => Close();
}
