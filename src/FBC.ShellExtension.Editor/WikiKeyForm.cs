using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.WikiImport;

namespace FBC.ShellExtension.Editor;

/// <summary>
/// The API key or token Send to Wiki.js uses for a wiki (ADR-0039): checked against the wiki
/// first, then kept encrypted where the runner reads it; or forgotten, so that the runner asks
/// again. A token is the user's own Wiki.js session, copied from the browser; when it stops
/// working, the user copies a fresh one.
/// </summary>
internal partial class WikiKeyForm : Form
{
    private readonly string _path = WikiSettings.DefaultPath;
    private readonly DpapiProtector _protector = new();
    private readonly HashSet<string> _stored = new(StringComparer.OrdinalIgnoreCase);
    private readonly string? _loadProblem;
    private CancellationTokenSource? _checking;

    public WikiKeyForm()
    {
        InitializeComponent();
        ApplyStrings();
        environmentLabel.Visible = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(WikiSettings.ApiKeyVariable));
        try
        {
            // A hand-edited entry without an address is no wiki to offer.
            _stored.UnionWith(WikiSettings.Load(_path).Sites
                .Where(site => site.ApiKey is not null && !string.IsNullOrEmpty(site.BaseUrl))
                .Select(site => site.BaseUrl));
        }
        catch (WikiException ex)
        {
            _loadProblem = ex.Message;
        }

        addressBox.Items.AddRange([.. _stored.Order(StringComparer.OrdinalIgnoreCase)]);
        if (addressBox.Items.Count > 0)
        {
            addressBox.SelectedIndex = 0;
        }

        ShowState();
    }

    private void ApplyStrings()
    {
        Text = Strings.WikiKeyTitle();
        addressLabel.Text = Strings.WikiKeyAddress();
        keyLabel.Text = Strings.WikiKeyKey();
        environmentLabel.Text = Strings.WikiKeyEnvironment();
        helpLabel.Text = Strings.WikiKeyHelp();
        saveButton.Text = Strings.WikiKeySave();
        forgetButton.Text = Strings.WikiKeyForget();
        closeButton.Text = Strings.ToolWindowClose();
    }

    private void OnInputChanged(object? sender, EventArgs e) => ShowState();

    /// <summary>Whether something is kept for the wiki in the box, and which buttons may act.</summary>
    private void ShowState()
    {
        string? site = WikiPageAddress.BaseUrlOf(addressBox.Text);
        bool stored = site is not null && _stored.Contains(site);
        statusLabel.Text = _loadProblem
            ?? (site is null ? "" : stored ? Strings.WikiKeyStored(site) : Strings.WikiKeyNoneStored(site));
        forgetButton.Enabled = stored && _checking is null;
        saveButton.Enabled = site is not null && keyBox.Text.Trim().Length >= WikiSettings.MinimumKeyLength && _checking is null;
    }

    private void SetBusy(bool busy)
    {
        addressBox.Enabled = keyBox.Enabled = !busy;
        UseWaitCursor = busy;
        if (busy)
        {
            saveButton.Enabled = forgetButton.Enabled = false;
        }
    }

    /// <summary>The wiki is asked first: a key or token it does not take is not kept.</summary>
    private async void OnSave(object? sender, EventArgs e)
    {
        if (WikiPageAddress.BaseUrlOf(addressBox.Text) is not { } site)
        {
            return;
        }

        string key = keyBox.Text.Trim();
        using var checking = new CancellationTokenSource();
        _checking = checking;
        SetBusy(true);
        statusLabel.Text = Strings.WikiKeyChecking(site);
        string? problem = null;
        try
        {
            using var client = new WikiClient(site, key);
            await client.GetSiteAsync(checking.Token);
            await client.CheckKeyAsync(checking.Token);

            // Closed while the answer was on its way: nothing is kept that the user walked away from.
            checking.Token.ThrowIfCancellationRequested();
            WikiSettings.Update(_path, settings => settings.SetKey(site, key, _protector));
        }
        catch (OperationCanceledException) when (checking.IsCancellationRequested)
        {
            // The window was closed meanwhile.
            return;
        }
        catch (Exception ex) when (ex is WikiException or IOException or UnauthorizedAccessException)
        {
            problem = ex.Message;
        }
        finally
        {
            _checking = null;
        }

        if (IsDisposed)
        {
            return;
        }

        SetBusy(false);
        if (problem is not null)
        {
            ShowState();
            statusLabel.Text = Strings.WikiKeyNotSaved(problem);

            // Disabling the fields moved the focus on to Close; Enter would close the window.
            keyBox.Focus();
            keyBox.SelectAll();
            return;
        }

        _stored.Add(site);
        MessageBox.Show(this, environmentLabel.Visible ? Strings.WikiKeySavedEnvironment(site) : Strings.WikiKeySaved(site), Text,
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        Close();
    }

    private void OnForget(object? sender, EventArgs e)
    {
        if (WikiPageAddress.BaseUrlOf(addressBox.Text) is not { } site
            || MessageBox.Show(this, environmentLabel.Visible ? Strings.WikiKeyForgetConfirmEnvironment(site) : Strings.WikiKeyForgetConfirm(site), Text, MessageBoxButtons.YesNo,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            WikiSettings.Update(_path, settings => settings.SetKey(site, null, _protector));
            _stored.Remove(site);
            ShowState();
        }
        catch (Exception ex) when (ex is WikiException or IOException or UnauthorizedAccessException)
        {
            statusLabel.Text = Strings.WikiKeyNotSaved(ex.Message);
        }
    }

    private void OnClose(object? sender, EventArgs e) => Close();

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _checking?.Cancel();
        base.OnFormClosing(e);
    }
}
