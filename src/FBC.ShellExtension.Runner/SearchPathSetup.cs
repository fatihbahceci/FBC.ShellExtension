using FBC.ShellExtension.Core.Tools;
using Microsoft.Win32;

namespace FBC.ShellExtension.Runner;

/// <summary>
/// The runner's own PATH, which its commands are found through (ADR-0011). The PATH it inherits
/// is its host's, and that may predate an ffmpeg installed since: an Explorer that missed the
/// change, or another program's Open dialog. So the current machine and user entries are read
/// from the registry, and a downloaded ffmpeg in the program folder comes first.
/// </summary>
internal static class SearchPathSetup
{
    public static void Apply()
    {
        try
        {
            using RegistryKey? machine = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment");
            using RegistryKey? user = Registry.CurrentUser.OpenSubKey("Environment");
            string tools = FfmpegInfo.ToolsFolder(AppContext.BaseDirectory);
            string? toolsIfPresent = File.Exists(Path.Combine(tools, FfmpegInfo.ExeName)) ? tools : null;

            // Process scope only: CreateProcess searches the caller's PATH for a bare "ffmpeg",
            // and children inherit it. The user's environment is not touched.
            Environment.SetEnvironmentVariable("PATH", SearchPath.Merge(
                machine?.GetValue("Path") as string,
                user?.GetValue("Path") as string,
                Environment.GetEnvironmentVariable("PATH"),
                toolsIfPresent));
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // The inherited PATH stays; commands are found as before.
        }
    }
}
