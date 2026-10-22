using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

/// <summary>Advanced rename's plan and Find and replace's reading and writing (ADR-0029).</summary>
public sealed partial class FileToolsTests
{
    private static readonly DateTime s_modified = new(2026, 3, 7, 10, 20, 30);

    private static RenamePreview Preview(RenameRules rules, Func<string, bool>? exists, params SelectedItem[] items) =>
        FileTools.PreviewRename(FileTools.RenameOrder(items), rules, exists ?? (_ => false), _ => s_modified);

    private static string[] NewNames(RenamePreview preview) => [.. preview.Rows.Select(row => row.NewName)];

    [TestMethod]
    public void ToolsOfVersion10_OpenTheEditorsWindow()
    {
        var config = new MenuConfig { Version = 9, Items = [new CommandEntry { Label = "Rename", Tool = BuiltinTool.AdvancedRename }] };

        Assert.AreEqual(10, config.RequiredVersion());
        Assert.IsTrue(BuiltinTools.UsesEditorWindow(BuiltinTool.FindReplace));
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.AdvancedRename, ItemKind.Folder));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.AdvancedRename, ItemKind.Background));
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.FindReplace, ItemKind.Background));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.FindReplace, ItemKind.Drive));
    }

    [TestMethod]
    public void RenameOrder_NumbersAsExplorerSorts()
    {
        RenamePreview preview = Preview(RenameRules.Unchanged with { NameTemplate = "photo-{n}", Digits = 3 }, null,
            File(@"C:\p\file10.jpg"), File(@"C:\p\file2.jpg"), File(@"C:\p\file1.jpg"));

        CollectionAssert.AreEqual(new[] { "file1.jpg", "file2.jpg", "file10.jpg" }, preview.Rows.Select(row => row.Item.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "photo-001.jpg", "photo-002.jpg", "photo-003.jpg" }, NewNames(preview));
        Assert.IsTrue(preview.CanRename);
    }

    [TestMethod]
    public void PreviewRename_ReplacesInTheNameAndFillsTheTemplates()
    {
        // Plain text keeps its $; the extension is left to its own template; a folder has none.
        Assert.AreEqual("Cost $1.TXT", NewNames(Preview(RenameRules.Unchanged with { Find = "price", Replace = "Cost $1" }, null, File(@"C:\p\PRICE.TXT")))[0]);
        Assert.AreEqual("2024-trip.jpg", NewNames(Preview(RenameRules.Unchanged with { Find = @"(\w+)_(\d+)", Replace = "$2-$1", RegularExpression = true }, null, File(@"C:\p\trip_2024.jpg")))[0]);
        Assert.AreEqual("a.jpeg", NewNames(Preview(RenameRules.Unchanged with { ExtensionTemplate = "jpeg" }, null, File(@"C:\p\a.JPG")))[0]);
        Assert.AreEqual("a", NewNames(Preview(RenameRules.Unchanged with { ExtensionTemplate = "" }, null, File(@"C:\p\a.JPG")))[0]);
        Assert.AreEqual("v1.2 old", NewNames(Preview(RenameRules.Unchanged with { NameTemplate = "{name} old", ExtensionTemplate = "x" }, null, Folder(@"C:\p\v1.2")))[0]);
        Assert.AreEqual("Photos 20260307 a.png", NewNames(Preview(RenameRules.Unchanged with { NameTemplate = "{parent} {date:yyyyMMdd} {name}" }, null, File(@"C:\Photos\a.png")))[0]);
        Assert.AreEqual(".gitignore old", NewNames(Preview(RenameRules.Unchanged with { NameTemplate = "{name} old" }, null, File(@"C:\p\.gitignore")))[0]);
        Assert.AreEqual("My Holiday Photo.jpg", NewNames(Preview(RenameRules.Unchanged with { Case = NameCase.Title }, null, File(@"C:\p\my HOLIDAY photo.JPG")))[0]);
    }

    [TestMethod]
    [DataRow("{x}", "{ext}", false)]
    [DataRow("{name:abc}", "{ext}", false)]
    [DataRow("{date:%}", "{ext}", false)]
    [DataRow("{name}", "{n:3}", false)]
    [DataRow("{NAME}-{date}", "{Ext}", true)]
    public void PreviewRename_RefusesATemplateItDoesNotKnow(string name, string extension, bool fine)
    {
        RenamePreview preview = Preview(RenameRules.Unchanged with { NameTemplate = name, ExtensionTemplate = extension }, null, File(@"C:\p\a.txt"));

        Assert.AreEqual(fine, preview.Problem is null, preview.Problem);
    }

    [TestMethod]
    public void PreviewRename_SaysWhatStandsInTheWay()
    {
        Assert.IsNotNull(Preview(RenameRules.Unchanged with { Find = "(", RegularExpression = true }, null, File(@"C:\p\a.txt")).Problem);

        RenamePreview invalid = Preview(RenameRules.Unchanged with { Find = "a", Replace = "a/b" }, null, File(@"C:\p\a.txt"));
        Assert.IsNotNull(invalid.Rows[0].Problem);
        Assert.IsFalse(invalid.CanRename);

        RenamePreview same = Preview(RenameRules.Unchanged with { NameTemplate = "same" }, null, File(@"C:\p\a.txt"), File(@"C:\p\b.txt"));
        Assert.IsTrue(same.Rows.All(row => row.Problem is not null));

        // Taken by an item outside the selection; the other folder's c.txt is no matter.
        RenamePreview taken = Preview(RenameRules.Unchanged with { NameTemplate = "c" }, path => path == @"C:\p\c.txt", File(@"C:\p\a.txt"), File(@"C:\q\b.txt"));
        Assert.IsNotNull(taken.Rows.Single(row => row.Item.Name == "a.txt").Problem);
        Assert.IsNull(taken.Rows.Single(row => row.Item.Name == "b.txt").Problem);

        // Nothing to do is no reason to rename.
        Assert.IsFalse(Preview(RenameRules.Unchanged, null, File(@"C:\p\a.txt")).CanRename);
    }

    [TestMethod]
    public void RenameSequence_FreesANameBeforeItIsTaken()
    {
        // 1 → 2 and 2 → 3: 2 has to go first.
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\p\1.txt", @"C:\p\2.txt" };
        RenamePreview chain = Preview(RenameRules.Unchanged with { NameTemplate = "{n}", Start = 2 }, existing.Contains, File(@"C:\p\1.txt"), File(@"C:\p\2.txt"));

        Assert.IsTrue(chain.CanRename);
        CollectionAssert.AreEqual(new[] { "2.txt", "1.txt" }, FileTools.RenameSequence(chain).Select(row => row.Item.Name).ToArray());

        // 1 → 2 and 2 → 1 go round: neither can go first.
        RenamePreview swap = Preview(RenameRules.Unchanged with { NameTemplate = "{n}", Start = 2, Step = -1 }, existing.Contains, File(@"C:\p\1.txt"), File(@"C:\p\2.txt"));
        Assert.IsTrue(swap.Rows.All(row => row.Problem is not null));

        // A change of case alone is the item's own name, not a taken one.
        RenamePreview upper = Preview(RenameRules.Unchanged with { Case = NameCase.Upper }, existing.Contains, File(@"C:\p\1.txt"));
        Assert.IsTrue(upper.CanRename);
        Assert.AreEqual("1.TXT", FileTools.RenameSequence(upper).Single().NewName);
    }

    [TestMethod]
    public void PreviewRename_ChangesCaseWithoutTheUsersLanguage()
    {
        CultureInfo user = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        try
        {
            // Turkish would give "fıle.gıf" and "PHOTO.GİF": no longer GIF files to Windows.
            Assert.AreEqual("file.gif", NewNames(Preview(RenameRules.Unchanged with { Case = NameCase.Lower }, null, File(@"C:\p\FILE.GIF")))[0]);
            Assert.AreEqual("PHOTO.GIF", NewNames(Preview(RenameRules.Unchanged with { Case = NameCase.Upper }, null, File(@"C:\p\photo.gif")))[0]);
            Assert.AreEqual("Movie.avi", NewNames(Preview(RenameRules.Unchanged with { Case = NameCase.Title }, null, File(@"C:\p\MOVIE.AVI")))[0]);
        }
        finally
        {
            CultureInfo.CurrentCulture = user;
        }
    }

    [TestMethod]
    public void RenameSequence_RenamesWhatIsInsideAFolderBeforeTheFolder()
    {
        RenamePreview preview = Preview(RenameRules.Unchanged with { NameTemplate = "{name}-x" }, null, Folder(@"C:\p\sub"), File(@"C:\p\sub\f.txt"));

        CollectionAssert.AreEqual(new[] { "f.txt", "sub" }, FileTools.RenameSequence(preview).Select(row => row.Item.Name).ToArray());
    }

    [TestMethod]
    public void PreviewRename_RefusesTooLongAName_AndTakesTwoSpellingsOfOnePath()
    {
        string name = new('a', 100);
        RenamePreview tooLong = Preview(RenameRules.Unchanged with { NameTemplate = "{name}{name}{name}" }, null, File($@"C:\p\{name}.txt"));
        Assert.IsNotNull(tooLong.Rows[0].Problem);

        // Only a case-sensitive folder holds both: a preview all the same, not a crash.
        RenamePreview twice = Preview(RenameRules.Unchanged with { NameTemplate = "{n}" }, null, File(@"C:\p\a.txt"), File(@"C:\p\A.txt"));
        Assert.AreEqual(2, twice.Rows.Count);
    }

    [TestMethod]
    public void NaturalComparer_ComparesNumbersByValue()
    {
        string[] names = ["b", "a10", "a2", "A1", "a01", "a"];
        Array.Sort(names, FileTools.NaturalComparer.Instance);

        // 01 and 1 are one value: the plain order of the text keeps them apart.
        CollectionAssert.AreEqual(new[] { "a", "A1", "a01", "a2", "a10", "b" }, names);
    }

    private static Regex Pattern(ReplaceRules rules)
    {
        Regex? pattern = FileTools.ReplacePattern(rules, out string? problem);
        Assert.IsNull(problem);
        return pattern!;
    }

    [TestMethod]
    public void ReplaceInFile_KeepsTheBomTheLineEndsAndABackup()
    {
        string path = Path.Combine(_folder, "a.txt");
        byte[] before = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("hello wörld\r\nhello\r\n")];
        System.IO.File.WriteAllBytes(path, before);
        var rules = new ReplaceRules("HELLO", "bye", MatchCase: false, WholeWord: false, RegularExpression: false);

        Assert.AreEqual(2, FileTools.CountMatches(path, Pattern(rules), Encoding.Latin1).Matches);
        Assert.AreEqual(2, FileTools.ReplaceInFile(path, Pattern(rules), FileTools.ReplaceText(rules), Encoding.Latin1, backup: true));

        CollectionAssert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("bye wörld\r\nbye\r\n")).ToArray(), System.IO.File.ReadAllBytes(path));
        CollectionAssert.AreEqual(before, System.IO.File.ReadAllBytes(Path.Combine(_folder, "a.txt.bak")));

        // A second backup does not take the first one's place.
        FileTools.ReplaceInFile(path, Pattern(rules with { Find = "bye" }), "hi", Encoding.Latin1, backup: true);
        Assert.IsTrue(System.IO.File.Exists(Path.Combine(_folder, "a.txt (2).bak")));
        Assert.AreEqual(0, Directory.GetFiles(_folder, "*.fbctools-tmp").Length);
    }

    [TestMethod]
    public void ReplaceInFile_WritesTheCodePageBackOrNothing()
    {
        string path = Path.Combine(_folder, "latin.txt");
        byte[] cafe = [(byte)'c', (byte)'a', (byte)'f', 0xE9, (byte)' ', (byte)'c', (byte)'a', (byte)'f', 0xE9];
        System.IO.File.WriteAllBytes(path, cafe);
        var rules = new ReplaceRules("café", "bar", MatchCase: true, WholeWord: true, RegularExpression: false);

        // Not UTF-8: the code page, here Latin-1, and the file stays in it.
        Assert.AreEqual(2, FileTools.ReplaceInFile(path, Pattern(rules with { Replace = "thé" }), "thé", Encoding.Latin1, backup: false));
        CollectionAssert.AreEqual(new byte[] { (byte)'t', (byte)'h', 0xE9, (byte)' ', (byte)'t', (byte)'h', 0xE9 }, System.IO.File.ReadAllBytes(path));

        // A letter the code page has not got: nothing is written.
        Assert.ThrowsExactly<ToolFailedException>(() => FileTools.ReplaceInFile(path, Pattern(rules with { Find = "thé" }), "ğ", Encoding.Latin1, backup: false));
        Assert.AreEqual(7, System.IO.File.ReadAllBytes(path).Length);
        Assert.IsFalse(System.IO.File.Exists(path + ".bak"));
    }

    [TestMethod]
    public void ReplaceInFile_LeavesWhatIsNotText()
    {
        string binary = Path.Combine(_folder, "a.bin");
        System.IO.File.WriteAllBytes(binary, [(byte)'a', 0, (byte)'a']);
        string utf16 = Path.Combine(_folder, "wide.txt");
        System.IO.File.WriteAllBytes(utf16, [0xFF, 0xFE, .. Encoding.Unicode.GetBytes("a-a")]);
        Regex a = Pattern(new ReplaceRules("a", "b", false, false, false));

        Assert.IsNotNull(FileTools.CountMatches(binary, a, Encoding.Latin1).Note);
        Assert.ThrowsExactly<ToolFailedException>(() => FileTools.ReplaceInFile(binary, a, "b", Encoding.Latin1, backup: false));

        Assert.AreEqual(2, FileTools.ReplaceInFile(utf16, a, "b", Encoding.Latin1, backup: false));
        CollectionAssert.AreEqual(new byte[] { 0xFF, 0xFE }.Concat(Encoding.Unicode.GetBytes("b-b")).ToArray(), System.IO.File.ReadAllBytes(utf16));
    }

    [TestMethod]
    public void ReplacePattern_WholeWordsGroupsAndProblems()
    {
        Assert.AreEqual("dog concat dog", Pattern(new ReplaceRules("cat", "dog", false, true, false)).Replace("cat concat CAT", "dog"));
        Assert.AreEqual("use Java, not a.NETb", Pattern(new ReplaceRules(".NET", "Java", true, true, false)).Replace("use .NET, not a.NETb", "Java"));
        Assert.AreEqual("b at a", Pattern(new ReplaceRules(@"(\w)@(\w)", "$2 at $1", false, false, true)).Replace("a@b", "$2 at $1"));
        Assert.AreEqual("$2", FileTools.ReplaceText(new ReplaceRules("x", "$2", false, false, false)).Replace("$$", "$"));

        Assert.IsNull(FileTools.ReplacePattern(new ReplaceRules("", "x", false, false, false), out string? empty));
        Assert.IsNotNull(empty);
        Assert.IsNull(FileTools.ReplacePattern(new ReplaceRules("[", "x", false, false, true), out string? invalid));
        Assert.IsNotNull(invalid);
    }

    [TestMethod]
    public void ReplaceSources_SkipHiddenFoldersAndLinks()
    {
        string root = Directory.CreateDirectory(Path.Combine(_folder, "root")).FullName;
        string git = Directory.CreateDirectory(Path.Combine(root, ".git")).FullName;

        // Git marks it hidden on Windows; Linux calls a dot name hidden by itself.
        new DirectoryInfo(git).Attributes |= FileAttributes.Hidden;
        string sub = Directory.CreateDirectory(Path.Combine(root, "sub")).FullName;
        string elsewhere = Directory.CreateDirectory(Path.Combine(_folder, "elsewhere")).FullName;
        string top = Path.Combine(root, "a.txt");
        string deep = Path.Combine(sub, "c.txt");
        string picked = Path.Combine(_folder, "picked.md");
        foreach (string file in (string[])[top, deep, picked, Path.Combine(root, "b.md"), Path.Combine(git, "config.txt"), Path.Combine(elsewhere, "far.txt")])
        {
            System.IO.File.WriteAllText(file, "x");
        }

        LinkFolder(Path.Combine(root, "link"), elsewhere);

        CollectionAssert.AreEqual(new[] { picked, top, deep },
            FileTools.ReplaceSources([File(picked), Folder(root)], ["*.txt"], subfolders: true, out List<string> errors, CancellationToken.None).ToArray());
        Assert.AreEqual(0, errors.Count);
        CollectionAssert.AreEqual(new[] { top },
            FileTools.ReplaceSources([Folder(root)], ["*.txt"], subfolders: false, out _, CancellationToken.None).ToArray());

        // Subfolders in the order of their names, after the folder's own files.
        string first = Path.Combine(Directory.CreateDirectory(Path.Combine(root, "a-first")).FullName, "x.txt");
        System.IO.File.WriteAllText(first, "x");
        CollectionAssert.AreEqual(new[] { top, first, deep },
            FileTools.ReplaceSources([Folder(root)], ["*.txt"], subfolders: true, out _, CancellationToken.None).ToArray());
    }
}
