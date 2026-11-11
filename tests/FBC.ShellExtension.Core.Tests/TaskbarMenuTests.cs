using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Execution;
using FBC.ShellExtension.Core.Menu;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class TaskbarMenuTests
{
    private static CommandEntry Program(string label, bool? taskbar = true, string arguments = "", string? workingDirectory = null) => new()
    {
        Label = label,
        ShowInTaskbar = taskbar,
        Match = new MatchRule { Extensions = ["never"] },
        Command = new CommandDefinition { FileName = "tool.exe", Arguments = arguments, WorkingDirectory = workingDirectory },
    };

    private static string Describe(IReadOnlyList<MenuNode> nodes) => string.Join(" ", nodes.Select(node => node switch
    {
        CommandNode c => c.Label,
        SubmenuNode s => $"{s.Label}[{Describe(s.Children)}]",
        _ => "|",
    }));

    [TestMethod]
    public void OnlyAProgramThatDoesNotNameTheSelection_Fits()
    {
        Assert.IsTrue(TaskbarMenu.Fits(Program("a", arguments: "--new %USERPROFILE%")));
        Assert.IsFalse(TaskbarMenu.Fits(Program("b", arguments: "\"{path}\"")));
        Assert.IsFalse(TaskbarMenu.Fits(Program("c", workingDirectory: "{dir}")));
        CommandEntry selectedProgram = Program("e");
        selectedProgram.Command!.FileName = @"{dir}\tool.exe";
        Assert.IsFalse(TaskbarMenu.Fits(selectedProgram));
        Assert.IsFalse(TaskbarMenu.Fits(new CommandEntry { Label = "d", Tool = BuiltinTool.CopyPath }));
    }

    [TestMethod]
    public void TheTaskbarMenu_HoldsTheMarkedCommands_AndTheSubmenusAroundThem()
    {
        MenuEntry[] entries =
        [
            Program("Shown"),
            Program("Not marked", taskbar: null),
            new SeparatorEntry(),
            new SubmenuEntry
            {
                Label = "Sub",
                Match = new MatchRule { Targets = [Selection.ItemKind.Drive] },
                Items = [Program("Ten", taskbar: null), new SeparatorEntry(), Program("One"), new SeparatorEntry()],
            },
            new SubmenuEntry { Label = "Empty", Items = [Program("x", taskbar: null)] },
            Program("Uses the selection", arguments: "{allPaths}"),
            Switched(Program("Off"), off: true),
            new SeparatorEntry(),
        ];

        IReadOnlyList<MenuNode> nodes = MenuBuilder.BuildForTaskbar(entries);

        Assert.AreEqual("Shown | Sub[One]", Describe(nodes));
        Assert.IsEmpty(((CommandNode)nodes[0]).Items);
    }

    private static CommandEntry Switched(CommandEntry entry, bool off)
    {
        entry.Enabled = !off;
        return entry;
    }

    [TestMethod]
    public void TheTaskbarFlag_NeedsVersion15()
    {
        Assert.AreEqual(15, new MenuConfig { Items = [Program("a", taskbar: true)] }.RequiredVersion());
        Assert.AreEqual(
            "$.items[0].showInTaskbar",
            ConfigValidator.Validate(new MenuConfig { Version = 14, Items = [Program("a")] }).Single().Path);
        Assert.IsEmpty(ConfigValidator.Validate(new MenuConfig { Items = [Program("a")] }));
    }

    [TestMethod]
    public void ACommandFromTheTaskbar_RunsOnce_InTheHomeFolder_WithOnlyTheEnvironmentExpanded()
    {
        var command = new CommandDefinition { FileName = "%TOOLS%\\a.exe", Arguments = "--x {notAVariable}" };

        PlannedProcess process = CommandPlanner.PlanWithoutSelection(command, @"C:\Users\u", Env(("TOOLS", @"D:\T"))).Single();

        Assert.AreEqual((@"D:\T\a.exe", "--x {notAVariable}", @"C:\Users\u"), (process.FileName, process.Arguments, process.WorkingDirectory));
        Assert.IsEmpty(process.Items);
        Assert.AreEqual(@"D:\T\a.exe", SummaryFormatter.Subject(process));

        command.WorkingDirectory = "%TOOLS%";
        Assert.AreEqual(@"D:\T", CommandPlanner.PlanWithoutSelection(command, @"C:\Users\u", Env(("TOOLS", @"D:\T"))).Single().WorkingDirectory);

        // Nothing stands in for the selection: such a command plans nothing.
        command.Arguments = "\"{path}\"";
        Assert.IsEmpty(CommandPlanner.PlanWithoutSelection(command, @"C:\Users\u", Env()));
    }
}
