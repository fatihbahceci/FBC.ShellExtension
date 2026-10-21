using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Editor;

/// <summary>A small modeless window for a long manager command, with a way to call it off.</summary>
public partial class ProgressForm : Form
{
    private readonly CancellationTokenSource _cancellation = new();

    public ProgressForm()
        : this("")
    {
    }

    public ProgressForm(string message)
    {
        InitializeComponent();
        Text = ProductInfo.Name;
        messageLabel.Text = message;
        cancelButton.Text = Strings.EditorCancel();
    }

    public CancellationToken Cancellation => _cancellation.Token;

    private void OnCancel(object? sender, EventArgs e)
    {
        cancelButton.Enabled = false;
        _cancellation.Cancel();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellation.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }
}
