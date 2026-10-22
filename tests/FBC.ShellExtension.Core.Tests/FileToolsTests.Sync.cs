using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

/// <summary>Synchronize folders: what the comparison finds (ADR-0033).</summary>
public sealed partial class FileToolsTests
{
    [TestMethod]
    public void ToolsOfVersion13_OpenTheEditorsWindow()
    {
        var config = new MenuConfig { Version = 12, Items = [new CommandEntry { Label = "Sync", Tool = BuiltinTool.SyncFolders }] };

        Assert.AreEqual(13, config.RequiredVersion());
        Assert.IsTrue(BuiltinTools.UsesEditorWindow(BuiltinTool.SyncFolders));
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.SyncFolders, ItemKind.Background));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.SyncFolders, ItemKind.File));
        Assert.AreEqual((@"C:\a", @"C:\b"), FileTools.SyncFolders([Folder(@"C:\a"), Folder(@"C:\b")]));
        Assert.AreEqual((@"C:\a", ""), FileTools.SyncFolders([Background(@"C:\a")]));
    }

    [TestMethod]
    public void SyncPlan_FindsWhatIsNewChangedNewerThereAndExtra()
    {
        string source = Directory.CreateDirectory(Path.Combine(_folder, "source")).FullName;
        string destination = Directory.CreateDirectory(Path.Combine(_folder, "destination")).FullName;
        var old = new DateTime(2020, 1, 1, 12, 0, 0);
        var later = old.AddHours(1);
        void Put(string root, string path, string text, DateTime modified)
        {
            string full = Path.Combine(root, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            System.IO.File.WriteAllText(full, text);
            System.IO.File.SetLastWriteTime(full, modified);
        }

        Put(source, "same.txt", "x", old);
        Put(destination, "same.txt", "x", old.AddSeconds(1));
        Put(source, "new.txt", "x", old);
        Put(source, Path.Combine("newdir", "inside.txt"), "x", old);
        Put(source, Path.Combine("sub", "changed.txt"), "new", later);
        Put(destination, Path.Combine("sub", "changed.txt"), "old", old);
        Put(source, "edited-there.txt", "x", old);
        Put(destination, "edited-there.txt", "y", later);
        Put(source, "size.txt", "longer", old);
        Put(destination, "size.txt", "short", old);
        Put(destination, "extra.txt", "x", old);
        Put(destination, Path.Combine("extradir", "inside.txt"), "x", old);
        Put(source, "Case.TXT", "x", old);
        Put(destination, "case.txt", "x", old);

        IReadOnlyList<SyncItem> plan = FileTools.SyncPlan(source, destination, out List<string> errors, CancellationToken.None);

        Assert.AreEqual(0, errors.Count);
        CollectionAssert.AreEqual(
            new[]
            {
                "edited-there.txt NewerThere", "extra.txt Extra", "extradir Extra", "new.txt New", "newdir New",
                "size.txt SizeDiffers", Path.Combine("sub", "changed.txt") + " Changed",
            },
            plan.Select(item => $"{item.Path} {item.Action}").ToArray());
        Assert.IsTrue(plan.Single(item => item.Path == "newdir").IsFolder);
    }

    [TestMethod]
    public void SyncProblem_RefusesAFolderInsideTheOther()
    {
        string source = Directory.CreateDirectory(Path.Combine(_folder, "source")).FullName;
        string inside = Directory.CreateDirectory(Path.Combine(source, "inside")).FullName;
        string other = Directory.CreateDirectory(Path.Combine(_folder, "other")).FullName;

        Assert.IsNotNull(FileTools.SyncProblem(source, ""));
        Assert.IsNotNull(FileTools.SyncProblem(source, Path.Combine(_folder, "gone")));
        Assert.IsNotNull(FileTools.SyncProblem(source, inside));
        Assert.IsNotNull(FileTools.SyncProblem(inside, source));
        Assert.IsNotNull(FileTools.SyncProblem(source, source));
        Assert.IsNull(FileTools.SyncProblem(source, other));

        // Other spellings of the same place: "..", a relative path, a long-path prefix.
        Assert.IsNotNull(FileTools.SyncProblem(source, Path.Combine(other, "..", "source", "inside")));
        Assert.IsNotNull(FileTools.SyncProblem(source, "other"));
        Assert.IsTrue(FileTools.SyncInside(@"\\?\C:\src", @"C:\src\sub"));
        Assert.IsTrue(FileTools.SyncInside(@"\\nas\share", @"\\?\UNC\nas\share\photos"));
        Assert.IsFalse(FileTools.SyncInside(@"C:\src", @"C:\src2"));
    }

    [TestMethod]
    public void SyncPlan_LeavesALinkOnEitherSideAlone()
    {
        string source = Directory.CreateDirectory(Path.Combine(_folder, "source")).FullName;
        string destination = Directory.CreateDirectory(Path.Combine(_folder, "destination")).FullName;
        string elsewhere = Directory.CreateDirectory(Path.Combine(_folder, "elsewhere")).FullName;
        System.IO.File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(source, "foo")).FullName, "a.txt"), "x");
        LinkFolder(Path.Combine(destination, "foo"), elsewhere);
        LinkFolder(Path.Combine(source, "bar"), elsewhere);
        Directory.CreateDirectory(Path.Combine(destination, "bar"));
        LinkFolder(Path.Combine(destination, "only-there"), elsewhere);

        IReadOnlyList<SyncItem> plan = FileTools.SyncPlan(source, destination, out List<string> errors, CancellationToken.None);

        // A junction taken for missing would have the copy write into elsewhere, or a folder deleted.
        Assert.AreEqual(0, plan.Count);
        Assert.AreEqual(3, errors.Count);
    }
}
