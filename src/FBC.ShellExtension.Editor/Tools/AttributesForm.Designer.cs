#nullable disable

namespace FBC.ShellExtension.Editor.Tools;

partial class AttributesForm
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
        insideCheck = new CheckBox();
        boxesPanel = new FlowLayoutPanel();
        readOnlyCheck = new CheckBox();
        hiddenCheck = new CheckBox();
        systemCheck = new CheckBox();
        archiveCheck = new CheckBox();
        hintLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        applyButton = new Button();
        layout.SuspendLayout();
        boxesPanel.SuspendLayout();
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
        layout.Controls.Add(insideCheck, 0, 1);
        layout.Controls.Add(boxesPanel, 0, 2);
        layout.Controls.Add(hintLabel, 0, 3);
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
        // selectionLabel
        //
        selectionLabel.AutoSize = true;
        selectionLabel.Margin = new Padding(3, 3, 3, 9);
        selectionLabel.MaximumSize = new Size(420, 0);
        selectionLabel.Name = "selectionLabel";
        selectionLabel.TabIndex = 0;
        selectionLabel.Text = "Selected:";
        selectionLabel.UseMnemonic = false;
        //
        // insideCheck
        //
        insideCheck.AutoSize = true;
        insideCheck.Name = "insideCheck";
        insideCheck.TabIndex = 1;
        insideCheck.Text = "Also everything inside the selected folders";
        insideCheck.UseVisualStyleBackColor = true;
        insideCheck.CheckedChanged += OnInsideChanged;
        //
        // boxesPanel
        //
        boxesPanel.AutoSize = true;
        boxesPanel.Controls.Add(readOnlyCheck);
        boxesPanel.Controls.Add(hiddenCheck);
        boxesPanel.Controls.Add(systemCheck);
        boxesPanel.Controls.Add(archiveCheck);
        boxesPanel.FlowDirection = FlowDirection.TopDown;
        boxesPanel.Margin = new Padding(12, 6, 3, 6);
        boxesPanel.Name = "boxesPanel";
        boxesPanel.TabIndex = 2;
        boxesPanel.WrapContents = false;
        //
        // readOnlyCheck
        //
        readOnlyCheck.AutoSize = true;
        readOnlyCheck.Name = "readOnlyCheck";
        readOnlyCheck.TabIndex = 0;
        readOnlyCheck.Text = "Read-only";
        readOnlyCheck.UseVisualStyleBackColor = true;
        //
        // hiddenCheck
        //
        hiddenCheck.AutoSize = true;
        hiddenCheck.Name = "hiddenCheck";
        hiddenCheck.TabIndex = 1;
        hiddenCheck.Text = "Hidden";
        hiddenCheck.UseVisualStyleBackColor = true;
        //
        // systemCheck
        //
        systemCheck.AutoSize = true;
        systemCheck.Name = "systemCheck";
        systemCheck.TabIndex = 2;
        systemCheck.Text = "System";
        systemCheck.UseVisualStyleBackColor = true;
        //
        // archiveCheck
        //
        archiveCheck.AutoSize = true;
        archiveCheck.Name = "archiveCheck";
        archiveCheck.TabIndex = 3;
        archiveCheck.Text = "Archive";
        archiveCheck.UseVisualStyleBackColor = true;
        //
        // hintLabel
        //
        hintLabel.AutoSize = true;
        hintLabel.ForeColor = SystemColors.GrayText;
        hintLabel.Margin = new Padding(3, 3, 3, 9);
        hintLabel.MaximumSize = new Size(420, 0);
        hintLabel.Name = "hintLabel";
        hintLabel.TabIndex = 3;
        hintLabel.Text = "Only what you change is written.";
        //
        // buttonPanel
        //
        buttonPanel.AutoSize = true;
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(applyButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
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
        cancelButton.Click += OnClose;
        //
        // applyButton
        //
        applyButton.AutoSize = true;
        applyButton.Name = "applyButton";
        applyButton.TabIndex = 0;
        applyButton.Text = "Apply";
        applyButton.UseVisualStyleBackColor = true;
        applyButton.Click += OnApply;
        //
        // AttributesForm
        //
        AcceptButton = applyButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        CancelButton = cancelButton;
        ClientSize = new Size(440, 300);
        Controls.Add(layout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "AttributesForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Attributes";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        boxesPanel.ResumeLayout(false);
        boxesPanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel layout;
    private Label selectionLabel;
    private CheckBox insideCheck;
    private FlowLayoutPanel boxesPanel;
    private CheckBox readOnlyCheck;
    private CheckBox hiddenCheck;
    private CheckBox systemCheck;
    private CheckBox archiveCheck;
    private Label hintLabel;
    private FlowLayoutPanel buttonPanel;
    private Button cancelButton;
    private Button applyButton;
}
