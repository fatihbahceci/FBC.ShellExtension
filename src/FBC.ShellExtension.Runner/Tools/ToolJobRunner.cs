using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Execution;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Tools;
using FBC.ShellExtension.Runner.Execution;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// Runs a windowed built-in tool for the runner's progress window, which sees each item as a
/// "process": started, its output lines, how far it has got, and how it ended (ADR-0017).
/// Items run one after another on a worker thread.
/// </summary>
internal sealed class ToolJobRunner(BuiltinTool tool, ToolInput input) : IJobRunner
{
    public Task<JobSummary> RunAsync(CommandDefinition command, IReadOnlyList<PlannedProcess> plan, IExecutionObserver observer, CancellationToken cancellationToken) =>
        Task.Run(() => Run(plan, observer, cancellationToken), CancellationToken.None);

    private JobSummary Run(IReadOnlyList<PlannedProcess> plan, IExecutionObserver observer, CancellationToken cancellationToken)
    {
        var results = new List<ProcessResult>(plan.Count);
        for (int index = 0; index < plan.Count; index++)
        {
            PlannedProcess step = plan[index];
            ProcessResult result;
            if (cancellationToken.IsCancellationRequested)
            {
                result = new ProcessResult(index, step, ProcessOutcome.NotStarted, null, Strings.ExecNotStarted());
            }
            else
            {
                observer.Started(index, step);
                result = RunOne(index, step, observer, cancellationToken);
            }

            observer.Finished(result);
            results.Add(result);
        }

        return new JobSummary(results);
    }

    private ProcessResult RunOne(int index, PlannedProcess step, IExecutionObserver observer, CancellationToken cancellationToken)
    {
        try
        {
            FileTools.Run(tool, step.Items[0], input, new Report(observer, index), cancellationToken);
            return new ProcessResult(index, step, ProcessOutcome.Succeeded, null, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new ProcessResult(index, step, ProcessOutcome.Cancelled, null, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // ToolFailedException says why in the user's terms; IO and access errors in Windows'.
            return new ProcessResult(index, step, ProcessOutcome.Failed, null, ex.Message);
        }
    }

    /// <summary>Passes a tool's lines on, and its progress only when it moved by a tenth of a percent.</summary>
    private sealed class Report(IExecutionObserver observer, int index) : IToolReport
    {
        private long _lastPermille = -1;

        public void Output(string line) => observer.Output(index, line);

        public void Progress(long done, long total)
        {
            long permille = total > 0 ? done * 1000 / total : 1000;
            if (permille != _lastPermille)
            {
                _lastPermille = permille;
                observer.Progress(index, done, total);
            }
        }
    }
}
