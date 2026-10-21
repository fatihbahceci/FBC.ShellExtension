using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Setup;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Manager;

/// <summary>
/// Checks ffmpeg and installs it, through winget or by downloading gyan.dev's build into the
/// program folder (ADR-0011). Nothing here runs without the user having asked in the editor.
/// </summary>
internal static class Ffmpeg
{
    private static readonly TimeSpan s_probeTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan s_installTimeout = TimeSpan.FromMinutes(30);

    public static string ToolsFolder => FfmpegInfo.ToolsFolder(AppContext.BaseDirectory);

    private static string? ToolsFolderIfPresent =>
        File.Exists(Path.Combine(ToolsFolder, FfmpegInfo.ExeName)) ? ToolsFolder : null;

    // ---- check -------------------------------------------------------------------------------

    public static ManagerExitCode Status(TextWriter output)
    {
        FfmpegStatus status = Probe();
        if (!status.Found)
        {
            output.WriteLine(Strings.FfmpegNotFound());
            return ManagerExitCode.Attention;
        }

        output.WriteLine(Strings.FfmpegFound(status.Path!, status.Version ?? "?"));
        output.WriteLine(status.Usable ? Strings.FfmpegAllEncoders() : Strings.FfmpegMissingEncoders(string.Join(", ", status.MissingEncoders)));
        return status.Usable ? ManagerExitCode.Success : ManagerExitCode.Attention;
    }

    /// <summary>The ffmpeg the runner would start: the same search order (ADR-0011).</summary>
    public static FfmpegStatus Probe()
    {
        string? exe = Resolve();
        if (exe is null)
        {
            return FfmpegStatus.NotFound;
        }

        string? versionOutput = Run(exe, "-version");
        string? encodersOutput = Run(exe, "-hide_banner -encoders");
        if (versionOutput is null || encodersOutput is null)
        {
            return new FfmpegStatus(exe, null, FfmpegInfo.RequiredEncoders);
        }

        return new FfmpegStatus(exe, FfmpegInfo.ParseVersion(versionOutput), FfmpegInfo.Missing(FfmpegInfo.ParseEncoders(encodersOutput)));
    }

    private static string? Resolve()
    {
        // The runner starts a tools-folder ffmpeg by its full path; for any other, Windows looks in
        // System32 and the Windows folder before PATH.
        IEnumerable<string> folders = ((string[])[Environment.SystemDirectory, Environment.GetFolderPath(Environment.SpecialFolder.Windows)])
            .Concat(SearchPath.Split(RegistryPath.Current(null)));
        if (ToolsFolderIfPresent is { } tools)
        {
            folders = folders.Prepend(tools);
        }

        foreach (string folder in folders)
        {
            try
            {
                string candidate = Path.Combine(Environment.ExpandEnvironmentVariables(folder), FfmpegInfo.ExeName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (ArgumentException)
            {
                // A PATH entry with characters a path cannot have.
            }
        }

        // winget links its packages here; normally on the user PATH already.
        string links = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WinGet\Links", FfmpegInfo.ExeName);
        return File.Exists(links) ? links : null;
    }

    /// <returns>Standard output and error together; null when it did not start or did not finish in time.</returns>
    private static string? Run(string exe, string arguments)
    {
        var start = new ProcessStartInfo(exe, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        try
        {
            using Process process = Process.Start(start)!;
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(s_probeTimeout))
            {
                process.Kill();
                return null;
            }

            return output.Result + errors.Result;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return null;
        }
    }

    // ---- winget ------------------------------------------------------------------------------

    public static ManagerExitCode InstallWithWinget(TextWriter output)
    {
        string? winget = FindWinget();
        if (winget is null)
        {
            output.WriteLine(Strings.FfmpegNoWinget());
            return ManagerExitCode.Failed;
        }

        output.WriteLine(Strings.FfmpegWingetRunning(FfmpegInfo.WingetPackageId));
        var start = new ProcessStartInfo(winget) { UseShellExecute = false, CreateNoWindow = true };
        foreach (string argument in FfmpegInfo.WingetInstallArguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)!;
        if (!process.WaitForExit(s_installTimeout))
        {
            process.Kill(entireProcessTree: true);
            output.WriteLine(Strings.FfmpegNotAnswering(winget));
            return ManagerExitCode.Failed;
        }

        if (!FfmpegInfo.IsWingetSuccess(process.ExitCode))
        {
            output.WriteLine(Strings.FfmpegWingetFailed(process.ExitCode));
            return ManagerExitCode.Failed;
        }

        // winget's own broadcast may not reach Explorer.
        Native.BroadcastEnvironmentChange();

        // winget can report success without leaving a working ffmpeg: the check decides.
        return Status(output);
    }

    private static string? FindWinget()
    {
        string alias = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WindowsApps\winget.exe");
        // A bare name is searched in the program folder, System32 and PATH: the manager's current
        // directory is System32 (Program.Main).
        foreach (string candidate in (string[])[alias, "winget.exe"])
        {
            try
            {
                using Process? process = Process.Start(new ProcessStartInfo(candidate, "--version")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                });

                if (process is null)
                {
                    continue;
                }

                if (!process.WaitForExit(s_probeTimeout))
                {
                    process.Kill();
                    continue;
                }

                if (process.ExitCode == 0)
                {
                    return candidate;
                }
            }
            catch (Win32Exception)
            {
                // Not there, or the alias is switched off.
            }
        }

        return null;
    }

    // ---- download ----------------------------------------------------------------------------

    public static ManagerExitCode Download(TextWriter output) => DownloadAsync(output).GetAwaiter().GetResult();

    private static readonly TimeSpan s_smallRequestTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_idleTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan s_downloadTimeout = TimeSpan.FromMinutes(25);
    private const string TempPrefix = "FBCTools-ffmpeg-";

    private static async Task<ManagerExitCode> DownloadAsync(TextWriter output)
    {
        RemoveLeftovers();

        // Timeout is per request here; the archive body has its own idle and overall limits.
        using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"{ProductInfo.Name}/{ProductInfo.Version}");

        string version = (await GetSmallAsync(http, FfmpegInfo.ReleaseVersionUrl).ConfigureAwait(false)).Trim();
        if (!FfmpegInfo.IsReleaseVersion(version))
        {
            output.WriteLine(Strings.FfmpegBadVersion(version.Length > 40 ? version[..40] : version));
            return ManagerExitCode.Failed;
        }

        // The checksum comes from gyan.dev, the archive from its GitHub mirror: two hosts.
        string? expected = FfmpegInfo.ParseSha256(await GetSmallAsync(http, FfmpegInfo.GyanSha256Url(version)).ConfigureAwait(false));
        if (expected is null)
        {
            output.WriteLine(Strings.FfmpegBadChecksum());
            return ManagerExitCode.Failed;
        }

        string archive = Path.Combine(Path.GetTempPath(), $"{TempPrefix}{Guid.NewGuid():N}.zip");
        try
        {
            if (!await TryDownloadAsync(http, FfmpegInfo.MirrorZipUrl(version), archive, version, output).ConfigureAwait(false)
                && !await TryDownloadAsync(http, FfmpegInfo.GyanZipUrl(version), archive, version, output).ConfigureAwait(false))
            {
                return ManagerExitCode.Failed;
            }

            string actual;
            await using (FileStream stream = File.OpenRead(archive))
            {
                actual = Convert.ToHexString(await SHA256.HashDataAsync(stream).ConfigureAwait(false));
            }

            if (actual != expected)
            {
                output.WriteLine(Strings.FfmpegBadChecksum());
                return ManagerExitCode.Failed;
            }

            if (!Extract(archive, output))
            {
                return ManagerExitCode.Failed;
            }
        }
        finally
        {
            File.Delete(archive);
        }

        output.WriteLine(Strings.FfmpegInstalled(ToolsFolder));
        return Status(output);
    }

    private static async Task<string> GetSmallAsync(HttpClient http, string url)
    {
        using var limit = new CancellationTokenSource(s_smallRequestTimeout);
        return await http.GetStringAsync(url, limit.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// One host's copy of the archive. A connection that goes quiet for a minute, or a download
    /// longer than the overall limit, gives up, so the other host gets its turn.
    /// </summary>
    private static async Task<bool> TryDownloadAsync(HttpClient http, string url, string target, string version, TextWriter output)
    {
        output.WriteLine(Strings.FfmpegDownloading(version, url));
        using var overall = new CancellationTokenSource(s_downloadTimeout);
        using var idle = CancellationTokenSource.CreateLinkedTokenSource(overall.Token);
        try
        {
            idle.CancelAfter(s_idleTimeout);
            using HttpResponseMessage response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, idle.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using Stream source = await response.Content.ReadAsStreamAsync(idle.Token).ConfigureAwait(false);
            await using FileStream file = File.Create(target);
            byte[] buffer = new byte[81920];
            int read;
            while ((read = await source.ReadAsync(buffer, idle.Token).ConfigureAwait(false)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read), idle.Token).ConfigureAwait(false);
                idle.CancelAfter(s_idleTimeout);
            }

            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException)
        {
            output.WriteLine(ex.Message);
            return false;
        }
    }

    /// <summary>Archives left by a run that was killed before it could clean up.</summary>
    private static void RemoveLeftovers()
    {
        foreach (string file in Directory.EnumerateFiles(Path.GetTempPath(), TempPrefix + "*.zip"))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Another run may be using it.
            }
        }
    }

    /// <summary>ffmpeg.exe and the licence texts go to tools\ffmpeg; nothing else of the archive is needed.</summary>
    private static bool Extract(string archive, TextWriter output)
    {
        using ZipArchive zip = ZipFile.OpenRead(archive);
        string? exeEntry = FfmpegInfo.FindExeEntry(zip.Entries.Select(entry => entry.FullName));
        if (exeEntry is null)
        {
            output.WriteLine(Strings.FfmpegNoExeInArchive());
            return false;
        }

        Directory.CreateDirectory(ToolsFolder);
        string exe = Path.Combine(ToolsFolder, FfmpegInfo.ExeName);
        zip.GetEntry(exeEntry)!.ExtractToFile(exe + ".new", overwrite: true);
        File.Move(exe + ".new", exe, overwrite: true);

        foreach (ZipArchiveEntry entry in zip.Entries.Where(e => e.Name is "LICENSE" or "README.txt"))
        {
            entry.ExtractToFile(Path.Combine(ToolsFolder, entry.Name == "LICENSE" ? "LICENSE.txt" : entry.Name), overwrite: true);
        }

        return true;
    }

    // ---- PATH --------------------------------------------------------------------------------

    /// <summary>Only on request: the runner finds the downloaded ffmpeg without it (ADR-0011).</summary>
    public static ManagerExitCode AddToPath(TextWriter output)
    {
        if (ToolsFolderIfPresent is null)
        {
            output.WriteLine(Strings.FfmpegPathNothingToAdd(ToolsFolder));
            return ManagerExitCode.Attention;
        }

        RegistryPath.SetUserValue(SearchPath.WithEntry(RegistryPath.UserValue(), ToolsFolder));
        output.WriteLine(Strings.FfmpegPathAdded(ToolsFolder));
        return ManagerExitCode.Success;
    }

    public static ManagerExitCode RemoveFromPath(TextWriter output)
    {
        string? current = RegistryPath.UserValue();
        if (SearchPath.Contains(current, ToolsFolder))
        {
            RegistryPath.SetUserValue(SearchPath.WithoutEntry(current, ToolsFolder));
        }

        output.WriteLine(Strings.FfmpegPathRemoved(ToolsFolder));
        return ManagerExitCode.Success;
    }
}
