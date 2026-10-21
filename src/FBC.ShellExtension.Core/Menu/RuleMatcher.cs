using System.IO.Enumeration;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Menu;

public static class RuleMatcher
{
    /// <summary>
    /// The items an entry receives for this selection, or null when the entry is not shown.
    /// A null rule accepts any non-empty selection.
    /// </summary>
    public static IReadOnlyList<SelectedItem>? Filter(MatchRule? rule, IReadOnlyList<SelectedItem> selection)
    {
        if (rule is null)
        {
            return selection.Count > 0 ? selection : null;
        }

        IReadOnlyList<SelectedItem> received;
        if (rule.Mode == MatchMode.All)
        {
            foreach (SelectedItem item in selection)
            {
                if (!Matches(rule, item))
                {
                    return null;
                }
            }

            received = selection;
        }
        else
        {
            received = selection.Where(item => Matches(rule, item)).ToList();
        }

        if (received.Count < rule.MinCount || received.Count > (rule.MaxCount ?? int.MaxValue))
        {
            return null;
        }

        return received;
    }

    public static bool Matches(MatchRule rule, SelectedItem item)
    {
        if (rule.Targets is { Count: > 0 } targets && !targets.Contains(item.Kind))
        {
            return false;
        }

        // Name rules mean nothing for a drive or a folder background.
        if (item.Kind is not (ItemKind.File or ItemKind.Folder))
        {
            return true;
        }

        string name = item.Name;
        return IsIncluded(rule, item.Kind, name) && !AnyWildcardMatches(rule.Exclude, name);
    }

    private static bool IsIncluded(MatchRule rule, ItemKind kind, string name)
    {
        bool hasExtensions = kind == ItemKind.File && rule.Extensions is { Count: > 0 };
        bool hasPatterns = rule.Patterns is { Count: > 0 };
        if (!hasExtensions && !hasPatterns)
        {
            return true;
        }

        return (hasExtensions && rule.Extensions!.Any(ext => HasExtension(name, ext)))
            || (hasPatterns && AnyWildcardMatches(rule.Patterns, name));
    }

    // Suffix comparison rather than Path.GetExtension, so that "tar.gz" works as an extension.
    // A null element passes the JSON reader (the validator reports it); the editor's preview
    // builds menus from such configs too.
    private static bool HasExtension(string name, string? extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        string ext = extension.TrimStart('.');
        return ext.Length > 0
            && name.Length > ext.Length + 1
            && name[^(ext.Length + 1)] == '.'
            && name.EndsWith(ext, StringComparison.OrdinalIgnoreCase);
    }

    private static bool AnyWildcardMatches(List<string>? patterns, string name) =>
        patterns is not null
        && patterns.Any(pattern => !string.IsNullOrEmpty(pattern)
            && FileSystemName.MatchesSimpleExpression(pattern, name, ignoreCase: true));
}
