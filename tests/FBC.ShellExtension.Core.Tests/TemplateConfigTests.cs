using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Menu;
using FBC.ShellExtension.Core.Templates;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

/// <summary>
/// dist/config.template.json is the menu FBCTools starts with; samples/runner-checks.json is what
/// the Runner's Windows checks use. Both must stay valid.
/// </summary>
[TestClass]
public sealed class TemplateConfigTests
{
    private const string TemplatePath = "dist/config.template.json";

    private static string RepoFile(string relativePath)
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.File.Exists(Path.Combine(dir.FullName, "FBC.ShellExtension.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.IsNotNull(dir, "repo root not found above " + AppContext.BaseDirectory);
        return Path.Combine([dir.FullName, .. relativePath.Split('/')]);
    }

    private static MenuConfig Load(string relativePath = TemplatePath) => ConfigSerializer.Load(RepoFile(relativePath));

    private static IEnumerable<MenuEntry> All(IEnumerable<MenuEntry> entries) =>
        entries.SelectMany(entry => entry is SubmenuEntry submenu ? [entry, .. All(submenu.Items)] : new[] { entry });

    [TestMethod]
    [DataRow(TemplatePath)]
    [DataRow("samples/runner-checks.json")]
    public void Config_IsValid(string relativePath)
    {
        Assert.IsEmpty(ConfigValidator.Validate(Load(relativePath)));
    }

    [TestMethod]
    public void Template_ShowsTheTextSubmenuOnlyForTextFiles()
    {
        MenuConfig template = Load();

        IReadOnlyList<MenuNode> forImage = MenuBuilder.Build(template.Items, [File(@"C:\a.png")]);
        IReadOnlyList<MenuNode> forMixed = MenuBuilder.Build(template.Items, [File(@"C:\a.png"), File(@"C:\b.txt")]);

        Assert.IsFalse(forImage.OfType<SubmenuNode>().Any(s => s.Label == "Text files"));
        var text = forMixed.OfType<SubmenuNode>().Single(s => s.Label == "Text files");
        Assert.AreEqual(@"C:\b.txt", ((ActionNode)text.Children[0]).Items.Single().Path);
    }

    [TestMethod]
    [DataRow(@"C:\v\clip.mkv", "To MP3|To MP4")]
    [DataRow(@"C:\v\clip.mp4", "To MP3")]
    [DataRow(@"C:\v\song.flac", "To MP3")]
    [DataRow(@"C:\v\song.mp3", "")]
    [DataRow(@"C:\i\photo.png", "To JPG")]
    [DataRow(@"C:\i\photo.JPG", "To PNG")]
    [DataRow(@"C:\i\anim.gif", "To JPG|To PNG")]
    [DataRow(@"C:\d\notes.txt", "")]
    public void Template_OffersOnlyTheConversionsThatApply(string path, string expected)
    {
        IReadOnlyList<MenuNode> menu = MenuBuilder.Build(Load().Items, [File(path)]);

        SubmenuNode? convert = menu.OfType<SubmenuNode>().SingleOrDefault(s => s.Label == "Convert");
        string offered = convert is null ? "" : string.Join("|", convert.Children.OfType<CommandNode>().Select(c => c.Label));
        Assert.AreEqual(expected, offered);
    }

    [TestMethod]
    public void Template_ConvertsAMixedSelectionOnlyWhereItCan()
    {
        IReadOnlyList<MenuNode> menu = MenuBuilder.Build(Load().Items, [File(@"C:\m\a.mkv"), File(@"C:\m\b.png"), File(@"C:\m\c.mp3")]);

        var convert = menu.OfType<SubmenuNode>().Single(s => s.Label == "Convert");
        var toMp3 = convert.Children.OfType<CommandNode>().Single(c => c.Label == "To MP3");
        var toJpg = convert.Children.OfType<CommandNode>().Single(c => c.Label == "To JPG");
        Assert.AreEqual(@"C:\m\a.mkv", toMp3.Items.Single().Path);
        Assert.AreEqual(@"C:\m\b.png", toJpg.Items.Single().Path);
        Assert.AreEqual(CommandOutputEncoding.Utf8, toMp3.Command.OutputEncoding);
    }

    [TestMethod]
    public void Template_GivesEveryEntryAnId()
    {
        // Without an id an entry added to a later template could not reach changed configs (ADR-0009).
        MenuConfig template = Load();

        Assert.AreEqual(MenuConfig.CurrentVersion, template.Version);
        Assert.IsTrue(All(template.Items).All(entry => !string.IsNullOrWhiteSpace(entry.Id)));
    }

    [TestMethod]
    [DataRow(2, "copy", "file-tools")]
    [DataRow(3, "file-tools")]
    [DataRow(4, "folder-tools")]
    [DataRow(5, "organize")]
    public void OlderDefaultMenu_GetsWhatCameSinceWithoutAState(int version, params string[] since)
    {
        // A developer copy of an older default menu, made without a state (ADR-0016).
        string templateText = System.IO.File.ReadAllText(RepoFile(TemplatePath));
        MenuConfig old = Load();
        old.Version = version;
        old.Items.RemoveAll(entry => since.Contains(entry.Id));
        string[] expected = [.. All(Load().Items.Where(entry => since.Contains(entry.Id))).Select(entry => entry.Id!)];

        TemplateOutcome outcome = TemplateMerger.Apply(templateText, ConfigSerializer.Serialize(old), null, new Version(0, 8));

        Assert.AreEqual(TemplateAction.Merged, outcome.Action);
        CollectionAssert.AreEquivalent(expected, outcome.AddedIds.ToArray());
    }

    [TestMethod]
    public void Readme_DescribesEveryTemplateEntry()
    {
        // README's "Varsayilan menu" table lists every labeled entry with its id in backticks.
        string readme = System.IO.File.ReadAllText(RepoFile("README.md"));
        IEnumerable<string> missing = All(Load().Items)
            .Where(entry => entry is LabeledEntry)
            .Select(entry => entry.Id!)
            .Where(id => !readme.Contains($"`{id}`", StringComparison.Ordinal));

        Assert.AreEqual("", string.Join(", ", missing));
    }

    [TestMethod]
    public void PublicReadme_ShowsEveryTemplateEntry()
    {
        // README.public.md, the public repository's README (ADR-0023), draws the default menu as a tree.
        string readme = System.IO.File.ReadAllText(RepoFile("README.public.md"));
        IEnumerable<string> missing = All(Load().Items)
            .OfType<LabeledEntry>()
            .Select(entry => entry.Label)
            .Where(label => !readme.Contains(label, StringComparison.Ordinal));

        Assert.AreEqual("", string.Join(", ", missing));
    }
}
