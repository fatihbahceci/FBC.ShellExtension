using System.Text;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Core.Menu;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

/// <summary>Built-in tools and <c>enabled</c>, config version 3 (ADR-0015).</summary>
[TestClass]
public sealed class BuiltinToolTests
{
    private static CommandEntry Tool(string label, BuiltinTool tool, MatchRule? match = null) =>
        new() { Label = label, Match = match, Tool = tool };

    private static CommandEntry Command(string label) =>
        new() { Label = label, Command = new CommandDefinition { FileName = "a.exe" } };

    private sealed class FakeHost : IToolHost
    {
        public Dictionary<string, byte[]> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> Universal { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Encoding Ansi { get; } = Encoding.Latin1;

        public Dictionary<string, string> Titles { get; } = new(StringComparer.OrdinalIgnoreCase);

        public string? Title(string path) => Titles.GetValueOrDefault(path);

        public string? UniversalName(string path) => Universal.GetValueOrDefault(path);

        public byte[] ReadFile(string path, long maxBytes) => [.. Read(path).Take((int)Math.Min(maxBytes + 1, int.MaxValue))];

        private byte[] Read(string path) =>
            Files.TryGetValue(path, out byte[]? bytes) ? bytes : throw new FileNotFoundException("missing", path);
    }

    [TestMethod]
    public void Config_ReadsAndWritesToolsAndEnabled()
    {
        MenuConfig config = ConfigSerializer.Deserialize("""
            {
              "version": 3,
              "items": [
                { "type": "command", "label": "Path", "tool": "copyUncPath", "enabled": false },
                { "type": "separator", "enabled": true }
              ]
            }
            """);

        var tool = (CommandEntry)config.Items[0];
        Assert.AreEqual(BuiltinTool.CopyUncPath, tool.Tool);
        Assert.IsNull(tool.Command);
        Assert.IsFalse(tool.IsEnabled);
        Assert.IsTrue(config.Items[1].IsEnabled);

        string json = ConfigSerializer.Serialize(config);
        StringAssert.Contains(json, "\"tool\": \"copyUncPath\"");
        StringAssert.Contains(json, "\"enabled\": false");
        Assert.IsEmpty(ConfigValidator.Validate(config));
    }

    [TestMethod]
    public void Enabled_IsNotWrittenWhenUnset()
    {
        string json = ConfigSerializer.Serialize(new MenuConfig { Items = [Command("Run")] });

        Assert.IsFalse(json.Contains("enabled", StringComparison.Ordinal), json);
    }

    [TestMethod]
    public void Validator_NeedsVersion3AndOneThingToRun()
    {
        var both = new CommandEntry { Label = "Both", Tool = BuiltinTool.CopyPath, Command = new CommandDefinition { FileName = "a.exe" } };
        var neither = new CommandEntry { Label = "Neither" };
        var current = new MenuConfig { Items = [Tool("Path", BuiltinTool.CopyPath), both, neither] };
        var old = new MenuConfig { Version = 2, Items = [Tool("Path", BuiltinTool.CopyPath), new SeparatorEntry { Enabled = false }] };

        CollectionAssert.AreEqual(
            new[] { "$.items[1].tool", "$.items[2].command" },
            ConfigValidator.Validate(current).Select(issue => issue.Path).ToArray());
        CollectionAssert.AreEqual(
            new[] { "$.items[0].tool", "$.items[1].enabled" },
            ConfigValidator.Validate(old).Select(issue => issue.Path).ToArray());
    }

    [TestMethod]
    public void RequiredVersion_FollowsWhatTheEntriesUse()
    {
        var plain = new MenuConfig { Items = [Command("Run")] };
        var withId = new MenuConfig { Items = [new SubmenuEntry { Label = "Sub", Items = [new SeparatorEntry { Id = "s" }] }] };
        var withTool = new MenuConfig { Items = [new SubmenuEntry { Label = "Sub", Items = [Tool("Path", BuiltinTool.CopyPath)] }] };
        var withEnabled = new MenuConfig { Items = [new SeparatorEntry { Id = "s", Enabled = true }] };

        Assert.AreEqual(1, plain.RequiredVersion());
        Assert.AreEqual(2, withId.RequiredVersion());
        Assert.AreEqual(3, withTool.RequiredVersion());
        Assert.AreEqual(3, withEnabled.RequiredVersion());
    }

    [TestMethod]
    public void MenuBuilder_LeavesOutDisabledEntriesAndWhatIsInThem()
    {
        MenuEntry[] entries =
        [
            Command("A"),
            new SeparatorEntry { Enabled = false },
            new CommandEntry { Label = "Off", Enabled = false, Command = new CommandDefinition { FileName = "b.exe" } },
            new SubmenuEntry { Label = "Sub", Enabled = false, Items = [Command("Inner")] },
            Command("B"),
        ];

        IReadOnlyList<MenuNode> menu = MenuBuilder.Build(entries, [File(@"C:\a.txt")]);

        CollectionAssert.AreEqual(new[] { "A", "B" }, menu.OfType<ActionNode>().Select(node => node.Label).ToArray());
        Assert.AreEqual(2, menu.Count);
    }

    [TestMethod]
    public void MenuBuilder_GivesAToolTheMatchingItems()
    {
        MenuEntry[] entries = [Tool("Content", BuiltinTool.CopyContent, new MatchRule { Targets = [ItemKind.File], Mode = MatchMode.Any })];

        IReadOnlyList<MenuNode> menu = MenuBuilder.Build(entries, [File(@"C:\a.txt"), Folder(@"C:\d")]);

        var tool = (ToolNode)menu.Single();
        Assert.AreEqual(BuiltinTool.CopyContent, tool.Tool);
        Assert.AreEqual(@"C:\a.txt", tool.Items.Single().Path);
    }

    [TestMethod]
    public void Job_CarriesTheToolThroughItsFile()
    {
        string folder = Directory.CreateTempSubdirectory("fbctools-tools-").FullName;
        try
        {
            var node = new ToolNode("Name", BuiltinTool.CopyName, [File(@"C:\a.txt")]);
            RunJob job = JobFile.ReadAndDelete(JobFile.Write(RunJob.For(node), folder));

            Assert.AreEqual(BuiltinTool.CopyName, job.Tool);
            Assert.AreEqual("Name", job.Label);
            Assert.AreEqual(@"C:\a.txt", job.Items.Single().Path);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public void CopyPathAndName_PutOneItemPerLine()
    {
        SelectedItem[] items = [File(@"C:\in\a.txt"), Folder(@"C:\in\sub"), Background(@"C:\in"), Drive(@"D:\"), Background(@"E:\")];
        var host = new FakeHost();

        Assert.AreEqual("C:\\in\\a.txt\r\nC:\\in\\sub\r\nC:\\in\r\nD:\\\r\nE:\\", ClipboardTools.Run(BuiltinTool.CopyPath, items, host).Text);
        Assert.AreEqual("a.txt\r\nsub\r\nin\r\nD:\r\nE:", ClipboardTools.Run(BuiltinTool.CopyName, items, host).Text);
    }

    [TestMethod]
    public void CopyTitle_CopiesEveryTitleOrNothing()
    {
        var host = new FakeHost();
        host.Titles[@"C:\m\a.mp3"] = "Song";
        host.Titles[@"C:\m\b.docx"] = "Report";

        Assert.AreEqual("Song\r\nReport", ClipboardTools.Run(BuiltinTool.CopyTitle, [File(@"C:\m\a.mp3"), File(@"C:\m\b.docx")], host).Text);

        ToolResult missing = ClipboardTools.Run(BuiltinTool.CopyTitle, [File(@"C:\m\a.mp3"), File(@"C:\m\c.txt")], host);
        Assert.IsNull(missing.Text);
        StringAssert.Contains(missing.Errors.Single(), "c.txt");
    }

    [TestMethod]
    public void CopyUncPath_ReplacesOnlyMappedPaths()
    {
        var host = new FakeHost();
        host.Universal[@"Z:\docs\a.txt"] = @"\\server\share\docs\a.txt";

        ToolResult result = ClipboardTools.Run(BuiltinTool.CopyUncPath, [File(@"Z:\docs\a.txt"), File(@"C:\b.txt")], host);

        Assert.AreEqual("\\\\server\\share\\docs\\a.txt\r\nC:\\b.txt", result.Text);
    }

    [TestMethod]
    public void CopyContent_ReadsBomUtf8AndTheWindowsCodePage()
    {
        var host = new FakeHost();
        host.Files[@"C:\bom.txt"] = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("çalışma")];
        host.Files[@"C:\utf16.txt"] = [0xFF, 0xFE, .. Encoding.Unicode.GetBytes("şöyle")];
        host.Files[@"C:\plain.txt"] = Encoding.UTF8.GetBytes("ğü");
        host.Files[@"C:\ansi.txt"] = [0x63, 0xE9]; // "cé" in Latin-1, not valid UTF-8

        ToolResult result = ClipboardTools.Run(
            BuiltinTool.CopyContent,
            [File(@"C:\bom.txt"), File(@"C:\utf16.txt"), File(@"C:\plain.txt"), File(@"C:\ansi.txt")],
            host);

        Assert.IsEmpty(result.Errors);
        Assert.AreEqual("çalışma\r\nşöyle\r\nğü\r\ncé", result.Text);
    }

    [TestMethod]
    public void CopyContent_CopiesNothingWhenAnItemFails()
    {
        var host = new FakeHost();
        host.Files[@"C:\a.txt"] = Encoding.UTF8.GetBytes("text");
        host.Files[@"C:\b.bin"] = [0x4D, 0x5A, 0x00, 0x01];

        ToolResult result = ClipboardTools.Run(
            BuiltinTool.CopyContent,
            [File(@"C:\a.txt"), File(@"C:\b.bin"), Folder(@"C:\d"), File(@"C:\missing.txt")],
            host);

        Assert.IsNull(result.Text);
        Assert.HasCount(3, result.Errors);
        StringAssert.Contains(result.Errors[0], @"C:\b.bin");
        StringAssert.Contains(result.Errors[1], @"C:\d");
        StringAssert.Contains(result.Errors[2], @"C:\missing.txt");
    }

    [TestMethod]
    public void CopyContent_RefusesANulAnywhere()
    {
        // The clipboard's text ends at the first NUL; what follows would be lost unnoticed.
        var host = new FakeHost();
        host.Files[@"C:\zeros.log"] = [.. Encoding.UTF8.GetBytes(new string('a', 9000)), 0, 0, 0];
        host.Files[@"C:\utf16.txt"] = [0xFF, 0xFE, (byte)'h', 0, 0, 0, (byte)'i', 0];

        ToolResult result = ClipboardTools.Run(BuiltinTool.CopyContent, [File(@"C:\zeros.log"), File(@"C:\utf16.txt")], host);

        Assert.IsNull(result.Text);
        Assert.HasCount(2, result.Errors);
    }

    [TestMethod]
    public void CopyContent_RefusesMoreThanTheLimit()
    {
        var host = new FakeHost();
        host.Files[@"C:\big.txt"] = new byte[ClipboardTools.MaxContentBytes + 1];

        ToolResult result = ClipboardTools.Run(BuiltinTool.CopyContent, [File(@"C:\big.txt")], host);

        Assert.IsNull(result.Text);
        Assert.HasCount(1, result.Errors);
    }
}
