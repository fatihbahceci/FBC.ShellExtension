using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Editing;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class IssueLocatorTests
{
    private static readonly CommandEntry s_inner = new() { Label = "inner", Command = new CommandDefinition() };
    private static readonly SubmenuEntry s_submenu = new() { Label = "sub", Items = [new SeparatorEntry(), s_inner] };
    private static readonly MenuConfig s_config = new() { Items = [new CommandEntry { Label = "first" }, s_submenu] };

    [TestMethod]
    [DataRow("$.items[1].items[1].command.fileName", "command.fileName")]
    [DataRow("$.items[1].items[1]", "")]
    public void NestedPath_ResolvesToTheEntryAndTheMemberInsideIt(string path, string member)
    {
        IssueTarget target = IssueLocator.Resolve(s_config, path);

        Assert.AreSame(s_inner, target.Entry);
        Assert.AreEqual(member, target.Member);
    }

    [TestMethod]
    public void SubmenuItemsPath_StopsAtTheSubmenu()
    {
        IssueTarget target = IssueLocator.Resolve(new MenuConfig { Items = [new SubmenuEntry { Label = "empty" }] }, "$.items[0].items");

        Assert.IsInstanceOfType<SubmenuEntry>(target.Entry);
        Assert.AreEqual("items", target.Member);
    }

    [TestMethod]
    public void DocumentPath_HasNoEntry()
    {
        IssueTarget target = IssueLocator.Resolve(s_config, "$.version");

        Assert.IsNull(target.Entry);
        Assert.AreEqual("version", target.Member);
    }

    [TestMethod]
    public void IndexOutOfRange_StopsAtTheLastEntryFound()
    {
        IssueTarget target = IssueLocator.Resolve(s_config, "$.items[1].items[7].label");

        Assert.AreSame(s_submenu, target.Entry);
        Assert.AreEqual("items[7].label", target.Member);
    }
}
