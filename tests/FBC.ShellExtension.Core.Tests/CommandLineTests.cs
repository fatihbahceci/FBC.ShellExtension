using FBC.ShellExtension.Core.Commands;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class CommandLineTests
{
    [TestMethod]
    [DataRow("plain", "\"plain\"")]
    [DataRow(@"C:\my files\a.txt", "\"C:\\my files\\a.txt\"")]
    [DataRow(@"C:\", "\"C:\\\\\"")]
    [DataRow("say \"hi\"", "\"say \\\"hi\\\"\"")]
    [DataRow("a\\\"b", "\"a\\\\\\\"b\"")]
    [DataRow("", "\"\"")]
    public void Quote_FollowsCommandLineToArgvRules(string value, string expected)
    {
        Assert.AreEqual(expected, CommandLine.Quote(value));
    }
}
