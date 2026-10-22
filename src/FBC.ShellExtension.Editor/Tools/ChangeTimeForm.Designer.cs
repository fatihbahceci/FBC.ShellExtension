#nullable disable

namespace FBC.ShellExtension.Editor.Tools;

partial class ChangeTimeForm
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
        timesPanel = new TableLayoutPanel();
        createdCheck = new CheckBox();
        createdPicker = new DateTimePicker();
        modifiedCheck = new CheckBox();
        modifiedPicker = new DateTimePicker();
        accessedCheck = new CheckBox();
        accessedPicker = new DateTimePicker();
        nowButton = new Button();
        hintLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        applyButton = new Button();
        layout.SuspendLayout();
        timesPanel.SuspendLayout();
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
        layout.Controls.Add(timesPanel, 0, 2);
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
        //
        // timesPanel
        //
        timesPanel.AutoSize = true;
        timesPanel.ColumnCount = 2;
        timesPanel.ColumnStyles.Add(new ColumnStyle());
        timesPanel.ColumnStyles.Add(new ColumnStyle());
        timesPanel.Controls.Add(createdCheck, 0, 0);
        timesPanel.Controls.Add(createdPicker, 1, 0);
        timesPanel.Controls.Add(modifiedCheck, 0, 1);
        timesPanel.Controls.Add(modifiedPicker, 1, 1);
        timesPanel.Controls.Add(accessedCheck, 0, 2);
        timesPanel.Controls.Add(accessedPicker, 1, 2);
        timesPanel.Controls.Add(nowButton, 1, 3);
        timesPanel.Margin = new Padding(3, 6, 3, 6);
        timesPanel.Name = "timesPanel";
        timesPanel.RowCount = 4;
        timesPanel.RowStyles.Add(new RowStyle());
        timesPanel.RowStyles.Add(new RowStyle());
        timesPanel.RowStyles.Add(new RowStyle());
        timesPanel.RowStyles.Add(new RowStyle());
        timesPanel.TabIndex = 2;
        //
        // createdCheck
        //
        createdCheck.Anchor = AnchorStyles.Left;
        createdCheck.AutoSize = true;
        createdCheck.Name = "createdCheck";
        createdCheck.TabIndex = 0;
        createdCheck.Text = "Created:";
        createdCheck.UseVisualStyleBackColor = true;
        //
        // createdPicker
        //
        createdPicker.Format = DateTimePickerFormat.Custom;
        createdPicker.Name = "createdPicker";
        createdPicker.Size = new Size(220, 23);
        createdPicker.TabIndex = 1;
        createdPicker.ValueChanged += OnTimeChanged;
        //
        // modifiedCheck
        //
        modifiedCheck.Anchor = AnchorStyles.Left;
        modifiedCheck.AutoSize = true;
        modifiedCheck.Name = "modifiedCheck";
        modifiedCheck.TabIndex = 2;
        modifiedCheck.Text = "Modified:";
        modifiedCheck.UseVisualStyleBackColor = true;
        //
        // modifiedPicker
        //
        modifiedPicker.Format = DateTimePickerFormat.Custom;
        modifiedPicker.Name = "modifiedPicker";
        modifiedPicker.Size = new Size(220, 23);
        modifiedPicker.TabIndex = 3;
        modifiedPicker.ValueChanged += OnTimeChanged;
        //
        // accessedCheck
        //
        accessedCheck.Anchor = AnchorStyles.Left;
        accessedCheck.AutoSize = true;
        accessedCheck.Name = "accessedCheck";
        accessedCheck.TabIndex = 4;
        accessedCheck.Text = "Accessed:";
        accessedCheck.UseVisualStyleBackColor = true;
        //
        // accessedPicker
        //
        accessedPicker.Format = DateTimePickerFormat.Custom;
        accessedPicker.Name = "accessedPicker";
        accessedPicker.Size = new Size(220, 23);
        accessedPicker.TabIndex = 5;
        accessedPicker.ValueChanged += OnTimeChanged;
        //
        // nowButton
        //
        nowButton.AutoSize = true;
        nowButton.Name = "nowButton";
        nowButton.TabIndex = 6;
        nowButton.Text = "Now";
        nowButton.UseVisualStyleBackColor = true;
        nowButton.Click += OnNow;
        //
        // hintLabel
        //
        hintLabel.AutoSize = true;
        hintLabel.ForeColor = SystemColors.GrayText;
        hintLabel.Margin = new Padding(3, 3, 3, 9);
        hintLabel.MaximumSize = new Size(420, 0);
        hintLabel.Name = "hintLabel";
        hintLabel.TabIndex = 3;
        hintLabel.Text = "Only the ticked times are written.";
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
        // ChangeTimeForm
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
        Name = "ChangeTimeForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Change time";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        timesPanel.ResumeLayout(false);
        timesPanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel layout;
    private Label selectionLabel;
    private CheckBox insideCheck;
    private TableLayoutPanel timesPanel;
    private CheckBox createdCheck;
    private DateTimePicker createdPicker;
    private CheckBox modifiedCheck;
    private DateTimePicker modifiedPicker;
    private CheckBox accessedCheck;
    private DateTimePicker accessedPicker;
    private Button nowButton;
    private Label hintLabel;
    private FlowLayoutPanel buttonPanel;
    private Button cancelButton;
    private Button applyButton;
}
