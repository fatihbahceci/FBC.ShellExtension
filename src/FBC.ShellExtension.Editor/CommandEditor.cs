using System.Runtime.CompilerServices;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Editor;

/// <summary>
/// Edits what a command entry runs: a program (<c>command</c>) or a built-in tool (<c>tool</c>).
/// Combo boxes follow the order of the arrays below.
/// </summary>
public partial class CommandEditor : UserControl, IEntryFieldEditor
{
    /// <summary>Null is "a program".</summary>
    private static readonly BuiltinTool?[] s_kinds = [null, .. Enum.GetValues<BuiltinTool>().Select(tool => (BuiltinTool?)tool)];
    private static readonly RunMode[] s_runModes = [RunMode.Track, RunMode.Launch];
    private static readonly MultiSelectMode[] s_multiModes = [MultiSelectMode.PerFileSequential, MultiSelectMode.PerFileParallel, MultiSelectMode.AllAtOnce];
    private static readonly CommandWindowStyle[] s_windowStyles = [CommandWindowStyle.Normal, CommandWindowStyle.Hidden, CommandWindowStyle.Minimized, CommandWindowStyle.Maximized];
    private static readonly CommandOutputEncoding[] s_encodings = [CommandOutputEncoding.Oem, CommandOutputEncoding.Utf8, CommandOutputEncoding.Ansi];

    private readonly Dictionary<Control, Action<CommandDefinition>> _writers;

    /// <summary>The program an entry ran before a tool was chosen for it, to bring back when "a program" is chosen again.</summary>
    private readonly ConditionalWeakTable<CommandEntry, CommandDefinition> _setAside = new();
    private CommandEntry? _entry;
    private bool _loading;

    public event Action<string?>? Changing;

    public event Action? Changed;

    public CommandEditor()
    {
        InitializeComponent();
        // Sized to its content at run time only. As the root of the designer, an auto-sized
        // control collapses to a sliver, because its docked layout takes its width from it.
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ApplyStrings();

        // Only the field behind the control that changed is written, so a value the controls
        // cannot show exactly is left as the file has it.
        _writers = new Dictionary<Control, Action<CommandDefinition>>
        {
            [fileNameBox] = c => c.FileName = fileNameBox.Text,
            [argumentsBox] = c => c.Arguments = NullIfEmpty(argumentsBox.Text),
            [workingDirectoryBox] = c => c.WorkingDirectory = NullIfEmpty(workingDirectoryBox.Text),
            [runModeCombo] = c => c.RunMode = Pick(s_runModes, runModeCombo, c.RunMode),
            [multiCombo] = c => c.MultiSelect = Pick(s_multiModes, multiCombo, c.MultiSelect),
            [parallelCheck] = WriteParallelism,
            [parallelCount] = WriteParallelism,
            [windowCombo] = c => c.WindowStyle = Pick(s_windowStyles, windowCombo, c.WindowStyle),
            [encodingCombo] = c => c.OutputEncoding = Pick(s_encodings, encodingCombo, c.OutputEncoding),
            [noWindowCheck] = c => c.CreateNoWindow = noWindowCheck.Checked,
            [shellExecuteCheck] = c => c.UseShellExecute = shellExecuteCheck.Checked,
            [closeOnSuccessCheck] = c => c.CloseOnSuccess = closeOnSuccessCheck.Checked,
            [showOutputCheck] = c => c.ShowOutput = showOutputCheck.Checked,
            [runAsCheck] = WriteVerb,
            [verbBox] = WriteVerb,
        };
    }

    private void WriteParallelism(CommandDefinition command) =>
        command.MaxParallelism = parallelCheck.Checked ? (int)parallelCount.Value : null;

    private void WriteVerb(CommandDefinition command) =>
        command.Verb = runAsCheck.Checked ? CommandDefinition.RunAsVerb : NullIfEmpty(verbBox.Text.Trim());

    private void ApplyStrings()
    {
        kindLabel.Text = Strings.EditorCommandKind();
        Fill(kindCombo, [.. s_kinds.Select(tool => tool is { } builtin ? ToolTexts.Name(builtin) : Strings.EditorRunsProgram())]);
        programLabel.Text = Strings.EditorCommandProgram();
        browseButton.Text = Strings.EditorCommandBrowse();
        argumentsLabel.Text = Strings.EditorCommandArguments();
        variablesButton.Text = Strings.EditorCommandInsertVariable();
        workingLabel.Text = Strings.EditorCommandWorkingDirectory();
        workingHint.Text = Strings.EditorCommandWorkingDirectoryHint();
        runModeLabel.Text = Strings.EditorCommandRunMode();
        Fill(runModeCombo, Strings.EditorRunModeTrack(), Strings.EditorRunModeLaunch());
        multiLabel.Text = Strings.EditorCommandMultiSelect();
        Fill(multiCombo, Strings.EditorMultiSequential(), Strings.EditorMultiParallel(), Strings.EditorMultiAllAtOnce());
        parallelCheck.Text = Strings.EditorCommandMaxParallelism();
        windowLabel.Text = Strings.EditorCommandWindow();
        Fill(windowCombo, Strings.EditorWindowNormal(), Strings.EditorWindowHidden(), Strings.EditorWindowMinimized(), Strings.EditorWindowMaximized());
        encodingLabel.Text = Strings.EditorCommandEncoding();
        Fill(encodingCombo, Strings.EditorEncodingOem(), Strings.EditorEncodingUtf8(), Strings.EditorEncodingAnsi());
        runAsCheck.Text = Strings.EditorCommandRunAs();
        noWindowCheck.Text = Strings.EditorCommandNoWindow();
        shellExecuteCheck.Text = Strings.EditorCommandShellExecute();
        closeOnSuccessCheck.Text = Strings.EditorCommandCloseOnSuccess();
        showOutputCheck.Text = Strings.EditorCommandShowOutput();
        verbLabel.Text = Strings.EditorCommandVerb();
        browseDialog.Filter = Strings.EditorBrowseFilter();

        variablesMenu.Items.Clear();
        foreach ((string token, string description) in Variables)
        {
            var item = new ToolStripMenuItem($"{token}   {description}") { Tag = token };
            item.Click += OnInsertVariable;
            variablesMenu.Items.Add(item);
        }
    }

    private static (string Token, string Description)[] Variables =>
    [
        ("{path}", Strings.EditorVarPath()),
        ("{dir}", Strings.EditorVarDir()),
        ("{name}", Strings.EditorVarName()),
        ("{nameNoExt}", Strings.EditorVarNameNoExt()),
        ("{ext}", Strings.EditorVarExt()),
        ("{pathNoExt}", Strings.EditorVarPathNoExt()),
        ("{allPaths}", Strings.EditorVarAllPaths()),
    ];

    private static void Fill(ComboBox combo, params string[] items)
    {
        combo.Items.Clear();
        combo.Items.AddRange(items);
    }

    /// <summary>Writes a number still being typed into the bound entry (see <see cref="MatchRuleEditor.CommitPendingEdits"/>).</summary>
    public void CommitPendingEdits() => _ = parallelCount.Value;

    public void Bind(CommandEntry entry)
    {
        CommitPendingEdits();
        _entry = entry;
        _loading = true;
        try
        {
            kindCombo.SelectedIndex = Array.IndexOf(s_kinds, entry.Tool);
            ShowFieldsFor(entry.Tool);
            // A tool entry has no command: its hidden fields show the defaults and write nothing.
            CommandDefinition command = entry.Command ?? new CommandDefinition();
            fileNameBox.Text = command.FileName;
            argumentsBox.Text = command.Arguments ?? "";
            workingDirectoryBox.Text = command.WorkingDirectory ?? "";
            runModeCombo.SelectedIndex = Array.IndexOf(s_runModes, command.RunMode);
            multiCombo.SelectedIndex = Array.IndexOf(s_multiModes, command.MultiSelect);
            parallelCheck.Checked = command.MaxParallelism is not null;
            parallelCount.Value = Math.Clamp(command.MaxParallelism ?? 2, (int)parallelCount.Minimum, (int)parallelCount.Maximum);
            windowCombo.SelectedIndex = Array.IndexOf(s_windowStyles, command.WindowStyle);
            encodingCombo.SelectedIndex = Array.IndexOf(s_encodings, command.OutputEncoding);
            runAsCheck.Checked = command.RunsElevated;
            noWindowCheck.Checked = command.CreateNoWindow;
            shellExecuteCheck.Checked = command.UseShellExecute;
            closeOnSuccessCheck.Checked = command.CloseOnSuccess;
            showOutputCheck.Checked = command.ShowOutput;
            verbBox.Text = command.RunsElevated ? "" : command.Verb ?? "";
            UpdateEnabled();
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>For an issue with the entry's <c>tool</c>, or with neither a tool nor a command.</summary>
    public bool FocusKind() => kindCombo.Focus();

    /// <summary>Puts the focus on the field a validation issue names, such as <c>fileName</c>.</summary>
    public bool FocusMember(string member)
    {
        Control? target = member switch
        {
            "fileName" => fileNameBox,
            "arguments" => argumentsBox,
            "workingDirectory" => workingDirectoryBox,
            "maxParallelism" => parallelCount.Enabled ? parallelCount : parallelCheck,
            "verb" => verbBox.Enabled ? verbBox : shellExecuteCheck,
            "showOutput" => showOutputCheck,
            "runMode" => runModeCombo,
            "multiSelect" => multiCombo,
            _ => null,
        };

        return target?.Focus() == true;
    }

    // Disabled fields keep their values in the config; the validator, not the form, reports a
    // combination that does not work.
    private void UpdateEnabled()
    {
        bool track = runModeCombo.SelectedIndex == Array.IndexOf(s_runModes, RunMode.Track);
        bool parallel = multiCombo.SelectedIndex == Array.IndexOf(s_multiModes, MultiSelectMode.PerFileParallel);
        parallelCheck.Enabled = parallel;
        parallelCount.Enabled = parallel && parallelCheck.Checked;
        closeOnSuccessCheck.Enabled = track;
        showOutputCheck.Enabled = track && !shellExecuteCheck.Checked;
        verbBox.Enabled = shellExecuteCheck.Checked && !runAsCheck.Checked;
    }

    /// <summary>A tool hides the program's fields and shows what the tool does instead.</summary>
    private void ShowFieldsFor(BuiltinTool? tool)
    {
        toolNote.Text = tool is { } builtin ? ToolTexts.Note(builtin) : "";
        layout.SuspendLayout();
        foreach (Control control in layout.Controls)
        {
            if (control != kindLabel && control != kindCombo)
            {
                control.Visible = (control == toolNote) == (tool is not null);
            }
        }

        layout.ResumeLayout(true);
    }

    private void OnKindChanged(object? sender, EventArgs e)
    {
        if (_loading || _entry is null)
        {
            return;
        }

        BuiltinTool? tool = Pick(s_kinds, kindCombo, _entry.Tool);
        if (tool == _entry.Tool)
        {
            return;
        }

        // A number still being typed belongs to the program, before the program is set aside.
        CommitPendingEdits();
        Changing?.Invoke(null);
        if (tool is not null && _entry.Command is { } command)
        {
            _setAside.AddOrUpdate(_entry, command);
        }

        // An entry runs one or the other; a command already there (written by hand next to a
        // tool) is kept when "a program" is chosen.
        _entry.Command = tool is not null ? null
            : _entry.Command ?? (_setAside.TryGetValue(_entry, out CommandDefinition? kept) ? kept : new CommandDefinition());
        _entry.Tool = tool;
        Changed?.Invoke();
        Bind(_entry);
    }

    private void OnFieldChanged(object? sender, EventArgs e)
    {
        UpdateEnabled();
        // The program's fields are hidden for a tool; nothing in them may bring a command back.
        if (_loading || _entry is null || _entry.Tool is not null)
        {
            return;
        }

        if (sender is not Control control || !_writers.TryGetValue(control, out Action<CommandDefinition>? write))
        {
            return;
        }

        string? key = control is TextBox or NumericUpDown ? $"command.{control.Name}" : null;
        Changing?.Invoke(key);
        write(_entry.Command ??= new CommandDefinition());
        Changed?.Invoke();
    }

    private static T Pick<T>(T[] values, ComboBox combo, T current) =>
        combo.SelectedIndex >= 0 && combo.SelectedIndex < values.Length ? values[combo.SelectedIndex] : current;

    private static string? NullIfEmpty(string text) => text.Length > 0 ? text : null;

    private void OnBrowse(object? sender, EventArgs e)
    {
        if (browseDialog.ShowDialog(this) == DialogResult.OK)
        {
            fileNameBox.Text = browseDialog.FileName;
        }
    }

    private void OnShowVariables(object? sender, EventArgs e) =>
        variablesMenu.Show(variablesButton, new Point(0, variablesButton.Height));

    private void OnInsertVariable(object? sender, EventArgs e)
    {
        if (sender is ToolStripItem { Tag: string token })
        {
            argumentsBox.SelectedText = token;
            argumentsBox.Focus();
        }
    }
}
