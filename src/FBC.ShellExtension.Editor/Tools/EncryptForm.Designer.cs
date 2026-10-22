#nullable disable

namespace FBC.ShellExtension.Editor.Tools;

partial class EncryptForm
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
        passwordRadio = new RadioButton();
        passwordPanel = new TableLayoutPanel();
        passwordLabel = new Label();
        passwordBox = new TextBox();
        confirmLabel = new Label();
        confirmBox = new TextBox();
        deleteCheck = new CheckBox();
        passwordHintLabel = new Label();
        efsRadio = new RadioButton();
        efsHintLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        encryptButton = new Button();
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
        layout.Controls.Add(passwordRadio, 0, 1);
        layout.Controls.Add(passwordPanel, 0, 2);
        layout.Controls.Add(deleteCheck, 0, 3);
        layout.Controls.Add(passwordHintLabel, 0, 4);
        layout.Controls.Add(efsRadio, 0, 5);
        layout.Controls.Add(efsHintLabel, 0, 6);
        layout.Controls.Add(buttonPanel, 0, 7);
        layout.Location = new Point(0, 0);
        layout.Name = "layout";
        layout.Padding = new Padding(8);
        layout.RowCount = 8;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
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
        // passwordRadio
        //
        passwordRadio.AutoSize = true;
        passwordRadio.Name = "passwordRadio";
        passwordRadio.TabIndex = 1;
        passwordRadio.TabStop = true;
        passwordRadio.Text = "With a password";
        passwordRadio.UseVisualStyleBackColor = true;
        passwordRadio.CheckedChanged += OnMethodChanged;
        //
        // passwordPanel
        //
        passwordPanel.AutoSize = true;
        passwordPanel.ColumnCount = 2;
        passwordPanel.ColumnStyles.Add(new ColumnStyle());
        passwordPanel.ColumnStyles.Add(new ColumnStyle());
        passwordPanel.Controls.Add(passwordLabel, 0, 0);
        passwordPanel.Controls.Add(passwordBox, 1, 0);
        passwordPanel.Controls.Add(confirmLabel, 0, 1);
        passwordPanel.Controls.Add(confirmBox, 1, 1);
        passwordPanel.Margin = new Padding(20, 3, 3, 3);
        passwordPanel.Name = "passwordPanel";
        passwordPanel.RowCount = 2;
        passwordPanel.RowStyles.Add(new RowStyle());
        passwordPanel.RowStyles.Add(new RowStyle());
        passwordPanel.TabIndex = 2;
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
        // confirmLabel
        //
        confirmLabel.Anchor = AnchorStyles.Left;
        confirmLabel.AutoSize = true;
        confirmLabel.Name = "confirmLabel";
        confirmLabel.TabIndex = 2;
        confirmLabel.Text = "Confirm:";
        //
        // confirmBox
        //
        confirmBox.Name = "confirmBox";
        confirmBox.Size = new Size(240, 23);
        confirmBox.TabIndex = 3;
        confirmBox.UseSystemPasswordChar = true;
        //
        // deleteCheck
        //
        deleteCheck.AutoSize = true;
        deleteCheck.Checked = true;
        deleteCheck.CheckState = CheckState.Checked;
        deleteCheck.Margin = new Padding(20, 3, 3, 3);
        deleteCheck.Name = "deleteCheck";
        deleteCheck.TabIndex = 3;
        deleteCheck.Text = "Delete the originals";
        deleteCheck.UseVisualStyleBackColor = true;
        //
        // passwordHintLabel
        //
        passwordHintLabel.AutoSize = true;
        passwordHintLabel.ForeColor = SystemColors.GrayText;
        passwordHintLabel.Margin = new Padding(20, 3, 3, 9);
        passwordHintLabel.MaximumSize = new Size(420, 0);
        passwordHintLabel.Name = "passwordHintLabel";
        passwordHintLabel.TabIndex = 4;
        passwordHintLabel.Text = "A forgotten password cannot be recovered.";
        //
        // efsRadio
        //
        efsRadio.AutoSize = true;
        efsRadio.Name = "efsRadio";
        efsRadio.TabIndex = 5;
        efsRadio.Text = "With Windows (EFS)";
        efsRadio.UseVisualStyleBackColor = true;
        efsRadio.CheckedChanged += OnMethodChanged;
        //
        // efsHintLabel
        //
        efsHintLabel.AutoSize = true;
        efsHintLabel.ForeColor = SystemColors.GrayText;
        efsHintLabel.Margin = new Padding(20, 3, 3, 9);
        efsHintLabel.MaximumSize = new Size(420, 0);
        efsHintLabel.Name = "efsHintLabel";
        efsHintLabel.TabIndex = 6;
        efsHintLabel.Text = "Only your Windows account opens them.";
        //
        // buttonPanel
        //
        buttonPanel.AutoSize = true;
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(encryptButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Name = "buttonPanel";
        buttonPanel.TabIndex = 7;
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
        // encryptButton
        //
        encryptButton.AutoSize = true;
        encryptButton.Name = "encryptButton";
        encryptButton.TabIndex = 0;
        encryptButton.Text = "Encrypt";
        encryptButton.UseVisualStyleBackColor = true;
        encryptButton.Click += OnEncrypt;
        //
        // EncryptForm
        //
        AcceptButton = encryptButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        CancelButton = cancelButton;
        ClientSize = new Size(460, 340);
        Controls.Add(layout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "EncryptForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Encrypt";
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
    private RadioButton passwordRadio;
    private TableLayoutPanel passwordPanel;
    private Label passwordLabel;
    private TextBox passwordBox;
    private Label confirmLabel;
    private TextBox confirmBox;
    private CheckBox deleteCheck;
    private Label passwordHintLabel;
    private RadioButton efsRadio;
    private Label efsHintLabel;
    private FlowLayoutPanel buttonPanel;
    private Button cancelButton;
    private Button encryptButton;
}
