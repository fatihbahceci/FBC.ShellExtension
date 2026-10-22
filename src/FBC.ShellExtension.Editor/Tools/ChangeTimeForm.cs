using System.Globalization;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// When the selected items were created, modified and last accessed (ADR-0028). The boxes start
/// with the first item's times; only a ticked one is written, and changing one ticks it.
/// </summary>
internal partial class ChangeTimeForm : Form
{
    private readonly IReadOnlyList<SelectedItem> _items;
    private readonly (CheckBox Box, DateTimePicker Picker)[] _rows;
    private bool _showing;

    public ChangeTimeForm(IReadOnlyList<SelectedItem> items)
    {
        _items = items;
        InitializeComponent();
        _rows = [(createdCheck, createdPicker), (modifiedCheck, modifiedPicker), (accessedCheck, accessedPicker)];
        ApplyStrings();
        insideCheck.Visible = items.Any(item => item.Kind == ItemKind.Folder);

        // To the second, as Explorer shows them, in the user's own date order.
        DateTimeFormatInfo format = CultureInfo.CurrentCulture.DateTimeFormat;
        FileSystemInfo first = items[0].Kind == ItemKind.File ? new FileInfo(items[0].Path) : new DirectoryInfo(items[0].Path);
        _showing = true;
        try
        {
            foreach ((_, DateTimePicker picker) in _rows)
            {
                picker.CustomFormat = $"{format.ShortDatePattern} {format.LongTimePattern}";
            }

            ShowTime(createdPicker, () => first.CreationTime);
            ShowTime(modifiedPicker, () => first.LastWriteTime);
            ShowTime(accessedPicker, () => first.LastAccessTime);
        }
        finally
        {
            _showing = false;
        }
    }

    private void ApplyStrings()
    {
        selectionLabel.Text = ToolWindows.Selection(_items);
        insideCheck.Text = Strings.ToolWindowInside();
        createdCheck.Text = Strings.TimeCreated();
        modifiedCheck.Text = Strings.TimeModified();
        accessedCheck.Text = Strings.TimeAccessed();
        nowButton.Text = Strings.TimeNow();
        hintLabel.Text = Strings.TimeHint();
        applyButton.Text = Strings.ToolWindowApply();
        cancelButton.Text = Strings.EditorCancel();
    }

    /// <summary>The item's time, or now for one the picker cannot show (a missing file reads as 1601).</summary>
    private static void ShowTime(DateTimePicker picker, Func<DateTime> read)
    {
        DateTime time;
        try
        {
            time = read();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            time = DateTime.Now;
        }

        picker.Value = time < picker.MinDate || time > picker.MaxDate ? DateTime.Now : time;
    }

    private void OnTimeChanged(object? sender, EventArgs e)
    {
        if (_showing)
        {
            return;
        }

        foreach ((CheckBox box, DateTimePicker picker) in _rows)
        {
            if (picker == sender)
            {
                box.Checked = true;
            }
        }
    }

    private void OnNow(object? sender, EventArgs e)
    {
        DateTime now = DateTime.Now;
        now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
        foreach ((_, DateTimePicker picker) in _rows)
        {
            picker.Value = now;
        }
    }

    // Not a dialog: the window is the program, and Cancel ends it.
    private void OnClose(object? sender, EventArgs e) => Close();

    private async void OnApply(object? sender, EventArgs e)
    {
        DateTime? Ticked(int row) => _rows[row].Box.Checked ? _rows[row].Picker.Value : null;
        var change = new TimeChange(Ticked(0), Ticked(1), Ticked(2));
        if (change.IsEmpty)
        {
            Close();
            return;
        }

        bool inside = insideCheck.Visible && insideCheck.Checked;
        IReadOnlyList<string>? errors = await ToolWindows.WithProgressAsync(this, Strings.ToolWindowWorking(),
            cancellation => FileTools.ChangeTimes(_items, change, inside, cancellation));

        // Cancelled: what was changed stays changed, and the window stays for another try.
        if (errors is null)
        {
            return;
        }

        ToolWindows.ShowErrors(this, Text, Strings.ToolWindowNotChanged(), errors);
        Close();
    }
}
