using System.Text.Json.Serialization;

namespace FBC.ShellExtension.Core.Configuration;

/// <summary>One entry of a menu level; the JSON <c>type</c> property selects the kind.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CommandEntry), "command")]
[JsonDerivedType(typeof(SubmenuEntry), "submenu")]
[JsonDerivedType(typeof(SeparatorEntry), "separator")]
public abstract class MenuEntry
{
    /// <summary>
    /// A permanent name for an entry that came from the template, so a later template can tell
    /// which of its entries the user already has or has deleted (ADR-0009). Entries the user
    /// makes have none.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// False keeps the entry in the config but out of the menu; for a submenu, everything in it
    /// too. Null (written as nothing) and true show it. Needs version 3 (ADR-0015).
    /// </summary>
    public bool? Enabled { get; set; }

    [JsonIgnore]
    public bool IsEnabled => Enabled != false;
}

/// <summary>An entry with a visible label whose visibility depends on the selection.</summary>
public abstract class LabeledEntry : MenuEntry
{
    public string Label { get; set; } = "";

    /// <summary>When null the entry matches any non-empty selection.</summary>
    public MatchRule? Match { get; set; }
}

/// <summary>Runs either a program (<see cref="Command"/>) or a built-in tool (<see cref="Tool"/>), never both.</summary>
public sealed class CommandEntry : LabeledEntry
{
    public CommandDefinition? Command { get; set; }

    /// <summary>Needs version 3 (ADR-0015).</summary>
    public BuiltinTool? Tool { get; set; }
}

/// <summary>
/// A nested menu. Its own rule filters the selection first; children are matched against
/// what is left, and a submenu with no visible child is not shown.
/// </summary>
public sealed class SubmenuEntry : LabeledEntry
{
    public List<MenuEntry> Items { get; set; } = [];
}

/// <summary>Leading, trailing and repeated separators are dropped when the menu is built.</summary>
public sealed class SeparatorEntry : MenuEntry
{
}
