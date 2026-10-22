#nullable disable

namespace FBC.ShellExtension.Editor.Tools;

partial class DecryptForm
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
        selectionLabel = new Label();
        statusLabel = new Label();
        passwordPanel = new FlowLayoutPanel();
        passwordLabel = new Label();
        passwordBox = new TextBox();
        deleteCheck = new CheckBox();
        hintLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        decryptButton = new Button();
        layout.SuspendLayout();
        passwordPanel.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // layout
        //
        layout.AutoSize = true;
        layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle());
        layout.Controls.Add(selectionLabel, 0, 0);
        layout.Controls.Add(statusLabel, 0, 1);
        layout.Controls.Add(passwordPanel, 0, 2);
        layout.Controls.Add(deleteCheck, 0, 3);
        layout.Controls.Add(hintLabel, 0, 4);
        layout.Controls.Add(buttonPanel, 0, 5);
        layout.Location = new Point(0, 0);
        layout.Name = "layout";
        layout.Padding = new Padding(8);
        layout.RowCount = 6;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.TabIndex = 0;
        //
        // selectionLabel
        //
        selectionLabel.AutoSize = true;
        selectionLabel.Margin = new Padding(3, 3, 3, 9);
        selectionLabel.MaximumSize = new Size(440, 0);
        selectionLabel.Name = "selectionLabel";
        selectionLabel.TabIndex = 0;
        selectionLabel.Text = "Selected:";
        selectionLabel.UseMnemonic = false;
        //
        // statusLabel
        //
        statusLabel.AutoSize = true;
        statusLabel.Margin = new Padding(3, 3, 3, 9);
        statusLabel.MaximumSize = new Size(440, 0);
        statusLabel.Name = "statusLabel";
        statusLabel.TabIndex = 1;
        statusLabel.UseMnemonic = false;
        //
        // passwordPanel
        //
        passwordPanel.AutoSize = true;
        passwordPanel.Controls.Add(passwordLabel);
        passwordPanel.Controls.Add(passwordBox);
        passwordPanel.Name = "passwordPanel";
        passwordPanel.TabIndex = 2;
        passwordPanel.Visible = false;
        passwordPanel.WrapContents = false;
        //
        // passwordLabel
        //
        passwordLabel.Anchor = AnchorStyles.Left;
        passwordLabel.AutoSize = true;
        passwordLabel.Name = "passwordLabel";
        passwordLabel.TabIndex = 0;
        passwordLabel.Text = "Password:";
        //
        // passwordBox
        //
        passwordBox.Name = "passwordBox";
        passwordBox.Size = new Size(240, 23);
        passwordBox.TabIndex = 1;
        passwordBox.UseSystemPasswordChar = true;
        //
        // deleteCheck
        //
        deleteCheck.AutoSize = true;
        deleteCheck.Checked = true;
        deleteCheck.CheckState = CheckState.Checked;
        deleteCheck.Name = "deleteCheck";
        deleteCheck.TabIndex = 3;
        deleteCheck.Text = "Delete the encrypted files";
        deleteCheck.UseVisualStyleBackColor = true;
        deleteCheck.Visible = false;
        //
        // hintLabel
        //
        hintLabel.AutoSize = true;
        hintLabel.ForeColor = SystemColors.GrayText;
        hintLabel.Margin = new Padding(3, 3, 3, 9);
        hintLabel.MaximumSize = new Size(440, 0);
        hintLabel.Name = "hintLabel";
        hintLabel.TabIndex = 4;
        hintLabel.Text = "A password file comes back next to itself.";
        //
        // buttonPanel
        //
        buttonPanel.AutoSize = true;
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(decryptButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Name = "buttonPanel";
        buttonPanel.TabIndex = 5;
        //
        // cancelButton
        //
        cancelButton.AutoSize = true;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Name = "cancelButton";
        cancelButton.TabIndex = 1;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += OnClose;
        //
        // decryptButton
        //
        decryptButton.AutoSize = true;
        decryptButton.Enabled = false;
        decryptButton.Name = "decryptButton";
        decryptButton.TabIndex = 0;
        decryptButton.Text = "Decrypt";
        decryptButton.UseVisualStyleBackColor = true;
        decryptButton.Click += OnDecrypt;
        //
        // DecryptForm
        //
        AcceptButton = decryptButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        CancelButton = cancelButton;
        ClientSize = new Size(460, 240);
        Controls.Add(layout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "DecryptForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Decrypt";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        passwordPanel.ResumeLayout(false);
        passwordPanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel layout;
    private Label selectionLabel;
    private Label statusLabel;
    private FlowLayoutPanel passwordPanel;
    private Label passwordLabel;
    private TextBox passwordBox;
    private CheckBox deleteCheck;
    private Label hintLabel;
    private FlowLayoutPanel buttonPanel;
    private Button cancelButton;
    private Button decryptButton;
}
