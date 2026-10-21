using FBC.ShellExtension.Core.Configuration;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class ConfigFileTests
{
    private string _folder = "";

    [TestInitialize]
    public void CreateFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fbctools-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void DeleteFolder() => Directory.Delete(_folder, recursive: true);

    [TestMethod]
    public void Save_WritesAReadableFileAndLeavesNoTemporaryFile()
    {
        string path = Path.Combine(_folder, "sub", "config.json");
        var config = new MenuConfig { Items = [new CommandEntry { Label = "Çalıştır", Command = new CommandDefinition { FileName = "a.exe" } }] };

        ConfigSerializer.Save(config, path);
        ConfigSerializer.Save(config, path);

        Assert.AreEqual("Çalıştır", ((CommandEntry)ConfigSerializer.Load(path).Items[0]).Label);
        CollectionAssert.AreEqual(new[] { "config.json" }, Directory.GetFiles(Path.GetDirectoryName(path)!).Select(Path.GetFileName).ToArray());
        byte[] bytes = File.ReadAllBytes(path);
        Assert.AreNotEqual(0xEF, bytes[0], "no byte order mark");
    }

    [TestMethod]
    [DataRow("{ // note\n \"version\": 1 }", true)]
    [DataRow("{ \"version\": 1, /* block */ \"items\": [] }", true)]
    [DataRow("{ \"version\": 1, \"items\": [ { \"label\": \"// not a comment\" } ] }", false)]
    [DataRow("not json // at all", false)]
    public void ContainsComments_SeesOnlyRealComments(string json, bool expected)
    {
        Assert.AreEqual(expected, ConfigSerializer.ContainsComments(json));
    }

    [TestMethod]
    public void BackupPath_UsesTheTimeAndAvoidsExistingFiles()
    {
        string config = Path.Combine(_folder, "config.json");
        var time = new DateTime(2026, 10, 20, 18, 58, 42);

        string first = ConfigLocation.BackupPath(config, time);
        File.WriteAllText(first, "");
        string second = ConfigLocation.BackupPath(config, time);

        Assert.AreEqual(Path.Combine(_folder, "config.20261020-185842.bak.json"), first);
        Assert.AreEqual(Path.Combine(_folder, "config.20261020-185842-2.bak.json"), second);
    }
}
