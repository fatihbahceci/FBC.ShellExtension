using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Editor;

/// <summary>
/// The password of a backup's keys, asked when one is restored (ADR-0048). OK restores with the
/// keys; Ignore restores everything else and keeps this computer's keys; Cancel restores nothing.
/// </summary>
internal partial class BackupPasswordForm : Form
{
    public BackupPasswordForm(string fileName, string? problem)
    {
        InitializeComponent();
        Text = Strings.BackupPasswordTitle();
        promptLabel.Text = Strings.BackupPasswordPrompt(fileName);
        passwordLabel.Text = Strings.CryptPasswordLabel();
        problemLabel.Text = problem ?? "";
        problemLabel.Visible = problem is not null;
        restoreButton.Text = Strings.BackupRestore();
        withoutKeysButton.Text = Strings.BackupWithoutKeys();
        cancelButton.Text = Strings.EditorCancel();
    }

    public string Password => passwordBox.Text;
}
