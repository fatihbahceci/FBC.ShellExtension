#nullable disable

namespace FBC.ShellExtension.Editor;

partial class BackupPasswordForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        layout = new TableLayoutPanel();
        promptLabel = new Label();
        fieldsPanel = new TableLayoutPanel();
        passwordLabel = new Label();
        passwordBox = new TextBox();
        problemLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        withoutKeysButton = new Button();
        restoreButton = new Button();
        layout.SuspendLayout();
        fieldsPanel.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // layout
        //
        layout.AutoSize = true;
        layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle());
        layout.Controls.Add(promptLabel, 0, 0);
        layout.Controls.Add(fieldsPanel, 0, 1);
        layout.Controls.Add(problemLabel, 0, 2);
        layout.Controls.Add(buttonPanel, 0, 3);
        layout.Location = new Point(0, 0);
        layout.Name = "layout";
        layout.Padding = new Padding(8);
        layout.RowCount = 4;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.TabIndex = 0;
        //
        // promptLabel
        //
        promptLabel.AutoSize = true;
        promptLabel.Margin = new Padding(3, 3, 3, 6);
        promptLabel.MaximumSize = new Size(440, 0);
        promptLabel.Name = "promptLabel";
        promptLabel.TabIndex = 0;
        promptLabel.UseMnemonic = false;
        //
        // fieldsPanel
        //
        fieldsPanel.AutoSize = true;
        fieldsPanel.ColumnCount = 2;
        fieldsPanel.ColumnStyles.Add(new ColumnStyle());
        fieldsPanel.ColumnStyles.Add(new ColumnStyle());
        fieldsPanel.Controls.Add(passwordLabel, 0, 0);
        fieldsPanel.Controls.Add(passwordBox, 1, 0);
        fieldsPanel.Name = "fieldsPanel";
        fieldsPanel.RowCount = 1;
        fieldsPanel.RowStyles.Add(new RowStyle());
        fieldsPanel.TabIndex = 1;
        //
        // passwordLabel
        //
        passwordLabel.Anchor = AnchorStyles.Left;
        passwordLabel.AutoSize = true;
        passwordLabel.Name = "passwordLabel";
        passwordLabel.TabIndex = 0;
        passwordLabel.Text = "Pass&word:";
        //
        // passwordBox
        //
        passwordBox.Name = "passwordBox";
        passwordBox.Size = new Size(260, 23);
        passwordBox.TabIndex = 1;
        passwordBox.UseSystemPasswordChar = true;
        //
        // problemLabel
        //
        problemLabel.AutoSize = true;
        problemLabel.ForeColor = Color.Firebrick;
        problemLabel.Margin = new Padding(3, 3, 3, 3);
        problemLabel.MaximumSize = new Size(440, 0);
        problemLabel.Name = "problemLabel";
        problemLabel.TabIndex = 2;
        problemLabel.UseMnemonic = false;
        //
        // buttonPanel
        //
        buttonPanel.AutoSize = true;
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(withoutKeysButton);
        buttonPanel.Controls.Add(restoreButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Margin = new Padding(3, 9, 3, 3);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.TabIndex = 3;
        //
        // cancelButton
        //
        cancelButton.AutoSize = true;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Name = "cancelButton";
        cancelButton.TabIndex = 2;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        //
        // withoutKeysButton
        //
        withoutKeysButton.AutoSize = true;
        withoutKeysButton.DialogResult = DialogResult.Ignore;
        withoutKeysButton.Name = "withoutKeysButton";
        withoutKeysButton.TabIndex = 1;
        withoutKeysButton.Text = "Restore without the &keys";
        withoutKeysButton.UseVisualStyleBackColor = true;
        //
        // restoreButton
        //
        restoreButton.AutoSize = true;
        restoreButton.DialogResult = DialogResult.OK;
        restoreButton.Name = "restoreButton";
        restoreButton.TabIndex = 0;
        restoreButton.Text = "&Restore";
        restoreButton.UseVisualStyleBackColor = true;
        //
        // BackupPasswordForm
        //
        AcceptButton = restoreButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        CancelButton = cancelButton;
        ClientSize = new Size(460, 160);
        Controls.Add(layout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "BackupPasswordForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Restore a backup";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        fieldsPanel.ResumeLayout(false);
        fieldsPanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel layout;
    private Label promptLabel;
    private TableLayoutPanel fieldsPanel;
    private Label passwordLabel;
    private TextBox passwordBox;
    private Label problemLabel;
    private FlowLayoutPanel buttonPanel;
    private Button cancelButton;
    private Button withoutKeysButton;
    private Button restoreButton;
}
