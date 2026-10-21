using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Editor;

/// <summary>The detail panel for a command or a submenu: its label, its rule and, for a command, what it runs.</summary>
public partial class EntryEditor : UserControl, IEntryFieldEditor
{
    private LabeledEntry? _entry;
    private bool _loading;

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
        matchHeader.Text = Strings.EditorGroupMatch();
        commandHeader.Text = Strings.EditorGroupCommand();

        matchEditor.Changing += key => Changing?.Invoke(key);
        matchEditor.Changed += () => Changed?.Invoke();
        commandEditor.Changing += key => Changing?.Invoke(key);
        commandEditor.Changed += () => Changed?.Invoke();
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
            _ => false,
        };

        if (!focused)
        {
            FocusLabel();
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
