using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Editor;

/// <summary>Edits the <c>match</c> rule of a command or submenu. An all-default rule is written as no rule at all.</summary>
public partial class MatchRuleEditor : UserControl, IEntryFieldEditor
{
    private static readonly char[] s_extensionSeparators = [',', ';', ' ', '\t'];

    private LabeledEntry? _entry;
    private bool _loading;

    public event Action<string?>? Changing;

    public event Action? Changed;

    public MatchRuleEditor()
    {
        InitializeComponent();
        // Sized to its content at run time only. As the root of the designer, an auto-sized
        // control collapses to a sliver, because its docked layout takes its width from it.
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ApplyStrings();
    }

    private void ApplyStrings()
    {
        targetsLabel.Text = Strings.EditorMatchTargets();
        filesCheck.Text = Strings.EditorMatchFiles();
        foldersCheck.Text = Strings.EditorMatchFolders();
        backgroundCheck.Text = Strings.EditorMatchBackground();
        drivesCheck.Text = Strings.EditorMatchDrives();
        targetsHint.Text = Strings.EditorMatchTargetsHint();
        extensionsLabel.Text = Strings.EditorMatchExtensions();
        extensionsHint.Text = Strings.EditorMatchExtensionsHint();
        patternsLabel.Text = Strings.EditorMatchPatterns();
        patternsHint.Text = Strings.EditorMatchPatternsHint();
        excludeLabel.Text = Strings.EditorMatchExclude();
        modeLabel.Text = Strings.EditorMatchMode();
        modeCombo.Items.Clear();
        modeCombo.Items.AddRange([Strings.EditorMatchModeAll(), Strings.EditorMatchModeAny()]);
        minLabel.Text = Strings.EditorMatchMinCount();
        maxCheck.Text = Strings.EditorMatchMaxCount();
    }

    private IEnumerable<(CheckBox Box, ItemKind Kind)> TargetBoxes =>
    [
        (filesCheck, ItemKind.File),
        (foldersCheck, ItemKind.Folder),
        (backgroundCheck, ItemKind.Background),
        (drivesCheck, ItemKind.Drive),
    ];

    /// <summary>
    /// A number typed into a spin box reaches the model only on Enter, on losing the focus, or
    /// when something reads <c>Value</c>. Menu shortcuts and toolbar buttons do neither, so the
    /// form calls this before every command; reading the value here writes it into the entry
    /// that is still bound.
    /// </summary>
    public void CommitPendingEdits()
    {
        _ = minCount.Value;
        _ = maxCount.Value;
    }

    public void Bind(LabeledEntry entry)
    {
        CommitPendingEdits();
        _entry = entry;
        _loading = true;
        try
        {
            MatchRule rule = entry.Match ?? new MatchRule();
            foreach ((CheckBox box, ItemKind kind) in TargetBoxes)
            {
                box.Checked = rule.Targets?.Contains(kind) == true;
            }

            extensionsBox.Text = rule.Extensions is null ? "" : string.Join(", ", rule.Extensions);
            patternsBox.Lines = rule.Patterns?.ToArray() ?? [];
            excludeBox.Lines = rule.Exclude?.ToArray() ?? [];
            modeCombo.SelectedIndex = rule.Mode == MatchMode.Any ? 1 : 0;
            minCount.Value = Math.Clamp(rule.MinCount, (int)minCount.Minimum, (int)minCount.Maximum);
            maxCheck.Checked = rule.MaxCount is not null;
            maxCount.Value = Math.Clamp(rule.MaxCount ?? rule.MinCount, (int)maxCount.Minimum, (int)maxCount.Maximum);
            maxCount.Enabled = maxCheck.Checked;
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>Puts the focus on the field a validation issue names, such as <c>extensions[0]</c>.</summary>
    public bool FocusMember(string member)
    {
        Control? target = member switch
        {
            _ when member.StartsWith("targets", StringComparison.Ordinal) => filesCheck,
            _ when member.StartsWith("extensions", StringComparison.Ordinal) => extensionsBox,
            _ when member.StartsWith("patterns", StringComparison.Ordinal) => patternsBox,
            _ when member.StartsWith("exclude", StringComparison.Ordinal) => excludeBox,
            _ when member.StartsWith("mode", StringComparison.Ordinal) => modeCombo,
            _ when member.StartsWith("minCount", StringComparison.Ordinal) => minCount,
            _ when member.StartsWith("maxCount", StringComparison.Ordinal) => maxCheck.Checked ? maxCount : maxCheck,
            _ => null,
        };

        return target?.Focus() == true;
    }

    private void OnFieldChanged(object? sender, EventArgs e)
    {
        maxCount.Enabled = maxCheck.Checked;
        if (_loading || _entry is null)
        {
            return;
        }

        // Typing and spinning coalesce into one undo step per field; a click is a step of its own.
        string? key = sender is TextBox or NumericUpDown ? $"match.{((Control)sender).Name}" : null;
        Changing?.Invoke(key);
        _entry.Match = BuildRule();
        Changed?.Invoke();
    }

    private MatchRule? BuildRule()
    {
        List<ItemKind> targets = [.. TargetBoxes.Where(t => t.Box.Checked).Select(t => t.Kind)];
        var rule = new MatchRule
        {
            Targets = targets.Count > 0 ? targets : null,
            Extensions = NullIfEmpty(extensionsBox.Text.Split(s_extensionSeparators, StringSplitOptions.RemoveEmptyEntries)),
            Patterns = NullIfEmpty(Lines(patternsBox)),
            Exclude = NullIfEmpty(Lines(excludeBox)),
            Mode = modeCombo.SelectedIndex == 1 ? MatchMode.Any : MatchMode.All,
            MinCount = (int)minCount.Value,
            MaxCount = maxCheck.Checked ? (int)maxCount.Value : null,
        };

        bool isDefault = rule.Targets is null && rule.Extensions is null && rule.Patterns is null && rule.Exclude is null
            && rule.Mode == MatchMode.All && rule.MinCount == 1 && rule.MaxCount is null;
        return isDefault ? null : rule;
    }

    private static IEnumerable<string> Lines(TextBox box) =>
        box.Lines.Select(line => line.Trim()).Where(line => line.Length > 0);

    private static List<string>? NullIfEmpty(IEnumerable<string> values)
    {
        List<string> list = [.. values];
        return list.Count > 0 ? list : null;
    }
}
