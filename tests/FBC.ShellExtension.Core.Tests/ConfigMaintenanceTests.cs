using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Setup;
using FBC.ShellExtension.Core.Templates;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class ConfigMaintenanceTests
{
    private static readonly DateTime s_now = new(2026, 10, 20, 12, 0, 0);
    private static readonly Version s_version = new(1, 0);
    private string _folder = "";
    private string _config = "";
    private string _template = "";

    private static string Json(params string[] ids) => ConfigSerializer.Serialize(new MenuConfig
    {
        Items = [.. ids.Select(id => (MenuEntry)new CommandEntry { Id = id, Label = id, Command = new CommandDefinition { FileName = "a.exe" } })],
    });

    private string[] Labels() => [.. ConfigSerializer.Load(_config).Items.Cast<LabeledEntry>().Select(e => e.Label)];

    [TestInitialize]
    public void CreateFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fbctools-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _config = Path.Combine(_folder, "config.json");
        _template = Path.Combine(_folder, "config.template.json");
    }

    [TestCleanup]
    public void DeleteFolder() => Directory.Delete(_folder, recursive: true);

    [TestMethod]
    public void Update_CreatesThenMergesWithABackup()
    {
        File.WriteAllText(_template, "// shipped\n" + Json("a"));
        Assert.AreEqual(TemplateAction.Created, ConfigMaintenance.UpdateFromTemplate(_config, _template, s_now, s_version).Action);
        StringAssert.StartsWith(File.ReadAllText(_config), "// shipped", "the template is written with its comments");

        File.WriteAllText(_config, Json("mine", "a"));
        File.WriteAllText(_template, Json("a", "b"));
        MaintenanceResult merged = ConfigMaintenance.UpdateFromTemplate(_config, _template, s_now, s_version);

        Assert.AreEqual(TemplateAction.Merged, merged.Action);
        CollectionAssert.AreEqual(new[] { "mine", "a", "b" }, Labels());
        Assert.IsNotNull(merged.Backup);
        Assert.AreEqual(Json("mine", "a"), File.ReadAllText(merged.Backup));
        Assert.AreEqual(TemplateAction.UpToDate, ConfigMaintenance.UpdateFromTemplate(_config, _template, s_now, s_version).Action);
    }

    [TestMethod]
    public void Restore_ReplacesTheConfigAndTreatsItAsTheUsersOwn()
    {
        File.WriteAllText(_template, Json("a"));
        ConfigMaintenance.UpdateFromTemplate(_config, _template, s_now, s_version);
        string chosen = Path.Combine(_folder, "chosen.json");
        File.WriteAllText(chosen, Json("restored"));

        string? backup = ConfigMaintenance.Restore(_config, chosen, s_now);

        CollectionAssert.AreEqual(new[] { "restored" }, Labels());
        Assert.IsNotNull(backup);
        Assert.IsNull(TemplateState.Load(TemplateState.PathFor(_config))!.ConfigHash);

        // A new template now merges instead of replacing the restored menu.
        File.WriteAllText(_template, Json("a", "b"));
        ConfigMaintenance.UpdateFromTemplate(_config, _template, s_now.AddMinutes(1), s_version);
        CollectionAssert.AreEqual(new[] { "restored", "b" }, Labels());
    }

    [TestMethod]
    public void Restore_RefusesAFileThatIsNoConfig()
    {
        File.WriteAllText(_config, Json("mine"));
        string chosen = Path.Combine(_folder, "chosen.json");
        File.WriteAllText(chosen, "{ not json");

        Assert.ThrowsExactly<ConfigLoadException>(() => ConfigMaintenance.Restore(_config, chosen, s_now));
        CollectionAssert.AreEqual(new[] { "mine" }, Labels());
    }

    [TestMethod]
    public void Reset_PutsTheTemplateBackAndKeepsWhatWasOffered()
    {
        File.WriteAllText(_template, Json("a", "b"));
        ConfigMaintenance.UpdateFromTemplate(_config, _template, s_now, s_version);
        File.WriteAllText(_config, Json("mine"));
        File.WriteAllText(_template, Json("a"));

        string? backup = ConfigMaintenance.Reset(_config, _template, s_now, s_version);

        CollectionAssert.AreEqual(new[] { "a" }, Labels());
        Assert.AreEqual(Json("mine"), File.ReadAllText(backup!));
        TemplateState state = TemplateState.Load(TemplateState.PathFor(_config))!;
        Assert.AreEqual(TemplateState.Hash(Json("a")), state.ConfigHash);
        CollectionAssert.Contains(state.AppliedIds, "b");
    }

    [TestMethod]
    public void Backup_CopiesTheConfig_OrSaysThereIsNone()
    {
        string target = Path.Combine(_folder, "copy.json");
        Assert.ThrowsExactly<FileNotFoundException>(() => ConfigMaintenance.Backup(_config, target));

        File.WriteAllText(_config, Json("mine"));
        ConfigMaintenance.Backup(_config, target);

        Assert.AreEqual(Json("mine"), File.ReadAllText(target));
    }

    [TestMethod]
    public void RegistrationKeys_NameTheClsidAndTheHandlers()
    {
        StringAssert.StartsWith(RegistrationKeys.InprocServerKey, @"Software\Classes\CLSID\{6A2B342C-");
        Assert.AreEqual(@"Software\Classes\Directory\Background\shellex\ContextMenuHandlers\FBCTools", RegistrationKeys.HandlerKey(@"Directory\Background"));
        Assert.HasCount(4, RegistrationKeys.HandlerClasses);
    }
}
