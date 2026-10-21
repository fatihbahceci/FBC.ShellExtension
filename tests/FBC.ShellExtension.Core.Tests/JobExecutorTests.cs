using System.Collections.Concurrent;
using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Execution;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class JobExecutorTests
{
    /// <summary>Behaves according to the planned file name, and records concurrency.</summary>
    private sealed class FakeLauncher : IProcessLauncher
    {
        private int _running;

        public int Calls;
        public int MaxConcurrent;
        public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<LaunchResult> RunAsync(PlannedProcess process, CommandDefinition command, Action<string> output, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            int running = Interlocked.Increment(ref _running);
            int max;
            while (running > (max = Volatile.Read(ref MaxConcurrent)) && Interlocked.CompareExchange(ref MaxConcurrent, running, max) != max)
            {
            }

            FirstStarted.TrySetResult();
            try
            {
                switch (process.FileName)
                {
                    case "ok":
                        await Task.Yield();
                        return new LaunchResult(0, "", false);
                    case "shell":
                        return new LaunchResult(null, "", false);
                    case "stderr":
                        output("first line");
                        return new LaunchResult(3, "boom\r\nsecond\r\n", false);
                    case "silent":
                        return new LaunchResult(5, "  \n", false);
                    case "crash":
                        return new LaunchResult(unchecked((int)0xC0000005), "", false);
                    case "nostart":
                        throw new ProcessStartException("cannot start nostart");
                    case "bug":
                        throw new InvalidOperationException("launcher bug");
                    case "stuck":
                        return new LaunchResult(null, "", Cancelled: true, StopIncomplete: true);
                    case "sleep":
                        await Task.Delay(100, CancellationToken.None);
                        return new LaunchResult(0, "", false);
                    default:
                        try
                        {
                            await Task.Delay(Timeout.Infinite, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                        }

                        return new LaunchResult(null, "", Cancelled: true);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _running);
            }
        }
    }

    private sealed class RecordingObserver : IExecutionObserver
    {
        public ConcurrentQueue<string> Events { get; } = new();

        public void Started(int index, PlannedProcess process) => Events.Enqueue($"start {index}");

        public void Output(int index, string line) => Events.Enqueue($"out {index} {line}");

        public void Finished(ProcessResult result) => Events.Enqueue($"end {result.Index} {result.Outcome}");
    }

    private static PlannedProcess Planned(string fileName, int n = 0) =>
        new(fileName, "", @"C:\in", [File($@"C:\in\f{n}.txt")]);

    private static CommandDefinition Track(MultiSelectMode mode = MultiSelectMode.PerFileSequential, int? parallelism = null) =>
        new() { FileName = "x", MultiSelect = mode, MaxParallelism = parallelism };

    [TestMethod]
    public async Task Outcomes_FollowTheExitCodeAndStderrRules()
    {
        PlannedProcess[] plan = [Planned("ok"), Planned("shell"), Planned("stderr"), Planned("silent"), Planned("crash"), Planned("nostart"), Planned("bug")];

        JobSummary summary = await new JobExecutor(new FakeLauncher()).RunAsync(Track(), plan, new RecordingObserver(), CancellationToken.None);

        ProcessResult[] r = [.. summary.Results];
        Assert.AreEqual(ProcessOutcome.Succeeded, r[0].Outcome);
        Assert.AreEqual(ProcessOutcome.Succeeded, r[1].Outcome, "no exit code means the shell handed it over");
        Assert.AreEqual("boom\r\nsecond", r[2].Message);
        Assert.AreEqual("An error occurred (exit code 5)", r[3].Message);
        Assert.AreEqual("An error occurred (exit code -1073741819 (0xC0000005))", r[4].Message);
        Assert.AreEqual("cannot start nostart", r[5].Message);
        Assert.AreEqual("Unexpected error: launcher bug", r[6].Message);
        Assert.AreEqual(2, summary.Succeeded);
        Assert.AreEqual(5, summary.Failed);
        Assert.IsFalse(summary.AllSucceeded);
    }

    [TestMethod]
    public async Task Sequential_RunsInOrderAndReportsOutput()
    {
        var observer = new RecordingObserver();

        await new JobExecutor(new FakeLauncher()).RunAsync(Track(), [Planned("ok"), Planned("stderr")], observer, CancellationToken.None);

        CollectionAssert.AreEqual(
            new[] { "start 0", "end 0 Succeeded", "start 1", "out 1 first line", "end 1 Failed" },
            observer.Events.ToArray());
    }

    [TestMethod]
    public async Task Parallel_KeepsToTheMaximum()
    {
        var launcher = new FakeLauncher();
        PlannedProcess[] plan = [.. Enumerable.Range(0, 8).Select(i => Planned("sleep", i))];

        JobSummary summary = await new JobExecutor(launcher).RunAsync(Track(MultiSelectMode.PerFileParallel, 3), plan, new RecordingObserver(), CancellationToken.None);

        Assert.IsTrue(summary.AllSucceeded);
        Assert.AreEqual(8, launcher.Calls);
        Assert.IsTrue(launcher.MaxConcurrent is > 1 and <= 3, $"max concurrent {launcher.MaxConcurrent}");
    }

    [TestMethod]
    public async Task Cancel_StopsTheRunningProcessAndSkipsTheRest()
    {
        var launcher = new FakeLauncher();
        using var cts = new CancellationTokenSource();
        Task<JobSummary> run = new JobExecutor(launcher).RunAsync(Track(), [Planned("ok"), Planned("wait", 1), Planned("ok", 2)], new RecordingObserver(), cts.Token);

        await WaitUntil(() => launcher.Calls == 2);
        cts.Cancel();
        JobSummary summary = await run;

        CollectionAssert.AreEqual(
            new[] { ProcessOutcome.Succeeded, ProcessOutcome.Cancelled, ProcessOutcome.NotStarted },
            summary.Results.Select(r => r.Outcome).ToArray());
        Assert.AreEqual(2, launcher.Calls);
    }

    [TestMethod]
    public async Task CancelInParallel_LeavesQueuedProcessesNotStarted()
    {
        var launcher = new FakeLauncher();
        using var cts = new CancellationTokenSource();
        PlannedProcess[] plan = [.. Enumerable.Range(0, 5).Select(i => Planned("wait", i))];
        Task<JobSummary> run = new JobExecutor(launcher).RunAsync(Track(MultiSelectMode.PerFileParallel, 2), plan, new RecordingObserver(), cts.Token);

        await WaitUntil(() => launcher.Calls == 2);
        cts.Cancel();
        JobSummary summary = await run;

        Assert.AreEqual(2, summary.Cancelled);
        Assert.AreEqual(3, summary.NotStarted);
        Assert.AreEqual("0 succeeded / 0 failed, 2 cancelled, 3 not started", SummaryFormatter.Headline(summary));
    }

    [TestMethod]
    public async Task TooLongCommandLine_FailsWithoutStarting()
    {
        var launcher = new FakeLauncher();
        var process = new PlannedProcess("ok", new string('a', CommandLine.MaxLength), @"C:\", [File(@"C:\a.txt")]);

        JobSummary summary = await new JobExecutor(launcher).RunAsync(Track(), [process], new RecordingObserver(), CancellationToken.None);

        Assert.AreEqual(0, launcher.Calls);
        StringAssert.StartsWith(summary.Results[0].Message, "The command line is ");
    }

    [TestMethod]
    public async Task CancelThatCouldNotStopEverything_SaysSo()
    {
        JobSummary summary = await new JobExecutor(new FakeLauncher()).RunAsync(Track(), [Planned("stuck")], new RecordingObserver(), CancellationToken.None);

        Assert.AreEqual(ProcessOutcome.Cancelled, summary.Results[0].Outcome);
        StringAssert.Contains(summary.Results[0].Message, "may still be running");
    }

    [TestMethod]
    [DataRow(MultiSelectMode.AllAtOnce, "instead of allAtOnce")]
    [DataRow(MultiSelectMode.PerFileSequential, "{allPaths}")]
    public void TooLongCommandLine_AdviceFitsTheMode(MultiSelectMode mode, string advice)
    {
        var process = new PlannedProcess("ok", new string('a', CommandLine.MaxLength), @"C:\", [File(@"C:\a.txt")]);

        JobSummary summary = new JobExecutor(new FakeLauncher()).RunAsync(Track(mode), [process], new RecordingObserver(), CancellationToken.None).GetAwaiter().GetResult();

        StringAssert.Contains(summary.Results[0].Message, advice);
    }

    [TestMethod]
    public void CommandLineLength_MatchesWhatDotNetBuilds()
    {
        Assert.AreEqual("\"tool.exe\" -a".Length, CommandLine.Length("tool.exe", "-a"));
        Assert.AreEqual("\"tool.exe\"".Length, CommandLine.Length("tool.exe", ""));
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (int i = 0; i < 500 && !condition(); i++)
        {
            await Task.Delay(10);
        }

        Assert.IsTrue(condition(), "condition not reached in 5 s");
    }
}
