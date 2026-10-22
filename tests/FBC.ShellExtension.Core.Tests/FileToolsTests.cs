using System.Diagnostics;
using System.Text;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

/// <summary>The windowed tools on real files in a temporary folder (ADR-0017).</summary>
[TestClass]
public sealed partial class FileToolsTests
{
    // "abc" is the test vector of every one of these algorithms.
    private const string AbcMd5 = "900150983cd24fb0d6963f7d28e17f72";
    private const string AbcSha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    private string _folder = "";

    [TestInitialize]
    public void CreateFolder() => _folder = Directory.CreateTempSubdirectory("fbctools-filetools-").FullName;

    [TestCleanup]
    public void DeleteFolder()
    {
        // Directory.Delete(recursive) unmounts a junction through DeleteVolumeMountPoint, which
        // Windows refuses a user without administrator rights; removing the junction itself is
        // allowed (G-0109).
        RemoveLinks(new DirectoryInfo(_folder));
        Directory.Delete(_folder, recursive: true);

        static void RemoveLinks(DirectoryInfo folder)
        {
            foreach (DirectoryInfo child in folder.GetDirectories("*", new EnumerationOptions { AttributesToSkip = 0 }))
            {
                if (child.LinkTarget is not null)
                {
                    child.Delete();
                }
                else
                {
                    RemoveLinks(child);
                }
            }
        }
    }

    private sealed class Report(Action? onFirstProgress = null) : IToolReport
    {
        private Action? _onFirstProgress = onFirstProgress;

        public List<string> Lines { get; } = [];

        public long LastDone { get; private set; } = -1;

        public void Output(string line) => Lines.Add(line);

        public void Progress(long done, long total)
        {
            LastDone = done;
            Interlocked.Exchange(ref _onFirstProgress, null)?.Invoke();
        }
    }

    private string Write(string name, string text) => Write(name, Encoding.UTF8.GetBytes(text));

    private string Write(string name, byte[] bytes)
    {
        string path = Path.Combine(_folder, name);
        System.IO.File.WriteAllBytes(path, bytes);
        return path;
    }

    private static Report Run(BuiltinTool tool, string path, ToolInput? input = null, CancellationToken cancellationToken = default)
    {
        var report = new Report();
        FileTools.Run(tool, File(path), input ?? new ToolInput(), report, cancellationToken);
        return report;
    }

    private static string Fail(BuiltinTool tool, string path, ToolInput? input = null) =>
        Assert.ThrowsExactly<ToolFailedException>(() => Run(tool, path, input)).Message;

    /// <summary>
    /// A link to a folder. dev-run runs these tests on Windows too, where a symbolic link needs
    /// Developer Mode or an administrator; a junction needs neither, and is a link all the same.
    /// </summary>
    private static void LinkFolder(string link, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(link, target);
            return;
        }

        using Process process = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
        })!;
        process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, "mklink /J " + link);
    }

    private string[] Files() => [.. Directory.GetFiles(_folder).Select(Path.GetFileName).Order(StringComparer.Ordinal)!];

    [TestMethod]
    public void Checksum_ShowsAllFourAndReportsProgress()
    {
        Report report = Run(BuiltinTool.Checksum, Write("abc.txt", "abc"));

        CollectionAssert.AreEqual(
            new[]
            {
                $"MD5      {AbcMd5}",
                "SHA-1    a9993e364706816aba3e25717850c26c9cd0d89d",
                $"SHA-256  {AbcSha256}",
                "SHA-512  ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f",
            },
            report.Lines);
        Assert.AreEqual(3, report.LastDone);
    }

    [TestMethod]
    public void Tools_RefuseAFolder()
    {
        StringAssert.Contains(
            Assert.ThrowsExactly<ToolFailedException>(() => FileTools.Run(BuiltinTool.Checksum, Folder(_folder), new ToolInput(), new Report(), default)).Message,
            _folder);
    }

    [TestMethod]
    [DataRow("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", AbcSha256)]
    [DataRow("SHA256: BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", AbcSha256)]
    [DataRow("900150983cd24fb0d6963f7d28e17f72 *abc.txt\r\n", AbcMd5)]
    [DataRow("MD5 (abc.txt) = 900150983cd24fb0d6963f7d28e17f72", AbcMd5)]
    [DataRow("not a checksum", null)]
    [DataRow("900150983cd24fb0d6963f7d28e17f72 and 00000000000000000000000000000000", null)]
    [DataRow("", null)]
    public void FindHex_TakesTheOneDigestOfAKnownLength(string text, string? expected)
    {
        Assert.AreEqual(expected, Checksums.FindHex(text));
    }

    [TestMethod]
    public void ChecksumFile_ReadsGnuAndBsdLines()
    {
        ChecksumList list = ChecksumFile.Parse(
            "# made by hand\r\n" +
            $"{AbcSha256} *abc.txt\r\n" +
            $"{AbcMd5}  sub/other name.txt\n" +
            $"SHA256 (third.bin) = {AbcSha256.ToUpperInvariant()}\n" +
            $"\\{AbcMd5}  back\\\\slash.txt\n" +
            "garbage\n");

        CollectionAssert.AreEqual(
            new[] { "abc.txt Sha256", "sub/other name.txt Md5", "third.bin Sha256", @"back\slash.txt Md5" },
            list.Entries.Select(entry => $"{entry.FileName} {entry.Algorithm}").ToArray());
        Assert.IsTrue(list.Entries.All(entry => entry.Hash == entry.Hash.ToLowerInvariant()));
        Assert.AreEqual(1, list.Unreadable);
    }

    [TestMethod]
    public void ChecksumFile_ReadsABareDigestAndCertUtilOutput()
    {
        ChecksumEntry bare = ChecksumFile.Parse(AbcSha256.ToUpperInvariant() + "\r\n").Entries.Single();
        ChecksumList certutil = ChecksumFile.Parse(
            "SHA256 hash of C:\\x\\abc.txt:\r\n" +
            string.Join(' ', Enumerable.Range(0, 32).Select(i => AbcSha256.Substring(i * 2, 2))) + "\r\n" +
            "CertUtil: -hashfile command completed successfully.\r\n");

        Assert.AreEqual("", bare.FileName);
        Assert.AreEqual(AbcSha256, bare.Hash);
        Assert.AreEqual("abc.txt", certutil.Entries.Single().FileName);
        Assert.AreEqual(AbcSha256, certutil.Entries.Single().Hash);
        Assert.AreEqual(0, certutil.Unreadable);
    }

    [TestMethod]
    public void Verify_ReadsABareDigestNextToTheFile_BeforeAStaleClipboard()
    {
        string file = Write("abc.zip", "abc");
        string sidecar = Write("abc.zip.sha256", AbcSha256.ToUpperInvariant() + "\r\n");

        StringAssert.Contains(Run(BuiltinTool.VerifyChecksum, file, new ToolInput(Clipboard: AbcMd5.Replace('9', '8'))).Lines.Single(), "abc.zip.sha256");
        StringAssert.Contains(Run(BuiltinTool.VerifyChecksum, sidecar).Lines[^1], "1");
    }

    [TestMethod]
    public void VerifyList_FailsWhenALineCannotBeRead()
    {
        Write("abc.txt", "abc");
        string list = Write("abc.md5", $"{AbcMd5} *abc.txt\nnot a checksum line\n");

        var report = new Report();
        Assert.ThrowsExactly<ToolFailedException>(() => FileTools.Run(BuiltinTool.VerifyChecksum, File(list), new ToolInput(), report, default));

        StringAssert.Contains(report.Lines[0], "abc.txt");
        StringAssert.Contains(report.Lines[^1], "1");
    }

    [TestMethod]
    public void VerifyList_ReadsNamesInTheWindowsCodePage()
    {
        Write("café.txt", "abc");
        string list = Write("SHA256SUMS", Encoding.Latin1.GetBytes($"{AbcSha256}  café.txt\r\n"));

        Report report = Run(BuiltinTool.VerifyChecksum, list, new ToolInput(Ansi: Encoding.Latin1));

        StringAssert.Contains(report.Lines[0], "café.txt");
    }

    [TestMethod]
    public void VerifyList_ReportsEveryFileAndFailsOnAnyBadOne()
    {
        Write("abc.txt", "abc");
        Write("changed.txt", "abd");
        string list = Write("SHA256SUMS", $"{AbcSha256} *abc.txt\n{AbcSha256} *changed.txt\n{AbcSha256} *gone.txt\n");

        var report = new Report();
        string message = Assert.ThrowsExactly<ToolFailedException>(() => FileTools.Run(BuiltinTool.VerifyChecksum, File(list), new ToolInput(), report, default)).Message;

        Assert.HasCount(3, report.Lines);
        StringAssert.Contains(report.Lines[0], "abc.txt");
        StringAssert.Contains(report.Lines[1], "changed.txt");
        StringAssert.Contains(report.Lines[2], "gone.txt");
        StringAssert.Contains(message, "2");
    }

    [TestMethod]
    public void Verify_PrefersTheChecksumFileNextToIt_ThenTheClipboard()
    {
        string file = Write("abc.txt", "abc");

        // Clipboard only: matches.
        StringAssert.Contains(Run(BuiltinTool.VerifyChecksum, file, new ToolInput(Clipboard: AbcMd5)).Lines.Single(), "MD5");

        // A wrong value on the clipboard loses to the checksum file next to the file.
        Write("abc.txt.sha256", $"{AbcSha256} *abc.txt\n");
        StringAssert.Contains(Run(BuiltinTool.VerifyChecksum, file, new ToolInput(Clipboard: new string('0', 64))).Lines.Single(), "abc.txt.sha256");

        // A wrong value in the checksum file fails, saying both values.
        Write("abc.txt.sha256", $"{new string('1', 64)} *abc.txt\n");
        StringAssert.Contains(Fail(BuiltinTool.VerifyChecksum, file), AbcSha256);
    }

    [TestMethod]
    public void Verify_WithNothingToCompare_SaysWhatToDo()
    {
        StringAssert.Contains(Fail(BuiltinTool.VerifyChecksum, Write("abc.txt", "abc"), new ToolInput(Clipboard: "hello")), ".sha256");
    }

    [TestMethod]
    public void SplitThenJoin_GivesTheFileBackAndChecksIt()
    {
        byte[] content = [.. Enumerable.Range(0, 5000).Select(i => (byte)(i * 7))];
        string original = Write("movie.mp4", content);

        Report split = Run(BuiltinTool.Split, original, new ToolInput(PartSize: 2048));

        CollectionAssert.AreEqual(
            new[] { "movie.mp4", "movie.mp4.001", "movie.mp4.002", "movie.mp4.003", "movie.mp4.sha256" },
            Files());
        Assert.AreEqual(2048, new FileInfo(original + ".001").Length);
        Assert.AreEqual(904, new FileInfo(original + ".003").Length);
        StringAssert.Contains(split.Lines[0], "movie.mp4.003");

        System.IO.File.Delete(original);
        IReadOnlyList<SelectedItem> targets = FileTools.Targets(BuiltinTool.Join, [File(original + ".002"), File(original + ".001")]);
        Report join = Run(BuiltinTool.Join, targets.Single().Path);

        CollectionAssert.AreEqual(content, System.IO.File.ReadAllBytes(original));
        StringAssert.Contains(join.Lines[^1], "SHA-256");
    }

    [TestMethod]
    public void Split_RefusesToOverwriteAndToMakeOnePart()
    {
        string file = Write("a.bin", new byte[3000]);
        StringAssert.Contains(Fail(BuiltinTool.Split, file, new ToolInput(PartSize: 4096)), "4 KB");

        Write("a.bin.002", "old");
        StringAssert.Contains(Fail(BuiltinTool.Split, file, new ToolInput(PartSize: 1024)), "a.bin.002");
        CollectionAssert.AreEqual(new[] { "a.bin", "a.bin.002" }, Files());
    }

    [TestMethod]
    public void Split_NumbersPastNineHundredNinetyNineAsSevenZipDoes()
    {
        string file = Write("big.bin", new byte[1001 * 1024]);

        Run(BuiltinTool.Split, file, new ToolInput(PartSize: 1024));

        Assert.IsTrue(System.IO.File.Exists(file + ".001"));
        Assert.IsTrue(System.IO.File.Exists(file + ".1001"));
        Assert.AreEqual(file + ".001", FileTools.Targets(BuiltinTool.Join, [File(file + ".1000")]).Single().Path);

        System.IO.File.Delete(file);
        Run(BuiltinTool.Join, file + ".001");
        Assert.AreEqual(1001 * 1024, new FileInfo(file).Length);
    }

    [TestMethod]
    public void Split_LeavesAFileThatAppearedMeanwhileAndNoticesGrowth()
    {
        string file = Write("r.bin", new byte[3000]);
        var appears = new Report(() => Write("r.bin.003", "theirs"));
        Assert.ThrowsExactly<IOException>(() => FileTools.Run(BuiltinTool.Split, File(file), new ToolInput(PartSize: 1024), appears, default));
        CollectionAssert.AreEqual(new[] { "r.bin", "r.bin.003" }, Files());
        Assert.AreEqual("theirs", System.IO.File.ReadAllText(file + ".003"));

        System.IO.File.Delete(file + ".003");
        var grows = new Report(() => System.IO.File.AppendAllText(file, "more"));
        Assert.ThrowsExactly<IOException>(() => FileTools.Run(BuiltinTool.Split, File(file), new ToolInput(PartSize: 1024), grows, default));
        CollectionAssert.AreEqual(new[] { "r.bin" }, Files());
    }

    [TestMethod]
    public void Split_RemovesWhatItWroteWhenCancelled()
    {
        string file = Write("a.bin", new byte[3000]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.ThrowsExactly<OperationCanceledException>(() => Run(BuiltinTool.Split, file, new ToolInput(PartSize: 1024), cancellation.Token));

        CollectionAssert.AreEqual(new[] { "a.bin" }, Files());
    }

    [TestMethod]
    public void Join_RefusesAGapAnExistingFileAndAWrongChecksum()
    {
        Write("a.bin.001", "one");
        Write("a.bin.003", "three");
        StringAssert.Contains(Fail(BuiltinTool.Join, Path.Combine(_folder, "a.bin.001")), "a.bin.002");
        Assert.IsFalse(System.IO.File.Exists(Path.Combine(_folder, "a.bin")));

        Write("a.bin.002", "two");
        Write("a.bin", "already here");
        StringAssert.Contains(Fail(BuiltinTool.Join, Path.Combine(_folder, "a.bin.001")), "a.bin");

        System.IO.File.Delete(Path.Combine(_folder, "a.bin"));
        Write("a.bin.sha256", $"{AbcSha256} *a.bin\n");
        StringAssert.Contains(Fail(BuiltinTool.Join, Path.Combine(_folder, "a.bin.001")), "a.bin.sha256");
        Assert.AreEqual("onetwothree", System.IO.File.ReadAllText(Path.Combine(_folder, "a.bin")));
    }

    [TestMethod]
    public void JoinTargets_TakeEachSetOnceByItsFirstPart()
    {
        IReadOnlyList<SelectedItem> targets = FileTools.Targets(BuiltinTool.Join,
            [File(@"C:\d\a.zip.003"), File(@"C:\d\a.zip.001"), File(@"C:\d\b.iso.0002"), File(@"C:\d\notes.txt")]);

        CollectionAssert.AreEqual(
            new[] { @"C:\d\a.zip.001", @"C:\d\b.iso.0001", @"C:\d\notes.txt" },
            targets.Select(item => item.Path).ToArray());
    }

    [TestMethod]
    [DataRow("", 0)]
    [DataRow("a", 1)]
    [DataRow("a\n", 1)]
    [DataRow("a\nb", 2)]
    [DataRow("a\r\nb\r\n", 2)]
    [DataRow("a\rb\r", 2)]
    [DataRow("a\n\n", 2)]
    [DataRow("\uFEFFa\nb", 2)]
    public void CountLines_CountsLikeGetContent(string text, int expected)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        Assert.AreEqual(expected, LineCounter.Count(stream, null, default));
    }

    [TestMethod]
    public void CountLines_ReadsUtf16ByItsByteOrderMark()
    {
        Report report = Run(BuiltinTool.CountLines, Write("u.txt", [0xFF, 0xFE, .. Encoding.Unicode.GetBytes("ş\r\nç\n\u0a0a")]));

        // U+0A0A holds two LF bytes, but is one character on the third line.
        StringAssert.StartsWith(report.Lines.Single(), "3 ");
    }

    [TestMethod]
    [DataRow("25 MB", 25L * 1024 * 1024)]
    [DataRow("1.5GB", 1536L * 1024 * 1024)]
    [DataRow("1,5 gb", 1536L * 1024 * 1024)]
    [DataRow("700", 700L * 1024 * 1024)]
    [DataRow("64 KB", 64L * 1024)]
    [DataRow("4095 MB", 4095L * 1024 * 1024)]
    public void SizeText_ReadsWhatPeopleWrite(string text, long expected)
    {
        Assert.IsTrue(SizeText.TryParse(text, out long bytes));
        Assert.AreEqual(expected, bytes);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("ten MB")]
    [DataRow("-5 MB")]
    [DataRow("512 B")]
    [DataRow("5 TB")]
    [DataRow("99999999999999999999 GB")]
    public void SizeText_RefusesNonsenseAndTinyParts(string text)
    {
        Assert.IsFalse(SizeText.TryParse(text, out _));
    }

    [TestMethod]
    public void SizeText_FormatsWithTheLargestUnit()
    {
        Assert.AreEqual("1.5 GB", SizeText.Format(1536L * 1024 * 1024));
        Assert.AreEqual("700 MB", SizeText.Format(700L * 1024 * 1024));
        Assert.AreEqual("512 bytes", SizeText.Format(512));
    }

    [TestMethod]
    [DataRow(@"C:\a b\ş#.txt", "file:///C:/a%20b/%C5%9F%23.txt")]
    [DataRow(@"C:\", "file:///C:/")]
    [DataRow(@"\\server\share\x y", "file://server/share/x%20y")]
    public void InternetPath_EscapesEverySegment(string path, string expected)
    {
        Assert.AreEqual(expected, ClipboardTools.InternetPath(path));
    }

    [TestMethod]
    public void Duplicate_NamesTheCopyAsExplorerDoesAndKeepsItsDate()
    {
        string file = Write("a.txt", "abc");
        var date = new DateTime(2020, 5, 6, 7, 8, 9, DateTimeKind.Utc);
        System.IO.File.SetLastWriteTimeUtc(file, date);
        string dotfile = Write(".gitignore", "bin/");

        Run(BuiltinTool.Duplicate, file);
        StringAssert.Contains(Run(BuiltinTool.Duplicate, file).Lines.Single(), "a - Copy (2).txt");
        Run(BuiltinTool.Duplicate, dotfile);

        CollectionAssert.AreEqual(new[] { ".gitignore", ".gitignore - Copy", "a - Copy (2).txt", "a - Copy.txt", "a.txt" }, Files());
        Assert.AreEqual("abc", System.IO.File.ReadAllText(Path.Combine(_folder, "a - Copy.txt")));
        Assert.AreEqual(date, System.IO.File.GetLastWriteTimeUtc(Path.Combine(_folder, "a - Copy.txt")));
    }

    [TestMethod]
    public void FolderSize_AddsUpTheTreeWithoutEnteringLinks()
    {
        string tree = Directory.CreateDirectory(Path.Combine(_folder, "tree")).FullName;
        System.IO.File.WriteAllBytes(Path.Combine(tree, "a.bin"), new byte[1000]);
        string sub = Directory.CreateDirectory(Path.Combine(tree, "sub")).FullName;
        System.IO.File.WriteAllBytes(Path.Combine(sub, "b.bin"), new byte[24]);
        LinkFolder(Path.Combine(tree, "loop"), tree);

        var report = new Report();
        FileTools.Run(BuiltinTool.FolderSize, Folder(tree), new ToolInput(), report, default);

        // Two files, 1,024 bytes; the subfolder and the link are folders, the link is not entered.
        Assert.AreEqual("1 KB (1,024 bytes) in 2 files and 2 folders", report.Lines.Single());
    }

    [TestMethod]
    public void FolderTools_AndFileTools_RefuseTheOtherKind()
    {
        StringAssert.Contains(Fail(BuiltinTool.FolderSize, Write("a.txt", "")), "a.txt");
        Assert.ThrowsExactly<ToolFailedException>(() => FileTools.Run(BuiltinTool.Duplicate, Folder(_folder), new ToolInput(), new Report(), default));
    }

    [TestMethod]
    [DataRow(new byte[] { 0x61 }, "a\r\nnew")]
    [DataRow(new byte[] { 0x61, 0x0A }, "a\nnew")]
    [DataRow(new byte[0], "new")]
    [DataRow(new byte[] { 0xEF, 0xBB, 0xBF }, "\uFEFFnew")]
    public void AppendText_PutsTheTextOnALineOfItsOwn(byte[] content, string expected)
    {
        string file = Write("a.txt", content);

        FileTools.AppendText(file, "new", Encoding.Latin1);

        Assert.AreEqual(expected, Encoding.UTF8.GetString(System.IO.File.ReadAllBytes(file)));
    }

    [TestMethod]
    public void AppendText_KeepsTheFilesEncoding()
    {
        string utf16 = Write("u.txt", [0xFF, 0xFE, .. Encoding.Unicode.GetBytes("a")]);
        string ansi = Write("l.txt", [0x63, 0xE9]); // "cé" in Latin-1, not UTF-8

        FileTools.AppendText(utf16, "ş", Encoding.Latin1);
        FileTools.AppendText(ansi, "ü", Encoding.Latin1);

        Assert.AreEqual("\uFEFFa\r\nş", Encoding.Unicode.GetString(System.IO.File.ReadAllBytes(utf16)));
        Assert.AreEqual("cé\r\nü", Encoding.Latin1.GetString(System.IO.File.ReadAllBytes(ansi)));
        Assert.ThrowsExactly<ToolFailedException>(() => FileTools.AppendText(Write("b.bin", [1, 0, 2]), "x", Encoding.Latin1));
    }

    [TestMethod]
    public void NewTools_NeedVersion5()
    {
        var config = new MenuConfig { Version = 4, Items = [new CommandEntry { Label = "Size", Tool = BuiltinTool.FolderSize }] };

        Assert.AreEqual("$.items[0].tool", ConfigValidator.Validate(config).Single().Path);
        Assert.AreEqual(5, config.RequiredVersion());
        Assert.IsFalse(BuiltinTools.UsesWindow(BuiltinTool.AppendClipboard));
        Assert.IsTrue(BuiltinTools.UsesWindow(BuiltinTool.Duplicate));
    }

    [TestMethod]
    [DataRow("Reports", null)]
    [DataRow("a.txt", "already exists")]
    [DataRow("CON", "cannot be")]
    [DataRow("com1.txt", "cannot be")]
    [DataRow("bad?name", "cannot be")]
    [DataRow("trailing.", "cannot be")]
    public void NameProblem_SaysWhyANameCannotBeUsed(string name, string? problem)
    {
        Write("a.txt", "");

        string? message = FileTools.NameProblem(_folder, name);

        if (problem is null)
        {
            Assert.IsNull(message);
        }
        else
        {
            StringAssert.Contains(message, problem);
        }
    }

    [TestMethod]
    public void FreeName_NumbersATakenName()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "New folder"));
        Directory.CreateDirectory(Path.Combine(_folder, "New folder (2)"));

        Assert.AreEqual("New folder (3)", FileTools.FreeName(_folder, "New folder"));
        Assert.AreEqual("Other", FileTools.FreeName(_folder, "Other"));
    }

    [TestMethod]
    public void PackInto_MovesTheItemsIntoANewFolder()
    {
        string a = Write("a.txt", "a");
        string sub = Directory.CreateDirectory(Path.Combine(_folder, "sub")).FullName;
        System.IO.File.WriteAllText(Path.Combine(sub, "b.txt"), "b");

        IReadOnlyList<string> errors = FileTools.PackInto([File(a), Folder(sub), File(Path.Combine(_folder, "gone.txt"))], _folder, "packed");

        Assert.HasCount(1, errors);
        StringAssert.Contains(errors[0], "gone.txt");
        Assert.AreEqual("a", System.IO.File.ReadAllText(Path.Combine(_folder, "packed", "a.txt")));
        Assert.AreEqual("b", System.IO.File.ReadAllText(Path.Combine(_folder, "packed", "sub", "b.txt")));
        CollectionAssert.AreEqual(new[] { "packed" }, Directory.GetFileSystemEntries(_folder).Select(Path.GetFileName).ToArray());
        Assert.ThrowsExactly<ToolFailedException>(() => FileTools.PackInto([File(a)], _folder, "packed"));
    }

    [TestMethod]
    public void Unpack_MovesEverythingUpUnlessANameIsTaken()
    {
        string box = Directory.CreateDirectory(Path.Combine(_folder, "box")).FullName;
        System.IO.File.WriteAllText(Path.Combine(box, "a.txt"), "inner");
        Directory.CreateDirectory(Path.Combine(box, "box"));
        Write("a.txt", "outer");

        StringAssert.Contains(Assert.ThrowsExactly<ToolFailedException>(() => FileTools.Unpack(box)).Message, "a.txt");
        Assert.AreEqual("inner", System.IO.File.ReadAllText(Path.Combine(box, "a.txt")));

        // A child named like the folder itself is why the folder is renamed aside first.
        System.IO.File.Delete(Path.Combine(_folder, "a.txt"));
        FileTools.Unpack(box);

        CollectionAssert.AreEqual(new[] { "a.txt", "box" }, Files().Concat(Directory.GetDirectories(_folder).Select(Path.GetFileName)).Order(StringComparer.Ordinal).ToArray()!);
        Assert.IsEmpty(Directory.GetFileSystemEntries(Path.Combine(_folder, "box")));
    }

    [TestMethod]
    public void Unpack_PutsEverythingBackWhenAMoveFails()
    {
        string box = Directory.CreateDirectory(Path.Combine(_folder, "box")).FullName;
        Directory.CreateDirectory(Path.Combine(box, "box"));
        System.IO.File.WriteAllText(Path.Combine(box, "a.txt"), "a");
        System.IO.File.WriteAllText(Path.Combine(box, "z.txt"), "z");

        void FailOnZ(string source, string destination)
        {
            if (source.EndsWith("z.txt", StringComparison.Ordinal))
            {
                throw new IOException("in use");
            }

            if (Directory.Exists(source))
            {
                Directory.Move(source, destination);
            }
            else
            {
                System.IO.File.Move(source, destination);
            }
        }

        StringAssert.Contains(Assert.ThrowsExactly<ToolFailedException>(() => FileTools.Unpack(box, FailOnZ)).Message, "in use");

        CollectionAssert.AreEqual(new[] { "box" }, Directory.GetFileSystemEntries(_folder).Select(Path.GetFileName).ToArray());
        CollectionAssert.AreEquivalent(new[] { "a.txt", "box", "z.txt" }, Directory.GetFileSystemEntries(box).Select(Path.GetFileName).ToArray());
    }

    [TestMethod]
    public void Unpack_RefusesALink()
    {
        string real = Directory.CreateDirectory(Path.Combine(_folder, "real")).FullName;
        Write(Path.Combine("real", "a.txt"), "a");
        string link = Path.Combine(_folder, "link");
        LinkFolder(link, real);

        Assert.ThrowsExactly<ToolFailedException>(() => FileTools.Unpack(link));
        Assert.IsTrue(System.IO.File.Exists(Path.Combine(real, "a.txt")));
    }

    [TestMethod]
    public void EmptyFolders_AreFoundDeepestFirstAndRemoved()
    {
        string root = Directory.CreateDirectory(Path.Combine(_folder, "root")).FullName;
        Directory.CreateDirectory(Path.Combine(root, "a", "b", "c"));
        Directory.CreateDirectory(Path.Combine(root, "kept"));

        // A fresh git repository's .git holds empty folders it cannot do without.
        DirectoryInfo git = Directory.CreateDirectory(Path.Combine(root, "repo", ".git", "refs", "heads"));
        git.Parent!.Parent!.Attributes |= FileAttributes.Hidden;
        System.IO.File.WriteAllText(Path.Combine(root, "kept", "file.txt"), "");
        LinkFolder(Path.Combine(root, "link"), Path.Combine(root, "a"));

        IReadOnlyList<string> empty = FileTools.FindEmptyFolders(root, default);

        CollectionAssert.AreEqual(
            new[] { Path.Combine(root, "a", "b", "c"), Path.Combine(root, "a", "b"), Path.Combine(root, "a") },
            empty.ToArray());
        Assert.IsEmpty(FileTools.DeleteFolders(empty));
        Assert.IsFalse(Directory.Exists(Path.Combine(root, "a")));
        Assert.IsTrue(Directory.Exists(Path.Combine(root, "kept")));
        Assert.IsTrue(Directory.Exists(Path.Combine(root, "repo", ".git", "refs", "heads")));
    }

    [TestMethod]
    public void DeleteEmptyFolders_RemovesOnlyWhatTheUserAgreedTo()
    {
        string root = Directory.CreateDirectory(Path.Combine(_folder, "root")).FullName;
        Directory.CreateDirectory(Path.Combine(root, "empty"));
        string? asked = null;

        var declined = new Report();
        FileTools.Run(BuiltinTool.DeleteEmptyFolders, Folder(root), new ToolInput(Confirm: text => (asked = text) is null), declined, default);
        Assert.IsTrue(Directory.Exists(Path.Combine(root, "empty")));
        StringAssert.Contains(asked, "empty");

        var agreed = new Report();
        FileTools.Run(BuiltinTool.DeleteEmptyFolders, Folder(root), new ToolInput(Confirm: _ => true), agreed, default);
        Assert.IsFalse(Directory.Exists(Path.Combine(root, "empty")));
        Assert.IsTrue(Directory.Exists(root));
    }

    [TestMethod]
    public void DeletePermanently_TakesReadOnlyFilesButNotWhatALinkPointsTo()
    {
        string outside = Directory.CreateDirectory(Path.Combine(_folder, "outside")).FullName;
        Write(Path.Combine("outside", "keep.txt"), "keep");
        string tree = Directory.CreateDirectory(Path.Combine(_folder, "tree")).FullName;
        string readOnly = Write(Path.Combine("tree", "ro.txt"), "x");
        System.IO.File.SetAttributes(readOnly, FileAttributes.ReadOnly);
        LinkFolder(Path.Combine(tree, "link"), outside);

        FileTools.Run(BuiltinTool.DeletePermanently, Folder(tree), new ToolInput(), new Report(), default);

        Assert.IsFalse(Directory.Exists(tree));
        Assert.IsTrue(System.IO.File.Exists(Path.Combine(outside, "keep.txt")));
    }

    [TestMethod]
    public void AppendText_TrustsTheUtf8MarkAndRefusesWhatTheCodePageCannotHold()
    {
        string marked = Write("m.txt", [0xEF, 0xBB, 0xBF, (byte)'a', 0xFE, (byte)'\n']);
        FileTools.AppendText(marked, "ş", Encoding.Latin1);
        Assert.IsTrue(System.IO.File.ReadAllBytes(marked).AsSpan().EndsWith(Encoding.UTF8.GetBytes("ş")));

        string ansi = Write("a.txt", [0x63, 0xE9]);
        Assert.ThrowsExactly<ToolFailedException>(() => FileTools.AppendText(ansi, "日本", Encoding.Latin1));
        CollectionAssert.AreEqual(new byte[] { 0x63, 0xE9 }, System.IO.File.ReadAllBytes(ansi));
    }

    [TestMethod]
    public void ToolsOfVersion6_AndTheItemsTheyTake()
    {
        var config = new MenuConfig { Version = 5, Items = [new CommandEntry { Label = "Pack", Tool = BuiltinTool.PackToFolder }] };

        Assert.AreEqual(6, config.RequiredVersion());
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.NewFolder, ItemKind.Background));
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.UnpackFolder, ItemKind.Folder));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.UnpackFolder, ItemKind.Drive));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.DeletePermanently, ItemKind.Drive));
        Assert.IsTrue(BuiltinTools.UsesWindow(BuiltinTool.DeletePermanently));
    }

    [TestMethod]
    public void WindowedTools_NeedVersion4()
    {
        var config = new MenuConfig { Version = 3, Items = [new CommandEntry { Label = "Split", Tool = BuiltinTool.Split }] };

        Assert.AreEqual("$.items[0].tool", ConfigValidator.Validate(config).Single().Path);
        Assert.AreEqual(4, config.RequiredVersion());
        Assert.IsTrue(BuiltinTools.UsesWindow(BuiltinTool.Join));
        Assert.IsFalse(BuiltinTools.UsesWindow(BuiltinTool.CopyPath));
    }

    [TestMethod]
    public void ToolsOfVersion8_AndTheItemsTheyTake()
    {
        var config = new MenuConfig { Version = 7, Items = [new CommandEntry { Label = "Copy to", Tool = BuiltinTool.CopyToFolder }] };

        Assert.AreEqual(8, config.RequiredVersion());
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.MoveToFolder, ItemKind.Folder));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.CopyToFolder, ItemKind.Background));
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.CreateLink, ItemKind.File));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.JoinFolders, ItemKind.File));
        Assert.IsTrue(BuiltinTools.Accepts(BuiltinTool.ChangeFolderIcon, ItemKind.Background));
        Assert.IsFalse(BuiltinTools.Accepts(BuiltinTool.RestoreFolderIcon, ItemKind.Drive));
        Assert.IsFalse(BuiltinTools.UsesWindow(BuiltinTool.CopyToFolder));
    }

    [TestMethod]
    public void FreeItemName_NumbersAFileBeforeItsExtension()
    {
        Write("report.pdf", "");
        Write(".gitignore", "");
        Directory.CreateDirectory(Path.Combine(_folder, "photos.2024"));

        Assert.AreEqual("report (2).pdf", FileTools.FreeItemName(_folder, "report.pdf", isFile: true));
        Assert.AreEqual("other.pdf", FileTools.FreeItemName(_folder, "other.pdf", isFile: true));
        Assert.AreEqual(".gitignore (2)", FileTools.FreeItemName(_folder, ".gitignore", isFile: true));
        Assert.AreEqual("photos.2024 (2)", FileTools.FreeItemName(_folder, "photos.2024", isFile: false));
    }

    [TestMethod]
    public void JoinProblem_WantsTwoFoldersSideBySideAndNoLink()
    {
        string a = Directory.CreateDirectory(Path.Combine(_folder, "a")).FullName;
        string b = Directory.CreateDirectory(Path.Combine(_folder, "b")).FullName;
        string inner = Directory.CreateDirectory(Path.Combine(a, "inner")).FullName;
        string link = Path.Combine(_folder, "link");
        LinkFolder(link, b);

        Assert.IsNull(FileTools.JoinProblem([Folder(a), Folder(b)]));
        Assert.IsNotNull(FileTools.JoinProblem([Folder(a)]));
        Assert.IsNotNull(FileTools.JoinProblem([Folder(b), Folder(inner)]));
        StringAssert.Contains(FileTools.JoinProblem([Folder(a), Folder(link)]), "link");
    }

    [TestMethod]
    public void JoinSources_TakeHiddenItems_AndAnEmptiedFolderGoes()
    {
        string a = Directory.CreateDirectory(Path.Combine(_folder, "a")).FullName;
        string b = Directory.CreateDirectory(Path.Combine(_folder, "b")).FullName;
        System.IO.File.WriteAllText(Path.Combine(a, "one.txt"), "1");
        string hidden = Path.Combine(b, "desktop.ini");
        System.IO.File.WriteAllText(hidden, "");
        System.IO.File.SetAttributes(hidden, FileAttributes.Hidden | FileAttributes.System);

        // Linux drops the attributes above but calls a dot name hidden: the check holds on both.
        string dotted = Path.Combine(b, ".hidden");
        System.IO.File.WriteAllText(dotted, "");
        Directory.CreateDirectory(Path.Combine(b, "sub"));

        CollectionAssert.AreEquivalent(
            new[] { Path.Combine(a, "one.txt"), hidden, dotted, Path.Combine(b, "sub") },
            FileTools.JoinSources([Folder(a), Folder(b)]).ToArray());

        Assert.IsFalse(FileTools.RemoveIfEmpty(b));
        System.IO.File.Delete(Path.Combine(a, "one.txt"));

        // Only Windows refuses to delete a read-only folder; Linux deletes it either way.
        new DirectoryInfo(a).Attributes |= FileAttributes.ReadOnly;
        Assert.IsTrue(FileTools.RemoveIfEmpty(a));
        Assert.IsFalse(Directory.Exists(a));
    }

    [TestMethod]
    public void LinkProblem_RefusesALoop()
    {
        Assert.IsNotNull(FileTools.LinkProblem(Folder(@"C:\Data\Photos"), @"C:\Data\Photos"));
        Assert.IsNotNull(FileTools.LinkProblem(Folder(@"C:\Data\Photos"), @"c:\data\photos\2024\"));
        Assert.IsNull(FileTools.LinkProblem(Folder(@"C:\Data\Photos"), @"C:\Data\Photos 2"));
        Assert.IsNull(FileTools.LinkProblem(File(@"C:\Data\a.txt"), @"C:\Data"));
    }

    [TestMethod]
    public void IconTools_LeaveADriveAlone_HoweverItComes()
    {
        Assert.IsNotNull(FileTools.IconProblem(Drive(@"E:\")));
        Assert.IsNotNull(FileTools.IconProblem(Background(@"E:\")));
        Assert.IsNotNull(FileTools.IconProblem(Background(@"\\server\share")));
        Assert.IsNull(FileTools.IconProblem(Background(@"E:\Photos")));
        Assert.IsNull(FileTools.IconProblem(Folder(@"\\server\share\Photos")));
    }

    [TestMethod]
    public void MountPointData_IsWhatMklinkWrites()
    {
        // fsutil reparsepoint query of "mklink /J Python34 "C:\Program Files\Python34"": data length
        // 0x78, then 00 00 3a 00 3c 00 32 00.
        byte[] data = FileTools.MountPointData(@"C:\Program Files\Python34\");

        Assert.AreEqual(128, data.Length);
        CollectionAssert.AreEqual(new byte[] { 0x03, 0x00, 0x00, 0xA0, 0x78, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3A, 0x00, 0x3C, 0x00, 0x32, 0x00 }, data[..16]);
        Assert.AreEqual(@"\??\C:\Program Files\Python34", Encoding.Unicode.GetString(data, 16, 58));
        Assert.AreEqual(@"C:\Program Files\Python34", Encoding.Unicode.GetString(data, 16 + 60, 50));
        Assert.AreEqual(0, data[16 + 58] | data[16 + 59] | data[^2] | data[^1]);

        Assert.AreEqual(@"\??\D:\", Encoding.Unicode.GetString(FileTools.MountPointData(@"\\?\D:\"), 16, 14));
        Assert.ThrowsExactly<ToolFailedException>(() => FileTools.MountPointData(@"\\server\share\folder"));
        Assert.ThrowsExactly<ToolFailedException>(() => FileTools.MountPointData(@"C:\" + new string('a', 8200)));
    }

    [TestMethod]
    public void DesktopIni_IsEmptyWithoutItsIconLines()
    {
        Assert.IsFalse(FileTools.DesktopIniHasOtherSettings("\uFEFF[.ShellClassInfo]\r\nIconResource=C:\\icons\\a.ico,0\r\n"));
        Assert.IsFalse(FileTools.DesktopIniHasOtherSettings("; made by hand\n[.ShellClassInfo]\nIconFile=a.ico\nIconIndex=0\n\n"));
        Assert.IsTrue(FileTools.DesktopIniHasOtherSettings("[.ShellClassInfo]\r\nLocalizedResourceName=@%SystemRoot%\\system32\\shell32.dll,-21770\r\nIconResource=x,0"));
        Assert.IsTrue(FileTools.DesktopIniHasOtherSettings("[.ShellClassInfo]\r\nIconResource=x,0\r\n[ViewState]\r\nFolderType=Pictures\r\n"));
    }
}
