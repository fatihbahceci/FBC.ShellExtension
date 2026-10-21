using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Restart;
using FBC.ShellExtension.Restarter.Interop;

namespace FBC.ShellExtension.Restarter;

/// <summary>
/// Save the windows, stop Explorer through Restart Manager, run the caller's command, bring
/// Explorer back, open the windows again (ADR-0008). Once Explorer is stopped, every path —
/// a failed command, Ctrl+C, an exception — still ends with Explorer running.
/// </summary>
internal sealed class ExplorerRestart(RestartOptions options)
{
    private static readonly TimeSpan s_saveTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan s_exitTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan s_restartTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan s_startTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_settleTime = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan s_restoreTimeout = TimeSpan.FromMinutes(3);

    private volatile bool _cancelled;
    private volatile bool _warnings;
    private volatile bool _stopped;
    private Process? _command;

    public RestartExitCode Run()
    {
        Console.CancelKeyPress += OnCancel;

        // Closing the console window ends the process after a few seconds, finally blocks or
        // not; with Explorer stopped there is just time to start it.
        using PosixSignalRegistration closing = PosixSignalRegistration.Create(PosixSignal.SIGHUP, OnConsoleClosing);

        ExplorerSnapshot snapshot = options.Restore ? SaveWindows() : ExplorerSnapshot.Empty;

        List<Process> explorers = ExplorerProcesses();
        RestartManagerSession? session = null;
        RestartExitCode result = RestartExitCode.Success;
        try
        {
            if (explorers.Count == 0)
            {
                Info(Strings.RestarterNotRunning());
            }
            else
            {
                Info(Strings.RestarterStopping());
                session = RestartManagerSession.Start();
                session.RegisterProcesses(explorers);
                if (Stop(session, explorers) is { } failure)
                {
                    Error(Strings.RestarterStopFailed(failure));
                    return RestartExitCode.StopFailed;
                }

                _stopped = true;
                Info(Strings.RestarterStopped());
            }

            ReportOtherUsers();
            if (options.Command.Count > 0 && !_cancelled)
            {
                result = RunCommand();
            }

            if (_cancelled)
            {
                Info(Strings.RestarterCancelled());
            }
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Error(Strings.RestarterStopFailed(ex.Message));
            result = RestartExitCode.StopFailed;
        }
        finally
        {
            BringBack(session, _stopped);
            session?.Dispose();
        }

        if (result != RestartExitCode.StopFailed && options.Restore && snapshot.Windows.Count > 0 && ShellIsUp())
        {
            RestoreWindows(snapshot);
        }

        Info(_warnings ? Strings.RestarterDoneWithWarnings() : Strings.RestarterDone());
        return result != RestartExitCode.Success ? result
            : _warnings ? RestartExitCode.Warnings
            : RestartExitCode.Success;
    }

    private void OnCancel(object? sender, ConsoleCancelEventArgs e)
    {
        // Explorer may be stopped: the process must not end before it is started again.
        e.Cancel = true;
        _cancelled = true;
        try
        {
            _command?.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or AggregateException)
        {
        }
    }

    private void OnConsoleClosing(PosixSignalContext context)
    {
        if (_stopped && !ShellIsUp())
        {
            StartShellDirectly();
        }
    }

    private static List<Process> ExplorerProcesses()
    {
        int session = Process.GetCurrentProcess().SessionId;
        return [.. Process.GetProcessesByName("explorer").Where(process => process.SessionId == session)];
    }

    private ExplorerSnapshot SaveWindows()
    {
        Info(Strings.RestarterSaving());
        ExplorerSnapshot snapshot = ExplorerSnapshot.Empty;
        bool finished = StaThread.Run(() =>
        {
            using var windows = new FolderWindows();
            snapshot = windows.Snapshot();
        }, s_saveTimeout, out Exception? error);

        if (!finished)
        {
            Warn(Strings.RestarterSaveTimeout());
            return ExplorerSnapshot.Empty;
        }

        if (error is not null)
        {
            Warn(Strings.RestarterSaveFailed(error.Message));
            return ExplorerSnapshot.Empty;
        }

        Info(Strings.RestarterSaved(snapshot.Windows.Count, snapshot.TabCount));
        return snapshot;
    }

    /// <returns>Null when every Explorer process ended; otherwise why not.</returns>
    private static string? Stop(RestartManagerSession session, List<Process> explorers)
    {
        // Politely first: Explorer gets the end-session messages and saves its state.
        int error = session.Shutdown(force: false);
        if (error == 0 && AllExited(explorers))
        {
            return null;
        }

        error = session.Shutdown(force: true);
        if (error != 0)
        {
            return new Win32Exception(error).Message;
        }

        return AllExited(explorers) ? null : new Win32Exception(Native.ERROR_FAIL_SHUTDOWN).Message;
    }

    private static bool AllExited(List<Process> processes) =>
        processes.All(process => process.WaitForExit(s_exitTimeout));

    /// <summary>With Explorer stopped, whatever else still has the files open keeps the old version (ADR-0008).</summary>
    private void ReportOtherUsers()
    {
        foreach (string file in options.Files)
        {
            try
            {
                using RestartManagerSession lookup = RestartManagerSession.Start();
                lookup.RegisterFiles([file]);
                List<ResourceUser> users = [.. lookup.Users().Where(user => !user.Explorer)];
                if (users.Count == 0)
                {
                    continue;
                }

                Warn(Strings.RestarterHolders(Path.GetFileName(file)));
                users.ForEach(user => Console.WriteLine(Strings.RestarterHolder(user.Name, user.ProcessId)));
            }
            catch (Win32Exception ex)
            {
                Warn(ex.Message);
            }
        }
    }

    private RestartExitCode RunCommand()
    {
        var start = new ProcessStartInfo(options.Command[0]) { UseShellExecute = false };
        foreach (string argument in options.Command.Skip(1))
        {
            start.ArgumentList.Add(argument);
        }

        Info(Strings.RestarterRunning(string.Join(' ', options.Command)));
        try
        {
            _command = Process.Start(start);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Error(Strings.RestarterCommandStartFailed(ex.Message));
            return RestartExitCode.CommandFailed;
        }

        if (_command is null)
        {
            return RestartExitCode.CommandFailed;
        }

        using (_command)
        {
            _command.WaitForExit();
            int exitCode = _command.ExitCode;
            _command = null;
            if (exitCode != 0)
            {
                Error(Strings.RestarterCommandFailed(exitCode));
                return RestartExitCode.CommandFailed;
            }
        }

        return RestartExitCode.Success;
    }

    private static bool ShellIsUp() =>
        Native.FindWindow("Shell_TrayWnd", null) != 0 && Native.GetShellWindow() != 0;

    private static bool WaitForShell(TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (!ShellIsUp())
        {
            if (DateTime.UtcNow >= deadline)
            {
                return false;
            }

            Thread.Sleep(250);
        }

        return true;
    }

    private void BringBack(RestartManagerSession? session, bool stopped)
    {
        // The shell lives on after this process; it must not inherit a caller's output pipe,
        // or whoever reads that pipe waits for its end as long as Explorer runs.
        foreach (int standard in (int[])[Native.STD_INPUT_HANDLE, Native.STD_OUTPUT_HANDLE, Native.STD_ERROR_HANDLE])
        {
            Native.SetHandleInformation(Native.GetStdHandle(standard), Native.HANDLE_FLAG_INHERIT, 0);
        }

        if (ShellIsUp())
        {
            // Winlogon restarts a shell that it thinks crashed, possibly before the command
            // replaced anything.
            if (stopped)
            {
                Warn(Strings.RestarterCameBack());
            }

            return;
        }

        Info(Strings.RestarterStarting());
        if (session is not null)
        {
            session.Restart();
            if (WaitForShell(s_restartTimeout))
            {
                return;
            }

            Warn(Strings.RestarterStartedDirectly());
        }

        if (StartShellDirectly() is { } failure)
        {
            Error(failure);
        }

        if (!WaitForShell(s_startTimeout))
        {
            Warn(Strings.RestarterShellTimeout((int)s_startTimeout.TotalSeconds));
        }
    }

    /// <returns>Null when explorer.exe was started; otherwise why not.</returns>
    private static string? StartShellDirectly()
    {
        try
        {
            // Through the shell, so no handle of this process is inherited.
            string explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            Process.Start(new ProcessStartInfo(explorer) { UseShellExecute = true })?.Dispose();
            return null;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return ex.Message;
        }
    }

    private void RestoreWindows(ExplorerSnapshot snapshot)
    {
        // The shell answers before its window list is ready.
        Thread.Sleep(s_settleTime);
        bool finished = StaThread.Run(() =>
        {
            using FolderWindows windows = OpenFolderWindows();
            List<LiveTab> open = windows.Tabs();
            List<IReadOnlyList<byte[]>> openFolders =
                [.. open.GroupBy(tab => tab.Frame).Select(frame => (IReadOnlyList<byte[]>)[.. frame.Select(tab => tab.IdList)])];
            open.ForEach(tab => tab.Dispose());

            IReadOnlyList<SavedWindow> missing = RestorePlanner.Missing(snapshot, openFolders);
            int alreadyOpen = snapshot.Windows.Count - missing.Count;
            if (alreadyOpen > 0)
            {
                Info(Strings.RestarterAlreadyOpen(alreadyOpen));
            }

            if (missing.Count > 0)
            {
                Info(Strings.RestarterRestoring(missing.Count));
                new WindowRestorer(windows, Warn).Restore(missing);
            }
        }, s_restoreTimeout, out Exception? error);

        if (!finished)
        {
            Warn(Strings.RestarterRestoreTimeout());
        }
        else if (error is not null)
        {
            Warn(Strings.RestarterSaveFailed(error.Message));
        }
    }

    /// <summary>The window list of a shell that has just started may take a moment to register.</summary>
    private static FolderWindows OpenFolderWindows()
    {
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (true)
        {
            try
            {
                return new FolderWindows();
            }
            catch (Exception) when (DateTime.UtcNow < deadline)
            {
                Thread.Sleep(500);
            }
        }
    }

    private static void Info(string message) => Console.WriteLine(message);

    private void Warn(string message)
    {
        _warnings = true;
        Console.WriteLine(message);
    }

    private void Error(string message)
    {
        _warnings = true;
        Console.Error.WriteLine(message);
    }
}
