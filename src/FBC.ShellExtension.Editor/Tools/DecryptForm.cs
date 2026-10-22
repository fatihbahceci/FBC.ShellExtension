using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Core.Selection;
using FBC.ShellExtension.Core.Tools;

namespace FBC.ShellExtension.Editor.Tools;

/// <summary>
/// Decrypts the selected files and the files in the selected folders (ADR-0032), whichever way
/// they were encrypted: a password file comes back next to itself, an EFS file is decrypted
/// where it is. The window first says how many of each there are.
/// </summary>
internal partial class DecryptForm : Form
{
    private readonly IReadOnlyList<SelectedItem> _items;
    private Found _found = new([], [], [], []);

    public DecryptForm(IReadOnlyList<SelectedItem> items)
    {
        _items = items;
        InitializeComponent();
        ApplyStrings();
    }

    private void ApplyStrings()
    {
        selectionLabel.Text = ToolWindows.Selection(_items);
        statusLabel.Text = Strings.DecryptSearching();
        passwordLabel.Text = Strings.CryptPasswordLabel();
        deleteCheck.Text = Strings.DecryptDeleteEncrypted();
        hintLabel.Text = Strings.DecryptHint();
        decryptButton.Text = Strings.DecryptButton();
        cancelButton.Text = Strings.EditorCancel();
    }

    /// <summary>The files encrypted each way, and the folders EFS marks.</summary>
    private sealed record Found(IReadOnlyList<string> Password, IReadOnlyList<string> Efs, IReadOnlyList<string> EfsFolders, IReadOnlyList<string> Errors);

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Found? found = await ToolWindows.WithProgressAsync(this, Strings.DecryptSearching(), cancellation =>
        {
            IReadOnlyList<string> files = FileTools.CryptSources(_items, out List<string> errors, cancellation);
            List<string> password = [.. files.Where(FileCrypto.IsEncrypted)];
            List<string> efs = [.. files.Except(password).Where(IsEfs)];
            List<string> folders = [.. FileTools.CryptFolders(_items, cancellation).Where(IsEfs)];
            return new Found(password, efs, folders, errors);
        });

        if (found is null)
        {
            Close();
            return;
        }

        _found = found;
        bool any = found.Password.Count + found.Efs.Count + found.EfsFolders.Count > 0;
        statusLabel.Text = any ? Strings.DecryptFound(found.Password.Count, found.Efs.Count) : Strings.DecryptNothing();
        passwordPanel.Visible = deleteCheck.Visible = found.Password.Count > 0;
        decryptButton.Enabled = any;
        if (passwordPanel.Visible)
        {
            passwordBox.Focus();
        }
    }

    private static bool IsEfs(string path)
    {
        try
        {
            return File.GetAttributes(path).HasFlag(FileAttributes.Encrypted);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private async void OnDecrypt(object? sender, EventArgs e)
    {
        string password = passwordBox.Text;
        if (_found.Password.Count > 0 && password.Length == 0)
        {
            MessageBox.Show(this, Strings.CryptPasswordEmpty(), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            passwordBox.Focus();
            return;
        }

        bool deleteEncrypted = deleteCheck.Checked;
        Found found = _found;
        IReadOnlyList<string>? errors = await ToolWindows.WithProgressAsync(this, Strings.ToolWindowWorking(), cancellation =>
        {
            var failed = new List<string>(found.Errors);
            failed.AddRange(FileTools.DecryptFiles(found.Password, password, deleteEncrypted, cancellation));
            failed.AddRange(EncryptForm.Efs(found.Efs.Concat(found.EfsFolders), File.Decrypt, cancellation));
            return failed;
        });

        // Cancelled: what was done stays done, and the window stays for another try.
        if (errors is null)
        {
            return;
        }

        ToolWindows.ShowErrors(this, Text, Strings.DecryptNotDecrypted(), errors);
        Close();
    }

    // Not a dialog: the window is the program, and Cancel ends it.
    private void OnClose(object? sender, EventArgs e) => Close();
}
