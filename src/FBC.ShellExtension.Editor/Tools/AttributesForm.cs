using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Read-only, hidden, system and archive for the selected items (ADR-0028). A box starts as the
/// items are: ticked when all have the attribute, empty when none, filled when some do. Only a
/// box the user changes is written, as in Explorer's Properties.
/// </summary>
internal partial class AttributesForm : Form
{
    private readonly IReadOnlyList<SelectedItem> _items;
    private readonly CheckBox[] _boxes;
    private readonly bool?[] _itemStates;
    private bool?[] _start;

    public AttributesForm(IReadOnlyList<SelectedItem> items)
    {
        _items = items;
        InitializeComponent();
        _boxes = [readOnlyCheck, hiddenCheck, systemCheck, archiveCheck];
        ApplyStrings();

        List<FileAttributes> current = [.. items.Select(Read).OfType<FileAttributes>()];
        _itemStates = [.. FileTools.EditableAttributes.Select(attribute => FileTools.AttributeState(current, attribute))];
        _start = _itemStates;
        insideCheck.Visible = items.Any(item => item.Kind == ItemKind.Folder);
        ShowStates();
    }

    private void ApplyStrings()
    {
        selectionLabel.Text = ToolWindows.Selection(_items);
        insideCheck.Text = Strings.ToolWindowInside();
        readOnlyCheck.Text = Strings.AttributeReadOnly();
        hiddenCheck.Text = Strings.AttributeHidden();
        systemCheck.Text = Strings.AttributeSystem();
        archiveCheck.Text = Strings.AttributeArchive();
        hintLabel.Text = Strings.AttributesHint();
        applyButton.Text = Strings.ToolWindowApply();
        cancelButton.Text = Strings.EditorCancel();
    }

    private static FileAttributes? Read(SelectedItem item)
    {
        try
        {
            return File.GetAttributes(item.Path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Gone or closed to us: applying will say so.
            return null;
        }
    }

    private static CheckState StateOf(bool? state) => state switch
    {
        true => CheckState.Checked,
        false => CheckState.Unchecked,
        null => CheckState.Indeterminate,
    };

    private void ShowStates()
    {
        // Read-only and system go on files only: with no file among the items, nothing to set.
        bool files = insideCheck.Checked || _items.Any(item => item.Kind == ItemKind.File);
        for (int i = 0; i < _boxes.Length; i++)
        {
            _boxes[i].ThreeState = _start[i] is null;
            _boxes[i].CheckState = StateOf(_start[i]);
            if ((FileTools.EditableAttributes[i] & FileTools.FileOnlyAttributes) != 0)
            {
                _boxes[i].Enabled = files;
            }
        }
    }

    /// <summary>What is inside the folders is not known: every box then starts filled, leaving it as it is.</summary>
    private void OnInsideChanged(object? sender, EventArgs e)
    {
        _start = insideCheck.Checked ? [.. _itemStates.Select(_ => (bool?)null)] : _itemStates;
        ShowStates();
    }

    // Not a dialog: the window is the program, and Cancel ends it.
    private void OnClose(object? sender, EventArgs e) => Close();

    private async void OnApply(object? sender, EventArgs e)
    {
        FileAttributes set = 0;
        FileAttributes clear = 0;
        for (int i = 0; i < _boxes.Length; i++)
        {
            CheckState state = _boxes[i].CheckState;
            if (!_boxes[i].Enabled || state == StateOf(_start[i]))
            {
                continue;
            }

            if (state == CheckState.Checked)
            {
                set |= FileTools.EditableAttributes[i];
            }
            else if (state == CheckState.Unchecked)
            {
                clear |= FileTools.EditableAttributes[i];
            }
        }

        var change = new AttributeChange(set, clear);
        if (change.IsEmpty)
        {
            Close();
            return;
        }

        bool inside = insideCheck.Visible && insideCheck.Checked;
        IReadOnlyList<string>? errors = await ToolWindows.WithProgressAsync(this, Strings.ToolWindowWorking(),
            cancellation => FileTools.ChangeAttributes(_items, change, inside, cancellation));

        // Cancelled: what was changed stays changed, and the window stays for another try.
        if (errors is null)
        {
            return;
        }

        ToolWindows.ShowErrors(this, Text, Strings.ToolWindowNotChanged(), errors);
        Close();
    }
}
