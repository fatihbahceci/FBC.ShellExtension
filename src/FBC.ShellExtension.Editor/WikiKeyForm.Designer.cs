#nullable disable

namespace FBC.ShellExtension.Editor;

partial class WikiKeyForm
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
        addressLabel = new Label();
        addressBox = new ComboBox();
        keyLabel = new Label();
        keyBox = new TextBox();
        statusLabel = new Label();
        environmentLabel = new Label();
        helpLabel = new Label();
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
        layout.Controls.Add(environmentLabel, 0, 2);
        layout.Controls.Add(helpLabel, 0, 3);
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
        // fieldsPanel
        //
        fieldsPanel.AutoSize = true;
        fieldsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        fieldsPanel.ColumnCount = 2;
        fieldsPanel.ColumnStyles.Add(new ColumnStyle());
        fieldsPanel.ColumnStyles.Add(new ColumnStyle());
        fieldsPanel.Controls.Add(addressLabel, 0, 0);
        fieldsPanel.Controls.Add(addressBox, 1, 0);
        fieldsPanel.Controls.Add(keyLabel, 0, 1);
        fieldsPanel.Controls.Add(keyBox, 1, 1);
        fieldsPanel.Name = "fieldsPanel";
        fieldsPanel.RowCount = 2;
        fieldsPanel.RowStyles.Add(new RowStyle());
        fieldsPanel.RowStyles.Add(new RowStyle());
        fieldsPanel.TabIndex = 0;
        //
        // addressLabel
        //
        addressLabel.Anchor = AnchorStyles.Left;
        addressLabel.AutoSize = true;
        addressLabel.Name = "addressLabel";
        addressLabel.TabIndex = 0;
        addressLabel.Text = "&Wiki address:";
        //
        // addressBox
        //
        addressBox.Name = "addressBox";
        addressBox.Size = new Size(320, 23);
        addressBox.TabIndex = 1;
        addressBox.TextChanged += OnInputChanged;
        //
        // keyLabel
        //
        keyLabel.Anchor = AnchorStyles.Left;
        keyLabel.AutoSize = true;
        keyLabel.Name = "keyLabel";
        keyLabel.TabIndex = 2;
        keyLabel.Text = "API &key or token:";
        //
        // keyBox
        //
        keyBox.Name = "keyBox";
        keyBox.Size = new Size(320, 23);
        keyBox.TabIndex = 3;
        keyBox.UseSystemPasswordChar = true;
        keyBox.TextChanged += OnInputChanged;
        //
        // statusLabel
        //
        statusLabel.AutoSize = true;
        statusLabel.Margin = new Padding(3, 6, 3, 6);
        statusLabel.MaximumSize = new Size(460, 0);
        statusLabel.Name = "statusLabel";
        statusLabel.TabIndex = 1;
        statusLabel.UseMnemonic = false;
        //
        // environmentLabel
        //
        environmentLabel.AutoSize = true;
        environmentLabel.ForeColor = Color.Firebrick;
        environmentLabel.Margin = new Padding(3, 0, 3, 6);
        environmentLabel.MaximumSize = new Size(460, 0);
        environmentLabel.Name = "environmentLabel";
        environmentLabel.TabIndex = 2;
        environmentLabel.UseMnemonic = false;
        environmentLabel.Visible = false;
        //
        // helpLabel
        //
        helpLabel.AutoSize = true;
        helpLabel.ForeColor = SystemColors.GrayText;
        helpLabel.Margin = new Padding(3, 3, 3, 9);
        helpLabel.MaximumSize = new Size(460, 0);
        helpLabel.Name = "helpLabel";
        helpLabel.TabIndex = 3;
        helpLabel.UseMnemonic = false;
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
        buttonPanel.TabIndex = 4;
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
        // WikiKeyForm
        //
        AcceptButton = saveButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        CancelButton = closeButton;
        ClientSize = new Size(480, 300);
        Controls.Add(layout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "WikiKeyForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Wiki.js key or token";
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
    private Label addressLabel;
    private ComboBox addressBox;
    private Label keyLabel;
    private TextBox keyBox;
    private Label statusLabel;
    private Label environmentLabel;
    private Label helpLabel;
    private FlowLayoutPanel buttonPanel;
    private Button closeButton;
    private Button saveButton;
    private Button forgetButton;
}
