using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Commands;

/// <summary>One process to start, with every placeholder already expanded.</summary>
/// <param name="Items">The items this process covers, for reporting which ones failed.</param>
public sealed record PlannedProcess(
    string FileName,
    string Arguments,
    string WorkingDirectory,
    IReadOnlyList<SelectedItem> Items);

public static class CommandPlanner
{
    private const string DefaultWorkingDirectory = "{dir}";

    /// <summary>
    /// One process per item for the per-file modes, one for <see cref="MultiSelectMode.AllAtOnce"/>,
    /// where the single-item placeholders refer to the first item. <c>{allPaths}</c> always
    /// holds every item the command received.
    /// </summary>
    public static IReadOnlyList<PlannedProcess> Plan(
        CommandDefinition command,
        IReadOnlyList<SelectedItem> items,
        Func<string, string?> getEnvironmentVariable)
    {
        if (items.Count == 0)
        {
            return [];
        }

        if (command.MultiSelect == MultiSelectMode.AllAtOnce)
        {
            return [PlanOne(command, items[0], items, items, getEnvironmentVariable)];
        }

        return items.Select(item => PlanOne(command, item, [item], items, getEnvironmentVariable)).ToList();
    }

    public static IReadOnlyList<PlannedProcess> Plan(CommandDefinition command, IReadOnlyList<SelectedItem> items) =>
        Plan(command, items, Environment.GetEnvironmentVariable);

    private static PlannedProcess PlanOne(
        CommandDefinition command,
        SelectedItem current,
        IReadOnlyList<SelectedItem> covered,
        IReadOnlyList<SelectedItem> all,
        Func<string, string?> env)
    {
        string workingDirectory = string.IsNullOrWhiteSpace(command.WorkingDirectory)
            ? DefaultWorkingDirectory
            : command.WorkingDirectory;

        return new PlannedProcess(
            VariableExpander.Expand(command.FileName, current, all, env),
            VariableExpander.Expand(command.Arguments ?? "", current, all, env),
            VariableExpander.Expand(workingDirectory, current, all, env),
            covered);
    }
}
