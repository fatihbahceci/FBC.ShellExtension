using System.Buffers.Binary;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Menu;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class MenuIconTests
{
    /// <summary>The start of a PNG as far as Core reads it: the signature and the IHDR chunk, padded to <paramref name="length"/>.</summary>
    internal static byte[] Png(int width = 32, int height = 32, int length = 64)
    {
        byte[] png = new byte[length];
        new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(png, 0);
        BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(8), 13);
        "IHDR"u8.CopyTo(png.AsSpan(12));
        BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(16), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(20), (uint)height);

        // Bytes that encode to "++++" and "////" in base64 (offset 39 starts a group of three);
        // a string would have had its "+" escaped.
        new byte[] { 0xFB, 0xEF, 0xBE, 0xFF, 0xFF, 0xFF }.CopyTo(png, 39);
        return png;
    }

    private static CommandEntry Command(byte[]? icon) =>
        new() { Label = "Run", Icon = icon, Command = new CommandDefinition { FileName = "tool.exe" } };

    [TestMethod]
    public void AnIcon_NeedsVersion14_AndIsCheckedAsAPng()
    {
        static string? Issue(byte[] icon, int version = MenuConfig.CurrentVersion) =>
            ConfigValidator.Validate(new MenuConfig { Version = version, Items = [Command(icon)] }).SingleOrDefault()?.Message;

        Assert.IsNull(Issue(Png()));
        StringAssert.Contains(Issue(Png(), version: 13), "version 14");
        StringAssert.Contains(Issue([1, 2, 3]), "not a PNG");
        StringAssert.Contains(Issue(Png(length: MenuIcon.MaxBytes + 1)), "at most 16 KB");
        StringAssert.Contains(Issue(Png(width: 512)), "512×32");
        Assert.AreEqual("$.items[0].icon", ConfigValidator.Validate(new MenuConfig { Items = [Command([0])] }).Single().Path);
    }

    [TestMethod]
    public void AnIconAnywhere_RequiresVersion14()
    {
        var config = new MenuConfig { Items = [new SubmenuEntry { Label = "Sub", Items = [Command(null)], Icon = Png() }] };

        Assert.AreEqual(14, config.RequiredVersion());
        Assert.AreEqual(1, new MenuConfig { Items = [Command(null)] }.RequiredVersion());
    }

    [TestMethod]
    public void AnIcon_IsWrittenLastAsPlainBase64_AndReadBack()
    {
        byte[] png = Png();
        CommandEntry entry = Command(png);
        entry.Id = "x";
        entry.Enabled = false;
        string json = ConfigSerializer.Serialize(new MenuConfig { Items = [entry] });

        string base64 = Convert.ToBase64String(png);
        StringAssert.Contains(base64, "+");
        StringAssert.Contains(json, $"\"icon\": \"{base64}\"");

        // After everything else, the base class's id and enabled too.
        int icon = json.IndexOf("\"icon\"", StringComparison.Ordinal);
        foreach (string other in new[] { "\"label\"", "\"command\"", "\"id\"", "\"enabled\"" })
        {
            Assert.IsTrue(icon > json.IndexOf(other, StringComparison.Ordinal), json);
        }
        CollectionAssert.AreEqual(png, ((CommandEntry)ConfigSerializer.Deserialize(json).Items[0]).Icon);
    }

    [TestMethod]
    public void TextThatIsNotBase64_DoesNotRead_AndNamesTheEntry()
    {
        var ex = Assert.ThrowsExactly<ConfigLoadException>(() => ConfigSerializer.Deserialize("""
            { "items": [ { "type": "command", "label": "x", "command": { "fileName": "a.exe" }, "icon": "not base64!" } ] }
            """));

        StringAssert.Contains(ex.Message, "$.items[0].icon");
    }

    [TestMethod]
    public void TheMenu_CarriesEachEntrysIcon()
    {
        byte[] png = Png();
        MenuEntry[] entries =
        [
            Command(png),
            new SubmenuEntry { Label = "Sub", Icon = png, Items = [Command(null)] },
            new CommandEntry { Label = "Copy", Tool = BuiltinTool.CopyPath, Icon = png },
        ];

        IReadOnlyList<MenuNode> nodes = MenuBuilder.Build(entries, [File(@"C:\a.txt")]);

        Assert.AreSame(png, nodes[0].Icon);
        Assert.AreSame(png, nodes[1].Icon);
        Assert.IsNull(((SubmenuNode)nodes[1]).Children[0].Icon);
        Assert.AreSame(png, nodes[2].Icon);
    }
}
