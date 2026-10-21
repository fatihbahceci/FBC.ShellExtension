using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class FfmpegTests
{
    private const string EncodersOutput = """
        Encoders:
         V..... = Video
         A..... = Audio
         ------
         V....D libx264              libx264 H.264 / AVC / MPEG-4 AVC (codec h264)
         A....D aac                  AAC (Advanced Audio Coding)
         A....D libmp3lame           libmp3lame MP3 (MPEG audio layer 3) (codec mp3)
         V....D png                  PNG (Portable Network Graphics) image
        """;

    [TestMethod]
    public void Version_ComesFromTheFirstLine()
    {
        Assert.AreEqual("9.0.2-full_build-www.gyan.dev",
            FfmpegInfo.ParseVersion("ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026\r\nbuilt with gcc"));
        Assert.IsNull(FfmpegInfo.ParseVersion("not ffmpeg"));
    }

    [TestMethod]
    public void Encoders_AreReadFromTheTableOnly()
    {
        IReadOnlySet<string> encoders = FfmpegInfo.ParseEncoders(EncodersOutput.Replace("\n", "\r\n"));

        Assert.IsTrue(encoders.SetEquals(["libx264", "aac", "libmp3lame", "png"]));
        Assert.IsEmpty(FfmpegInfo.Missing(encoders));
        CollectionAssert.AreEqual(new[] { "libx264" }, FfmpegInfo.Missing(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "aac", "LIBMP3LAME" }).ToArray());
    }

    [TestMethod]
    public void Status_IsUsableOnlyWithEveryEncoder()
    {
        Assert.IsFalse(FfmpegStatus.NotFound.Usable);
        Assert.IsTrue(new FfmpegStatus(@"C:\ffmpeg.exe", "9", []).Usable);
        Assert.IsFalse(new FfmpegStatus(@"C:\ffmpeg.exe", "9", ["libx264"]).Usable);
    }

    [TestMethod]
    public void Download_UrlsChecksumsAndEntries()
    {
        Assert.AreEqual("https://github.com/GyanD/codexffmpeg/releases/download/9.0.2/ffmpeg-9.0.2-essentials_build.zip", FfmpegInfo.MirrorZipUrl("9.0.2"));
        Assert.AreEqual("https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-9.0.2-essentials_build.zip.sha256", FfmpegInfo.GyanSha256Url("9.0.2"));
        Assert.IsTrue(FfmpegInfo.IsReleaseVersion("9.0"));
        Assert.IsFalse(FfmpegInfo.IsReleaseVersion("2026-10-19-git-2344518cc5"));
        Assert.AreEqual(new string('A', 64), FfmpegInfo.ParseSha256(new string('a', 64) + "  ffmpeg.zip\n"));
        Assert.IsNull(FfmpegInfo.ParseSha256("<html>"));
        Assert.AreEqual("ffmpeg-9.0.2-essentials_build/bin/ffmpeg.exe",
            FfmpegInfo.FindExeEntry(["ffmpeg-9.0.2-essentials_build/bin/ffprobe.exe", "ffmpeg-9.0.2-essentials_build/bin/ffmpeg.exe"]));
    }

    [TestMethod]
    public void Winget_TreatsAlreadyInstalledAsSuccess()
    {
        Assert.IsTrue(FfmpegInfo.IsWingetSuccess(0));
        Assert.IsTrue(FfmpegInfo.IsWingetSuccess(-1978335189));
        Assert.IsFalse(FfmpegInfo.IsWingetSuccess(1));
        CollectionAssert.Contains(FfmpegInfo.WingetInstallArguments.ToArray(), "Gyan.FFmpeg");
    }

    [TestMethod]
    public void Merge_PutsToolsFirstThenRegistryThenTheRest()
    {
        string merged = SearchPath.Merge(@"C:\Windows\system32;C:\Windows", @"C:\Users\u\Links\", @"C:\Windows;D:\host\bin", @"C:\app\tools\ffmpeg");

        Assert.AreEqual(@"C:\app\tools\ffmpeg;C:\Windows\system32;C:\Windows;C:\Users\u\Links\;D:\host\bin", merged);
        Assert.AreEqual(@"C:\a", SearchPath.Merge(null, null, @"C:\a;c:\A\", null));
    }

    [TestMethod]
    public void UserPath_GainsAndLosesExactlyOneEntry()
    {
        const string path = @"%USERPROFILE%\bin;C:\tools";

        Assert.AreEqual(@"%USERPROFILE%\bin;C:\tools;C:\app\tools\ffmpeg", SearchPath.WithEntry(path, @"C:\app\tools\ffmpeg"));
        Assert.AreEqual(path, SearchPath.WithEntry(path, @"c:\TOOLS\"));
        Assert.AreEqual(@"%USERPROFILE%\bin", SearchPath.WithoutEntry(path, @"C:\tools\"));
        Assert.IsTrue(SearchPath.Contains(path, @"C:\Tools"));
    }

    [TestMethod]
    [DataRow("ffmpeg", true)]
    [DataRow("FFMPEG.exe", true)]
    [DataRow("ffprobe", false)]
    [DataRow(@"C:\bin\ffmpeg.exe", false)]
    [DataRow("%ComSpec%", false)]
    public void BareNames_AreTakenFromTheToolsFolder(string fileName, bool expected)
    {
        string? found = FfmpegInfo.FromToolsFolder(fileName, "tools", path => path.EndsWith("ffmpeg.exe", StringComparison.OrdinalIgnoreCase));

        Assert.AreEqual(expected, found is not null);
    }
}
