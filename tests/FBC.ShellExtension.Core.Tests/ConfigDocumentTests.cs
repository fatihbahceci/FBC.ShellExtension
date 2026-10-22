using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Editing;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class ConfigDocumentTests
{
    private static readonly DateTime s_now = new(2026, 10, 20, 12, 0, 0);
    private string _folder = "";
    private string _path = "";

    [TestInitialize]
    public void CreateFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fbctools-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _path = Path.Combine(_folder, "config.json");
    }

    [TestCleanup]
    public void DeleteFolder() => Directory.Delete(_folder, recursive: true);

    private string[] Backups() => Directory.GetFiles(_folder, "*.bak.json");

    [TestMethod]
    public void MissingFile_GivesAnEmptyCleanDocument_AndSaveCreatesIt()
    {
        ConfigDocument document = ConfigDocument.Load(_path);

        Assert.IsEmpty(document.Config.Items);
        Assert.IsFalse(document.IsDirty);
        Assert.IsFalse(document.BacksUpOnSave);

        document.Config.Items.Add(new SeparatorEntry());
        Assert.IsTrue(document.IsDirty);
        Assert.IsNull(document.Save(s_now));
        Assert.IsFalse(document.IsDirty);
        Assert.IsTrue(File.Exists(_path));
    }

    [TestMethod]
    public void Formatting_DoesNotCountAsAChange()
    {
        File.WriteAllText(_path, "{\"version\":1,\"items\":[{\"type\":\"separator\"}]}");

        ConfigDocument document = ConfigDocument.Load(_path);

        Assert.IsFalse(document.IsDirty);
        Assert.IsFalse(document.HasComments);
    }

    [TestMethod]
    public void CommentedFile_IsBackedUpOnTheFirstSaveOnly()
    {
        const string original = "{ // mine\n \"items\": [] }";
        File.WriteAllText(_path, original);
        ConfigDocument document = ConfigDocument.Load(_path);
        Assert.IsTrue(document.HasComments);
        Assert.IsTrue(document.BacksUpOnSave);

        string? backup = document.Save(s_now);

        Assert.IsNotNull(backup);
        Assert.AreEqual(original, File.ReadAllText(backup));
        Assert.IsFalse(document.HasComments);
        Assert.IsFalse(document.BacksUpOnSave);
        Assert.IsNull(document.Save(s_now.AddMinutes(1)));
        Assert.HasCount(1, Backups());
    }

    [TestMethod]
    public void StartOver_BacksUpTheUnreadableFile()
    {
        File.WriteAllText(_path, "{ not json");
        Assert.ThrowsExactly<ConfigLoadException>(() => ConfigDocument.Load(_path));

        ConfigDocument document = ConfigDocument.StartOver(_path);
        string? backup = document.Save(s_now);

        Assert.IsNotNull(backup);
        Assert.AreEqual("{ not json", File.ReadAllText(backup));
        Assert.IsEmpty(ConfigSerializer.Load(_path).Items);
    }

    [TestMethod]
    public void ChangedOnDisk_SeesOtherWritersUntilAccepted()
    {
        ConfigDocument document = ConfigDocument.Load(_path);
        document.Save(s_now);
        Assert.IsFalse(document.ChangedOnDisk());

        File.AppendAllText(_path, " ");
        Assert.IsTrue(document.ChangedOnDisk());

        document.AcceptDiskState();
        Assert.IsFalse(document.ChangedOnDisk());

        File.Delete(_path);
        Assert.IsTrue(document.ChangedOnDisk());
    }

    [TestMethod]
    public void KeepDespiteUnreadableFile_BacksUpTheBrokenFileOnTheNextSave()
    {
        ConfigDocument document = ConfigDocument.Load(_path);
        document.Save(s_now);
        File.WriteAllText(_path, "{ broken by hand");
        Assert.IsTrue(document.ChangedOnDisk());

        document.KeepDespiteUnreadableFile();

        Assert.IsFalse(document.ChangedOnDisk());
        Assert.IsTrue(document.BacksUpOnSave);
        string? backup = document.Save(s_now.AddMinutes(1));
        Assert.IsNotNull(backup);
        Assert.AreEqual("{ broken by hand", File.ReadAllText(backup));
    }

    [TestMethod]
    public void Save_DoesNotOverwriteWhatANewerVersionWrote()
    {
        ConfigDocument document = ConfigDocument.Load(_path);
        document.Config.Items.Add(new SeparatorEntry());
        string newer = $$"""{ "version": {{MenuConfig.CurrentVersion + 1}}, "items": [ { "type": "command", "label": "x", "tool": "somethingNew" } ] }""";
        File.WriteAllText(_path, newer);

        // Kept despite an unreadable file, the next save would copy it aside first: not even that.
        document.KeepDespiteUnreadableFile();
        ConfigNewerException ex = Assert.ThrowsExactly<ConfigNewerException>(() => document.Save(s_now));

        Assert.AreEqual(MenuConfig.CurrentVersion + 1, ex.Version);
        Assert.AreEqual(newer, File.ReadAllText(_path));
        Assert.IsEmpty(Backups());
    }

    [TestMethod]
    public void Save_GoesOverANewerFileItCouldRead()
    {
        int newer = MenuConfig.CurrentVersion + 1;
        File.WriteAllText(_path, $$"""{ "version": {{newer}}, "items": [] }""");
        ConfigDocument document = ConfigDocument.Load(_path);
        document.Config.Items.Add(new SeparatorEntry());

        document.Save(s_now);

        Assert.AreEqual(newer, ConfigSerializer.Load(_path).Version);
        Assert.HasCount(1, ConfigSerializer.Load(_path).Items);
    }
}
