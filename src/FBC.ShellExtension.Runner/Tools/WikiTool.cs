using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Execution;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;
using FBC.ShellExtension.Runner.Execution;
using FBC.ShellExtension.Runner.UI;
using FBC.ShellExtension.WikiImport;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// Send to Wiki.js (ADR-0020). Before the progress window: the page's address, the site's API
/// key (asked once, then kept encrypted), a check that the key works and whether the page
/// exists, and the user's yes to replace it. Then, in the window, the images and the page.
/// </summary>
internal static class WikiTool
{
    public static int Run(RunJob job)
    {
        string title = Strings.RunnerWindowTitle(job.Label);
        if (job.Items is not [{ Kind: ItemKind.File } item])
        {
            return Fail(title, Strings.WikiOneFile());
        }

        string settingsPath = WikiSettings.DefaultPath;
        WikiSettings settings;
        try
        {
            settings = WikiSettings.Load(settingsPath);
        }
        catch (WikiException ex)
        {
            return Fail(title, ex.Message);
        }

        if (TextPrompt.Ask(new TextPromptOptions(title, Strings.WikiAskUrl(TextPrompt.ShortName(item.Name)), settings.TargetFor(item.Path) ?? "",
            Hint: Strings.WikiAskUrlHint(), Validate: WikiPageAddress.Problem)) is not { } url)
        {
            return 0;
        }

        WikiPageAddress address = WikiPageAddress.Parse(url);
        var protector = new DpapiProtector();
        string? environmentKey = Environment.GetEnvironmentVariable(WikiSettings.ApiKeyVariable) is { Length: > 0 } variable ? variable : null;
        string? key = environmentKey ?? settings.KeyFor(address.BaseUrl, protector);
        bool typed = false;

        // A stored key the wiki no longer takes is asked for again, once.
        for (int attempt = 0; ; attempt++)
        {
            if (key is null)
            {
                key = TextPrompt.Ask(new TextPromptOptions(title, Strings.WikiAskKey(new Uri(address.BaseUrl).Authority), "",
                    Hint: Strings.WikiAskKeyHint(), Secret: true, Validate: text => text.Length < 20 ? Strings.WikiKeyTooShort() : null));
                if (key is null)
                {
                    return 0;
                }

                typed = true;
            }

            try
            {
                using var client = new WikiClient(address.BaseUrl, key);
                WikiSite site = client.GetSiteAsync(CancellationToken.None).GetAwaiter().GetResult();
                client.CheckKeyAsync(CancellationToken.None).GetAwaiter().GetResult();
                string locale = address.Locale ?? site.DefaultLocale;
                WikiPage? existing = client.FindPageAsync(address.Path, locale, CancellationToken.None).GetAwaiter().GetResult();

                // The settings are a convenience: when they cannot be saved, the import still runs.
                string? notSaved = null;
                string typedKey = key;
                try
                {
                    WikiSettings.Update(settingsPath, fresh =>
                    {
                        if (environmentKey is null && typed)
                        {
                            fresh.SetKey(address.BaseUrl, typedKey, protector);
                        }

                        fresh.SetTarget(item.Path, url);
                    });
                }
                catch (Exception ex) when (ex is WikiException or IOException or UnauthorizedAccessException)
                {
                    notSaved = Strings.WikiSettingsNotSaved(settingsPath, ex.Message);
                }

                if (existing is not null)
                {
                    // Written at its own path: another spelling would move it.
                    address = address with { Path = existing.Path };
                    locale = existing.Locale;
                    if (!existing.IsMarkdown)
                    {
                        return Fail(title, Strings.WikiPageNotMarkdown(address.PageUrl(locale), existing.ContentType));
                    }

                    if (MessageBoxW(0, Strings.WikiConfirmUpdate(address.PageUrl(locale)), title,
                        MB_YESNO | MB_ICONQUESTION | MB_DEFBUTTON2 | MB_TOPMOST | MB_SETFOREGROUND) != IDYES)
                    {
                        return 0;
                    }
                }

                return RunInWindow(job, item, address, locale, existing, settings, key, notSaved);
            }
            catch (WikiKeyException ex) when (environmentKey is null && attempt == 0)
            {
                MessageBoxW(0, ex.Message, title, MB_ICONERROR | MB_TOPMOST | MB_SETFOREGROUND);
                key = null;
            }
            catch (Exception ex) when (ex is WikiException or IOException or UnauthorizedAccessException)
            {
                return Fail(title, ex.Message);
            }
        }
    }

    private static int RunInWindow(RunJob job, SelectedItem item, WikiPageAddress address, string locale, WikiPage? existing, WikiSettings settings, string key, string? notSaved)
    {
        job.Command = new CommandDefinition { ShowOutput = true, CloseOnSuccess = false, MultiSelect = MultiSelectMode.PerFileSequential };
        IReadOnlyList<PlannedProcess> plan = [new PlannedProcess(nameof(BuiltinTool.WikiImport), "", item.Directory, [item])];
        var window = new RunnerWindow(job, plan, Program.VersionMismatch(job));
        return window.Run(new WikiJobRunner(address, locale, existing, settings, key, notSaved));
    }

    private static int Fail(string title, string message)
    {
        MessageBoxW(0, message, title, MB_ICONERROR | MB_TOPMOST | MB_SETFOREGROUND);
        return 1;
    }

    /// <summary>The import as the progress window's one "process".</summary>
    /// <param name="notSaved">Why the settings were not saved; shown first, null when they were.</param>
    private sealed class WikiJobRunner(WikiPageAddress address, string locale, WikiPage? existing, WikiSettings settings, string key, string? notSaved) : IJobRunner
    {
        public Task<JobSummary> RunAsync(CommandDefinition command, IReadOnlyList<PlannedProcess> plan, IExecutionObserver observer, CancellationToken cancellationToken) =>
            Task.Run(async () =>
            {
                PlannedProcess step = plan[0];
                observer.Started(0, step);
                if (notSaved is not null)
                {
                    observer.Output(0, notSaved);
                }

                ProcessResult result;
                try
                {
                    ImportPlan import = WikiImporter.Prepare(step.Items[0].Path, settings.RemoveFirstHeading, CodePages.Ansi);
                    using var client = new WikiClient(address.BaseUrl, key);
                    ImportResult done = await WikiImporter.RunAsync(import, address, locale, existing, settings.LinkStyle, client,
                        new Report(observer), cancellationToken).ConfigureAwait(false);

                    // Saved, but with holes: shown as a failure, so the missing images are not overlooked.
                    result = done.Missing.Count > 0
                        ? new ProcessResult(0, step, ProcessOutcome.Failed, null,
                            Strings.WikiSavedWithMissing(done.PageUrl, done.Missing.Count, string.Join("\n", done.Missing)))
                        : new ProcessResult(0, step, ProcessOutcome.Succeeded, null, null);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    result = new ProcessResult(0, step, ProcessOutcome.Cancelled, null, null);
                }
                catch (Exception ex) when (ex is WikiException or IOException or UnauthorizedAccessException)
                {
                    result = new ProcessResult(0, step, ProcessOutcome.Failed, null, ex.Message);
                }

                observer.Finished(result);
                return new JobSummary([result]);
            }, CancellationToken.None);
    }

    private sealed class Report(IExecutionObserver observer) : IToolReport
    {
        public void Output(string line) => observer.Output(0, line);

        public void Progress(long done, long total) => observer.Progress(0, done, total);
    }
}
