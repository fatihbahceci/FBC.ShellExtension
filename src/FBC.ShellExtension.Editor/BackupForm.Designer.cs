#nullable disable

namespace FBC.ShellExtension.Editor;

partial class BackupForm
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
        introLabel = new Label();
        keysGroup = new GroupBox();
        keysLayout = new TableLayoutPanel();
        keysLabel = new Label();
        passwordRadio = new RadioButton();
        passwordPanel = new TableLayoutPanel();
        passwordLabel = new Label();
        passwordBox = new TextBox();
        confirmLabel = new Label();
        confirmBox = new TextBox();
        plainRadio = new RadioButton();
        noneRadio = new RadioButton();
        unreadableLabel = new Label();
        problemsLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        saveButton = new Button();
        layout.SuspendLayout();
        keysGroup.SuspendLayout();
        keysLayout.SuspendLayout();
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
        layout.Controls.Add(introLabel, 0, 0);
        layout.Controls.Add(keysGroup, 0, 1);
        layout.Controls.Add(unreadableLabel, 0, 2);
        layout.Controls.Add(problemsLabel, 0, 3);
        layout.Controls.Add(buttonPanel, 0, 4);
        layout.Location = new Point(0, 0);
        layout.Name = "layout";
        layout.Padding = new Padding(8);
        layout.RowCount = 5;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.TabIndex = 0;
        //
        // introLabel
        //
        introLabel.AutoSize = true;
        introLabel.Margin = new Padding(3, 3, 3, 9);
        introLabel.MaximumSize = new Size(480, 0);
        introLabel.Name = "introLabel";
        introLabel.TabIndex = 0;
        introLabel.UseMnemonic = false;
        //
        // keysGroup
        //
        keysGroup.AutoSize = true;
        keysGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        keysGroup.Controls.Add(keysLayout);
        keysGroup.Dock = DockStyle.Fill;
        keysGroup.Name = "keysGroup";
        keysGroup.Padding = new Padding(8, 4, 8, 8);
        keysGroup.TabIndex = 1;
        keysGroup.TabStop = false;
        keysGroup.Text = "Keys";
        //
        // keysLayout
        //
        keysLayout.AutoSize = true;
        keysLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        keysLayout.ColumnCount = 1;
        keysLayout.ColumnStyles.Add(new ColumnStyle());
        keysLayout.Controls.Add(keysLabel, 0, 0);
        keysLayout.Controls.Add(passwordRadio, 0, 1);
        keysLayout.Controls.Add(passwordPanel, 0, 2);
        keysLayout.Controls.Add(plainRadio, 0, 3);
        keysLayout.Controls.Add(noneRadio, 0, 4);
        keysLayout.Dock = DockStyle.Fill;
        keysLayout.Name = "keysLayout";
        keysLayout.RowCount = 5;
        keysLayout.RowStyles.Add(new RowStyle());
        keysLayout.RowStyles.Add(new RowStyle());
        keysLayout.RowStyles.Add(new RowStyle());
        keysLayout.RowStyles.Add(new RowStyle());
        keysLayout.RowStyles.Add(new RowStyle());
        keysLayout.TabIndex = 0;
        //
        // keysLabel
        //
        keysLabel.AutoSize = true;
        keysLabel.Margin = new Padding(3, 3, 3, 6);
        keysLabel.MaximumSize = new Size(450, 0);
        keysLabel.Name = "keysLabel";
        keysLabel.TabIndex = 0;
        keysLabel.UseMnemonic = false;
        //
        // passwordRadio
        //
        passwordRadio.AutoSize = true;
        passwordRadio.Name = "passwordRadio";
        passwordRadio.TabIndex = 1;
        passwordRadio.TabStop = true;
        passwordRadio.Text = "&Encrypt them with a password";
        passwordRadio.UseVisualStyleBackColor = true;
        passwordRadio.CheckedChanged += OnKeysChoiceChanged;
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
        passwordPanel.Margin = new Padding(20, 0, 3, 3);
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
        passwordLabel.Text = "Pass&word:";
        //
        // passwordBox
        //
        passwordBox.Name = "passwordBox";
        passwordBox.Size = new Size(220, 23);
        passwordBox.TabIndex = 1;
        passwordBox.UseSystemPasswordChar = true;
        //
        // confirmLabel
        //
        confirmLabel.Anchor = AnchorStyles.Left;
        confirmLabel.AutoSize = true;
        confirmLabel.Name = "confirmLabel";
        confirmLabel.TabIndex = 2;
        confirmLabel.Text = "C&onfirm:";
        //
        // confirmBox
        //
        confirmBox.Name = "confirmBox";
        confirmBox.Size = new Size(220, 23);
        confirmBox.TabIndex = 3;
        confirmBox.UseSystemPasswordChar = true;
        //
        // plainRadio
        //
        plainRadio.AutoSize = true;
        plainRadio.MaximumSize = new Size(450, 0);
        plainRadio.Name = "plainRadio";
        plainRadio.TabIndex = 3;
        plainRadio.Text = "Write them as &plain text";
        plainRadio.UseVisualStyleBackColor = true;
        plainRadio.CheckedChanged += OnKeysChoiceChanged;
        //
        // noneRadio
        //
        noneRadio.AutoSize = true;
        noneRadio.MaximumSize = new Size(450, 0);
        noneRadio.Name = "noneRadio";
        noneRadio.TabIndex = 4;
        noneRadio.Text = "&Leave them out";
        noneRadio.UseVisualStyleBackColor = true;
        noneRadio.CheckedChanged += OnKeysChoiceChanged;
        //
        // unreadableLabel
        //
        unreadableLabel.AutoSize = true;
        unreadableLabel.ForeColor = SystemColors.GrayText;
        unreadableLabel.Margin = new Padding(3, 6, 3, 3);
        unreadableLabel.MaximumSize = new Size(480, 0);
        unreadableLabel.Name = "unreadableLabel";
        unreadableLabel.TabIndex = 2;
        unreadableLabel.UseMnemonic = false;
        //
        // problemsLabel
        //
        problemsLabel.AutoSize = true;
        problemsLabel.ForeColor = Color.Firebrick;
        problemsLabel.Margin = new Padding(3, 6, 3, 3);
        problemsLabel.MaximumSize = new Size(480, 0);
        problemsLabel.Name = "problemsLabel";
        problemsLabel.TabIndex = 3;
        problemsLabel.UseMnemonic = false;
        //
        // buttonPanel
        //
        buttonPanel.AutoSize = true;
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Margin = new Padding(3, 9, 3, 3);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.TabIndex = 4;
        //
        // cancelButton
        //
        cancelButton.AutoSize = true;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Name = "cancelButton";
        cancelButton.TabIndex = 1;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        //
        // saveButton
        //
        saveButton.AutoSize = true;
        saveButton.Name = "saveButton";
        saveButton.TabIndex = 0;
        saveButton.Text = "&Save…";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += OnSave;
        //
        // BackupForm
        //
        AcceptButton = saveButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        CancelButton = cancelButton;
        ClientSize = new Size(500, 300);
        Controls.Add(layout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "BackupForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Back up";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        keysGroup.ResumeLayout(false);
        keysGroup.PerformLayout();
        keysLayout.ResumeLayout(false);
        keysLayout.PerformLayout();
        passwordPanel.ResumeLayout(false);
        passwordPanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel layout;
    private Label introLabel;
    private GroupBox keysGroup;
    private TableLayoutPanel keysLayout;
    private Label keysLabel;
    private RadioButton passwordRadio;
    private TableLayoutPanel passwordPanel;
    private Label passwordLabel;
    private TextBox passwordBox;
    private Label confirmLabel;
    private TextBox confirmBox;
    private RadioButton plainRadio;
    private RadioButton noneRadio;
    private Label unreadableLabel;
    private Label problemsLabel;
    private FlowLayoutPanel buttonPanel;
    private Button cancelButton;
    private Button saveButton;
}
