using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

/// <summary>The tools whose window is the editor's (ADR-0028): what they decide and write.</summary>
public sealed partial class FileToolsTests
{
    [TestMethod]
    public void ToolsOfVersion9_OpenTheEditorsWindow()
    {
        var config = new MenuConfig { Version = 8, Items = [new CommandEntry { Label = "Attributes", Tool = BuiltinTool.Attributes }] };

        Assert.AreEqual(9, config.RequiredVersion());
        Assert.IsTrue(BuiltinTools.UsesEditorWindow(BuiltinTool.AdvancedDelete));
        Assert.IsFalse(BuiltinTools.UsesWindow(BuiltinTool.ChangeTime));
        Assert.IsFalse(BuiltinTools.UsesEditorWindow(BuiltinTool.CopyToFolder));
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.Attributes, ItemKind.Folder));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.ChangeTime, ItemKind.Background));
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.OrganizeIntoFolders, ItemKind.Background));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.OrganizeIntoFolders, ItemKind.Drive));
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.AdvancedDelete, ItemKind.Drive));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.AdvancedDelete, ItemKind.File));
    }

    [TestMethod]
    public void AttributeState_IsAllNoneOrSome_AndReadOnlyCountsFilesOnly()
    {
        FileAttributes[] items = [FileAttributes.ReadOnly | FileAttributes.Archive, FileAttributes.Archive, FileAttributes.Directory | FileAttributes.Hidden];

        Assert.AreEqual(true, FileTools.AttributeState(items[..2], FileAttributes.Archive));
        Assert.IsNull(FileTools.AttributeState(items, FileAttributes.Archive));
        Assert.AreEqual(false, FileTools.AttributeState(items[..2], FileAttributes.Hidden));
        Assert.IsNull(FileTools.AttributeState(items[..2], FileAttributes.ReadOnly));

        // The folder's read-only mark is its desktop.ini's, not counted; with no file, nothing is known.
        Assert.AreEqual(true, FileTools.AttributeState([items[0], FileAttributes.Directory], FileAttributes.ReadOnly));
        Assert.IsNull(FileTools.AttributeState([FileAttributes.Directory | FileAttributes.ReadOnly], FileAttributes.ReadOnly));
    }

    [TestMethod]
    public void ChangedAttributes_LeavesAFoldersCustomizationAlone()
    {
        var change = new AttributeChange(FileAttributes.Hidden | FileAttributes.System, FileAttributes.ReadOnly | FileAttributes.Archive);

        Assert.AreEqual(FileAttributes.Hidden | FileAttributes.System,
            FileTools.ChangedAttributes(FileAttributes.ReadOnly | FileAttributes.Archive, change));
        Assert.AreEqual(FileAttributes.Directory | FileAttributes.ReadOnly | FileAttributes.Hidden,
            FileTools.ChangedAttributes(FileAttributes.Directory | FileAttributes.ReadOnly | FileAttributes.Archive, change));
    }

    [TestMethod]
    public void ChangeAttributes_ReachesInsideButNotThroughALink()
    {
        string folder = Directory.CreateDirectory(Path.Combine(_folder, "folder")).FullName;
        string inside = Path.Combine(Directory.CreateDirectory(Path.Combine(folder, "sub")).FullName, "inside.txt");
        string elsewhere = Directory.CreateDirectory(Path.Combine(_folder, "elsewhere")).FullName;
        string beyond = Path.Combine(elsewhere, "beyond.txt");
        string top = Path.Combine(_folder, "top.txt");
        foreach (string file in (string[])[inside, beyond, top])
        {
            System.IO.File.WriteAllText(file, "x");
        }

        LinkFolder(Path.Combine(folder, "link"), elsewhere);
        var readOnly = new AttributeChange(FileAttributes.ReadOnly, 0);
        try
        {
            Assert.AreEqual(0, FileTools.ChangeAttributes([File(top), Folder(folder)], readOnly, inside: false, CancellationToken.None).Count);
            Assert.IsTrue(new FileInfo(top).IsReadOnly);
            Assert.IsFalse(new FileInfo(inside).IsReadOnly);

            Assert.AreEqual(0, FileTools.ChangeAttributes([Folder(folder)], readOnly, inside: true, CancellationToken.None).Count);
            Assert.IsTrue(new FileInfo(inside).IsReadOnly);
            Assert.IsFalse(new FileInfo(beyond).IsReadOnly);

            Assert.AreEqual(1, FileTools.ChangeAttributes([File(Path.Combine(_folder, "gone.txt"))], readOnly, inside: false, CancellationToken.None).Count);
        }
        finally
        {
            // The cleanup cannot delete a read-only file on Windows.
            FileTools.ChangeAttributes([File(top), Folder(folder)], new AttributeChange(0, FileAttributes.ReadOnly), inside: true, CancellationToken.None);
        }
    }

    [TestMethod]
    public void ChangeTimes_WritesOnlyWhatWasAskedFor()
    {
        string folder = Directory.CreateDirectory(Path.Combine(_folder, "folder")).FullName;
        string file = Path.Combine(folder, "a.txt");
        System.IO.File.WriteAllText(file, "x");
        DateTime accessed = System.IO.File.GetLastAccessTime(file);
        var modified = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Local);

        IReadOnlyList<string> errors = FileTools.ChangeTimes([Folder(folder)], new TimeChange(null, modified, null), inside: true, CancellationToken.None);

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(modified, System.IO.File.GetLastWriteTime(file));
        Assert.AreEqual(modified, Directory.GetLastWriteTime(folder));
        Assert.IsTrue((System.IO.File.GetLastAccessTime(file) - accessed).Duration() < TimeSpan.FromSeconds(1), "accessed");

        Assert.AreEqual(1, FileTools.ChangeTimes([File(Path.Combine(_folder, "gone.txt"))], new TimeChange(null, modified, null), inside: false, CancellationToken.None).Count);
    }

    [TestMethod]
    public void OrganizeSources_TakeTheFolderFilesButNotTheHiddenOnes()
    {
        string folder = Directory.CreateDirectory(Path.Combine(_folder, "folder")).FullName;
        string b = Path.Combine(folder, "b.jpg");
        string a = Path.Combine(folder, "a.jpg");
        string hidden = Path.Combine(folder, ".hidden");
        Directory.CreateDirectory(Path.Combine(folder, "sub"));
        foreach (string file in (string[])[b, a, hidden])
        {
            System.IO.File.WriteAllText(file, "x");
        }

        // Linux calls a dot name hidden by itself; Windows needs the attribute.
        System.IO.File.SetAttributes(hidden, FileAttributes.Hidden);

        IReadOnlyList<string> files = FileTools.OrganizeSources([File(b), Folder(folder), Folder(Path.Combine(_folder, "gone"))], out List<string> errors);

        CollectionAssert.AreEqual(new[] { b, a }, files.ToArray());
        Assert.AreEqual(1, errors.Count);
    }

    [TestMethod]
    public void OrganizePlan_NamesTheFolderAfterTheDateOrTheExtension()
    {
        var date = new DateTime(2026, 3, 7);

        Assert.AreEqual("2026", FileTools.DateFolder(date, DateFolders.Year));
        Assert.AreEqual("2026-03", FileTools.DateFolder(date, DateFolders.YearMonth));
        Assert.AreEqual("2026-03-07", FileTools.DateFolder(date, DateFolders.YearMonthDay));
        Assert.AreEqual(Path.Combine("2026", "03"), FileTools.DateFolder(date, DateFolders.YearThenMonth));

        Assert.AreEqual("PDF", FileTools.ExtensionFolder("report.pdf"));
        Assert.AreEqual("GZ", FileTools.ExtensionFolder("logs.tar.gz"));
        Assert.AreEqual("No extension", FileTools.ExtensionFolder("README"));
        Assert.AreEqual("No extension", FileTools.ExtensionFolder(".gitignore"));
        Assert.AreEqual("NUL_", FileTools.ExtensionFolder("a.nul"));

        string photo = Path.Combine(_folder, "photo.jpg");
        IReadOnlyList<OrganizeMove> plan = FileTools.OrganizePlan([photo], OrganizeBy.Date, DateFolders.YearMonth, _ => date);
        Assert.AreEqual("2026-03", plan[0].Folder);
        Assert.AreEqual(Path.Combine(_folder, "2026-03"), plan[0].Destination);
        Assert.AreEqual("JPG", FileTools.OrganizePlan([photo], OrganizeBy.Extension, DateFolders.Year, _ => throw new AssertFailedException("no date by extension"))[0].Folder);
    }

    [TestMethod]
    [DataRow("*.tmp; ~* ,Thumbs.db", 3)]
    [DataRow("*.", 1)]
    [DataRow("README.", 0)]
    [DataRow("*.tmp.", 0)]
    [DataRow(" ; ", 0)]
    [DataRow(@"sub\*.tmp", 0)]
    [DataRow("a|b", 0)]
    [DataRow("*.tmp;*.bak", 2)]
    public void ParsePatterns_TakesNamesWithWildcardsOnly(string text, int count)
    {
        IReadOnlyList<string>? patterns = FileTools.ParsePatterns(text, out string? problem);

        Assert.AreEqual(count, patterns?.Count ?? 0);
        Assert.AreEqual(count == 0, problem is not null);
    }

    [TestMethod]
    [DataRow("report.TMP", "*.tmp", true)]
    [DataRow("~$draft.docx", "~*", true)]
    [DataRow("README", "*.*", true)]
    [DataRow("README", "*.", true)]
    [DataRow("a.txt", "*.", false)]
    [DataRow("file1.txt", "file?.txt", true)]
    [DataRow("file10.txt", "file?.txt", false)]
    public void MatchesAny_MatchesAsDirDoes(string name, string pattern, bool expected) =>
        Assert.AreEqual(expected, FileTools.MatchesAny(name, [pattern]));

    [TestMethod]
    public void FindMatches_FindsHiddenOnesButNeitherEntersNorFindsALink()
    {
        string root = Directory.CreateDirectory(Path.Combine(_folder, "root")).FullName;
        string deep = Directory.CreateDirectory(Path.Combine(root, "a", "b")).FullName;
        string cache = Directory.CreateDirectory(Path.Combine(root, "cache.tmp")).FullName;
        string elsewhere = Directory.CreateDirectory(Path.Combine(_folder, "elsewhere")).FullName;
        string top = Path.Combine(root, "one.tmp");
        string nested = Path.Combine(deep, "two.TMP");
        string hidden = Path.Combine(root, ".three.tmp");
        foreach (string file in (string[])[top, nested, hidden, Path.Combine(cache, "inside.tmp"), Path.Combine(elsewhere, "beyond.tmp"), Path.Combine(root, "keep.txt")])
        {
            System.IO.File.WriteAllText(file, "x");
        }

        System.IO.File.SetAttributes(hidden, FileAttributes.Hidden | FileAttributes.System);
        LinkFolder(Path.Combine(root, "link.tmp"), elsewhere);

        FindResult all = FileTools.FindMatches([root, deep], ["*.tmp"], files: true, folders: true, subfolders: true, CancellationToken.None);
        CollectionAssert.AreEquivalent(new[] { top, nested, hidden, cache }, all.Items.Select(item => item.Path).ToArray());
        Assert.IsTrue(all.Items.Single(item => item.Path == cache).IsFolder);
        Assert.AreEqual(1, all.Items.Single(item => item.Path == top).Length);

        FindResult filesHere = FileTools.FindMatches([root], ["*.tmp"], files: true, folders: false, subfolders: false, CancellationToken.None);
        CollectionAssert.AreEquivalent(new[] { top, hidden }, filesHere.Items.Select(item => item.Path).ToArray());

        // A folder inside another selected one is searched too, also when subfolders are not.
        FindResult nestedToo = FileTools.FindMatches([root, deep + Path.DirectorySeparatorChar, deep], ["*.tmp"], files: true, folders: false, subfolders: false, CancellationToken.None);
        CollectionAssert.AreEquivalent(new[] { top, hidden, nested }, nestedToo.Items.Select(item => item.Path).ToArray());

        // What lies in a found folder goes with it, not on its own.
        FindResult inFound = FileTools.FindMatches([root, cache], ["*.tmp"], files: true, folders: true, subfolders: false, CancellationToken.None);
        CollectionAssert.AreEquivalent(new[] { top, hidden, cache }, inFound.Items.Select(item => item.Path).ToArray());

        FindResult missing = FileTools.FindMatches([Path.Combine(_folder, "gone")], ["*"], files: true, folders: true, subfolders: true, CancellationToken.None);
        Assert.AreEqual(1, missing.Unreadable.Count);
    }
}
