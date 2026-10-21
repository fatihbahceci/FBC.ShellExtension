using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Menu;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Jobs;

/// <summary>
/// What the shell extension hands to the runner for one click: a snapshot of the command
/// and the items it received, so a config edited in between does not change the job.
/// </summary>
public sealed class RunJob
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    public string Label { get; set; } = "";

    /// <summary>The program to run; left empty when <see cref="Tool"/> is set.</summary>
    public CommandDefinition Command { get; set; } = new();

    /// <summary>A built-in tool the runner does itself instead of starting a program (ADR-0015).</summary>
    public BuiltinTool? Tool { get; set; }

    public List<SelectedItem> Items { get; set; } = [];

    /// <summary>
    /// Version of the program that wrote the job: the shell extension, or the editor when it runs
    /// a command to test it. Explorer keeps an old DLL loaded until it restarts, so this is how
    /// a runner can tell it is talking to an older build.
    /// </summary>
    public string? ShellExtVersion { get; set; }

    /// <summary>The job for a clicked menu item, stamped with this build's version.</summary>
    public static RunJob For(ActionNode action) => new()
    {
        Label = action.Label,
        Command = (action as CommandNode)?.Command ?? new CommandDefinition(),
        Tool = (action as ToolNode)?.Tool,
        Items = [.. action.Items],
        ShellExtVersion = ProductInfo.InformationalVersion,
    };
}
