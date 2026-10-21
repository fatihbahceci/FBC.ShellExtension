using System.Text.Json.Serialization;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Configuration;

/// <summary>Decides whether an entry is shown for a selection, and which items it receives.</summary>
public sealed class MatchRule
{
    /// <summary>Item kinds the entry applies to. Null or empty means every kind.</summary>
    public List<ItemKind>? Targets { get; set; }

    /// <summary>
    /// File extensions, with or without the leading dot (<c>jpg</c>, <c>.tar.gz</c>).
    /// Applies to files only. A file is included when it matches an extension or a pattern.
    /// </summary>
    public List<string>? Extensions { get; set; }

    /// <summary>Wildcards (<c>*</c>, <c>?</c>) matched against the file or folder name, not the path.</summary>
    public List<string>? Patterns { get; set; }

    /// <summary>Wildcards matched against the file or folder name; a match removes the item.</summary>
    public List<string>? Exclude { get; set; }

    public MatchMode Mode { get; set; } = MatchMode.All;

    /// <summary>Lower bound on the number of items the entry would receive.</summary>
    public int MinCount { get; set; } = 1;

    /// <summary>Upper bound on the number of items the entry would receive; null means no limit.</summary>
    public int? MaxCount { get; set; }
}

[JsonConverter(typeof(CamelCaseEnumConverter<MatchMode>))]
public enum MatchMode
{
    /// <summary>Shown only when every selected item matches; the command receives all of them.</summary>
    All,

    /// <summary>Shown when at least one item matches; the command receives only the matching ones.</summary>
    Any,
}
