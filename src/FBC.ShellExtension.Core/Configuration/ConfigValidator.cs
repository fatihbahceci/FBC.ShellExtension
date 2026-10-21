using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Core.Configuration;

/// <param name="Path">Location in the document, in the same <c>$.items[0].label</c> form as JSON errors.</param>
public sealed record ConfigIssue(string Path, string Message);

/// <summary>
/// Checks what the JSON schema cannot express. A config with issues is not shown as a menu;
/// the caller decides how to surface them.
/// </summary>
public static class ConfigValidator
{
    private static readonly char[] s_nameSeparators = ['\\', '/'];
    private static readonly char[] s_wildcards = ['*', '?'];

    public static IReadOnlyList<ConfigIssue> Validate(MenuConfig config)
    {
        var issues = new List<ConfigIssue>();
        if (config.Version is < MenuConfig.MinimumVersion or > MenuConfig.CurrentVersion)
        {
            issues.Add(new("$.version", Strings.ValidationUnsupportedVersion(config.Version, MenuConfig.CurrentVersion)));
        }

        ValidateEntries(config.Items, "$.items", config.Version, new HashSet<string>(StringComparer.OrdinalIgnoreCase), issues);
        return issues;
    }

    private static void ValidateEntries(List<MenuEntry> entries, string path, int version, HashSet<string> ids, List<ConfigIssue> issues)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            string entryPath = $"{path}[{i}]";
            MenuEntry? entry = entries[i];
            if (entry is null)
            {
                issues.Add(new(entryPath, Strings.ValidationNullEntry()));
                continue;
            }

            if (entry.Id is { } id)
            {
                if (version < 2)
                {
                    issues.Add(new($"{entryPath}.id", Strings.ValidationIdNeedsVersion()));
                }
                else if (string.IsNullOrWhiteSpace(id))
                {
                    issues.Add(new($"{entryPath}.id", Strings.ValidationIdEmpty()));
                }
                else if (!ids.Add(id))
                {
                    issues.Add(new($"{entryPath}.id", Strings.ValidationIdDuplicate(id)));
                }
            }

            if (entry.Enabled is not null && version < 3)
            {
                issues.Add(new($"{entryPath}.enabled", Strings.ValidationEnabledNeedsVersion()));
            }

            if (entry is LabeledEntry labeled)
            {
                if (string.IsNullOrWhiteSpace(labeled.Label))
                {
                    issues.Add(new($"{entryPath}.label", Strings.ValidationLabelRequired()));
                }

                if (labeled.Match is { } match)
                {
                    ValidateMatch(match, $"{entryPath}.match", issues);
                }
            }

            switch (entry)
            {
                case CommandEntry { Tool: not null, Command: not null }:
                    issues.Add(new($"{entryPath}.tool", Strings.ValidationToolAndCommand()));
                    break;
                case CommandEntry { Tool: { } tool }:
                    if (version < BuiltinTools.SinceVersion(tool))
                    {
                        issues.Add(new($"{entryPath}.tool", Strings.ValidationToolNeedsVersion(BuiltinTools.SinceVersion(tool))));
                    }

                    break;
                case CommandEntry { Command: null }:
                    issues.Add(new($"{entryPath}.command", Strings.ValidationCommandRequired()));
                    break;
                case CommandEntry { Command: { } command }:
                    ValidateCommand(command, $"{entryPath}.command", issues);
                    break;
                case SubmenuEntry submenu when submenu.Items.Count == 0:
                    issues.Add(new($"{entryPath}.items", Strings.ValidationSubmenuEmpty()));
                    break;
                case SubmenuEntry submenu:
                    ValidateEntries(submenu.Items, $"{entryPath}.items", version, ids, issues);
                    break;
            }
        }
    }

    private static void ValidateMatch(MatchRule match, string path, List<ConfigIssue> issues)
    {
        if (match.MinCount < 1)
        {
            issues.Add(new($"{path}.minCount", Strings.ValidationMinCount()));
        }

        if (match.MaxCount is int max && max < match.MinCount)
        {
            issues.Add(new($"{path}.maxCount", Strings.ValidationMaxCount(match.MinCount)));
        }

        ValidateNames(match.Extensions, $"{path}.extensions", allowWildcards: false, issues);
        ValidateNames(match.Patterns, $"{path}.patterns", allowWildcards: true, issues);
        ValidateNames(match.Exclude, $"{path}.exclude", allowWildcards: true, issues);
    }

    private static void ValidateNames(List<string>? values, string path, bool allowWildcards, List<ConfigIssue> issues)
    {
        if (values is null)
        {
            return;
        }

        for (int i = 0; i < values.Count; i++)
        {
            string? value = values[i];
            bool valid = !string.IsNullOrWhiteSpace(value)
                && value.Trim('.').Length > 0
                && value.IndexOfAny(s_nameSeparators) < 0
                && (allowWildcards || value.IndexOfAny(s_wildcards) < 0);
            if (!valid)
            {
                issues.Add(new($"{path}[{i}]", allowWildcards
                    ? Strings.ValidationPatternInvalid(value ?? "")
                    : Strings.ValidationExtensionInvalid(value ?? "")));
            }
        }
    }

    private static void ValidateCommand(CommandDefinition command, string path, List<ConfigIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            issues.Add(new($"{path}.fileName", Strings.ValidationFileNameRequired()));
        }

        if (command.MaxParallelism is < 1)
        {
            issues.Add(new($"{path}.maxParallelism", Strings.ValidationMaxParallelism()));
        }

        if (!string.IsNullOrWhiteSpace(command.Verb) && !command.RunsElevated && !command.UseShellExecute)
        {
            issues.Add(new($"{path}.verb", Strings.ValidationVerbNeedsShellExecute(command.Verb)));
        }

        if (command.ShowOutput && command.UseShellExecute)
        {
            issues.Add(new($"{path}.showOutput", Strings.ValidationShowOutputNeedsNoShellExecute()));
        }

        if (command.ShowOutput && command.RunMode == RunMode.Launch)
        {
            issues.Add(new($"{path}.showOutput", Strings.ValidationShowOutputNeedsTrack()));
        }
    }
}
