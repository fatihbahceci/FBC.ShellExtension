using FBC.ShellExtension.Core.Commands;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class ProgramLocatorTests
{
    private static readonly string[] s_folders = [@"C:\FBCTools\tools", @"C:\FBCTools", @"C:\Windows\System32", @"C:\Windows", @"D:\Bin\"];

    private static string? Find(string fileName, params string[] files) =>
        ProgramLocator.Find(fileName, Env(("ProgramFiles", @"C:\Program Files")), s_folders,
            path => files.Contains(path, StringComparer.OrdinalIgnoreCase));

    [TestMethod]
    public void AFullPath_IsTakenAsItIs_WithItsVariablesAndQuotes()
    {
        Assert.AreEqual(@"C:\Program Files\mRemoteNG\mRemoteNG.exe",
            Find(@" ""%ProgramFiles%\mRemoteNG\mRemoteNG.exe"" ", @"C:\Program Files\mRemoteNG\mRemoteNG.exe"));
        Assert.AreEqual(@"\\server\tools\x.exe", Find(@"\\server\tools\x.exe", @"\\server\tools\x.exe"));
        Assert.IsNull(Find(@"C:\Gone\x.exe"));
    }

    [TestMethod]
    public void ABareName_IsLookedForInTheFoldersInOrder_WithExeAdded()
    {
        Assert.AreEqual(@"C:\Windows\System32\notepad.exe", Find("notepad", @"C:\Windows\System32\notepad.exe", @"C:\Windows\notepad.exe"));
        Assert.AreEqual(@"C:\FBCTools\tools\ffmpeg.exe", Find("ffmpeg.exe", @"C:\FBCTools\tools\ffmpeg.exe", @"D:\Bin\ffmpeg.exe"));
        Assert.AreEqual(@"D:\Bin\run.cmd", Find("run.cmd", @"D:\Bin\run.cmd"));
        Assert.IsNull(Find("nowhere"));
    }

    [TestMethod]
    public void WhatDependsOnTheSelectionOrOnExplorersFolder_IsNotFound()
    {
        Assert.IsNull(Find("{path}", @"C:\Windows\{path}.exe"));
        // As written, "{dir}" would be a folder name; it is refused before that.
        Assert.IsNull(Find(@"C:\{dir}\setup.exe", @"C:\{dir}\setup.exe"));
        Assert.IsNull(Find(@"bin\tool.exe", @"C:\Windows\bin\tool.exe"));
        Assert.IsNull(Find("  "));
    }

    [TestMethod]
    [DataRow("notepad.exe", false)]
    [DataRow("\"{path}\"", true)]
    [DataRow("{allPaths}", true)]
    [DataRow("-File \"{pathNoExt}.txt\"", true)]
    [DataRow("-Command \"& { Get-Date }\"", false)]
    [DataRow("%ProgramFiles%\\x.exe", false)]
    public void UsesSelection_SeesTheSelectionsNamesOnly(string template, bool expected)
    {
        Assert.AreEqual(expected, VariableExpander.UsesSelection(template));
    }
}
