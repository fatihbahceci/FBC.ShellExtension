using FBC.ShellExtension.Core.Commands;

namespace FBC.ShellExtension.Core.Configuration;

/// <summary>
/// What may go in the taskbar menu (ADR-0043): there nothing is selected, so only a command that
/// starts a program without the selection's names. A built-in tool always works on a selection.
/// </summary>
public static class TaskbarMenu
{
    /// <summary>Whether the command can run with nothing selected: a program, and none of its fields names the selection.</summary>
    public static bool Fits(CommandEntry entry) =>
        entry is { Tool: null, Command: { } command }
        && !VariableExpander.UsesSelection(command.FileName)
        && !VariableExpander.UsesSelection(command.Arguments ?? "")
        && !VariableExpander.UsesSelection(command.WorkingDirectory ?? "");

    /// <summary>The commands under <paramref name="entries"/> that could go in the taskbar menu, at any depth.</summary>
    public static IEnumerable<CommandEntry> FittingCommands(IEnumerable<MenuEntry?> entries)
    {
        foreach (MenuEntry? entry in entries)
        {
            switch (entry)
            {
                case CommandEntry command when Fits(command):
                    yield return command;
                    break;
                case SubmenuEntry submenu:
                    foreach (CommandEntry inner in FittingCommands(submenu.Items))
                    {
                        yield return inner;
                    }

                    break;
            }
        }
    }
}
