using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Menu;
using FBC.ShellExtension.Core.Selection;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class RuleMatcherTests
{
    [TestMethod]
    public void NullRule_AcceptsAnyNonEmptySelection()
    {
        SelectedItem[] selection = [File(@"C:\a.txt"), Drive(@"D:\")];

        Assert.AreSame(selection, RuleMatcher.Filter(null, selection));
        Assert.IsNull(RuleMatcher.Filter(null, []));
    }

    [TestMethod]
    [DataRow(ItemKind.File, true)]
    [DataRow(ItemKind.Folder, true)]
    [DataRow(ItemKind.Background, false)]
    [DataRow(ItemKind.Drive, false)]
    public void Targets_RestrictTheItemKind(ItemKind kind, bool expected)
    {
        var rule = new MatchRule { Targets = [ItemKind.File, ItemKind.Folder] };

        Assert.AreEqual(expected, RuleMatcher.Matches(rule, new SelectedItem(@"C:\x", kind)));
    }

    [TestMethod]
    [DataRow("jpg", "photo.jpg", true)]
    [DataRow(".jpg", "PHOTO.JPG", true)]
    [DataRow("jpg", "photo.jpeg", false)]
    [DataRow("jpg", "jpg", false)]
    [DataRow("jpg", "photojpg", false)]
    [DataRow("tar.gz", "backup.tar.gz", true)]
    [DataRow("gz", "backup.tar.gz", true)]
    public void Extensions_MatchTheEndOfTheFileName(string extension, string fileName, bool expected)
    {
        var rule = new MatchRule { Extensions = [extension] };

        Assert.AreEqual(expected, RuleMatcher.Matches(rule, File(@"C:\in\" + fileName)));
    }

    [TestMethod]
    public void Extensions_DoNotRestrictFolders()
    {
        var rule = new MatchRule { Extensions = ["txt"] };

        Assert.IsTrue(RuleMatcher.Matches(rule, Folder(@"C:\src")));
    }

    [TestMethod]
    public void ExtensionsAndPatterns_AreEitherOr()
    {
        var rule = new MatchRule { Extensions = ["txt"], Patterns = ["README*"] };

        Assert.IsTrue(RuleMatcher.Matches(rule, File(@"C:\notes.txt")));
        Assert.IsTrue(RuleMatcher.Matches(rule, File(@"C:\README.md")));
        Assert.IsFalse(RuleMatcher.Matches(rule, File(@"C:\notes.md")));
    }

    [TestMethod]
    [DataRow("*.min.js", "app.min.js", true)]
    [DataRow("*.min.js", "app.js", false)]
    [DataRow("report-??.pdf", "report-07.pdf", true)]
    [DataRow("report-??.pdf", "report-7.pdf", false)]
    public void Patterns_MatchTheName(string pattern, string fileName, bool expected)
    {
        var rule = new MatchRule { Patterns = [pattern] };

        Assert.AreEqual(expected, RuleMatcher.Matches(rule, File(@"C:\web\" + fileName)));
    }

    [TestMethod]
    public void Patterns_ApplyToFolderNamesToo()
    {
        var rule = new MatchRule { Targets = [ItemKind.Folder], Patterns = ["bin", "obj"] };

        Assert.IsTrue(RuleMatcher.Matches(rule, Folder(@"C:\proj\BIN")));
        Assert.IsFalse(RuleMatcher.Matches(rule, Folder(@"C:\proj\src")));
    }

    [TestMethod]
    public void Exclude_WinsOverInclude()
    {
        var rule = new MatchRule { Extensions = ["js"], Exclude = ["*.min.js"] };

        Assert.IsTrue(RuleMatcher.Matches(rule, File(@"C:\app.js")));
        Assert.IsFalse(RuleMatcher.Matches(rule, File(@"C:\app.min.js")));
    }

    [TestMethod]
    public void NameRules_AreIgnoredForBackgroundAndDrive()
    {
        var rule = new MatchRule { Patterns = ["*.txt"], Exclude = ["*"] };

        Assert.IsTrue(RuleMatcher.Matches(rule, Background(@"C:\work")));
        Assert.IsTrue(RuleMatcher.Matches(rule, Drive(@"C:\")));
    }

    [TestMethod]
    public void ModeAll_HidesWhenAnyItemFails()
    {
        var rule = new MatchRule { Extensions = ["jpg"], Mode = MatchMode.All };

        Assert.IsNull(RuleMatcher.Filter(rule, [File(@"C:\a.jpg"), File(@"C:\b.txt")]));
        Assert.HasCount(2, RuleMatcher.Filter(rule, [File(@"C:\a.jpg"), File(@"C:\b.JPG")])!);
    }

    [TestMethod]
    public void ModeAny_PassesOnlyTheMatchingItems()
    {
        var rule = new MatchRule { Extensions = ["jpg"], Mode = MatchMode.Any };

        IReadOnlyList<SelectedItem>? received = RuleMatcher.Filter(rule, [File(@"C:\a.jpg"), File(@"C:\b.txt"), File(@"C:\c.jpg")]);

        CollectionAssert.AreEqual(new[] { @"C:\a.jpg", @"C:\c.jpg" }, received!.Select(i => i.Path).ToArray());
        Assert.IsNull(RuleMatcher.Filter(rule, [File(@"C:\b.txt")]));
    }

    [TestMethod]
    [DataRow(MatchMode.All, 1, 2, 3, false)]
    [DataRow(MatchMode.All, 2, 3, 3, true)]
    [DataRow(MatchMode.All, 4, null, 3, false)]
    [DataRow(MatchMode.Any, 1, 1, 1, true)]
    public void Counts_ApplyToTheReceivedItems(MatchMode mode, int min, int? max, int selected, bool shown)
    {
        var rule = new MatchRule { Mode = mode, MinCount = min, MaxCount = max };
        SelectedItem[] selection = Enumerable.Range(0, selected).Select(i => File($@"C:\f{i}.txt")).ToArray();

        Assert.AreEqual(shown, RuleMatcher.Filter(rule, selection) is not null);
    }

    [TestMethod]
    public void ModeAny_CountsOnlyMatchingItems()
    {
        var rule = new MatchRule { Extensions = ["jpg"], Mode = MatchMode.Any, MaxCount = 1 };

        Assert.IsNotNull(RuleMatcher.Filter(rule, [File(@"C:\a.jpg"), File(@"C:\b.txt"), File(@"C:\c.txt")]));
        Assert.IsNull(RuleMatcher.Filter(rule, [File(@"C:\a.jpg"), File(@"C:\b.jpg")]));
    }

    [TestMethod]
    public void NullExtension_IsSkippedInsteadOfThrowing()
    {
        // A hand-written [null] loads; the validator reports it, the editor's preview still builds.
        var rule = new MatchRule { Extensions = [null!, "txt"] };

        Assert.IsTrue(RuleMatcher.Matches(rule, File(@"C:\a.txt")));
        Assert.IsFalse(RuleMatcher.Matches(rule, File(@"C:\a.png")));
    }
}
