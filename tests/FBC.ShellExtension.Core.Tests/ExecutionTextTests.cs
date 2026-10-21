using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Execution;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class ExecutionTextTests
{
    private static readonly PlannedProcess s_one = new("a.exe", "", @"C:\in", [File(@"C:\in\a.txt")]);
    private static readonly PlannedProcess s_many = new("a.exe", "", @"C:\in", [File(@"C:\in\a.txt"), File(@"C:\in\b.txt")]);

    [TestMethod]
    public void Report_ListsTheFailuresUnderTheHeadline()
    {
        var summary = new JobSummary(
        [
            new ProcessResult(0, s_one, ProcessOutcome.Succeeded, 0, null),
            new ProcessResult(1, s_many, ProcessOutcome.Failed, 2, "line one\r\nline two"),
        ]);

        Assert.AreEqual(
            "1 succeeded / 1 failed\n✗ 2 items: line one\n    line two",
            SummaryFormatter.Report(summary));
    }

    [TestMethod]
    public void Line_NamesTheItem()
    {
        Assert.AreEqual(@"✓ C:\in\a.txt", SummaryFormatter.Line(new ProcessResult(0, s_one, ProcessOutcome.Succeeded, 0, null)));
        Assert.AreEqual(@"– C:\in\a.txt: cancelled", SummaryFormatter.Line(new ProcessResult(0, s_one, ProcessOutcome.Cancelled, null, "x")));
    }

    [TestMethod]
    public void TailBuffer_KeepsWholeLinesAtTheEndAndMarksTheCut()
    {
        var buffer = new TailBuffer(8);
        buffer.AppendLine("first");
        buffer.AppendLine("second");

        Assert.AreEqual("…\nsecond\n", buffer.ToString());
    }

    [TestMethod]
    public void Headline_LeavesOutZeroCounts()
    {
        var summary = new JobSummary([new ProcessResult(0, s_one, ProcessOutcome.NotStarted, null, "x")]);

        Assert.AreEqual("0 succeeded / 0 failed, 1 not started", SummaryFormatter.Headline(summary));
    }

    [TestMethod]
    public void Report_ShortensLongFailuresAndNamesTheReasonForNotStarting()
    {
        const string refused = "Not started because administrator approval was not given.";
        var summary = new JobSummary(
        [
            new ProcessResult(0, s_one, ProcessOutcome.Failed, 1, "a\nb\n\nc\nd\ne"),
            new ProcessResult(1, s_one, ProcessOutcome.NotStarted, null, refused),
            new ProcessResult(2, s_one, ProcessOutcome.NotStarted, null, refused),
            new ProcessResult(3, s_one, ProcessOutcome.NotStarted, null, "Not started because the run was cancelled."),
        ]);

        Assert.AreEqual(
            "0 succeeded / 1 failed, 3 not started\n✗ C:\\in\\a.txt: …\n    c\n    d\n    e\n– " + refused,
            SummaryFormatter.Report(summary));
        Assert.AreEqual(@"– C:\in\a.txt: " + refused, SummaryFormatter.Line(summary.Results[1]));
        Assert.AreEqual(@"– C:\in\a.txt: not started", SummaryFormatter.Line(summary.Results[3]));
    }

    [TestMethod]
    public void WorkerMessages_RoundTripOnOneLine()
    {
        WorkerMessage[] messages =
        [
            new WorkerJobMessage(new CommandDefinition { FileName = "cmd.exe", Verb = "runas", ShowOutput = true }, [s_one, s_many]),
            new WorkerCancelMessage(),
            new WorkerStartedMessage(1),
            new WorkerOutputMessage(1, "a\r\nb"),
            new WorkerFinishedMessage(1, ProcessOutcome.Failed, 3, "boom"),
            new WorkerFinishedMessage(1, ProcessOutcome.Succeeded, 0, null),
            new WorkerFinishedMessage(1, ProcessOutcome.Succeeded, null, null),
            new WorkerFinishedMessage(1, ProcessOutcome.NotStarted, null, "refused"),
            new WorkerDoneMessage(),
        ];

        foreach (WorkerMessage message in messages)
        {
            string line = WorkerProtocol.Serialize(message);
            Assert.DoesNotContain("\n", line);
            WorkerMessage copy = WorkerProtocol.Deserialize(line);
            Assert.AreEqual(line, WorkerProtocol.Serialize(copy));
            Assert.AreEqual(message, copy is WorkerJobMessage ? message : copy, line);
        }

        var job = (WorkerJobMessage)WorkerProtocol.Deserialize(WorkerProtocol.Serialize(messages[0]));
        Assert.IsTrue(job.Command.RunsElevated);
        Assert.AreEqual(@"C:\in\b.txt", job.Processes[1].Items[1].Path);
    }

    [TestMethod]
    [DataRow("not json")]
    [DataRow("""{"type":"launch"}""")]
    [DataRow("""{"type":"finished","index":1}""")]
    public void InvalidWorkerMessage_IsRejected(string line)
    {
        Assert.ThrowsExactly<WorkerProtocolException>(() => WorkerProtocol.Deserialize(line));
    }

    [TestMethod]
    public void ShowOutput_NeedsTrackModeWithoutShellExecute()
    {
        var shell = new CommandEntry { Label = "x", Command = new CommandDefinition { FileName = "a", ShowOutput = true, UseShellExecute = true } };
        var launch = new CommandEntry { Label = "x", Command = new CommandDefinition { FileName = "a", ShowOutput = true, RunMode = RunMode.Launch } };

        IReadOnlyList<ConfigIssue> issues = ConfigValidator.Validate(new MenuConfig { Items = [shell, launch] });

        CollectionAssert.AreEqual(
            new[] { "$.items[0].command.showOutput", "$.items[1].command.showOutput" },
            issues.Select(i => i.Path).ToArray());
        Assert.IsTrue(new CommandDefinition().CloseOnSuccess);
    }
}
