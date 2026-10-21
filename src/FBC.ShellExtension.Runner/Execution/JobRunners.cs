using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Execution;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Runner.Interop;

namespace FBC.ShellExtension.Runner.Execution;

/// <summary>Runs a plan here or in an elevated worker; the window cannot tell which.</summary>
internal interface IJobRunner
{
    Task<JobSummary> RunAsync(CommandDefinition command, IReadOnlyList<PlannedProcess> plan, IExecutionObserver observer, CancellationToken cancellationToken);
}

internal sealed class LocalJobRunner : IJobRunner
{
    public Task<JobSummary> RunAsync(CommandDefinition command, IReadOnlyList<PlannedProcess> plan, IExecutionObserver observer, CancellationToken cancellationToken) =>
        new JobExecutor(new ProcessLauncher()).RunAsync(command, plan, observer, cancellationToken);
}

/// <summary>
/// Runs the plan in an elevated copy of the runner, so the whole job costs one UAC prompt and
/// stderr can still be captured (ADR-0006). The job goes over a named pipe rather than a file:
/// under Windows 11 administrator protection the worker is another account, with its own
/// %TEMP%. What the worker runs is whatever this process sends, so the approval is for the
/// user's own config, not for a particular command line (ADR-0006, trust).
/// </summary>
internal sealed class ElevatedJobRunner(Action<string> status) : IJobRunner
{
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false);

    public async Task<JobSummary> RunAsync(CommandDefinition command, IReadOnlyList<PlannedProcess> plan, IExecutionObserver observer, CancellationToken cancellationToken)
    {
        var results = new ProcessResult?[plan.Count];
        string pipeName = $"FBCTools.Runner.{Environment.ProcessId}.{Guid.NewGuid():N}";

        // FirstPipeInstance: if anything already owns this name, fail rather than share it.
        using NamedPipeServerStream server = NamedPipeServerStreamAcl.Create(pipeName, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.FirstPipeInstance, 0, 0, PipeAccess());
        status(Strings.RunnerWaitingForElevation());

        Process worker;
        try
        {
            worker = StartWorker(pipeName);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == Native.ERROR_CANCELLED)
        {
            return Complete(results, plan, observer, ProcessOutcome.NotStarted, Strings.ExecElevationRefused());
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return Complete(results, plan, observer, ProcessOutcome.Failed, Strings.ExecWorkerFailed(ex.Message));
        }

        using (worker)
        {
            try
            {
                await ConnectAsync(server, worker, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Disposing the pipe makes a worker that has not connected yet give up.
                return Complete(results, plan, observer, ProcessOutcome.NotStarted, Strings.ExecNotStarted());
            }
            catch (IOException ex)
            {
                return Complete(results, plan, observer, ProcessOutcome.Failed, Strings.ExecWorkerFailed(ex.Message));
            }

            using var reader = new StreamReader(server, s_utf8, false, 4096, leaveOpen: true);
            using var writer = new StreamWriter(server, s_utf8, 4096, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
            using var writeLock = new SemaphoreSlim(1, 1);
            Task? cancelSent = null;
            try
            {
                await SendAsync(writer, writeLock, new WorkerJobMessage(command, plan)).ConfigureAwait(false);
                using CancellationTokenRegistration onCancel = cancellationToken.Register(
                    () => cancelSent = SendAsync(writer, writeLock, new WorkerCancelMessage()));

                while (await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(false) is { } line)
                {
                    switch (WorkerProtocol.Deserialize(line))
                    {
                        case WorkerStartedMessage m when IsIndex(m.Index, plan):
                            observer.Started(m.Index, plan[m.Index]);
                            break;
                        case WorkerOutputMessage m when IsIndex(m.Index, plan):
                            observer.Output(m.Index, m.Line);
                            break;
                        case WorkerFinishedMessage m when IsIndex(m.Index, plan):
                            var result = new ProcessResult(m.Index, plan[m.Index], m.Outcome, m.ExitCode, m.Message);
                            results[m.Index] = result;
                            observer.Finished(result);
                            break;
                        case WorkerDoneMessage:
                            return Complete(results, plan, observer, ProcessOutcome.Failed, Strings.WorkerProtocolError("done"));
                        default:
                            throw new WorkerProtocolException(Strings.WorkerProtocolError(line));
                    }
                }

                // End of stream without "done": the worker died.
                string exit = worker.WaitForExit(TimeSpan.FromSeconds(2))
                    ? JobExecutor.FormatExitCode(worker.ExitCode)
                    : "?";
                return Complete(results, plan, observer, ProcessOutcome.Failed, Strings.ExecWorkerFailed(Strings.ExecExitCode(exit)));
            }
            catch (Exception ex) when (ex is IOException or WorkerProtocolException or ObjectDisposedException)
            {
                return Complete(results, plan, observer, ProcessOutcome.Failed, Strings.ExecWorkerFailed(ex.Message));
            }
            finally
            {
                // The writer cannot be disposed while a write is in flight.
                if (cancelSent is not null)
                {
                    await cancelSent.ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    /// The user and Administrators may connect; the worker has Administrators enabled even when
    /// it runs as another account. Nobody else, and nothing from the network: the default
    /// security would let any account open the only instance and block the real worker.
    /// </summary>
    private static PipeSecurity PipeAccess()
    {
        var security = new PipeSecurity();
        SecurityIdentifier user = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("The current user has no SID.");
        security.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));
        return security;
    }

    private static Process StartWorker(string pipeName)
    {
        string runner = Environment.ProcessPath ?? throw new InvalidOperationException("Environment.ProcessPath is null.");
        var info = new ProcessStartInfo(runner)
        {
            UseShellExecute = true,
            Verb = CommandDefinition.RunAsVerb,
            Arguments = string.Create(CultureInfo.InvariantCulture, $"{WorkerProtocol.WorkerArgument} {pipeName} {Environment.ProcessId}"),
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        return Process.Start(info) ?? throw new InvalidOperationException(Strings.ExecWorkerFailed("no process"));
    }

    /// <summary>
    /// Waits until the worker just started is connected. Another client that gets there first
    /// is disconnected and the wait goes on, rather than failing the job.
    /// </summary>
    private static async Task ConnectAsync(NamedPipeServerStream server, Process worker, CancellationToken cancellationToken)
    {
        while (true)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task connected = server.WaitForConnectionAsync(linked.Token);
            Task exited = worker.WaitForExitAsync(linked.Token);
            Task first = await Task.WhenAny(connected, exited).ConfigureAwait(false);
            linked.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            if (first != connected)
            {
                throw new IOException(Strings.ExecExitCode(JobExecutor.FormatExitCode(worker.ExitCode)));
            }

            await connected.ConfigureAwait(false);
            if (PipePeer.ClientIs(server, worker.Id))
            {
                return;
            }

            server.Disconnect();
        }
    }

    private static async Task SendAsync(StreamWriter writer, SemaphoreSlim writeLock, WorkerMessage message)
    {
        await writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await writer.WriteLineAsync(WorkerProtocol.Serialize(message)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // The worker is gone; the read loop reports it.
        }
        finally
        {
            writeLock.Release();
        }
    }

    private static bool IsIndex(int index, IReadOnlyList<PlannedProcess> plan) => (uint)index < (uint)plan.Count;

    /// <summary>Gives every process without a result the given outcome, and reports it.</summary>
    private static JobSummary Complete(ProcessResult?[] results, IReadOnlyList<PlannedProcess> plan, IExecutionObserver observer, ProcessOutcome outcome, string message)
    {
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] is null)
            {
                results[i] = new ProcessResult(i, plan[i], outcome, null, message);
                observer.Finished(results[i]!);
            }
        }

        return new JobSummary(results!);
    }
}
