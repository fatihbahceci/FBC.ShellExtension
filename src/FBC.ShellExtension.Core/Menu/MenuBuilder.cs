using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Menu;

public abstract record MenuNode
{
    /// <summary>The entry's icon, a PNG (ADR-0041); null for none and for separators.</summary>
    public byte[]? Icon { get; init; }
}

/// <summary>A clickable item: what the shell extension hands to the runner as a job.</summary>
/// <param name="Items">The items it runs on: the whole selection, or the matching part of it.</param>
public abstract record ActionNode(string Label, IReadOnlyList<SelectedItem> Items) : MenuNode;

public sealed record CommandNode(string Label, CommandDefinition Command, IReadOnlyList<SelectedItem> Items) : ActionNode(Label, Items);

/// <summary>A built-in tool; the runner does the work itself (ADR-0015).</summary>
public sealed record ToolNode(string Label, BuiltinTool Tool, IReadOnlyList<SelectedItem> Items) : ActionNode(Label, Items);

public sealed record SubmenuNode(string Label, IReadOnlyList<MenuNode> Children) : MenuNode;

public sealed record SeparatorNode : MenuNode
{
    public static SeparatorNode Instance { get; } = new();

    private SeparatorNode()
    {
    }
}

/// <summary>
/// Turns the configured entries into the menu to show for one selection. Pure: no file
/// system access, so it is cheap enough for Explorer's UI thread.
/// </summary>
public static class MenuBuilder
{
    public static IReadOnlyList<MenuNode> Build(IReadOnlyList<MenuEntry?> entries, IReadOnlyList<SelectedItem> selection)
    {
        var nodes = new List<MenuNode>(entries.Count);
        foreach (MenuEntry? entry in entries)
        {
            if (entry is { IsEnabled: false })
            {
                continue;
            }

            switch (entry)
            {
                case CommandEntry { Tool: { } tool } toolEntry:
                    if (RuleMatcher.Filter(toolEntry.Match, selection) is { } toolItems)
                    {
                        nodes.Add(new ToolNode(toolEntry.Label, tool, toolItems) { Icon = toolEntry.Icon });
                    }

                    break;

                case CommandEntry { Command: { } command } commandEntry:
                    if (RuleMatcher.Filter(commandEntry.Match, selection) is { } items)
                    {
                        nodes.Add(new CommandNode(commandEntry.Label, command, items) { Icon = commandEntry.Icon });
                    }

                    break;

                case SubmenuEntry submenu:
                    if (RuleMatcher.Filter(submenu.Match, selection) is { } received
                        && Build(submenu.Items, received) is { Count: > 0 } children)
                    {
                        nodes.Add(new SubmenuNode(submenu.Label, children) { Icon = submenu.Icon });
                    }

                    break;

                case SeparatorEntry:
                    nodes.Add(SeparatorNode.Instance);
                    break;
            }
        }

        return TrimSeparators(nodes);
    }

    /// <summary>
    /// The taskbar menu (ADR-0043): the commands marked for it that can run with nothing selected,
    /// and the submenus that hold any, with nothing else in them. The rules for the selection do
    /// not apply; every command is given no items. Switched-off entries stay out, as in Explorer.
    /// </summary>
    public static IReadOnlyList<MenuNode> BuildForTaskbar(IReadOnlyList<MenuEntry?> entries)
    {
        var nodes = new List<MenuNode>(entries.Count);
        foreach (MenuEntry? entry in entries)
        {
            if (entry is { IsEnabled: false })
            {
                continue;
            }

            switch (entry)
            {
                case CommandEntry { ShowInTaskbar: true, Command: { } command } commandEntry when TaskbarMenu.Fits(commandEntry):
                    nodes.Add(new CommandNode(commandEntry.Label, command, []) { Icon = commandEntry.Icon });
                    break;

                case SubmenuEntry submenu:
                    if (BuildForTaskbar(submenu.Items) is { Count: > 0 } children)
                    {
                        nodes.Add(new SubmenuNode(submenu.Label, children) { Icon = submenu.Icon });
                    }

                    break;

                case SeparatorEntry:
                    nodes.Add(SeparatorNode.Instance);
                    break;
            }
        }

        return TrimSeparators(nodes);
    }

    private static List<MenuNode> TrimSeparators(List<MenuNode> nodes)
    {
        var result = new List<MenuNode>(nodes.Count);
        foreach (MenuNode node in nodes)
        {
            if (node is SeparatorNode && (result.Count == 0 || result[^1] is SeparatorNode))
            {
                continue;
            }

            result.Add(node);
        }

        if (result.Count > 0 && result[^1] is SeparatorNode)
        {
            result.RemoveAt(result.Count - 1);
        }

        return result;
    }
}
