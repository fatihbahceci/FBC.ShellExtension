using System.Diagnostics;
using System.Text;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Setup;

namespace FBC.ShellExtension.Editor;

/// <param name="Output">What the manager wrote, for showing to the user as it is.</param>
internal sealed record ManagerResult(ManagerExitCode ExitCode, string Output);

/// <summary>
/// Runs the manager next to the editor (Aşama 5): the editor does not register, merge or
/// restore anything itself, so the manager stays the one place that does.
/// </summary>
internal static class ManagerClient
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);

    public static string Path => System.IO.Path.Combine(AppContext.BaseDirectory, ProductInfo.ManagerFileName);

    public static bool IsAvailable => File.Exists(Path);

    public static ManagerResult Run(params string[] arguments) => Run(s_timeout, CancellationToken.None, arguments);

    /// <summary>Cancelling ends the manager and everything it started.</summary>
    public static ManagerResult Run(TimeSpan timeout, CancellationToken cancellation, params string[] arguments)
    {
        if (!IsAvailable)
        {
            return new ManagerResult(ManagerExitCode.Failed, Strings.EditorManagerMissing(Path));
        }

        var start = new ProcessStartInfo(Path)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        try
        {
            using Process process = Process.Start(start)!;
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> errors = process.StandardError.ReadToEndAsync();
            DateTime deadline = DateTime.UtcNow + timeout;
            while (!process.WaitForExit(200))
            {
                if (cancellation.IsCancellationRequested || DateTime.UtcNow >= deadline)
                {
                    process.Kill(entireProcessTree: true);
                    return new ManagerResult(ManagerExitCode.Failed,
                        cancellation.IsCancellationRequested ? Strings.EditorManagerCancelled() : Strings.EditorManagerTimeout());
                }
            }

            string text = (output.Result + errors.Result).Trim();
            return new ManagerResult((ManagerExitCode)process.ExitCode, text);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException or InvalidOperationException)
        {
            // Blocked by policy or antivirus, for example: the editor still opens.
            return new ManagerResult(ManagerExitCode.Failed, Strings.ManagerFailed(ex.Message));
        }
    }
}
