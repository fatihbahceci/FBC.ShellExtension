using System.Text.Json.Serialization;
using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;

namespace FBC.ShellExtension.Core.Execution;

[JsonConverter(typeof(CamelCaseEnumConverter<ProcessOutcome>))]
public enum ProcessOutcome
{
    Succeeded,
    Failed,

    /// <summary>Running when the user cancelled; its process tree was terminated.</summary>
    Cancelled,

    /// <summary>Still waiting when the user cancelled, or never started because elevation was refused.</summary>
    NotStarted,
}

/// <param name="Index">Position of the process in the plan.</param>
/// <param name="ExitCode">Null when the process did not run to an exit code, or Windows gave none back.</param>
/// <param name="Message">Why the process did not succeed; null when it did.</param>
public sealed record ProcessResult(int Index, PlannedProcess Process, ProcessOutcome Outcome, int? ExitCode, string? Message);

/// <summary>What a launcher reports for a process that did start.</summary>
/// <param name="ExitCode">Null in launch mode, and when a shell-executed request was handed to a program that was already running.</param>
/// <param name="StopIncomplete">Cancelled, but part of the process tree could not be stopped and may still run.</param>
public sealed record LaunchResult(int? ExitCode, string StandardError, bool Cancelled, bool StopIncomplete = false);

/// <summary>Starts one planned process. The Windows implementation lives in the runner; tests use a fake.</summary>
public interface IProcessLauncher
{
    /// <summary>
    /// Track mode: waits for the process and returns its exit code and stderr; lines go to
    /// <paramref name="output"/> when the command shows output. Launch mode: returns once the
    /// process has started. Cancellation terminates the process tree and returns Cancelled.
    /// </summary>
    /// <exception cref="ProcessStartException">The process could not be started.</exception>
    Task<LaunchResult> RunAsync(PlannedProcess process, CommandDefinition command, Action<string> output, CancellationToken cancellationToken);
}

public sealed class ProcessStartException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>
/// Receives progress from any thread, in parallel mode from several at once. Implementations
/// must be thread-safe and must not throw.
/// </summary>
public interface IExecutionObserver
{
    void Started(int index, PlannedProcess process);

    void Output(int index, string line);

    /// <summary>
    /// How far a long step on one item has got, for the built-in tools that read large files
    /// (ADR-0017). Processes do not report it.
    /// </summary>
    void Progress(int index, long done, long total)
    {
    }

    void Finished(ProcessResult result);
}

public sealed class JobSummary(IReadOnlyList<ProcessResult> results)
{
    public IReadOnlyList<ProcessResult> Results { get; } = results;

    public int Succeeded => Count(ProcessOutcome.Succeeded);

    public int Failed => Count(ProcessOutcome.Failed);

    public int Cancelled => Count(ProcessOutcome.Cancelled);

    public int NotStarted => Count(ProcessOutcome.NotStarted);

    public bool AllSucceeded => Results.Count > 0 && Succeeded == Results.Count;

    private int Count(ProcessOutcome outcome) => Results.Count(r => r.Outcome == outcome);
}

/// <summary>
/// Keeps about the last <paramref name="capacity"/> characters of a stream of lines, cut at a
/// line boundary; when the start was dropped, the text begins with "…".
/// </summary>
public sealed class TailBuffer(int capacity)
{
    private readonly System.Text.StringBuilder _text = new();
    private readonly Lock _lock = new();
    private bool _truncated;

    public void AppendLine(string line)
    {
        lock (_lock)
        {
            _text.Append(line).Append('\n');
            if (_text.Length <= capacity)
            {
                return;
            }

            int cut = _text.Length - capacity;
            while (cut < _text.Length && _text[cut - 1] != '\n')
            {
                cut++;
            }

            _text.Remove(0, cut);
            _truncated = true;
        }
    }

    public override string ToString()
    {
        lock (_lock)
        {
            return _truncated ? "…\n" + _text : _text.ToString();
        }
    }
}
