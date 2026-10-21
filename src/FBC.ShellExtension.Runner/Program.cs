using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Execution;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Runner.Execution;
using FBC.ShellExtension.Runner.Tools;
using FBC.ShellExtension.Runner.UI;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner;

/// <summary>
/// Started by the shell extension with <c>--job &lt;file&gt;</c>, or by itself, elevated, with
/// <c>--worker &lt;pipe&gt; &lt;pid&gt;</c> for commands that run as administrator (ADR-0006).
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            // The runner inherits the current directory of whatever showed the menu, even another
            // program's Open dialog. A bare file name such as "ffmpeg" is searched there first, so
            // a planted ffmpeg.exe would run. System32 holds nothing a user can plant.
            Environment.CurrentDirectory = Environment.SystemDirectory;

            // Before both paths below: an elevated worker starts with a fresh environment.
            SearchPathSetup.Apply();

            return args switch
            {
                [WorkerProtocol.WorkerArgument, var pipe, var parent] => WorkerHost.Run(pipe, parent),
                [JobFile.Argument, var jobPath] => RunJob(jobPath),
                _ => ShowMessage(Strings.RunnerUsage(), MB_ICONINFORMATION, 2),
            };
        }
        catch (Exception ex)
        {
            return ShowMessage(Strings.RunnerFatal(ex.Message), MB_ICONERROR, 3);
        }
    }

    private static int RunJob(string jobPath)
    {
        RunJob job;
        try
        {
            job = JobFile.ReadAndDelete(jobPath);
        }
        catch (JobFileException ex)
        {
            return ShowMessage(ex.Message, MB_ICONERROR, 1);
        }

        if (job.Tool is { } tool)
        {
            return ToolRunner.Run(job, tool);
        }

        // Expanded here, as the user: an elevated worker may be another account (ADR-0006).
        IReadOnlyList<PlannedProcess> plan = CommandPlanner.Plan(job.Command, job.Items);

        if (job.Command.RunMode == RunMode.Launch)
        {
            return Launch(job, plan);
        }

        var window = new RunnerWindow(job, plan, VersionMismatch(job));
        IJobRunner runner = job.Command.RunsElevated ? new ElevatedJobRunner(window.Status) : new LocalJobRunner();
        return window.Run(runner);
    }

    /// <summary>Launch mode: no window, only a message when something could not be started.</summary>
    private static int Launch(RunJob job, IReadOnlyList<PlannedProcess> plan)
    {
        IJobRunner runner = job.Command.RunsElevated ? new ElevatedJobRunner(_ => { }) : new LocalJobRunner();
        JobSummary summary = runner.RunAsync(job.Command, plan, NullObserver.Instance, CancellationToken.None).GetAwaiter().GetResult();

        // Explorer keeps an old DLL until it restarts (G-0101); say so even when all went well.
        string? mismatch = VersionMismatch(job);
        if (summary.Failed == 0)
        {
            if (mismatch is not null)
            {
                MessageBoxW(0, mismatch, Strings.RunnerWindowTitle(job.Label), MB_ICONINFORMATION);
            }

            return 0;
        }

        string report = SummaryFormatter.Report(summary);
        MessageBoxW(0, mismatch is null ? report : $"{report}\n\n{mismatch}", Strings.RunnerWindowTitle(job.Label), MB_ICONERROR);
        return 1;
    }

    /// <summary>The warning to show when the job came from a different shell extension build, or null.</summary>
    internal static string? VersionMismatch(RunJob job) =>
        job.ShellExtVersion is { } shellExt && shellExt != ProductInfo.InformationalVersion
            ? Strings.RunnerVersionMismatch(shellExt, ProductInfo.InformationalVersion)
            : null;

    private static int ShowMessage(string text, uint icon, int exitCode)
    {
        MessageBoxW(0, text, ProductInfo.Name, icon);
        return exitCode;
    }

    private sealed class NullObserver : IExecutionObserver
    {
        public static NullObserver Instance { get; } = new();

        public void Started(int index, PlannedProcess process)
        {
        }

        public void Output(int index, string line)
        {
        }

        public void Finished(ProcessResult result)
        {
        }
    }
}
