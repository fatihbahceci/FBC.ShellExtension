using System.Globalization;
using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Core.Execution;

/// <summary>
/// Runs a plan: one after another, or for <see cref="MultiSelectMode.PerFileParallel"/> up to
/// <see cref="CommandDefinition.MaxParallelism"/> at a time. Decides each outcome (ADR-0006):
/// exit code 0 succeeds; otherwise stderr is the message, or "An error occurred (exit code N)"
/// when stderr is empty. One process failing never stops the others; cancelling does.
/// </summary>
public sealed class JobExecutor(IProcessLauncher launcher)
{
    public async Task<JobSummary> RunAsync(
        CommandDefinition command,
        IReadOnlyList<PlannedProcess> plan,
        IExecutionObserver observer,
        CancellationToken cancellationToken)
    {
        var results = new ProcessResult[plan.Count];
        bool parallel = command.RunMode == RunMode.Track
            && command.MultiSelect == MultiSelectMode.PerFileParallel
            && plan.Count > 1;

        if (parallel)
        {
            int degree = Math.Max(1, command.MaxParallelism ?? Environment.ProcessorCount);
            using var gate = new SemaphoreSlim(degree);
            await Task.WhenAll(plan.Select((process, index) => Task.Run(async () =>
            {
                try
                {
                    await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    results[index] = Finish(observer, NotStarted(index, process));
                    return;
                }

                try
                {
                    results[index] = await RunOneAsync(index, process, command, observer, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    gate.Release();
                }
            }))).ConfigureAwait(false);
        }
        else
        {
            for (int i = 0; i < plan.Count; i++)
            {
                results[i] = await RunOneAsync(i, plan[i], command, observer, cancellationToken).ConfigureAwait(false);
            }
        }

        return new JobSummary(results);
    }

    private async Task<ProcessResult> RunOneAsync(
        int index,
        PlannedProcess process,
        CommandDefinition command,
        IExecutionObserver observer,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Finish(observer, NotStarted(index, process));
        }

        int length = CommandLine.Length(process.FileName, process.Arguments);
        if (!command.UseShellExecute && length > CommandLine.MaxLength)
        {
            string message = command.MultiSelect == MultiSelectMode.AllAtOnce
                ? Strings.ExecCommandLineTooLong(length, CommandLine.MaxLength)
                : Strings.ExecCommandLineTooLongPerFile(length, CommandLine.MaxLength);
            return Finish(observer, new ProcessResult(index, process, ProcessOutcome.Failed, null, message));
        }

        observer.Started(index, process);
        ProcessResult result;
        try
        {
            LaunchResult launched = await launcher
                .RunAsync(process, command, line => observer.Output(index, line), cancellationToken)
                .ConfigureAwait(false);
            result = Decide(index, process, launched);
        }
        catch (ProcessStartException ex)
        {
            result = new ProcessResult(index, process, ProcessOutcome.Failed, null, ex.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            result = new ProcessResult(index, process, ProcessOutcome.Cancelled, null, Strings.ExecCancelled());
        }
        catch (Exception ex)
        {
            result = new ProcessResult(index, process, ProcessOutcome.Failed, null, Strings.ExecUnexpected(ex.Message));
        }

        return Finish(observer, result);
    }

    private static ProcessResult Decide(int index, PlannedProcess process, LaunchResult launched)
    {
        if (launched.Cancelled)
        {
            string reason = launched.StopIncomplete ? Strings.ExecCancelledIncomplete() : Strings.ExecCancelled();
            return new ProcessResult(index, process, ProcessOutcome.Cancelled, launched.ExitCode, reason);
        }

        if (launched.ExitCode is null or 0)
        {
            return new ProcessResult(index, process, ProcessOutcome.Succeeded, launched.ExitCode, null);
        }

        string stderr = launched.StandardError.Trim();
        string message = stderr.Length > 0 ? stderr : Strings.ExecExitCode(FormatExitCode(launched.ExitCode.Value));
        return new ProcessResult(index, process, ProcessOutcome.Failed, launched.ExitCode, message);
    }

    private static ProcessResult NotStarted(int index, PlannedProcess process) =>
        new(index, process, ProcessOutcome.NotStarted, null, Strings.ExecNotStarted());

    private static ProcessResult Finish(IExecutionObserver observer, ProcessResult result)
    {
        observer.Finished(result);
        return result;
    }

    /// <summary>Decimal, plus hex for the NTSTATUS-style codes a crash leaves (-1073741819 is 0xC0000005).</summary>
    public static string FormatExitCode(int exitCode) =>
        exitCode is < 0 or > 0xFFFF
            ? string.Create(CultureInfo.InvariantCulture, $"{exitCode} (0x{exitCode:X8})")
            : exitCode.ToString(CultureInfo.InvariantCulture);
}
