using System.Diagnostics;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Tools;
using FBC.ShellExtension.Editor.Tools;
using FBC.ShellExtension.WikiImport;

namespace FBC.ShellExtension.Editor;

/// <summary>
/// The OMDb API key Rename from IMDb uses (ADR-0046): checked against OMDb first, then kept
/// encrypted for the Windows user; or forgotten. Opened from Tools and from the tool's window.
/// </summary>
internal partial class OmdbKeyForm : Form
{
    private readonly string _path = OmdbSettings.DefaultPath;
    private readonly DpapiProtector _protector = new();
    private bool _stored;
    private bool _unreadable;
    private string? _loadProblem;
    private CancellationTokenSource? _checking;

    public OmdbKeyForm()
    {
        InitializeComponent();
        ApplyStrings();
        try
        {
            // Kept is not enough: another Windows user's or machine's key cannot be decrypted here.
            OmdbSettings settings = OmdbSettings.Load(_path);
            _stored = settings.Key(_protector) is not null;
            _unreadable = !_stored && settings.ApiKey is not null;
        }
        catch (IOException ex)
        {
            _loadProblem = ex.Message;
        }

        ShowState();
    }

    /// <summary>Whether a key is kept once the window is closed.</summary>
    public bool HasKey => _stored;

    private void ApplyStrings()
    {
        Text = Strings.OmdbKeyTitle();
        keyLabel.Text = Strings.OmdbKeyLabel();
        string help = Strings.OmdbKeyHelp(Omdb.ApiKeyPage);
        helpLabel.Text = help;
        int link = help.IndexOf(Omdb.ApiKeyPage, StringComparison.Ordinal);
        helpLabel.LinkArea = link >= 0 ? new LinkArea(link, Omdb.ApiKeyPage.Length) : new LinkArea(0, 0);
        saveButton.Text = Strings.OmdbKeySave();
        forgetButton.Text = Strings.OmdbKeyForget();
        closeButton.Text = Strings.ToolWindowClose();
    }

    private void OnInputChanged(object? sender, EventArgs e) => ShowState();

    private void ShowState()
    {
        statusLabel.Text = _loadProblem
            ?? (_stored ? Strings.OmdbKeyStored() : _unreadable ? Strings.OmdbKeyUnreadable() : Strings.OmdbKeyNoneStored());
        forgetButton.Enabled = (_stored || _unreadable) && _checking is null;
        saveButton.Enabled = keyBox.Text.Trim().Length > 0 && _checking is null;
    }

    private void OnHelpLink(object? sender, LinkLabelLinkClickedEventArgs e) =>
        Process.Start(new ProcessStartInfo(Omdb.ApiKeyPage) { UseShellExecute = true })?.Dispose();

    /// <summary>OMDb is asked first: a key it does not take is not kept.</summary>
    private async void OnSave(object? sender, EventArgs e)
    {
        string key = keyBox.Text.Trim();
        using var checking = new CancellationTokenSource();
        _checking = checking;
        keyBox.Enabled = saveButton.Enabled = forgetButton.Enabled = false;
        UseWaitCursor = true;
        statusLabel.Text = Strings.ImdbAsking();
        string? problem = null;
        try
        {
            try
            {
                await OmdbClient.FetchAsync(Omdb.IdUri(key, Omdb.CheckId), checking.Token);
            }
            catch (OmdbException ex) when (ex.Failure == OmdbFailure.LimitReached)
            {
                // Only a key OMDb knows has a limit to reach.
            }

            checking.Token.ThrowIfCancellationRequested();
            OmdbSettings.Update(_path, settings => settings.SetKey(key, _protector));
        }
        catch (OperationCanceledException) when (checking.IsCancellationRequested)
        {
            // The window was closed meanwhile: nothing is kept that the user walked away from.
            return;
        }
        catch (Exception ex) when (ex is OmdbException or IOException or UnauthorizedAccessException)
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

        keyBox.Enabled = true;
        UseWaitCursor = false;
        if (problem is not null)
        {
            ShowState();
            statusLabel.Text = Strings.OmdbKeyNotSaved(problem);

            // Disabling the box moved the focus on to Close; Enter would close the window.
            keyBox.Focus();
            keyBox.SelectAll();
            return;
        }

        _stored = true;
        _unreadable = false;
        _loadProblem = null;
        MessageBox.Show(this, Strings.OmdbKeySaved(), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        Close();
    }

    private void OnForget(object? sender, EventArgs e)
    {
        if (MessageBox.Show(this, Strings.OmdbKeyForgetConfirm(), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            OmdbSettings.Update(_path, settings => settings.SetKey(null, _protector));
            _stored = _unreadable = false;
            ShowState();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            statusLabel.Text = Strings.OmdbKeyNotSaved(ex.Message);
        }
    }

    private void OnClose(object? sender, EventArgs e) => Close();

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _checking?.Cancel();
        base.OnFormClosing(e);
    }
}
