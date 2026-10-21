using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Templates;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class TemplateMergerTests
{
    private static CommandEntry Command(string id) =>
        new() { Id = id, Label = id, Command = new CommandDefinition { FileName = "tool.exe" } };

    private static SubmenuEntry Submenu(string id, params MenuEntry[] items) => new() { Id = id, Label = id, Items = [.. items] };

    private static string Json(params MenuEntry[] items) => ConfigSerializer.Serialize(new MenuConfig { Items = [.. items] });

    private static string Describe(string json) => Describe(ConfigSerializer.Deserialize(json).Items);

    private static string Describe(IEnumerable<MenuEntry> entries) => string.Join(" ", entries.Select(entry => entry switch
    {
        SubmenuEntry s => $"{s.Label}[{Describe(s.Items)}]",
        LabeledEntry l => l.Label,
        _ => "|",
    }));

    private static readonly Version V1 = new(1, 0);

    // Version 1 of the template, and the state a first run leaves for it.
    private static readonly string s_v1 = Json(Command("a"), Command("b"), Submenu("sub", Command("x")));

    private static TemplateState FirstRun() => TemplateMerger.Apply(s_v1, null, null, V1).State;

    [TestMethod]
    public void NoConfig_TheTemplateBecomesIt()
    {
        TemplateOutcome outcome = TemplateMerger.Apply(s_v1, null, null, V1);

        Assert.AreEqual(TemplateAction.Created, outcome.Action);
        Assert.AreEqual(s_v1, outcome.ConfigText);
        CollectionAssert.AreEquivalent(new[] { "a", "b", "sub", "x" }, outcome.State.AppliedIds);
        Assert.AreEqual(TemplateState.Hash(s_v1), outcome.State.ConfigHash);
    }

    [TestMethod]
    public void ConfigWithoutState_IsLeftAloneAndItsTemplateEntriesCountAsOffered()
    {
        string own = Json(Command("mine"));

        TemplateOutcome outcome = TemplateMerger.Apply(s_v1, own, null, V1);

        Assert.AreEqual(TemplateAction.UpToDate, outcome.Action);
        Assert.IsNull(outcome.ConfigText);
        Assert.IsNull(outcome.State.ConfigHash);
        CollectionAssert.AreEquivalent(new[] { "a", "b", "sub", "x" }, outcome.State.AppliedIds);
    }

    [TestMethod]
    public void UnchangedConfig_IsReplacedByANewTemplate()
    {
        string v2 = Json(Command("a"), Command("c"));

        TemplateOutcome same = TemplateMerger.Apply(s_v1, s_v1, FirstRun(), V1);
        TemplateOutcome newer = TemplateMerger.Apply(v2, s_v1, FirstRun(), V1);

        Assert.AreEqual(TemplateAction.UpToDate, same.Action);
        Assert.AreEqual(TemplateAction.Replaced, newer.Action);
        Assert.AreEqual(v2, newer.ConfigText);
        Assert.AreEqual(TemplateState.Hash(v2), newer.State.ConfigHash);
    }

    [TestMethod]
    public void ChangedConfig_GetsNewEntriesAfterTheirEarlierSibling()
    {
        string changed = Json(Command("mine"), Command("a"), Command("b"), Submenu("sub", Command("x")));
        string v2 = Json(Command("a"), Command("c"), Command("b"), Submenu("sub", Command("x")));

        TemplateOutcome outcome = TemplateMerger.Apply(v2, changed, FirstRun(), V1);

        Assert.AreEqual(TemplateAction.Merged, outcome.Action);
        Assert.AreEqual("mine a c b sub[x]", Describe(outcome.ConfigText!));
        CollectionAssert.AreEqual(new[] { "c" }, outcome.AddedIds.ToArray());
        Assert.IsNull(outcome.State.ConfigHash);
        CollectionAssert.Contains(outcome.State.AppliedIds, "c");
    }

    [TestMethod]
    public void DeletedTemplateEntries_DoNotComeBack()
    {
        string changed = Json(Command("a"), Submenu("sub", Command("x")));

        TemplateOutcome outcome = TemplateMerger.Apply(s_v1, changed, FirstRun(), V1);

        Assert.AreEqual(TemplateAction.UpToDate, outcome.Action);
        Assert.IsNull(outcome.ConfigText);
    }

    [TestMethod]
    public void NewEntryOfASubmenu_GoesIntoTheUsersSubmenu_OrToTheRootWhenItIsGone()
    {
        string v2 = Json(Command("a"), Command("b"), Submenu("sub", Command("x"), Command("y")));
        string kept = Json(Command("a"), Submenu("sub", Command("x"), Command("mine")));
        string deleted = Json(Command("a"));

        Assert.AreEqual("a sub[x y mine]", Describe(TemplateMerger.Apply(v2, kept, FirstRun(), V1).ConfigText!));
        Assert.AreEqual("a y", Describe(TemplateMerger.Apply(v2, deleted, FirstRun(), V1).ConfigText!));
    }

    [TestMethod]
    public void NewSubmenu_ComesWithItsChildrenOnce()
    {
        string v2 = Json(Command("a"), Command("b"), Submenu("sub", Command("x")), Submenu("new", Command("n1"), Command("n2")));
        string changed = Json(Command("b"), Command("a"));

        TemplateOutcome outcome = TemplateMerger.Apply(v2, changed, FirstRun(), V1);

        // The nearest earlier template sibling the user still has is b ("sub" was deleted).
        Assert.AreEqual("b new[n1 n2] a", Describe(outcome.ConfigText!));
        CollectionAssert.AreEqual(new[] { "new", "n1", "n2" }, outcome.AddedIds.ToArray());
    }

    [TestMethod]
    public void MergedOlderConfig_BecomesTheCurrentVersion()
    {
        string v2 = Json(Command("a"), Command("c"));
        string old = """{ "version": 1, "items": [ { "type": "separator" } ] }""";

        TemplateOutcome outcome = TemplateMerger.Apply(v2, old, FirstRun(), V1);

        Assert.AreEqual(MenuConfig.CurrentVersion, ConfigSerializer.Deserialize(outcome.ConfigText!).Version);
    }

    [TestMethod]
    public void State_SurvivesARoundTrip()
    {
        string folder = Path.Combine(Path.GetTempPath(), "fbctools-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string path = TemplateState.PathFor(Path.Combine(folder, "config.json"));
            FirstRun().Save(path);

            TemplateState? loaded = TemplateState.Load(path);

            Assert.IsNotNull(loaded);
            CollectionAssert.AreEquivalent(new[] { "a", "b", "sub", "x" }, loaded.AppliedIds);
            File.WriteAllText(path, "{ broken");
            Assert.IsNull(TemplateState.Load(path));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public void EntriesTheUserHasOrDeleted_AreLeftOutOfANewSubmenu()
    {
        // The template moves x (already given) into a new submenu next to a new entry z.
        string v2 = Json(Command("a"), Command("b"), Submenu("sub"), Submenu("group", Command("x"), Command("z")));
        string kept = Json(Command("mine"), Command("a"), Command("b"), Submenu("sub", Command("x")));
        string deleted = Json(Command("a"), Command("b"));

        TemplateOutcome withX = TemplateMerger.Apply(v2, kept, FirstRun(), V1);
        TemplateOutcome withoutX = TemplateMerger.Apply(v2, deleted, FirstRun(), V1);

        Assert.AreEqual("mine a b sub[x] group[z]", Describe(withX.ConfigText!));
        CollectionAssert.AreEqual(new[] { "group", "z" }, withX.AddedIds.ToArray());
        Assert.AreEqual("a b group[z]", Describe(withoutX.ConfigText!));
        Assert.IsEmpty(ConfigValidator.Validate(ConfigSerializer.Deserialize(withX.ConfigText!)));
    }

    [TestMethod]
    public void NewSubmenuOfOnlyKnownEntries_IsNotAdded()
    {
        string v2 = Json(Command("a"), Command("b"), Submenu("sub", Command("x")), Submenu("group", Command("b")));
        string changed = Json(Command("mine"), Command("a"), Command("b"), Submenu("sub", Command("x")));

        TemplateOutcome outcome = TemplateMerger.Apply(v2, changed, FirstRun(), V1);

        Assert.AreEqual(TemplateAction.UpToDate, outcome.Action);
        CollectionAssert.Contains(outcome.State.AppliedIds, "group");
    }

    [TestMethod]
    public void OlderVersion_LeavesTheConfigToTheNewerOne()
    {
        TemplateState newer = TemplateMerger.Apply(s_v1, null, null, new Version(2, 0)).State;

        TemplateOutcome outcome = TemplateMerger.Apply(Json(Command("a")), s_v1, newer, V1);

        Assert.AreEqual(TemplateAction.ManagedByNewer, outcome.Action);
        Assert.IsNull(outcome.ConfigText);
        Assert.AreEqual("2.0", outcome.State.AppliedBy);
    }

    [TestMethod]
    public void NullEntries_DoNotBreakTheMerge()
    {
        string changed = """{ "version": 2, "items": [ { "type": "separator", "id": "a" }, null ] }""";

        TemplateOutcome outcome = TemplateMerger.Apply(Json(Command("a"), Command("c")), changed, FirstRun(), V1);

        Assert.AreEqual(TemplateAction.Merged, outcome.Action);
        Assert.AreEqual("| c |", Describe(outcome.ConfigText!));
    }

    [TestMethod]
    public void MergeThatWouldAddAnIssue_IsSkipped()
    {
        CommandEntry broken = Command("c");
        broken.Command!.FileName = "";
        string changed = Json(Command("mine"), Command("a"));

        TemplateOutcome outcome = TemplateMerger.Apply(Json(Command("a"), broken), changed, FirstRun(), V1);

        Assert.AreEqual(TemplateAction.Skipped, outcome.Action);
        Assert.IsNull(outcome.ConfigText);
    }

    [TestMethod]
    public void ConfigCopiedFromAnOlderTemplate_GetsWhatCameSinceButNotWhatWasDeleted()
    {
        // A version 2 copy of the 0.6 template without a state, missing to-png; the new template
        // drops copy-paths and adds copy-path (the case of a developer copy, ADR-0016).
        string copy = ConfigSerializer.Serialize(new MenuConfig
        {
            Version = 2,
            Items = [Command("open-in-notepad"), Command("copy-paths"), Command("to-jpg")],
        });
        string template = Json(Command("open-in-notepad"), Command("copy-path"), Command("to-jpg"), Command("to-png"));

        TemplateOutcome outcome = TemplateMerger.Apply(template, copy, null, V1);

        Assert.AreEqual(TemplateAction.Merged, outcome.Action);
        Assert.AreEqual("open-in-notepad copy-path copy-paths to-jpg", Describe(outcome.ConfigText!));
        CollectionAssert.AreEqual(new[] { "copy-path" }, outcome.AddedIds.ToArray());
        Assert.AreEqual(MenuConfig.CurrentVersion, ConfigSerializer.Deserialize(outcome.ConfigText!).Version);
    }

    [TestMethod]
    public void OlderConfigWithoutTemplateIds_IsLeftAlone()
    {
        string own = ConfigSerializer.Serialize(new MenuConfig { Version = 2, Items = [Command("mine")] });

        TemplateOutcome outcome = TemplateMerger.Apply(Json(Command("copy-path")), own, null, V1);

        Assert.AreEqual(TemplateAction.UpToDate, outcome.Action);
        CollectionAssert.AreEquivalent(new[] { "copy-path" }, outcome.State.AppliedIds);
    }

    [TestMethod]
    public void UnreadableConfigWithoutState_IsLeftAlone()
    {
        TemplateOutcome outcome = TemplateMerger.Apply(s_v1, "{ broken", null, V1);

        Assert.AreEqual(TemplateAction.UpToDate, outcome.Action);
        Assert.IsNull(outcome.ConfigText);
    }

    [TestMethod]
    public void TemplateIdHistory_CoversEveryOlderVersionWithIds()
    {
        // Raising MenuConfig.CurrentVersion needs the ids of the last template of the old version.
        CollectionAssert.AreEquivalent(
            Enumerable.Range(2, MenuConfig.CurrentVersion - 2).ToArray(),
            TemplateMerger.TemplateIdsByVersion.Keys.ToArray());
    }

    [TestMethod]
    public void NewFirstEntryOfASubmenu_GoesBeforeItsNextSibling()
    {
        string v2 = Json(Submenu("sub", Command("first"), Command("x")));
        string changed = Json(Command("mine"), Submenu("sub", Command("x"), Command("own")));

        TemplateOutcome outcome = TemplateMerger.Apply(v2, changed, FirstRun(), V1);

        Assert.AreEqual("mine sub[first x own]", Describe(outcome.ConfigText!));
    }

    [TestMethod]
    public void BrokenTemplate_IsReportedAsTheTemplate()
    {
        Assert.ThrowsExactly<TemplateLoadException>(() => TemplateMerger.Apply("{ broken", null, null, V1));
    }

    [TestMethod]
    public void State_WithNullIds_LoadsAsEmpty()
    {
        string folder = Path.Combine(Path.GetTempPath(), "fbctools-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, TemplateState.FileName);
            File.WriteAllText(path, """{ "appliedIds": null }""");

            Assert.IsEmpty(TemplateState.Load(path)!.AppliedIds);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
