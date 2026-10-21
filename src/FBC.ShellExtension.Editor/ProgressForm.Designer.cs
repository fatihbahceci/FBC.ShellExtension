#nullable disable

namespace FBC.ShellExtension.Editor;

partial class ProgressForm
{
    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        messageLabel = new Label();
        bar = new ProgressBar();
        cancelButton = new Button();
        SuspendLayout();
        //
        // messageLabel
        //
        messageLabel.Dock = DockStyle.Top;
        messageLabel.Location = new Point(12, 12);
        messageLabel.Name = "messageLabel";
        messageLabel.Size = new Size(396, 40);
        messageLabel.TabIndex = 0;
        messageLabel.Text = "Working…";
        //
        // bar
        //
        bar.Dock = DockStyle.Top;
        bar.Location = new Point(12, 52);
        bar.MarqueeAnimationSpeed = 30;
        bar.Name = "bar";
        bar.Size = new Size(396, 20);
        bar.Style = ProgressBarStyle.Marquee;
        bar.TabIndex = 1;
        //
        // cancelButton
        //
        cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        cancelButton.AutoSize = true;
        cancelButton.Location = new Point(321, 82);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(87, 27);
        cancelButton.TabIndex = 2;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += OnCancel;
        //
        // ProgressForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(420, 121);
        ControlBox = false;
        Controls.Add(cancelButton);
        Controls.Add(bar);
        Controls.Add(messageLabel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Name = "ProgressForm";
        Padding = new Padding(12);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "FBCTools";
        ResumeLayout(false);
    }

    #endregion

    private Label messageLabel;
    private ProgressBar bar;
    private Button cancelButton;
}
