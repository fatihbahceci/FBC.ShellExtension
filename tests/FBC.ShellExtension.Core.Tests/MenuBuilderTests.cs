using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Menu;
using FBC.ShellExtension.Core.Selection;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class MenuBuilderTests
{
    private static CommandEntry Command(string label, MatchRule? match = null) =>
        new() { Label = label, Match = match, Command = new CommandDefinition { FileName = "tool.exe" } };

    private static readonly MatchRule s_textOnly = new() { Extensions = ["txt"] };

    private static string Describe(IReadOnlyList<MenuNode> nodes) => string.Join(" ", nodes.Select(node => node switch
    {
        CommandNode c => c.Label,
        SubmenuNode s => $"{s.Label}[{Describe(s.Children)}]",
        _ => "|",
    }));

    [TestMethod]
    public void Separators_AreTrimmedAroundHiddenEntries()
    {
        MenuEntry[] entries =
        [
            new SeparatorEntry(),
            Command("Edit"),
            new SeparatorEntry(),
            Command("TextOnly", s_textOnly),
            new SeparatorEntry(),
            Command("Copy"),
            new SeparatorEntry(),
        ];

        Assert.AreEqual("Edit | Copy", Describe(MenuBuilder.Build(entries, [File(@"C:\a.png")])));
        Assert.AreEqual("Edit | TextOnly | Copy", Describe(MenuBuilder.Build(entries, [File(@"C:\a.txt")])));
    }

    [TestMethod]
    public void SubmenuWithoutVisibleChildren_IsDropped()
    {
        var submenu = new SubmenuEntry { Label = "Text", Items = [new SeparatorEntry(), Command("Wrap", s_textOnly)] };

        Assert.AreEqual("", Describe(MenuBuilder.Build([submenu], [File(@"C:\a.png")])));
        Assert.AreEqual("Text[Wrap]", Describe(MenuBuilder.Build([submenu], [File(@"C:\a.txt")])));
    }

    [TestMethod]
    public void SubmenuRule_NarrowsTheItemsItsChildrenSee()
    {
        var submenu = new SubmenuEntry
        {
            Label = "Images",
            Match = new MatchRule { Extensions = ["jpg"], Mode = MatchMode.Any },
            Items = [Command("Resize"), Command("Single", new MatchRule { MaxCount = 1 })],
        };

        IReadOnlyList<MenuNode> menu = MenuBuilder.Build([submenu], [File(@"C:\a.jpg"), File(@"C:\b.txt")]);

        var images = (SubmenuNode)menu.Single();
        var resize = (CommandNode)images.Children[0];
        Assert.AreEqual(@"C:\a.jpg", resize.Items.Single().Path);
        Assert.AreEqual("Images[Resize Single]", Describe(menu));
    }

    [TestMethod]
    public void EntriesWithoutCommandAndNullEntries_AreSkipped()
    {
        MenuEntry?[] entries = [null, new CommandEntry { Label = "Broken" }, Command("Ok")];

        Assert.AreEqual("Ok", Describe(MenuBuilder.Build(entries, [File(@"C:\a.txt")])));
    }

    [TestMethod]
    public void EmptySelection_BuildsNothing()
    {
        Assert.IsEmpty(MenuBuilder.Build([Command("Edit")], Array.Empty<SelectedItem>()));
    }
}
