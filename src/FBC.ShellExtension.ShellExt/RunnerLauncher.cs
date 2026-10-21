using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Core.Menu;

namespace FBC.ShellExtension.ShellExt;

/// <summary>Writes the job file and starts the runner next to this DLL; never waits for it.</summary>
internal static class RunnerLauncher
{
    public static void Launch(ActionNode command)
    {
        string runner = System.IO.Path.Combine(Module.Directory, ProductInfo.RunnerFileName);
        string jobPath = JobFile.Write(RunJob.For(command));

        try
        {
            ProcessStarter.Start(runner, $"{CommandLine.Quote(runner)} {JobFile.Argument} {CommandLine.Quote(jobPath)}");
            Diagnostics.Trace($"InvokeCommand: started the runner for '{command.Label}' with {command.Items.Count} item(s)");
        }
        catch
        {
            try
            {
                File.Delete(jobPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Leaves a file in %TEMP%; the original failure is the one to report.
            }

            throw;
        }
    }
}
