using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Selection;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class VariableExpanderTests
{
    private static readonly Func<string, string?> s_noEnv = _ => null;

    private static string Expand(string template, SelectedItem item, Func<string, string?>? env = null) =>
        VariableExpander.Expand(template, item, [item], env ?? s_noEnv);

    [TestMethod]
    [DataRow("{path}", @"C:\work\report.final.pdf")]
    [DataRow("{dir}", @"C:\work")]
    [DataRow("{name}", "report.final.pdf")]
    [DataRow("{nameNoExt}", "report.final")]
    [DataRow("{ext}", ".pdf")]
    [DataRow("{pathNoExt}.mp3", @"C:\work\report.final.mp3")]
    [DataRow("{PATH}|{NameNoExt}", @"C:\work\report.final.pdf|report.final")]
    public void Placeholders_AreReplaced(string template, string expected)
    {
        Assert.AreEqual(expected, Expand(template, File(@"C:\work\report.final.pdf")));
    }

    [TestMethod]
    public void PathNoExt_AvoidsTheDoubleBackslashOfDirPlusName()
    {
        SelectedItem atRoot = File(@"C:\clip.mkv");

        Assert.AreEqual(@"""C:\clip.mp3""", Expand("\"{pathNoExt}.mp3\"", atRoot));
        Assert.AreEqual(@"""C:\\clip.mp3""", Expand("\"{dir}\\{nameNoExt}.mp3\"", atRoot));
        Assert.AreEqual(@"C:\work", Expand("{pathNoExt}", Folder(@"C:\work")), "a folder has no extension");
    }

    [TestMethod]
    public void AllPaths_QuotesEveryPath()
    {
        SelectedItem[] all = [File(@"C:\my files\a.txt"), File(@"C:\b.txt"), Drive(@"D:\")];

        string result = VariableExpander.Expand("--files {allPaths}", all[0], all, s_noEnv);

        Assert.AreEqual(@"--files ""C:\my files\a.txt"" ""C:\b.txt"" ""D:\\""", result);
    }

    [TestMethod]
    public void EnvironmentVariables_AreExpanded()
    {
        var env = Env(("TOOLS", @"C:\tools"));

        Assert.AreEqual(@"C:\tools\x.exe", Expand(@"%TOOLS%\x.exe", File(@"C:\a.txt"), env));
        Assert.AreEqual(@"C:\tools\x.exe", Expand(@"%tools%\x.exe", File(@"C:\a.txt"), env));
    }

    [TestMethod]
    [DataRow("%UNDEFINED%\\x.exe")]
    [DataRow("100%")]
    [DataRow("50% of %")]
    [DataRow("%%")]
    public void UndefinedOrUnpairedPercent_IsLeftAsWritten(string template)
    {
        Assert.AreEqual(template, Expand(template, File(@"C:\a.txt"), Env(("OTHER", "x"))));
    }

    [TestMethod]
    public void InsertedValues_AreNotExpandedAgain()
    {
        var env = Env(("FOO", "expanded"));

        Assert.AreEqual(@"C:\in\%FOO%{name}.txt", Expand("{path}", File(@"C:\in\%FOO%{name}.txt"), env));
    }

    [TestMethod]
    public void UnknownBraces_AreLeftAsWritten()
    {
        string template = """-Command "& { Get-Item '{path}' | Out-Host }" {unknown}""";

        Assert.AreEqual(
            """-Command "& { Get-Item 'C:\a.txt' | Out-Host }" {unknown}""",
            Expand(template, File(@"C:\a.txt")));
    }

    [TestMethod]
    public void TrailingBackslashBeforeQuote_IsDoubled()
    {
        // "C:\" would be split as C:" by CommandLineToArgvW; "C:\\" is read as C:\.
        Assert.AreEqual(@"""C:\\""", Expand("\"{path}\"", Drive(@"C:\")));
        Assert.AreEqual(@"""C:\\"" /s", Expand("\"{dir}\" /s", File(@"C:\a.txt")));
        Assert.AreEqual(@"C:\", Expand("{dir}", File(@"C:\a.txt")));
    }
}
