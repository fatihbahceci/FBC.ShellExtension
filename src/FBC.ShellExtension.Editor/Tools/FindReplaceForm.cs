using System.Text.RegularExpressions;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Finds text in the content of files and replaces it (ADR-0029). The preview counts the
/// matches of every file before anything is written; a change of the options asks for a new
/// preview. The old file can stay next to the new one as <c>name.bak</c>.
/// </summary>
internal partial class FindReplaceForm : Form
{
    private readonly IReadOnlyList<SelectedItem> _items;
    private IReadOnlyList<ReplaceCount> _counts = [];
    private Regex? _pattern;
    private string _replacement = "";

    public FindReplaceForm(IReadOnlyList<SelectedItem> items)
    {
        _items = items;
        InitializeComponent();
        ApplyStrings();
        filesPanel.Visible = items.Any(item => item.Kind != ItemKind.File);
    }

    private void ApplyStrings()
    {
        selectionLabel.Text = ToolWindows.Selection(_items);
        findLabel.Text = Strings.SearchFindLabel();
        replaceLabel.Text = Strings.SearchReplaceLabel();
        matchCaseCheck.Text = Strings.SearchMatchCase();
        wholeWordCheck.Text = Strings.ReplaceWholeWord();
        regexCheck.Text = Strings.SearchRegularExpression();
        filesLabel.Text = Strings.ReplaceFilesLabel();
        subfoldersCheck.Text = Strings.ReplaceSubfolders();
        backupCheck.Text = Strings.ReplaceBackup();
        hintLabel.Text = Strings.ReplaceHint(FileTools.MaxReplaceMegabytes);
        fileColumn.Text = Strings.ReplaceFileColumn();
        matchesColumn.Text = Strings.ReplaceMatchesColumn();
        noteColumn.Text = Strings.ReplaceNoteColumn();
        previewButton.Text = Strings.ReplacePreview();
        replaceButton.Text = Strings.ReplaceButton();
        closeButton.Text = Strings.ToolWindowClose();
    }

    private ReplaceRules Rules() => new(findBox.Text, replaceBox.Text, matchCaseCheck.Checked, wholeWordCheck.Checked, regexCheck.Checked);

    /// <summary>The counts were made with other options: they no longer say what Replace would do.</summary>
    private void OnOptionsChanged(object? sender, EventArgs e)
    {
        if (_pattern is null)
        {
            return;
        }

        _pattern = null;
        replaceButton.Enabled = false;
        statusLabel.Text = Strings.ReplaceStale();
    }

    private async void OnPreview(object? sender, EventArgs e)
    {
        ReplaceRules rules = Rules();
        if (FileTools.ReplacePattern(rules, out string? problem) is not { } pattern)
        {
            Warn(problem, findBox);
            return;
        }

        IReadOnlyList<string> names = ["*"];
        if (filesPanel.Visible && FileTools.ParsePatterns(filesBox.Text, out problem) is { } parsed)
        {
            names = parsed;
        }
        else if (filesPanel.Visible)
        {
            Warn(problem, filesBox);
            return;
        }

        bool subfolders = subfoldersCheck.Checked;
        Search? search = await ToolWindows.WithProgressAsync(this, Strings.ReplaceSearching(), cancellation =>
        {
            IReadOnlyList<string> files = FileTools.ReplaceSources(_items, names, subfolders, out List<string> unreadable, cancellation);
            List<ReplaceCount> counts = [];
            foreach (string file in files)
            {
                cancellation.ThrowIfCancellationRequested();
                counts.Add(FileTools.CountMatches(file, pattern, CodePage.Ansi));
            }

            return new Search(counts, unreadable.Count);
        });

        if (search is null)
        {
            return;
        }

        // A file without a match is not worth a line, unless it was left alone for a reason.
        _counts = search.Counts;
        string folder = _items[0].Directory;
        resultList.BeginUpdate();
        resultList.Items.Clear();
        resultList.Items.AddRange([.. _counts.Where(count => count.Matches > 0 || count.Note is not null).Select(count =>
            new ListViewItem([Path.GetRelativePath(folder, count.Path), count.Matches > 0 ? count.Matches.ToString(System.Globalization.CultureInfo.CurrentCulture) : "", count.Note ?? ""]))]);
        resultList.EndUpdate();

        int matches = _counts.Sum(count => count.Matches);
        string status = matches > 0 ? Strings.ReplaceSummary(matches, _counts.Count(count => count.Matches > 0)) : Strings.ReplaceNothing();
        statusLabel.Text = search.Unreadable > 0 ? $"{status} {Strings.ReplaceUnreadable(search.Unreadable)}" : status;
        _pattern = pattern;
        _replacement = FileTools.ReplaceText(rules);
        replaceButton.Enabled = matches > 0;
    }

    private sealed record Search(IReadOnlyList<ReplaceCount> Counts, int Unreadable);

    private void Warn(string? problem, Control field)
    {
        MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        field.Focus();
    }

    private async void OnReplace(object? sender, EventArgs e)
    {
        if (_pattern is not { } pattern)
        {
            return;
        }

        List<ReplaceCount> files = [.. _counts.Where(count => count.Matches > 0)];
        if (MessageBox.Show(this, Strings.ReplaceConfirm(files.Sum(count => count.Matches), files.Count), Text,
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        string replacement = _replacement;
        bool backup = backupCheck.Checked;
        List<string>? errors = await ToolWindows.WithProgressAsync(this, Strings.ToolWindowWorking(), cancellation =>
        {
            var failed = new List<string>();
            foreach (ReplaceCount file in files)
            {
                cancellation.ThrowIfCancellationRequested();
                try
                {
                    FileTools.ReplaceInFile(file.Path, pattern, replacement, CodePage.Ansi, backup);
                }
                catch (ToolFailedException ex)
                {
                    failed.Add(ex.Message);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    failed.Add(Strings.ToolItemFailed(file.Path, ex.Message));
                }
            }

            return failed;
        });

        // Cancelled: the files done so far stay changed; the counts no longer hold.
        if (errors is null)
        {
            _pattern = null;
            replaceButton.Enabled = false;
            statusLabel.Text = Strings.ReplaceStale();
            return;
        }

        ToolWindows.ShowErrors(this, Text, Strings.ReplaceNotReplaced(), errors);
        Close();
    }

    private void OnClose(object? sender, EventArgs e) => Close();
}
