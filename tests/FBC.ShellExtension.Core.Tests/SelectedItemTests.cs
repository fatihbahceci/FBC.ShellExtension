using FBC.ShellExtension.Core.Selection;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class SelectedItemTests
{
    [TestMethod]
    [DataRow(@"C:\work\report.final.pdf", ItemKind.File, @"C:\work", "report.final.pdf", "report.final", ".pdf")]
    [DataRow(@"C:\a.txt", ItemKind.File, @"C:\", "a.txt", "a", ".txt")]
    [DataRow(@"C:\work\Makefile", ItemKind.File, @"C:\work", "Makefile", "Makefile", "")]
    [DataRow(@"C:\work\.gitignore", ItemKind.File, @"C:\work", ".gitignore", "", ".gitignore")]
    [DataRow(@"C:\work\my.lib", ItemKind.Folder, @"C:\work", "my.lib", "my.lib", "")]
    [DataRow(@"C:\work\src\", ItemKind.Folder, @"C:\work", "src", "src", "")]
    [DataRow(@"C:\work", ItemKind.Background, @"C:\work", "work", "work", "")]
    [DataRow(@"D:\", ItemKind.Drive, @"D:\", "D", "D", "")]
    [DataRow(@"\\srv\share\a.txt", ItemKind.File, @"\\srv\share", "a.txt", "a", ".txt")]
    [DataRow(@"\\srv\share", ItemKind.Folder, @"\\srv\share", "share", "share", "")]
    public void PathParts_FollowWindowsRules(string path, ItemKind kind, string dir, string name, string nameNoExt, string ext)
    {
        var item = new SelectedItem(path, kind);

        Assert.AreEqual(dir, item.Directory, "dir");
        Assert.AreEqual(name, item.Name, "name");
        Assert.AreEqual(nameNoExt, item.NameWithoutExtension, "nameNoExt");
        Assert.AreEqual(ext, item.Extension, "ext");
    }

    [TestMethod]
    [DataRow(@"C:\", true, "Drive")]
    [DataRow(@"C:\", false, "Drive")]
    [DataRow(@"C:\Windows", true, "Folder")]
    [DataRow(@"C:\Windows\win.ini", false, "File")]
    [DataRow(@"\\server\share", true, "Folder")]
    public void Classify_TreatsADriveRootAsADriveWithoutAskingTheDisk(string path, bool isDirectory, string expected)
    {
        bool asked = false;
        ItemKind kind = SelectedItem.Classify(path, _ => { asked = true; return isDirectory; });

        Assert.AreEqual(expected, kind.ToString());
        Assert.AreEqual(expected != "Drive", asked);
    }
}
