using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Core.Execution;

/// <summary>The text the runner shows for results; kept here so tests pin it down.</summary>
public static class SummaryFormatter
{
    private const int ReportLines = 3;

    /// <summary>"3 succeeded / 1 failed", plus the cancelled and not-started counts when they are not zero.</summary>
    public static string Headline(JobSummary summary)
    {
        string headline = Strings.SummaryHeadline(summary.Succeeded, summary.Failed);
        if (summary.Cancelled > 0)
        {
            headline += Strings.SummaryCancelled(summary.Cancelled);
        }

        if (summary.NotStarted > 0)
        {
            headline += Strings.SummaryNotStarted(summary.NotStarted);
        }

        return headline;
    }

    /// <summary>The item a process ran on, or how many when it ran on several.</summary>
    public static string Subject(PlannedProcess process) =>
        process.Items.Count == 1 ? process.Items[0].Path : Strings.SummaryItems(process.Items.Count);

    /// <summary>One log line for a finished process. A multi-line message is indented under it.</summary>
    public static string Line(ProcessResult result)
    {
        string subject = Subject(result.Process);
        string message = Indent(result.Message ?? "");
        return result.Outcome switch
        {
            ProcessOutcome.Succeeded => Strings.LogSucceeded(subject),
            ProcessOutcome.Failed => Strings.LogFailed(subject, message),
            ProcessOutcome.Cancelled => Strings.LogCancelled(subject),
            _ when HasOwnReason(result) => Strings.LogNotStartedReason(subject, message),
            _ => Strings.LogNotStarted(subject),
        };
    }

    /// <summary>A process that was not started for a reason other than the user cancelling, such as refused elevation.</summary>
    public static bool HasOwnReason(ProcessResult result) =>
        result.Outcome == ProcessOutcome.NotStarted
        && result.Message is { } message
        && message != Strings.ExecNotStarted();

    /// <summary>
    /// Headline, a line per failure with the last few lines of its message (the full text is
    /// in the log already), and each distinct reason a process was not started.
    /// </summary>
    public static string Report(JobSummary summary)
    {
        var text = new System.Text.StringBuilder(Headline(summary));
        foreach (ProcessResult failure in summary.Results.Where(r => r.Outcome == ProcessOutcome.Failed))
        {
            text.Append('\n').Append(Line(failure with { Message = LastLines(failure.Message ?? "") }));
        }

        foreach (string reason in summary.Results.Where(HasOwnReason).Select(r => r.Message!).Distinct())
        {
            text.Append('\n').Append("– ").Append(reason);
        }

        return text.ToString();
    }

    private static string LastLines(string message)
    {
        string[] lines = message.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.Length <= ReportLines
            ? string.Join('\n', lines)
            : "…\n" + string.Join('\n', lines[^ReportLines..]);
    }

    private static string Indent(string message) =>
        message.Replace("\r\n", "\n").Replace("\n", "\n    ");
}
