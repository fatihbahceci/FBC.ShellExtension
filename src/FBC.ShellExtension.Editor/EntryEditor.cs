using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Editor;

/// <summary>The detail panel for a command or a submenu: its label, its icon, whether the taskbar menu shows it, its rule and, for a command, what it runs.</summary>
public partial class EntryEditor : UserControl, IEntryFieldEditor
{
    private LabeledEntry? _entry;
    private bool _loading;
    private CheckState _shownTaskbarState;

    public event Action<string?>? Changing;

    public event Action? Changed;

    public EntryEditor()
    {
        InitializeComponent();
        // Sized to its content at run time only. As the root of the designer, an auto-sized
        // control collapses to a sliver, because its docked layout takes its width from it.
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        matchHeader.Font = new Font(Font, FontStyle.Bold);
        commandHeader.Font = new Font(Font, FontStyle.Bold);
        labelLabel.Text = Strings.EditorFieldLabel();
        iconLabel.Text = Strings.EditorFieldIcon();
        chooseIconButton.Text = Strings.EditorIconChoose();
        refreshIconButton.Text = Strings.EditorIconRefresh();
        removeIconButton.Text = Strings.EditorIconRemove();
        iconDialog.Filter = Strings.EditorIconFilter();
        taskbarCheck.Text = Strings.EditorTaskbarShow();
        matchHeader.Text = Strings.EditorGroupMatch();
        commandHeader.Text = Strings.EditorGroupCommand();

        matchEditor.Changing += key => Changing?.Invoke(key);
        matchEditor.Changed += () => Changed?.Invoke();
        commandEditor.Changing += key => Changing?.Invoke(key);
        commandEditor.Changed += () =>
        {
            // The program may have changed: whether its icon can be read, whether it fits the taskbar.
            ShowIconActions();
            ShowTaskbar();
            Changed?.Invoke();
        };
    }

    /// <summary>Writes numbers still being typed into the bound entry; the form calls it before every command.</summary>
    public void CommitPendingEdits()
    {
        if (_entry is null)
        {
            return;
        }

        matchEditor.CommitPendingEdits();
        if (commandEditor.Visible)
        {
            commandEditor.CommitPendingEdits();
        }
    }

    public void Bind(LabeledEntry entry)
    {
        _entry = entry;
        _loading = true;
        try
        {
            labelBox.Text = entry.Label;
            ShowIcon();
            ShowTaskbar();
            matchEditor.Bind(entry);
            bool isCommand = entry is CommandEntry;
            commandHeader.Visible = isCommand;
            commandEditor.Visible = isCommand;
            if (entry is CommandEntry command)
            {
                commandEditor.Bind(command);
            }
        }
        finally
        {
            _loading = false;
        }
    }

    public void FocusLabel()
    {
        labelBox.Focus();
        labelBox.SelectAll();
    }

    /// <summary>Puts the focus on the field a validation issue names, such as <c>command.fileName</c> or <c>tool</c>.</summary>
    public void FocusMember(string member)
    {
        bool focused = member switch
        {
            _ when member.StartsWith("match.", StringComparison.Ordinal) => matchEditor.FocusMember(member["match.".Length..]),
            _ when member.StartsWith("command.", StringComparison.Ordinal) => commandEditor.FocusMember(member["command.".Length..]),
            "tool" or "command" => commandEditor.FocusKind(),
            "icon" => chooseIconButton.Focus(),
            _ => false,
        };

        if (!focused)
        {
            FocusLabel();
        }
    }

    // ---- the icon (ADR-0041) --------------------------------------------------------------------

    private void ShowIcon()
    {
        Image? old = iconPicture.Image;
        iconPicture.Image = IconPicker.ToBitmap(_entry?.Icon);
        old?.Dispose();
        ShowIconActions();
    }

    /// <summary>"Refresh icon" only where there is a program to read it from; "Remove icon" only where there is one.</summary>
    private void ShowIconActions()
    {
        refreshIconButton.Visible = _entry is CommandEntry { Tool: null, Command.FileName.Length: > 0 };
        removeIconButton.Enabled = _entry?.Icon is not null;
    }

    private void OnChooseIcon(object? sender, EventArgs e)
    {
        if (_entry is null || iconDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        SetIcon(IconPicker.FromFile(iconDialog.FileName, this, out string? problem), problem);
    }

    private void OnRefreshIcon(object? sender, EventArgs e)
    {
        if (_entry is CommandEntry { Command: { } command })
        {
            SetIcon(IconPicker.FromProgram(command, out string? problem), problem);
        }
    }

    private void OnRemoveIcon(object? sender, EventArgs e) => SetIcon(null, null, remove: true);

    /// <summary>One undo step per press. Nothing changes when no icon came and none was to be removed.</summary>
    private void SetIcon(byte[]? icon, string? problem, bool remove = false)
    {
        if (_entry is null)
        {
            return;
        }

        if (icon is null && !remove)
        {
            if (problem is not null)
            {
                MessageBox.Show(this, Strings.EditorIconFailed(problem), Core.ProductInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return;
        }

        Changing?.Invoke(null);
        _entry.Icon = icon;
        ShowIcon();
        Changed?.Invoke();
    }

    // ---- the taskbar menu (ADR-0043) ------------------------------------------------------------

    /// <summary>
    /// A command's box is its own flag. A submenu has no flag: its box stands for the commands in
    /// it that fit the taskbar menu, ticked when all are shown, grey when some are.
    /// </summary>
    private void ShowTaskbar()
    {
        string note = "";
        switch (_entry)
        {
            case CommandEntry command:
                bool fits = TaskbarMenu.Fits(command);
                bool shown = command.ShowInTaskbar == true;

                // A flag left on a command that no longer fits can still be taken off.
                taskbarCheck.Enabled = fits || shown;
                taskbarCheck.CheckState = shown ? CheckState.Checked : CheckState.Unchecked;
                note = fits ? ""
                    : command.Tool is not null ? Strings.EditorTaskbarTool()
                    : command.Command is null ? ""
                    : Strings.EditorTaskbarSelection();
                break;

            case SubmenuEntry submenu:
                List<CommandEntry> fitting = [.. TaskbarMenu.FittingCommands(submenu.Items)];
                int marked = fitting.Count(command => command.ShowInTaskbar == true);
                taskbarCheck.Enabled = fitting.Count > 0 || AllCommands(submenu.Items).Any(command => command.ShowInTaskbar == true);
                taskbarCheck.CheckState = marked == 0 ? CheckState.Unchecked
                    : marked == fitting.Count ? CheckState.Checked
                    : CheckState.Indeterminate;
                note = fitting.Count == 0 ? Strings.EditorTaskbarNothingInside() : "";
                break;
        }

        taskbarNote.Text = note;
        taskbarNote.Visible = note.Length > 0;
        _shownTaskbarState = taskbarCheck.CheckState;
    }

    /// <summary>
    /// The box changes only through <see cref="OnTaskbarClick"/>. Anything else that toggles it
    /// (an accessibility tool) is put back, so it never shows what the config does not hold.
    /// </summary>
    private void OnTaskbarStateChanged(object? sender, EventArgs e)
    {
        if (taskbarCheck.CheckState != _shownTaskbarState)
        {
            ShowTaskbar();
        }
    }

    /// <summary>
    /// Ticking a submenu shows every command in it that fits; unticking takes every one off, those
    /// that do not fit too. One undo step.
    /// </summary>
    private void OnTaskbarClick(object? sender, EventArgs e)
    {
        if (_entry is null || !taskbarCheck.Enabled)
        {
            return;
        }

        // A submenu with nothing in it that fits only holds flags left over: a click takes them off.
        bool show = taskbarCheck.CheckState != CheckState.Checked
            && (_entry is not SubmenuEntry submenuEntry || TaskbarMenu.FittingCommands(submenuEntry.Items).Any());
        List<CommandEntry> targets = _entry switch
        {
            CommandEntry command => [command],
            SubmenuEntry submenu when show => [.. TaskbarMenu.FittingCommands(submenu.Items)],
            SubmenuEntry submenu => [.. AllCommands(submenu.Items)],
            _ => [],
        };

        bool? value = show ? true : null;
        if (targets.All(command => command.ShowInTaskbar == value))
        {
            return;
        }

        Changing?.Invoke(null);
        foreach (CommandEntry command in targets)
        {
            command.ShowInTaskbar = value;
        }

        ShowTaskbar();
        Changed?.Invoke();
    }

    private static IEnumerable<CommandEntry> AllCommands(IEnumerable<MenuEntry?> entries)
    {
        foreach (MenuEntry? entry in entries)
        {
            if (entry is CommandEntry command)
            {
                yield return command;
            }
            else if (entry is SubmenuEntry submenu)
            {
                foreach (CommandEntry inner in AllCommands(submenu.Items))
                {
                    yield return inner;
                }
            }
        }
    }

    private void OnLabelChanged(object? sender, EventArgs e)
    {
        if (_loading || _entry is null)
        {
            return;
        }

        Changing?.Invoke("label");
        _entry.Label = labelBox.Text;
        Changed?.Invoke();
    }
}
