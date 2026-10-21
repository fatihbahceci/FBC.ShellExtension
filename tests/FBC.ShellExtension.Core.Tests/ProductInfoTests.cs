using FBC.ShellExtension.Core.Configuration;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class ProductInfoTests
{
    [TestMethod]
    public void InformationalVersion_ComesFromTheBuild()
    {
        Assert.AreNotEqual("unknown", ProductInfo.InformationalVersion);
        Assert.IsTrue(char.IsAsciiDigit(ProductInfo.InformationalVersion[0]), ProductInfo.InformationalVersion);
    }

    [TestMethod]
    public void ConfigPath_IsUnderTheProductFolder()
    {
        Assert.EndsWith(Path.Combine(ProductInfo.Name, ConfigLocation.FileName), ConfigLocation.DefaultPath);
    }
}
