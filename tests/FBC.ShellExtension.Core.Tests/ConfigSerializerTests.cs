using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class ConfigSerializerTests
{
    internal const string SampleJson = """
        {
          // Comments and trailing commas are accepted: the file is also edited by hand.
          "version": 1,
          "items": [
            {
              "type": "command",
              "label": "Open in VS Code",
              "match": { "targets": ["file", "folder", "background"] },
              "command": {
                "fileName": "%LOCALAPPDATA%\\Programs\\Microsoft VS Code\\Code.exe",
                "arguments": "\"{path}\"",
                "runMode": "launch",
              },
            },
            { "type": "separator" },
            {
              "type": "submenu",
              "label": "Images",
              "match": { "targets": ["file"], "extensions": ["jpg", ".png"], "mode": "any" },
              "items": [
                {
                  "type": "command",
                  "label": "Convert to WebP",
                  "command": {
                    "fileName": "cwebp",
                    "arguments": "\"{path}\" -o \"{dir}\\{nameNoExt}.webp\"",
                    "windowStyle": "hidden",
                    "createNoWindow": true,
                    "multiSelect": "perFileParallel",
                    "maxParallelism": 4
                  }
                }
              ]
            }
          ]
        }
        """;

    [TestMethod]
    public void Deserialize_ReadsEveryEntryKind()
    {
        MenuConfig config = ConfigSerializer.Deserialize(SampleJson);

        Assert.AreEqual(1, config.Version);
        Assert.HasCount(3, config.Items);

        var open = (CommandEntry)config.Items[0];
        Assert.AreEqual("Open in VS Code", open.Label);
        CollectionAssert.AreEqual(new[] { ItemKind.File, ItemKind.Folder, ItemKind.Background }, open.Match!.Targets);
        Assert.AreEqual(RunMode.Launch, open.Command!.RunMode);
        Assert.AreEqual(MultiSelectMode.PerFileSequential, open.Command.MultiSelect);

        Assert.IsInstanceOfType<SeparatorEntry>(config.Items[1]);

        var images = (SubmenuEntry)config.Items[2];
        Assert.AreEqual(MatchMode.Any, images.Match!.Mode);
        var convert = (CommandEntry)images.Items[0];
        Assert.AreEqual(CommandWindowStyle.Hidden, convert.Command!.WindowStyle);
        Assert.IsTrue(convert.Command.CreateNoWindow);
        Assert.AreEqual(RunMode.Track, convert.Command.RunMode);
        Assert.AreEqual(MultiSelectMode.PerFileParallel, convert.Command.MultiSelect);
        Assert.AreEqual(4, convert.Command.MaxParallelism);
    }

    [TestMethod]
    public void Serialize_WritesCamelCaseEnumsAndRoundTrips()
    {
        MenuConfig original = ConfigSerializer.Deserialize(SampleJson);

        string json = ConfigSerializer.Serialize(original);
        MenuConfig copy = ConfigSerializer.Deserialize(json);

        Assert.Contains("\"type\": \"submenu\"", json);
        Assert.Contains("\"multiSelect\": \"perFileParallel\"", json);
        Assert.Contains("\"windowStyle\": \"hidden\"", json);
        Assert.DoesNotContain("runsElevated", json);
        Assert.AreEqual(json, ConfigSerializer.Serialize(copy));
    }

    [TestMethod]
    public void Deserialize_AcceptsTypeAfterOtherProperties()
    {
        MenuConfig config = ConfigSerializer.Deserialize("""
            { "items": [ { "label": "x", "command": { "fileName": "a.exe" }, "type": "command" } ] }
            """);

        Assert.IsInstanceOfType<CommandEntry>(config.Items[0]);
    }

    [TestMethod]
    public void Deserialize_MissingVersionMeansCurrent()
    {
        Assert.AreEqual(MenuConfig.CurrentVersion, ConfigSerializer.Deserialize("{}").Version);
    }

    [TestMethod]
    [DataRow("""{ "items": [ { "type": "command", "label": "x", "command": { "fileName": "a", "argument": "" } } ] }""", DisplayName = "misspelt property")]
    [DataRow("""{ "items": [ { "type": "command", "label": "x", "command": { "fileName": "a", "runMode": 1 } } ] }""", DisplayName = "enum as number")]
    [DataRow("""{ "items": [ { "type": "command", "label": "x", "command": { "fileName": "a", "runMode": "later" } } ] }""", DisplayName = "unknown enum name")]
    [DataRow("""{ "items": [ { "type": "command", "label": "x", "tool": "copyName, copyUncPath" } ] }""", DisplayName = "enum list")]
    [DataRow("""{ "items": [ { "type": "command", "label": "x", "command": { "fileName": "a", "windowStyle": "hidden,minimized" } } ] }""", DisplayName = "enum list without space")]
    [DataRow("""{ "items": [ { "type": "button", "label": "x" } ] }""", DisplayName = "unknown entry type")]
    [DataRow("""{ "items": [ { "label": "x" } ] }""", DisplayName = "missing entry type")]
    [DataRow("""{ "items": null }""", DisplayName = "null list")]
    [DataRow("""{ "items": [ { "type": "command", "label": null } ] }""", DisplayName = "null label")]
    [DataRow("null", DisplayName = "null document")]
    [DataRow("{ \"items\": [", DisplayName = "truncated")]
    public void Deserialize_RejectsInvalidDocument(string json)
    {
        Assert.ThrowsExactly<ConfigLoadException>(() => ConfigSerializer.Deserialize(json));
    }

    [TestMethod]
    public void Deserialize_ErrorNamesTheLocation()
    {
        var ex = Assert.ThrowsExactly<ConfigLoadException>(() => ConfigSerializer.Deserialize("""
            { "items": [ { "type": "command", "label": "x", "command": { "fileName": "a", "argument": "" } } ] }
            """));

        Assert.Contains("argument", ex.Message);
    }

    [TestMethod]
    [DataRow("""{ "version": 13, "items": [] }""", 13, DisplayName = "first")]
    [DataRow("""{ "items": [ { "type": "submenu", "version": 99, "items": [] } ], "version": 7 }""", 7, DisplayName = "after a nested one")]
    [DataRow("""
        // comment
        { /* c */ "version": 14, }
        """, 14, DisplayName = "comments and a trailing comma")]
    [DataRow("""{ "items": [] }""", null, DisplayName = "none")]
    [DataRow("""{ "version": "13" }""", null, DisplayName = "not a number")]
    [DataRow("""{ "items": [ , "version": 13 }""", null, DisplayName = "broken before it")]
    [DataRow("not json", null, DisplayName = "not json")]
    public void DeclaredVersion_ReadsTheTopLevelVersionOnly(string json, int? expected)
    {
        Assert.AreEqual(expected, ConfigSerializer.DeclaredVersion(json));
    }

    [TestMethod]
    public void Deserialize_ANewerVersionsDocument_SaysSo()
    {
        int newer = MenuConfig.CurrentVersion + 1;
        var ex = Assert.ThrowsExactly<ConfigNewerException>(() => ConfigSerializer.Deserialize($$"""
            { "version": {{newer}}, "items": [ { "type": "command", "label": "x", "tool": "somethingNew" } ] }
            """));

        Assert.AreEqual(newer, ex.Version);
        Assert.Contains("newer FBCTools", ex.Message);
        Assert.Contains("$.items[0].tool", ex.Message);

        // The same mistake in a document of this version is just invalid.
        Assert.ThrowsExactly<ConfigLoadException>(() => ConfigSerializer.Deserialize($$"""
            { "version": {{MenuConfig.CurrentVersion}}, "items": [ { "type": "command", "label": "x", "tool": "somethingNew" } ] }
            """));
    }
}
