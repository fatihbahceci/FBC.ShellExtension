using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Editing;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class EditHistoryTests
{
    private static MenuConfig Config(params string[] labels) =>
        new() { Items = [.. labels.Select(label => (MenuEntry)new SubmenuEntry { Label = label })] };

    private static string Labels(MenuConfig? config) =>
        config is null ? "-" : string.Join(",", config.Items.Cast<LabeledEntry>().Select(e => e.Label));

    [TestMethod]
    public void UndoAndRedo_WalkThroughTheSnapshots()
    {
        var history = new EditHistory();
        MenuConfig current = Config("a");
        history.Record(current);
        current = Config("a", "b");
        history.Record(current);
        current = Config("a", "b", "c");

        current = history.Undo(current)!;
        Assert.AreEqual("a,b", Labels(current));
        current = history.Undo(current)!;
        Assert.AreEqual("a", Labels(current));
        Assert.IsFalse(history.CanUndo);
        Assert.IsNull(history.Undo(current));

        current = history.Redo(current)!;
        Assert.AreEqual("a,b", Labels(current));
        current = history.Redo(current)!;
        Assert.AreEqual("a,b,c", Labels(current));
        Assert.IsFalse(history.CanRedo);
    }

    [TestMethod]
    public void SameKey_CoalescesIntoOneStep()
    {
        var history = new EditHistory();
        history.Record(Config("a"), "label");
        history.Record(Config("ab"), "label");
        history.Record(Config("abc"), "label");

        MenuConfig? undone = history.Undo(Config("abcd"));

        Assert.AreEqual("a", Labels(undone));
        Assert.IsFalse(history.CanUndo);
    }

    [TestMethod]
    public void Seal_EndsTheCoalescingRun()
    {
        var history = new EditHistory();
        history.Record(Config("a"), "label");
        history.Seal();
        history.Record(Config("ab"), "label");

        Assert.AreEqual("ab", Labels(history.Undo(Config("abc"))));
        Assert.IsTrue(history.CanUndo);
    }

    [TestMethod]
    public void NewChange_ClearsRedo()
    {
        var history = new EditHistory();
        history.Record(Config("a"));
        MenuConfig current = history.Undo(Config("a", "b"))!;
        Assert.IsTrue(history.CanRedo);

        history.Record(current);

        Assert.IsFalse(history.CanRedo);
    }

    [TestMethod]
    public void Capacity_DropsTheOldestSteps()
    {
        var history = new EditHistory(capacity: 2);
        history.Record(Config("1"));
        history.Record(Config("2"));
        history.Record(Config("3"));

        MenuConfig current = Config("4");
        current = history.Undo(current)!;
        current = history.Undo(current)!;

        Assert.AreEqual("2", Labels(current));
        Assert.IsFalse(history.CanUndo);
    }

    [TestMethod]
    public void RecordedSnapshot_IsWhatUndoReturns()
    {
        var history = new EditHistory();
        history.Record(ConfigSerializer.Serialize(Config("a")));

        Assert.AreEqual("a", Labels(history.Undo(Config("a", "b"))));
    }
}
