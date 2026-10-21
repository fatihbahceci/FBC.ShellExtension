using FBC.ShellExtension.Core.Configuration;

namespace FBC.ShellExtension.Core.Editing;

/// <param name="Entry">The entry the issue is about; null for the document itself (<c>$.version</c>).</param>
/// <param name="Member">What is left of the path inside the entry, such as <c>command.fileName</c>; empty for the entry itself.</param>
public sealed record IssueTarget(MenuEntry? Entry, string Member);

/// <summary>Turns the path of a <see cref="ConfigIssue"/> back into the entry it points at.</summary>
public static class IssueLocator
{
    private const string Items = ".items[";

    public static IssueTarget Resolve(MenuConfig config, string path)
    {
        if (!path.StartsWith('$'))
        {
            return new IssueTarget(null, path);
        }

        ReadOnlySpan<char> rest = path.AsSpan(1);
        List<MenuEntry>? entries = config.Items;
        MenuEntry? entry = null;
        while (entries is not null && rest.StartsWith(Items, StringComparison.Ordinal))
        {
            ReadOnlySpan<char> afterBracket = rest[Items.Length..];
            int close = afterBracket.IndexOf(']');
            if (close <= 0 || !int.TryParse(afterBracket[..close], out int index) || index < 0 || index >= entries.Count)
            {
                break;
            }

            entry = entries[index];
            rest = afterBracket[(close + 1)..];
            entries = (entry as SubmenuEntry)?.Items;
        }

        return new IssueTarget(entry, rest.TrimStart('.').ToString());
    }
}
