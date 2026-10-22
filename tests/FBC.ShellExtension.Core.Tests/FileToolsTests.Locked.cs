using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

/// <summary>Unlock and delete: what is asked about, and the order of deletion at restart (ADR-0031).</summary>
public sealed partial class FileToolsTests
{
    [TestMethod]
    public void ToolsOfVersion11_OpenTheEditorsWindow()
    {
        var config = new MenuConfig { Version = 10, Items = [new CommandEntry { Label = "Unlock", Tool = BuiltinTool.DeleteLocked }] };

        Assert.AreEqual(11, config.RequiredVersion());
        Assert.IsTrue(BuiltinTools.UsesEditorWindow(BuiltinTool.DeleteLocked));
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.DeleteLocked, ItemKind.Folder));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.DeleteLocked, ItemKind.Drive));
    }

    [TestMethod]
    public void LockCheckFiles_TakeTheFilesInsideButNotThroughALink()
    {
        string folder = Directory.CreateDirectory(Path.Combine(_folder, "folder")).FullName;
        string inside = Path.Combine(Directory.CreateDirectory(Path.Combine(folder, "sub")).FullName, "in.txt");
        string elsewhere = Directory.CreateDirectory(Path.Combine(_folder, "elsewhere")).FullName;
        string picked = Path.Combine(_folder, "picked.txt");
        foreach (string file in (string[])[inside, picked, Path.Combine(elsewhere, "far.txt")])
        {
            System.IO.File.WriteAllText(file, "x");
        }

        LinkFolder(Path.Combine(folder, "link"), elsewhere);

        IReadOnlyList<string> files = FileTools.LockCheckFiles([File(picked), Folder(folder)], out bool cut, out List<string> errors, CancellationToken.None);

        CollectionAssert.AreEquivalent(new[] { picked, inside }, files.ToArray());
        Assert.IsFalse(cut);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void RebootDeletionOrder_PutsWhatIsInAFolderBeforeIt()
    {
        string folder = Directory.CreateDirectory(Path.Combine(_folder, "folder")).FullName;
        string sub = Directory.CreateDirectory(Path.Combine(folder, "sub")).FullName;
        string deep = Path.Combine(sub, "deep.txt");
        string top = Path.Combine(folder, "top.txt");
        string elsewhere = Directory.CreateDirectory(Path.Combine(_folder, "elsewhere")).FullName;
        foreach (string file in (string[])[deep, top, Path.Combine(elsewhere, "far.txt")])
        {
            System.IO.File.WriteAllText(file, "x");
        }

        string link = Path.Combine(folder, "link");
        LinkFolder(link, elsewhere);

        IReadOnlyList<RebootEntry> entries = FileTools.RebootDeletionOrder([folder, top, Path.Combine(_folder, "gone")], out List<string> errors);
        List<string> order = [.. entries.Select(entry => entry.Path)];

        // Every path once; each folder after what is in it; the link as itself, flagged, not what it leads to.
        CollectionAssert.AreEquivalent(new[] { deep, top, sub, link, folder }, order);
        Assert.IsTrue(order.IndexOf(deep) < order.IndexOf(sub));
        Assert.AreEqual(folder, order[^1]);
        CollectionAssert.AreEqual(new[] { link }, entries.Where(entry => entry.IsLink).Select(entry => entry.Path).ToArray());
        Assert.IsTrue(entries.Single(entry => entry.Path == link).IsFolder);
        Assert.AreEqual(1, errors.Count);
    }
}
