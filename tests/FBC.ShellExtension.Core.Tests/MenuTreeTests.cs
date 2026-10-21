using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Editing;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class MenuTreeTests
{
    private static CommandEntry Command(string label) =>
        new() { Label = label, Command = new CommandDefinition { FileName = "tool.exe", Arguments = "\"{path}\"" } };

    private static SubmenuEntry Submenu(string label, params MenuEntry[] items) => new() { Label = label, Items = [.. items] };

    private static string Describe(IEnumerable<MenuEntry> entries) => string.Join(" ", entries.Select(entry => entry switch
    {
        SubmenuEntry s => $"{s.Label}[{Describe(s.Items)}]",
        LabeledEntry l => l.Label,
        _ => "|",
    }));

    // a, Sub[b, c], d
    private static (MenuConfig Config, CommandEntry A, SubmenuEntry Sub, CommandEntry B, CommandEntry C, CommandEntry D) Sample()
    {
        CommandEntry a = Command("a"), b = Command("b"), c = Command("c"), d = Command("d");
        SubmenuEntry sub = Submenu("Sub", b, c);
        return (new MenuConfig { Items = [a, sub, d] }, a, sub, b, c, d);
    }

    [TestMethod]
    public void Locate_FindsNestedEntriesByReference()
    {
        var (config, _, sub, _, c, _) = Sample();

        EntryLocation? location = MenuTree.Locate(config, c);

        Assert.IsNotNull(location);
        Assert.AreEqual(1, location.Index);
        Assert.AreSame(sub, location.Parent);
        Assert.IsNull(MenuTree.Locate(config, Command("c")));
    }

    [TestMethod]
    public void PathOf_And_At_RoundTrip()
    {
        var (config, a, sub, _, c, d) = Sample();

        CollectionAssert.AreEqual(new[] { 1, 1 }, MenuTree.PathOf(config, c));
        CollectionAssert.AreEqual(new[] { 2 }, MenuTree.PathOf(config, d));
        Assert.AreSame(c, MenuTree.At(config, [1, 1]));
        Assert.AreSame(sub, MenuTree.At(config, [1]));
        Assert.AreSame(a, MenuTree.At(config, [0]));
        Assert.IsNull(MenuTree.At(config, [0, 0]), "a command has no children");
        Assert.IsNull(MenuTree.At(config, [5]));
        Assert.IsNull(MenuTree.At(config, []));
    }

    [TestMethod]
    public void InsertAfter_PutsTheEntryAtTheAnchorsLevel()
    {
        var (config, _, _, b, _, _) = Sample();

        MenuTree.InsertAfter(config, b, Command("x"));
        MenuTree.InsertAfter(config, null, Command("y"));

        Assert.AreEqual("a Sub[b x c] d y", Describe(config.Items));
    }

    [TestMethod]
    public void Remove_TakesTheEntryOutWhereverItIs()
    {
        var (config, _, _, b, _, _) = Sample();

        Assert.IsTrue(MenuTree.Remove(config, b));
        Assert.IsFalse(MenuTree.Remove(config, b));
        Assert.AreEqual("a Sub[c] d", Describe(config.Items));
    }

    [TestMethod]
    public void Duplicate_InsertsADeepCopyAfterTheEntry()
    {
        var (config, _, sub, _, _, _) = Sample();

        var copy = (SubmenuEntry?)MenuTree.Duplicate(config, sub);

        Assert.IsNotNull(copy);
        Assert.AreEqual("a Sub[b c] Sub[b c] d", Describe(config.Items));
        Assert.AreNotSame(sub.Items[0], copy.Items[0]);
        ((CommandEntry)copy.Items[0]).Label = "changed";
        Assert.AreEqual("b", ((CommandEntry)sub.Items[0]).Label);
        Assert.AreEqual("\"{path}\"", ((CommandEntry)copy.Items[1]).Command!.Arguments);
    }

    [TestMethod]
    public void MoveUpAndDown_SwapWithinTheList()
    {
        var (config, a, _, b, _, d) = Sample();

        Assert.IsFalse(MenuTree.CanMoveUp(config, a), "a is first in the root list");
        Assert.IsFalse(MenuTree.CanMoveDown(config, d), "d is last in the root list");
        Assert.IsTrue(MenuTree.MoveDown(config, b));
        Assert.IsTrue(MenuTree.MoveUp(config, d));

        Assert.AreEqual("a d Sub[c b]", Describe(config.Items));
    }

    [TestMethod]
    public void MoveUp_FromTheTopOfASubmenu_LeavesItAbove()
    {
        var (config, _, _, b, _, _) = Sample();

        Assert.IsTrue(MenuTree.CanMoveUp(config, b));
        Assert.IsTrue(MenuTree.MoveUp(config, b));

        Assert.AreEqual("a b Sub[c] d", Describe(config.Items));
    }

    [TestMethod]
    public void MoveDown_FromTheBottomOfASubmenu_LeavesItBelow()
    {
        var (config, _, _, _, c, _) = Sample();

        Assert.IsTrue(MenuTree.CanMoveDown(config, c));
        Assert.IsTrue(MenuTree.MoveDown(config, c));

        Assert.AreEqual("a Sub[b] c d", Describe(config.Items));
    }

    [TestMethod]
    public void MoveIntoPrevious_NeedsASubmenuRightAbove()
    {
        var (config, a, _, _, _, d) = Sample();

        Assert.IsFalse(MenuTree.CanMoveIntoPrevious(config, a));
        Assert.IsTrue(MenuTree.CanMoveIntoPrevious(config, d));
        Assert.IsTrue(MenuTree.MoveIntoPrevious(config, d));

        Assert.AreEqual("a Sub[b c d]", Describe(config.Items));
    }

    [TestMethod]
    public void MoveOut_PutsTheEntryRightAfterItsSubmenu()
    {
        var (config, a, _, b, _, _) = Sample();

        Assert.IsFalse(MenuTree.CanMoveOut(config, a));
        Assert.IsTrue(MenuTree.MoveOut(config, b));

        Assert.AreEqual("a Sub[c] b d", Describe(config.Items));
    }

    [TestMethod]
    public void Move_CountsTheIndexAsTheListLooksBeforeTheMove()
    {
        var (config, a, sub, _, _, d) = Sample();

        // Dropping a before d: index 2 in [a, Sub, d].
        Assert.IsTrue(MenuTree.Move(config, a, null, 2));
        Assert.AreEqual("Sub[b c] a d", Describe(config.Items));

        Assert.IsTrue(MenuTree.Move(config, d, sub, 0));
        Assert.AreEqual("Sub[d b c] a", Describe(config.Items));

        Assert.IsTrue(MenuTree.Move(config, a, sub, 99));
        Assert.AreEqual("Sub[d b c a]", Describe(config.Items));
    }

    [TestMethod]
    public void Move_RefusesToPutASubmenuInsideItself()
    {
        CommandEntry leaf = Command("leaf");
        SubmenuEntry inner = Submenu("Inner", leaf);
        SubmenuEntry outer = Submenu("Outer", inner);
        var config = new MenuConfig { Items = [outer] };

        Assert.IsFalse(MenuTree.CanMove(config, outer, outer));
        Assert.IsFalse(MenuTree.CanMove(config, outer, inner));
        Assert.IsFalse(MenuTree.Move(config, outer, inner, 0));
        Assert.IsTrue(MenuTree.CanMove(config, inner, null));
        Assert.IsTrue(MenuTree.CanMove(config, leaf, outer));
        Assert.AreEqual("Outer[Inner[leaf]]", Describe(config.Items));
    }

    [TestMethod]
    public void Move_RefusesATargetOutsideTheTree()
    {
        var (config, a, _, _, _, _) = Sample();

        Assert.IsFalse(MenuTree.Move(config, a, Submenu("Elsewhere"), 0));
        Assert.AreEqual("a Sub[b c] d", Describe(config.Items));
    }

    [TestMethod]
    public void Duplicate_DropsTemplateIds()
    {
        var (config, _, sub, _, _, _) = Sample();
        sub.Id = "sub";
        sub.Items[0].Id = "b";

        var copy = (SubmenuEntry)MenuTree.Duplicate(config, sub)!;

        Assert.IsNull(copy.Id);
        Assert.IsNull(copy.Items[0].Id);
        Assert.AreEqual("sub", sub.Id);
    }
}
