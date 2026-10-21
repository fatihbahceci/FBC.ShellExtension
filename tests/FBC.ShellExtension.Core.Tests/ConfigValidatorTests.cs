using FBC.ShellExtension.Core.Configuration;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class ConfigValidatorTests
{
    private static CommandEntry Command(string fileName = "tool.exe", MatchRule? match = null) =>
        new() { Label = "Run", Match = match, Command = new CommandDefinition { FileName = fileName } };

    private static IReadOnlyList<ConfigIssue> Validate(params MenuEntry[] items) =>
        ConfigValidator.Validate(new MenuConfig { Items = [.. items] });

    [TestMethod]
    public void SampleConfig_HasNoIssues()
    {
        MenuConfig config = ConfigSerializer.Deserialize(ConfigSerializerTests.SampleJson);

        Assert.IsEmpty(ConfigValidator.Validate(config));
    }

    [TestMethod]
    public void UnsupportedVersion_IsReported()
    {
        IReadOnlyList<ConfigIssue> issues = ConfigValidator.Validate(new MenuConfig { Version = MenuConfig.CurrentVersion + 1 });

        Assert.AreEqual("$.version", issues.Single().Path);
        Assert.IsEmpty(ConfigValidator.Validate(new MenuConfig { Version = 1 }));
    }

    [TestMethod]
    public void Ids_MustBeUniqueNonEmptyAndNeedVersion2()
    {
        CommandEntry Entry(string? id) => new() { Id = id, Label = "Run", Command = new CommandDefinition { FileName = "a.exe" } };
        var nested = new SubmenuEntry { Id = "sub", Label = "Sub", Items = [Entry("A")] };

        IReadOnlyList<ConfigIssue> issues = ConfigValidator.Validate(new MenuConfig { Items = [Entry("a"), nested, Entry(" ")] });
        IReadOnlyList<ConfigIssue> old = ConfigValidator.Validate(new MenuConfig { Version = 1, Items = [Entry("a")] });

        CollectionAssert.AreEqual(new[] { "$.items[1].items[0].id", "$.items[2].id" }, issues.Select(i => i.Path).ToArray());
        Assert.AreEqual("$.items[0].id", old.Single().Path);
    }

    [TestMethod]
    public void BlankLabel_IsReportedWithItsPath()
    {
        CommandEntry entry = Command();
        entry.Label = " ";

        Assert.AreEqual("$.items[0].label", Validate(entry).Single().Path);
    }

    [TestMethod]
    public void MissingCommandOrFileName_IsReported()
    {
        IReadOnlyList<ConfigIssue> issues = Validate(new CommandEntry { Label = "x" }, Command(fileName: ""));

        CollectionAssert.AreEqual(
            new[] { "$.items[0].command", "$.items[1].command.fileName" },
            issues.Select(i => i.Path).ToArray());
    }

    [TestMethod]
    public void NestedIssues_CarryTheFullPath()
    {
        var submenu = new SubmenuEntry
        {
            Label = "Tools",
            Items = [new SeparatorEntry(), Command(fileName: ""), new SubmenuEntry { Label = "Empty" }],
        };

        CollectionAssert.AreEqual(
            new[] { "$.items[0].items[1].command.fileName", "$.items[0].items[2].items" },
            Validate(submenu).Select(i => i.Path).ToArray());
    }

    [TestMethod]
    public void NullEntry_IsReported()
    {
        Assert.AreEqual("$.items[0]", Validate((MenuEntry)null!).Single().Path);
    }

    [TestMethod]
    [DataRow(0, null, "$.items[0].match.minCount")]
    [DataRow(3, 2, "$.items[0].match.maxCount")]
    public void InvalidCounts_AreReported(int min, int? max, string expectedPath)
    {
        var issues = Validate(Command(match: new MatchRule { MinCount = min, MaxCount = max }));

        Assert.AreEqual(expectedPath, issues.Single().Path);
    }

    [TestMethod]
    public void EqualMinAndMax_IsValid()
    {
        Assert.IsEmpty(Validate(Command(match: new MatchRule { MinCount = 2, MaxCount = 2 })));
    }

    [TestMethod]
    public void ExtensionsAndPatterns_AreChecked()
    {
        var match = new MatchRule
        {
            Extensions = ["jpg", ".png", "*.gif", "", "."],
            Patterns = ["*.min.js", "src\\*.cs"],
            Exclude = ["bin/obj"],
        };

        CollectionAssert.AreEqual(
            new[]
            {
                "$.items[0].match.extensions[2]",
                "$.items[0].match.extensions[3]",
                "$.items[0].match.extensions[4]",
                "$.items[0].match.patterns[1]",
                "$.items[0].match.exclude[0]",
            },
            Validate(Command(match: match)).Select(i => i.Path).ToArray());
    }

    [TestMethod]
    [DataRow("runas", false, true)]
    [DataRow("RunAs", false, true)]
    [DataRow("print", true, true)]
    [DataRow("print", false, false)]
    public void Verb_OtherThanRunAsNeedsShellExecute(string verb, bool useShellExecute, bool valid)
    {
        CommandEntry entry = Command();
        entry.Command!.Verb = verb;
        entry.Command.UseShellExecute = useShellExecute;

        Assert.AreEqual(valid, Validate(entry).Count == 0);
    }

    [TestMethod]
    public void MaxParallelismBelowOne_IsReported()
    {
        CommandEntry entry = Command();
        entry.Command!.MaxParallelism = 0;

        Assert.AreEqual("$.items[0].command.maxParallelism", Validate(entry).Single().Path);
    }
}
