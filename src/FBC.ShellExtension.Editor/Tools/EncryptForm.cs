using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Encrypts the selected files and the files in the selected folders (ADR-0032), the way the
/// user picks: Windows' EFS, tied to the Windows account and with no password, or a password,
/// which opens anywhere FBCTools runs.
/// </summary>
internal partial class EncryptForm : Form
{
    private const int ShortPassword = 8;

    private readonly IReadOnlyList<SelectedItem> _items;

    public EncryptForm(IReadOnlyList<SelectedItem> items)
    {
        _items = items;
        InitializeComponent();
        ApplyStrings();
        passwordRadio.Checked = true;
    }

    private void ApplyStrings()
    {
        selectionLabel.Text = ToolWindows.Selection(_items);
        efsRadio.Text = Strings.EncryptWithEfs();
        efsHintLabel.Text = Strings.EncryptEfsHint();
        passwordRadio.Text = Strings.EncryptWithPassword();
        passwordLabel.Text = Strings.CryptPasswordLabel();
        confirmLabel.Text = Strings.CryptConfirmLabel();
        deleteCheck.Text = Strings.EncryptDeleteOriginals();
        passwordHintLabel.Text = Strings.EncryptPasswordHint();
        encryptButton.Text = Strings.EncryptButton();
        cancelButton.Text = Strings.EditorCancel();
    }

    private void OnMethodChanged(object? sender, EventArgs e) =>
        passwordPanel.Enabled = deleteCheck.Enabled = passwordRadio.Checked;

    // Not a dialog: the window is the program, and Cancel ends it.
    private void OnClose(object? sender, EventArgs e) => Close();

    private async void OnEncrypt(object? sender, EventArgs e)
    {
        bool withPassword = passwordRadio.Checked;
        string password = passwordBox.Text;
        if (withPassword && !PasswordAccepted(password))
        {
            return;
        }

        bool deleteOriginals = deleteCheck.Checked;
        IReadOnlyList<string>? errors = await ToolWindows.WithProgressAsync(this, Strings.ToolWindowWorking(), cancellation =>
        {
            IReadOnlyList<string> files = FileTools.CryptSources(_items, out List<string> failed, cancellation);
            if (withPassword)
            {
                // The slow part, once for all the files.
                using FileCrypto.Key key = FileCrypto.DeriveKey(password);
                failed.AddRange(FileTools.EncryptFiles(files, key, deleteOriginals, cancellation));
            }
            else
            {
                failed.AddRange(Efs(files.Concat(FileTools.CryptFolders(_items, cancellation)), File.Encrypt, cancellation));
            }

            return failed;
        });

        // Cancelled: what was done stays done, and the window stays for another try.
        if (errors is null)
        {
            return;
        }

        ToolWindows.ShowErrors(this, Text, Strings.EncryptNotEncrypted(), errors);
        Close();
    }

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

    /// <summary>Windows' EFS on each path, a file or a folder; Windows Home has none and says so for each.</summary>
    internal static List<string> Efs(IEnumerable<string> paths, Action<string> change, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        foreach (string path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                change(path);
            }
            // NotSupported: a volume without EFS (FAT, exFAT); ArgumentException: a network path.
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                errors.Add(Strings.ToolItemFailed(path, ex.Message));
            }
        }

        return errors;
    }
}
