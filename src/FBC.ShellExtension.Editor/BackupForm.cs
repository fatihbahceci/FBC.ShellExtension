using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Setup;
using FBC.ShellExtension.Core.Tools;
using FBC.ShellExtension.WikiImport;

namespace FBC.ShellExtension.Editor;

/// <summary>
/// Tools › Back up (ADR-0048): one <c>.zip</c> with the menu and the tools' settings. The keys
/// go in so that another computer can read them, as the user picks: encrypted with a password,
/// as plain text, or not at all. The menu is the one saved on the disk, as the manager's backup
/// was.
/// </summary>
internal partial class BackupForm : Form
{
    private const int ShortPassword = 8;

    private readonly string _configPath;
    private readonly Backups.Settings _settings;

    public BackupForm(string configPath)
    {
        _configPath = configPath;
        _settings = Backups.Collect(new DpapiProtector());
        InitializeComponent();
        ApplyStrings();
        passwordRadio.Checked = true;
        keysGroup.Visible = _settings.Keys.Count > 0;
        unreadableLabel.Visible = _settings.Unreadable.Count > 0;
        problemsLabel.Visible = _settings.Problems.Count > 0;
    }

    private void ApplyStrings()
    {
        Text = Strings.BackupTitle();
        introLabel.Text = Strings.BackupIntro();
        keysGroup.Text = Strings.BackupKeysGroup();
        List<string> names = [];
        if (_settings.Keys.Omdb is not null)
        {
            names.Add(Strings.BackupKeyOmdb());
        }

        names.AddRange(_settings.Keys.Wiki.Keys.Order(StringComparer.OrdinalIgnoreCase).Select(Strings.BackupKeyWiki));
        keysLabel.Text = Strings.BackupKeysList(string.Join("; ", names));
        passwordRadio.Text = Strings.BackupKeysPassword();
        passwordLabel.Text = Strings.CryptPasswordLabel();
        confirmLabel.Text = Strings.CryptConfirmLabel();
        plainRadio.Text = Strings.BackupKeysPlain();
        noneRadio.Text = Strings.BackupKeysNone();
        unreadableLabel.Text = Strings.BackupUnreadable(string.Join("; ", _settings.Unreadable));
        problemsLabel.Text = Strings.BackupProblems(string.Join(" ", _settings.Problems));
        saveButton.Text = Strings.BackupSave();
        cancelButton.Text = Strings.EditorCancel();
    }

    private void OnKeysChoiceChanged(object? sender, EventArgs e) => passwordPanel.Enabled = passwordRadio.Checked;

    private async void OnSave(object? sender, EventArgs e)
    {
        bool withKeys = keysGroup.Visible && !noneRadio.Checked;
        bool withPassword = withKeys && passwordRadio.Checked;
        string password = passwordBox.Text;
        if (withPassword && !PasswordAccepted(password))
        {
            return;
        }

        string config;
        try
        {
            config = File.ReadAllText(_configPath);
        }
        catch (FileNotFoundException)
        {
            MessageBox.Show(this, Strings.BackupNoConfig(_configPath), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, Strings.BackupNotSaved(ex.Message), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = Strings.EditorBackupFilter(),
            FileName = $"{ProductInfo.Name}-backup-{DateTime.Now:yyyyMMdd-HHmmss}{BackupArchive.Extension}",
            OverwritePrompt = true,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string path = dialog.FileName;
        BackupKeys? keys = withKeys ? _settings.Keys : null;
        Enabled = false;
        UseWaitCursor = true;
        string? problem = null;
        try
        {
            // The password's key is slow to derive on purpose: off the window's thread.
            await Task.Run(() =>
            {
                using FileCrypto.Key? key = withPassword ? FileCrypto.DeriveKey(password) : null;
                BackupArchive.Write(path, config, _settings.Omdb, _settings.Wiki, keys, key);
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            problem = ex.Message;
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }

        if (problem is not null)
        {
            MessageBox.Show(this, Strings.BackupNotSaved(problem), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show(this, Strings.BackupSaved(path), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
    }

    /// <summary>The rules of Encrypt's password: not empty, typed twice the same, and a short one only when the user insists.</summary>
    private bool PasswordAccepted(string password)
    {
        string? problem = password.Length == 0 ? Strings.CryptPasswordEmpty()
            : password != confirmBox.Text ? Strings.CryptPasswordsDiffer()
            : null;
        if (problem is not null)
        {
            MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            passwordBox.Focus();
            return false;
        }

        return password.Length >= ShortPassword
            || MessageBox.Show(this, Strings.CryptPasswordShort(ShortPassword), Text, MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }
}
