using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Selection;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class CommandPlannerTests
{
    private static readonly SelectedItem[] s_selection =
    [
        File(@"C:\in\a.txt"),
        File(@"C:\in\b.txt"),
        File(@"D:\other\c.txt"),
    ];

    private static IReadOnlyList<PlannedProcess> Plan(CommandDefinition command, params SelectedItem[] items) =>
        CommandPlanner.Plan(command, items, Env(("TOOLS", @"C:\tools")));

    [TestMethod]
    [DataRow(MultiSelectMode.PerFileSequential)]
    [DataRow(MultiSelectMode.PerFileParallel)]
    public void PerFileModes_PlanOneProcessPerItem(MultiSelectMode mode)
    {
        var command = new CommandDefinition { FileName = @"%TOOLS%\conv.exe", Arguments = "\"{path}\"", MultiSelect = mode };

        IReadOnlyList<PlannedProcess> plan = Plan(command, s_selection);

        CollectionAssert.AreEqual(
            new[] { "\"C:\\in\\a.txt\"", "\"C:\\in\\b.txt\"", "\"D:\\other\\c.txt\"" },
            plan.Select(p => p.Arguments).ToArray());
        Assert.IsTrue(plan.All(p => p.FileName == @"C:\tools\conv.exe"));
        Assert.AreEqual(@"D:\other\c.txt", plan[2].Items.Single().Path);
    }

    [TestMethod]
    public void AllAtOnce_PlansOneProcessForTheFirstItem()
    {
        var command = new CommandDefinition { FileName = "zip.exe", Arguments = "\"{dir}\\{nameNoExt}.zip\" {allPaths}", MultiSelect = MultiSelectMode.AllAtOnce };

        PlannedProcess process = Plan(command, s_selection).Single();

        Assert.AreEqual("\"C:\\in\\a.zip\" \"C:\\in\\a.txt\" \"C:\\in\\b.txt\" \"D:\\other\\c.txt\"", process.Arguments);
        Assert.HasCount(3, process.Items);
    }

    [TestMethod]
    public void PerFileModes_StillSeeTheWholeSelectionInAllPaths()
    {
        var command = new CommandDefinition { FileName = "diff.exe", Arguments = "{allPaths}" };

        Assert.IsTrue(Plan(command, s_selection).All(p => p.Arguments.Split(' ').Length == 3));
    }

    [TestMethod]
    public void WorkingDirectory_DefaultsToDir()
    {
        var command = new CommandDefinition { FileName = "tool.exe" };

        PlannedProcess process = Plan(command, File(@"C:\in\a.txt")).Single();

        Assert.AreEqual(@"C:\in", process.WorkingDirectory);
        Assert.AreEqual("", process.Arguments);
    }

    [TestMethod]
    public void WorkingDirectory_IsExpandedWhenGiven()
    {
        var command = new CommandDefinition { FileName = "tool.exe", WorkingDirectory = @"%TOOLS%\{nameNoExt}" };

        Assert.AreEqual(@"C:\tools\a", Plan(command, File(@"C:\in\a.txt")).Single().WorkingDirectory);
    }

    [TestMethod]
    public void EmptySelection_PlansNothing()
    {
        Assert.IsEmpty(Plan(new CommandDefinition { FileName = "tool.exe" }));
    }
}
