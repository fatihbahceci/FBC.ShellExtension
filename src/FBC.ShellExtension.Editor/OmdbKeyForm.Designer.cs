#nullable disable

namespace FBC.ShellExtension.Editor;

partial class OmdbKeyForm
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
        fieldsPanel = new TableLayoutPanel();
        keyLabel = new Label();
        keyBox = new TextBox();
        statusLabel = new Label();
        helpLabel = new LinkLabel();
        buttonPanel = new FlowLayoutPanel();
        closeButton = new Button();
        saveButton = new Button();
        forgetButton = new Button();
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
        layout.Controls.Add(fieldsPanel, 0, 0);
        layout.Controls.Add(statusLabel, 0, 1);
        layout.Controls.Add(helpLabel, 0, 2);
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
        // fieldsPanel
        //
        fieldsPanel.AutoSize = true;
        fieldsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        fieldsPanel.ColumnCount = 2;
        fieldsPanel.ColumnStyles.Add(new ColumnStyle());
        fieldsPanel.ColumnStyles.Add(new ColumnStyle());
        fieldsPanel.Controls.Add(keyLabel, 0, 0);
        fieldsPanel.Controls.Add(keyBox, 1, 0);
        fieldsPanel.Name = "fieldsPanel";
        fieldsPanel.RowCount = 1;
        fieldsPanel.RowStyles.Add(new RowStyle());
        fieldsPanel.TabIndex = 0;
        //
        // keyLabel
        //
        keyLabel.Anchor = AnchorStyles.Left;
        keyLabel.AutoSize = true;
        keyLabel.Name = "keyLabel";
        keyLabel.TabIndex = 0;
        keyLabel.Text = "API &key:";
        //
        // keyBox
        //
        keyBox.Name = "keyBox";
        keyBox.Size = new Size(320, 23);
        keyBox.TabIndex = 1;
        keyBox.UseSystemPasswordChar = true;
        keyBox.TextChanged += OnInputChanged;
        //
        // statusLabel
        //
        statusLabel.AutoSize = true;
        statusLabel.Margin = new Padding(3, 6, 3, 6);
        statusLabel.MaximumSize = new Size(440, 0);
        statusLabel.Name = "statusLabel";
        statusLabel.TabIndex = 1;
        statusLabel.UseMnemonic = false;
        //
        // helpLabel
        //
        helpLabel.AutoSize = true;
        helpLabel.ForeColor = SystemColors.GrayText;
        helpLabel.Margin = new Padding(3, 3, 3, 9);
        helpLabel.MaximumSize = new Size(440, 0);
        helpLabel.Name = "helpLabel";
        helpLabel.TabIndex = 2;
        helpLabel.TabStop = true;
        helpLabel.UseMnemonic = false;
        helpLabel.LinkClicked += OnHelpLink;
        //
        // buttonPanel
        //
        buttonPanel.AutoSize = true;
        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(forgetButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Name = "buttonPanel";
        buttonPanel.TabIndex = 3;
        //
        // closeButton
        //
        closeButton.AutoSize = true;
        closeButton.DialogResult = DialogResult.Cancel;
        closeButton.Name = "closeButton";
        closeButton.TabIndex = 2;
        closeButton.Text = "Close";
        closeButton.UseVisualStyleBackColor = true;
        closeButton.Click += OnClose;
        //
        // saveButton
        //
        saveButton.AutoSize = true;
        saveButton.Enabled = false;
        saveButton.Name = "saveButton";
        saveButton.TabIndex = 0;
        saveButton.Text = "Check and &save";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += OnSave;
        //
        // forgetButton
        //
        forgetButton.AutoSize = true;
        forgetButton.Enabled = false;
        forgetButton.Name = "forgetButton";
        forgetButton.TabIndex = 1;
        forgetButton.Text = "&Forget";
        forgetButton.UseVisualStyleBackColor = true;
        forgetButton.Click += OnForget;
        //
        // OmdbKeyForm
        //
        AcceptButton = saveButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        CancelButton = closeButton;
        ClientSize = new Size(460, 200);
        Controls.Add(layout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "OmdbKeyForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "OMDb API key";
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
    private TableLayoutPanel fieldsPanel;
    private Label keyLabel;
    private TextBox keyBox;
    private Label statusLabel;
    private LinkLabel helpLabel;
    private FlowLayoutPanel buttonPanel;
    private Button closeButton;
    private Button saveButton;
    private Button forgetButton;
}
