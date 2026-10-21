using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Execution;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Tools;
using FBC.ShellExtension.Runner.Interop;

namespace FBC.ShellExtension.Runner.Execution;

/// <summary>Starts a planned process with System.Diagnostics.Process, as its command describes.</summary>
internal sealed class ProcessLauncher : IProcessLauncher
{
    private const int MaxCapturedChars = 32 * 1024;

    // A grandchild that inherited the stderr pipe keeps it open after the process exits
    // ("cmd /c start app"); the result does not wait for that grandchild.
    private static readonly TimeSpan s_streamGrace = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan s_killWait = TimeSpan.FromSeconds(5);

    // A redirected stream carries whatever bytes the program chose: most console programs use
    // the OEM code page (857 on a Turkish system), some UTF-8. The command says which (G-0104).
    private static readonly Encoding s_oem = CodePages.Oem;
    private static readonly Encoding s_ansi = CodePages.Ansi;
    private static readonly Encoding s_utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public async Task<LaunchResult> RunAsync(PlannedProcess planned, CommandDefinition command, Action<string> output, CancellationToken cancellationToken)
    {
        bool track = command.RunMode == RunMode.Track;
        bool capture = track && !command.UseShellExecute;

        // A console nobody can see would leave a prompt ("Overwrite? [y/N]", pause) waiting
        // forever, since the runner has no standard input of its own to pass on. Giving such a
        // process a closed stdin makes the prompt read end-of-file instead (ADR-0006).
        bool hiddenConsole = !command.UseShellExecute
            && (command.CreateNoWindow || command.WindowStyle == CommandWindowStyle.Hidden);

        // A downloaded ffmpeg is started by its full path: Windows would search System32 and the
        // Windows folder before PATH (ADR-0011).
        string fileName = FfmpegInfo.FromToolsFolder(planned.FileName, FfmpegInfo.ToolsFolder(AppContext.BaseDirectory), File.Exists)
            ?? planned.FileName;
        var info = new ProcessStartInfo(fileName)
        {
            Arguments = planned.Arguments,
            WorkingDirectory = planned.WorkingDirectory,
            UseShellExecute = command.UseShellExecute,
            CreateNoWindow = command.CreateNoWindow,
            WindowStyle = ToProcessWindowStyle(command.WindowStyle),
            RedirectStandardInput = hiddenConsole,
            RedirectStandardError = capture,
            RedirectStandardOutput = capture && command.ShowOutput,
        };

        if (capture)
        {
            Encoding encoding = command.OutputEncoding switch
            {
                CommandOutputEncoding.Utf8 => s_utf8,
                CommandOutputEncoding.Ansi => s_ansi,
                _ => s_oem,
            };
            info.StandardErrorEncoding = encoding;
            if (command.ShowOutput)
            {
                info.StandardOutputEncoding = encoding;
            }
        }

        // Elevation is the worker's job (ADR-0006); only the other shell verbs are passed on.
        if (command.UseShellExecute && !command.RunsElevated && !string.IsNullOrWhiteSpace(command.Verb))
        {
            info.Verb = command.Verb;
        }

        Process? process;
        try
        {
            process = Process.Start(info);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            throw new ProcessStartException(Strings.ExecStartFailed(planned.FileName, ex.Message), ex);
        }

        // Null: the shell handed the request to a program that was already running.
        if (process is null)
        {
            return new LaunchResult(null, "", false);
        }

        using (process)
        {
            if (hiddenConsole)
            {
                CloseInput(process);
            }

            if (!track)
            {
                return new LaunchResult(null, "", false);
            }

            // The job holds the whole tree, including descendants whose parent has already
            // exited, which Process.Kill(entireProcessTree) cannot find. Only possible when
            // this process created the child itself, not through the shell.
            using SafeJobHandle? job = command.UseShellExecute ? null : TryPutInJob(process);

            var stderr = new TailBuffer(MaxCapturedChars);
            Task errors = capture
                ? PumpAsync(process.StandardError, line =>
                {
                    stderr.AppendLine(line);
                    if (command.ShowOutput)
                    {
                        output(line);
                    }
                })
                : Task.CompletedTask;
            Task lines = capture && command.ShowOutput ? PumpAsync(process.StandardOutput, output) : Task.CompletedTask;

            bool cancelled = false;
            bool stopIncomplete = false;
            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                stopIncomplete = !Terminate(process, job);
            }

            await Task.WhenAny(Task.WhenAll(errors, lines), Task.Delay(s_streamGrace, CancellationToken.None)).ConfigureAwait(false);
            int? exitCode = !cancelled && process.HasExited ? process.ExitCode : null;
            return new LaunchResult(exitCode, stderr.ToString(), cancelled, stopIncomplete);
        }
    }

    private static void CloseInput(Process process)
    {
        try
        {
            process.StandardInput.Close();
        }
        catch (IOException)
        {
            // The process already ended, which is fine.
        }
    }

    private static SafeJobHandle? TryPutInJob(Process process)
    {
        SafeJobHandle job = Native.CreateJobObjectW(0, 0);
        if (job.IsInvalid || !Native.AssignProcessToJobObject(job, process.SafeHandle))
        {
            // Kill(entireProcessTree) is the fallback.
            job.Dispose();
            return null;
        }

        return job;
    }

    /// <returns>Whether the process ended; false when part of the tree could not be stopped.</returns>
    private static bool Terminate(Process process, SafeJobHandle? job)
    {
        bool complete = true;
        if (job is not null)
        {
            Native.TerminateJobObject(job, 1);
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Already gone.
        }
        catch (Exception ex) when (ex is AggregateException or Win32Exception or NotSupportedException)
        {
            // Some process in the tree is not ours to stop, typically one that elevated itself.
            complete = false;
        }

        try
        {
            return process.WaitForExit(s_killWait) && complete;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            return complete;
        }
    }

    private static async Task PumpAsync(StreamReader reader, Action<string> onLine)
    {
        try
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                onLine(line);
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            // The process was disposed while a grandchild still held the pipe.
        }
    }

    private static ProcessWindowStyle ToProcessWindowStyle(CommandWindowStyle style) => style switch
    {
        CommandWindowStyle.Hidden => ProcessWindowStyle.Hidden,
        CommandWindowStyle.Minimized => ProcessWindowStyle.Minimized,
        CommandWindowStyle.Maximized => ProcessWindowStyle.Maximized,
        _ => ProcessWindowStyle.Normal,
    };
}
