#nullable disable

namespace FBC.ShellExtension.Editor;

partial class MatchRuleEditor
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

    #region Component Designer generated code

    private void InitializeComponent()
    {
        layout = new TableLayoutPanel();
        targetsLabel = new Label();
        targetsPanel = new FlowLayoutPanel();
        filesCheck = new CheckBox();
        foldersCheck = new CheckBox();
        backgroundCheck = new CheckBox();
        drivesCheck = new CheckBox();
        targetsHint = new Label();
        extensionsLabel = new Label();
        extensionsBox = new TextBox();
        extensionsHint = new Label();
        patternsLabel = new Label();
        patternsBox = new TextBox();
        patternsHint = new Label();
        excludeLabel = new Label();
        excludeBox = new TextBox();
        modeLabel = new Label();
        modeCombo = new ComboBox();
        countPanel = new FlowLayoutPanel();
        minLabel = new Label();
        minCount = new NumericUpDown();
        maxCheck = new CheckBox();
        maxCount = new NumericUpDown();
        layout.SuspendLayout();
        targetsPanel.SuspendLayout();
        countPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)minCount).BeginInit();
        ((System.ComponentModel.ISupportInitialize)maxCount).BeginInit();
        SuspendLayout();
        //
        // layout
        //
        layout.AutoSize = true;
        layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        layout.ColumnCount = 2;
        layout.ColumnStyles.Add(new ColumnStyle());
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(targetsLabel, 0, 0);
        layout.Controls.Add(targetsPanel, 1, 0);
        layout.Controls.Add(targetsHint, 1, 1);
        layout.Controls.Add(extensionsLabel, 0, 2);
        layout.Controls.Add(extensionsBox, 1, 2);
        layout.Controls.Add(extensionsHint, 1, 3);
        layout.Controls.Add(patternsLabel, 0, 4);
        layout.Controls.Add(patternsBox, 1, 4);
        layout.Controls.Add(patternsHint, 1, 5);
        layout.Controls.Add(excludeLabel, 0, 6);
        layout.Controls.Add(excludeBox, 1, 6);
        layout.Controls.Add(modeLabel, 0, 7);
        layout.Controls.Add(modeCombo, 1, 7);
        layout.Controls.Add(countPanel, 1, 8);
        layout.Dock = DockStyle.Top;
        layout.Location = new Point(0, 0);
        layout.Name = "layout";
        layout.RowCount = 9;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.Size = new Size(480, 300);
        layout.TabIndex = 0;
        //
        // targetsLabel
        //
        targetsLabel.Anchor = AnchorStyles.Left;
        targetsLabel.AutoSize = true;
        targetsLabel.Name = "targetsLabel";
        targetsLabel.TabIndex = 0;
        targetsLabel.Text = "Item types:";
        //
        // targetsPanel
        //
        targetsPanel.AutoSize = true;
        targetsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        targetsPanel.Controls.Add(filesCheck);
        targetsPanel.Controls.Add(foldersCheck);
        targetsPanel.Controls.Add(backgroundCheck);
        targetsPanel.Controls.Add(drivesCheck);
        targetsPanel.Dock = DockStyle.Fill;
        targetsPanel.Margin = new Padding(0);
        targetsPanel.Name = "targetsPanel";
        targetsPanel.TabIndex = 1;
        //
        // filesCheck
        //
        filesCheck.AutoSize = true;
        filesCheck.Name = "filesCheck";
        filesCheck.TabIndex = 0;
        filesCheck.Text = "Files";
        filesCheck.CheckedChanged += OnFieldChanged;
        //
        // foldersCheck
        //
        foldersCheck.AutoSize = true;
        foldersCheck.Name = "foldersCheck";
        foldersCheck.TabIndex = 1;
        foldersCheck.Text = "Folders";
        foldersCheck.CheckedChanged += OnFieldChanged;
        //
        // backgroundCheck
        //
        backgroundCheck.AutoSize = true;
        backgroundCheck.Name = "backgroundCheck";
        backgroundCheck.TabIndex = 2;
        backgroundCheck.Text = "Folder background";
        backgroundCheck.CheckedChanged += OnFieldChanged;
        //
        // drivesCheck
        //
        drivesCheck.AutoSize = true;
        drivesCheck.Name = "drivesCheck";
        drivesCheck.TabIndex = 3;
        drivesCheck.Text = "Drives";
        drivesCheck.CheckedChanged += OnFieldChanged;
        //
        // targetsHint
        //
        targetsHint.AutoSize = true;
        targetsHint.ForeColor = SystemColors.GrayText;
        targetsHint.Name = "targetsHint";
        targetsHint.TabIndex = 2;
        targetsHint.Text = "None checked: every type.";
        //
        // extensionsLabel
        //
        extensionsLabel.Anchor = AnchorStyles.Left;
        extensionsLabel.AutoSize = true;
        extensionsLabel.Name = "extensionsLabel";
        extensionsLabel.TabIndex = 3;
        extensionsLabel.Text = "Extensions:";
        //
        // extensionsBox
        //
        extensionsBox.Dock = DockStyle.Fill;
        extensionsBox.Name = "extensionsBox";
        extensionsBox.TabIndex = 4;
        extensionsBox.TextChanged += OnFieldChanged;
        //
        // extensionsHint
        //
        extensionsHint.AutoSize = true;
        extensionsHint.ForeColor = SystemColors.GrayText;
        extensionsHint.Name = "extensionsHint";
        extensionsHint.TabIndex = 5;
        extensionsHint.Text = "Files only, separated by commas";
        //
        // patternsLabel
        //
        patternsLabel.AutoSize = true;
        patternsLabel.Name = "patternsLabel";
        patternsLabel.Padding = new Padding(0, 3, 0, 0);
        patternsLabel.TabIndex = 6;
        patternsLabel.Text = "Names:";
        //
        // patternsBox
        //
        patternsBox.AcceptsReturn = true;
        patternsBox.Dock = DockStyle.Fill;
        patternsBox.Multiline = true;
        patternsBox.Name = "patternsBox";
        patternsBox.ScrollBars = ScrollBars.Vertical;
        patternsBox.Size = new Size(380, 52);
        patternsBox.TabIndex = 7;
        patternsBox.TextChanged += OnFieldChanged;
        //
        // patternsHint
        //
        patternsHint.AutoSize = true;
        patternsHint.ForeColor = SystemColors.GrayText;
        patternsHint.Name = "patternsHint";
        patternsHint.TabIndex = 8;
        patternsHint.Text = "One per line";
        //
        // excludeLabel
        //
        excludeLabel.AutoSize = true;
        excludeLabel.Name = "excludeLabel";
        excludeLabel.Padding = new Padding(0, 3, 0, 0);
        excludeLabel.TabIndex = 9;
        excludeLabel.Text = "Except names:";
        //
        // excludeBox
        //
        excludeBox.AcceptsReturn = true;
        excludeBox.Dock = DockStyle.Fill;
        excludeBox.Multiline = true;
        excludeBox.Name = "excludeBox";
        excludeBox.ScrollBars = ScrollBars.Vertical;
        excludeBox.Size = new Size(380, 52);
        excludeBox.TabIndex = 10;
        excludeBox.TextChanged += OnFieldChanged;
        //
        // modeLabel
        //
        modeLabel.Anchor = AnchorStyles.Left;
        modeLabel.AutoSize = true;
        modeLabel.Name = "modeLabel";
        modeLabel.TabIndex = 11;
        modeLabel.Text = "Several items:";
        //
        // modeCombo
        //
        modeCombo.Dock = DockStyle.Fill;
        modeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        modeCombo.Name = "modeCombo";
        modeCombo.TabIndex = 12;
        modeCombo.SelectedIndexChanged += OnFieldChanged;
        //
        // countPanel
        //
        countPanel.AutoSize = true;
        countPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        countPanel.Controls.Add(minLabel);
        countPanel.Controls.Add(minCount);
        countPanel.Controls.Add(maxCheck);
        countPanel.Controls.Add(maxCount);
        countPanel.Dock = DockStyle.Fill;
        countPanel.Margin = new Padding(0);
        countPanel.Name = "countPanel";
        countPanel.TabIndex = 13;
        countPanel.WrapContents = false;
        //
        // minLabel
        //
        minLabel.Anchor = AnchorStyles.Left;
        minLabel.AutoSize = true;
        minLabel.Name = "minLabel";
        minLabel.TabIndex = 0;
        minLabel.Text = "At least:";
        //
        // minCount
        //
        minCount.Maximum = new decimal(new int[] { 2147483647, 0, 0, 0 });
        minCount.Name = "minCount";
        minCount.Size = new Size(70, 23);
        minCount.TabIndex = 1;
        minCount.ValueChanged += OnFieldChanged;
        //
        // maxCheck
        //
        maxCheck.Anchor = AnchorStyles.Left;
        maxCheck.AutoSize = true;
        maxCheck.Margin = new Padding(16, 3, 3, 3);
        maxCheck.Name = "maxCheck";
        maxCheck.TabIndex = 2;
        maxCheck.Text = "At most:";
        maxCheck.CheckedChanged += OnFieldChanged;
        //
        // maxCount
        //
        maxCount.Enabled = false;
        maxCount.Maximum = new decimal(new int[] { 2147483647, 0, 0, 0 });
        maxCount.Name = "maxCount";
        maxCount.Size = new Size(70, 23);
        maxCount.TabIndex = 3;
        maxCount.ValueChanged += OnFieldChanged;
        //
        // MatchRuleEditor
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(layout);
        Name = "MatchRuleEditor";
        Size = new Size(480, 300);
        layout.ResumeLayout(false);
        layout.PerformLayout();
        targetsPanel.ResumeLayout(false);
        targetsPanel.PerformLayout();
        countPanel.ResumeLayout(false);
        countPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)minCount).EndInit();
        ((System.ComponentModel.ISupportInitialize)maxCount).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel layout;
    private Label targetsLabel;
    private FlowLayoutPanel targetsPanel;
    private CheckBox filesCheck;
    private CheckBox foldersCheck;
    private CheckBox backgroundCheck;
    private CheckBox drivesCheck;
    private Label targetsHint;
    private Label extensionsLabel;
    private TextBox extensionsBox;
    private Label extensionsHint;
    private Label patternsLabel;
    private TextBox patternsBox;
    private Label patternsHint;
    private Label excludeLabel;
    private TextBox excludeBox;
    private Label modeLabel;
    private ComboBox modeCombo;
    private FlowLayoutPanel countPanel;
    private Label minLabel;
    private NumericUpDown minCount;
    private CheckBox maxCheck;
    private NumericUpDown maxCount;
}
