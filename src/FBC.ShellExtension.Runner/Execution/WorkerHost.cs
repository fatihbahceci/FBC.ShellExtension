using System.Globalization;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Execution;

namespace FBC.ShellExtension.Runner.Execution;

/// <summary>
/// The elevated side (ADR-0006): connects back to the runner that started it, runs the job
/// it receives, and streams every event back. It shows no UI; when anything goes wrong the
/// runner sees the pipe close and reports it.
/// </summary>
internal static class WorkerHost
{
    private const int ConnectTimeoutMs = 10_000;
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false);

    public static int Run(string pipeName, string parentProcessId)
    {
        if (!int.TryParse(parentProcessId, NumberStyles.None, CultureInfo.InvariantCulture, out int parentId))
        {
            return 2;
        }

        try
        {
            return RunAsync(pipeName, parentId).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // Nobody to show it to: the runner on the other end reports the broken pipe.
            return 1;
        }
    }

    private static async Task<int> RunAsync(string pipeName, int parentId)
    {
        // Identification only: whoever turns out to be the server must not be able to act as
        // this elevated process.
        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous,
            TokenImpersonationLevel.Identification);
        await client.ConnectAsync(ConnectTimeoutMs).ConfigureAwait(false);

        // The server must be the process named on the command line. That keeps out a pipe that
        // took the name after the runner gave it up; it does not make the job trustworthy: any
        // process of the same user decides what the runner sends (ADR-0006, trust).
        if (!PipePeer.ServerIs(client, parentId))
        {
            return 3;
        }

        using var reader = new StreamReader(client, s_utf8, false, 4096, leaveOpen: true);
        using var writer = new StreamWriter(client, s_utf8, 4096, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
        if (await reader.ReadLineAsync().ConfigureAwait(false) is not { } first
            || WorkerProtocol.Deserialize(first) is not WorkerJobMessage job)
        {
            return 4;
        }

        // Not disposed: the listening task may still cancel it after the job has ended.
        var cts = new CancellationTokenSource();
        var observer = new PipeObserver(writer, cts);
        Task listening = ListenAsync(reader, cts);

        JobSummary summary = await new JobExecutor(new ProcessLauncher())
            .RunAsync(job.Command, job.Processes, observer, cts.Token)
            .ConfigureAwait(false);
        observer.Send(new WorkerDoneMessage());
        await Task.WhenAny(listening, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
        return summary.AllSucceeded ? 0 : 1;
    }

    /// <summary>A cancel message, or the runner going away, cancels the job.</summary>
    private static async Task ListenAsync(StreamReader reader, CancellationTokenSource cts)
    {
        try
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                if (WorkerProtocol.Deserialize(line) is WorkerCancelMessage)
                {
                    cts.Cancel();
                }
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or WorkerProtocolException)
        {
        }

        cts.Cancel();
    }

    private sealed class PipeObserver(StreamWriter writer, CancellationTokenSource cts) : IExecutionObserver
    {
        private readonly Lock _lock = new();

        public void Started(int index, PlannedProcess process) => Send(new WorkerStartedMessage(index));

        public void Output(int index, string line) => Send(new WorkerOutputMessage(index, line));

        public void Finished(ProcessResult result) =>
            Send(new WorkerFinishedMessage(result.Index, result.Outcome, result.ExitCode, result.Message));

        public void Send(WorkerMessage message)
        {
            string line = WorkerProtocol.Serialize(message);
            bool broken = false;
            lock (_lock)
            {
                try
                {
                    writer.WriteLine(line);
                }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException)
                {
                    broken = true;
                }
            }

            // The runner is gone: stop the job rather than run it unwatched. Outside the lock,
            // because cancelling runs callbacks that report back through this method.
            if (broken)
            {
                cts.Cancel();
            }
        }
    }
}
