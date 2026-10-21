using FBC.ShellExtension.Core.Restart;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class RestartTests
{
    private static readonly SavedPlacement s_placement = new(1, 0, 0, 0, 0, 0, 10, 10, 800, 600);

    private static byte[] Id(byte value) => [4, 0, value, 0, 0, 0];

    private static SavedWindow Window(params byte[] folders) =>
        new([.. folders.Select(f => new SavedTab(Id(f), $"folder {f}"))], 0, s_placement);

    private static IReadOnlyList<byte[]> Open(params byte[] folders) => [.. folders.Select(Id)];

    [TestMethod]
    public void Missing_LeavesOutWindowsThatAreOpenWithTheSameFolders()
    {
        var snapshot = new ExplorerSnapshot([Window(1, 2), Window(3), Window(4)]);

        IReadOnlyList<SavedWindow> missing = RestorePlanner.Missing(snapshot, [Open(2, 1), Open(4)]);

        CollectionAssert.AreEqual(new[] { "folder 3" }, missing.Select(w => w.Tabs[0].DisplayName).ToArray());
    }

    [TestMethod]
    public void Missing_UsesEachOpenWindowForOneSavedWindowOnly()
    {
        var snapshot = new ExplorerSnapshot([Window(1), Window(1)]);

        Assert.HasCount(1, RestorePlanner.Missing(snapshot, [Open(1)]));
    }

    [TestMethod]
    public void Missing_CountsAWindowWithFewerTabsAsNotOpen()
    {
        var snapshot = new ExplorerSnapshot([Window(1, 2)]);

        Assert.HasCount(1, RestorePlanner.Missing(snapshot, [Open(1)]));
    }

    [TestMethod]
    public void Snapshot_CountsTabs()
    {
        Assert.AreEqual(3, new ExplorerSnapshot([Window(1, 2), Window(3)]).TabCount);
        Assert.AreEqual(0, ExplorerSnapshot.Empty.TabCount);
    }

    [TestMethod]
    public void Parse_ReadsFilesRestoreAndTheCommand()
    {
        RestartOptions options = RestartArguments.Parse(
            ["--file", @"C:\a.dll", "--no-restore", "--file", @"C:\b.dll", "--", "powershell", "-File", "--file", "x.ps1"]);

        CollectionAssert.AreEqual(new[] { @"C:\a.dll", @"C:\b.dll" }, options.Files.ToArray());
        Assert.IsFalse(options.Restore);
        CollectionAssert.AreEqual(new[] { "powershell", "-File", "--file", "x.ps1" }, options.Command.ToArray());
    }

    [TestMethod]
    public void Parse_DefaultsToRestoringWithoutACommand()
    {
        RestartOptions options = RestartArguments.Parse([]);

        Assert.IsTrue(options.Restore);
        Assert.IsEmpty(options.Command);
        Assert.IsFalse(options.ShowHelp);
        Assert.IsTrue(RestartArguments.Parse(["--help"]).ShowHelp);
    }

    [TestMethod]
    [DataRow("--file")]
    [DataRow("--")]
    [DataRow("--restore")]
    public void Parse_RejectsWhatItCannotUse(string argument)
    {
        Assert.ThrowsExactly<ArgumentException>(() => RestartArguments.Parse([argument]));
    }

    [TestMethod]
    public void SavedTab_ComparesTheIdListBytes()
    {
        var tab = new SavedTab(Id(7), "x");

        Assert.IsTrue(tab.IsSameFolder(Id(7)));
        Assert.IsFalse(tab.IsSameFolder(Id(8)));
    }
}
