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

    /// <summary>Alt+F4 closes it despite the missing close box: that is a cancel too, not a window gone while the work goes on.</summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cancellation.Cancel();
        base.OnFormClosing(e);
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
