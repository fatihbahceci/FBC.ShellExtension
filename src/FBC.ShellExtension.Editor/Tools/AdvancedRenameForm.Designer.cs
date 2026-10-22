#nullable disable

namespace FBC.ShellExtension.Editor.Tools;

partial class AdvancedRenameForm
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
        components = new System.ComponentModel.Container();
        layout = new TableLayoutPanel();
        selectionLabel = new Label();
        rulesPanel = new TableLayoutPanel();
        findLabel = new Label();
        findBox = new TextBox();
        replaceLabel = new Label();
        replaceBox = new TextBox();
        searchOptionsPanel = new FlowLayoutPanel();
        matchCaseCheck = new CheckBox();
        regexCheck = new CheckBox();
        nameLabel = new Label();
        nameBox = new TextBox();
        extensionLabel = new Label();
        extensionBox = new TextBox();
        tokensLabel = new Label();
        numbersLabel = new Label();
        numbersPanel = new FlowLayoutPanel();
        startLabel = new Label();
        startBox = new NumericUpDown();
        stepLabel = new Label();
        stepBox = new NumericUpDown();
        digitsLabel = new Label();
        digitsBox = new NumericUpDown();
        caseLabel = new Label();
        caseCombo = new ComboBox();
        previewList = new ListView();
        oldColumn = new ColumnHeader();
        newColumn = new ColumnHeader();
        problemColumn = new ColumnHeader();
        statusLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        renameButton = new Button();
        refreshTimer = new System.Windows.Forms.Timer(components);
        layout.SuspendLayout();
        rulesPanel.SuspendLayout();
        searchOptionsPanel.SuspendLayout();
        numbersPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)startBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)stepBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)digitsBox).BeginInit();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // layout
        //
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(selectionLabel, 0, 0);
        layout.Controls.Add(rulesPanel, 0, 1);
        layout.Controls.Add(previewList, 0, 2);
        layout.Controls.Add(statusLabel, 0, 3);
        layout.Controls.Add(buttonPanel, 0, 4);
        layout.Dock = DockStyle.Fill;
        layout.Location = new Point(0, 0);
        layout.Name = "layout";
        layout.Padding = new Padding(8);
        layout.RowCount = 5;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.Size = new Size(760, 580);
        layout.TabIndex = 0;
        //
        // selectionLabel
        //
        selectionLabel.AutoSize = true;
        selectionLabel.Margin = new Padding(3, 3, 3, 6);
        selectionLabel.Name = "selectionLabel";
        selectionLabel.TabIndex = 0;
        selectionLabel.Text = "Selected:";
        selectionLabel.UseMnemonic = false;
        //
        // rulesPanel
        //
        rulesPanel.AutoSize = true;
        rulesPanel.ColumnCount = 4;
        rulesPanel.ColumnStyles.Add(new ColumnStyle());
        rulesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        rulesPanel.ColumnStyles.Add(new ColumnStyle());
        rulesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        rulesPanel.Controls.Add(findLabel, 0, 0);
        rulesPanel.Controls.Add(findBox, 1, 0);
        rulesPanel.Controls.Add(replaceLabel, 2, 0);
        rulesPanel.Controls.Add(replaceBox, 3, 0);
        rulesPanel.Controls.Add(searchOptionsPanel, 1, 1);
        rulesPanel.Controls.Add(nameLabel, 0, 2);
        rulesPanel.Controls.Add(nameBox, 1, 2);
        rulesPanel.Controls.Add(extensionLabel, 2, 2);
        rulesPanel.Controls.Add(extensionBox, 3, 2);
        rulesPanel.Controls.Add(tokensLabel, 1, 3);
        rulesPanel.Controls.Add(numbersLabel, 0, 4);
        rulesPanel.Controls.Add(numbersPanel, 1, 4);
        rulesPanel.Controls.Add(caseLabel, 0, 5);
        rulesPanel.Controls.Add(caseCombo, 1, 5);
        rulesPanel.Dock = DockStyle.Fill;
        rulesPanel.Margin = new Padding(0, 0, 0, 6);
        rulesPanel.Name = "rulesPanel";
        rulesPanel.RowCount = 6;
        rulesPanel.RowStyles.Add(new RowStyle());
        rulesPanel.RowStyles.Add(new RowStyle());
        rulesPanel.RowStyles.Add(new RowStyle());
        rulesPanel.RowStyles.Add(new RowStyle());
        rulesPanel.RowStyles.Add(new RowStyle());
        rulesPanel.RowStyles.Add(new RowStyle());
        rulesPanel.SetColumnSpan(searchOptionsPanel, 3);
        rulesPanel.SetColumnSpan(tokensLabel, 3);
        rulesPanel.SetColumnSpan(numbersPanel, 3);
        rulesPanel.TabIndex = 1;
        //
        // findLabel
        //
        findLabel.Anchor = AnchorStyles.Left;
        findLabel.AutoSize = true;
        findLabel.Name = "findLabel";
        findLabel.TabIndex = 0;
        findLabel.Text = "Find:";
        //
        // findBox
        //
        findBox.Dock = DockStyle.Fill;
        findBox.Name = "findBox";
        findBox.TabIndex = 1;
        findBox.TextChanged += OnRulesChanged;
        //
        // replaceLabel
        //
        replaceLabel.Anchor = AnchorStyles.Left;
        replaceLabel.AutoSize = true;
        replaceLabel.Name = "replaceLabel";
        replaceLabel.TabIndex = 2;
        replaceLabel.Text = "Replace with:";
        //
        // replaceBox
        //
        replaceBox.Dock = DockStyle.Fill;
        replaceBox.Name = "replaceBox";
        replaceBox.TabIndex = 3;
        replaceBox.TextChanged += OnRulesChanged;
        //
        // searchOptionsPanel
        //
        searchOptionsPanel.AutoSize = true;
        searchOptionsPanel.Controls.Add(matchCaseCheck);
        searchOptionsPanel.Controls.Add(regexCheck);
        searchOptionsPanel.Margin = new Padding(0, 0, 0, 6);
        searchOptionsPanel.Name = "searchOptionsPanel";
        searchOptionsPanel.TabIndex = 4;
        //
        // matchCaseCheck
        //
        matchCaseCheck.AutoSize = true;
        matchCaseCheck.Name = "matchCaseCheck";
        matchCaseCheck.TabIndex = 0;
        matchCaseCheck.Text = "Match case";
        matchCaseCheck.UseVisualStyleBackColor = true;
        matchCaseCheck.CheckedChanged += OnRulesChanged;
        //
        // regexCheck
        //
        regexCheck.AutoSize = true;
        regexCheck.Name = "regexCheck";
        regexCheck.TabIndex = 1;
        regexCheck.Text = "Regular expression";
        regexCheck.UseVisualStyleBackColor = true;
        regexCheck.CheckedChanged += OnRulesChanged;
        //
        // nameLabel
        //
        nameLabel.Anchor = AnchorStyles.Left;
        nameLabel.AutoSize = true;
        nameLabel.Name = "nameLabel";
        nameLabel.TabIndex = 5;
        nameLabel.Text = "Name:";
        //
        // nameBox
        //
        nameBox.Dock = DockStyle.Fill;
        nameBox.Name = "nameBox";
        nameBox.TabIndex = 6;
        nameBox.Text = "{name}";
        nameBox.TextChanged += OnRulesChanged;
        //
        // extensionLabel
        //
        extensionLabel.Anchor = AnchorStyles.Left;
        extensionLabel.AutoSize = true;
        extensionLabel.Name = "extensionLabel";
        extensionLabel.TabIndex = 7;
        extensionLabel.Text = "Extension:";
        //
        // extensionBox
        //
        extensionBox.Dock = DockStyle.Fill;
        extensionBox.Name = "extensionBox";
        extensionBox.TabIndex = 8;
        extensionBox.Text = "{ext}";
        extensionBox.TextChanged += OnRulesChanged;
        //
        // tokensLabel
        //
        tokensLabel.AutoSize = true;
        tokensLabel.ForeColor = SystemColors.GrayText;
        tokensLabel.Margin = new Padding(3, 0, 3, 6);
        tokensLabel.Name = "tokensLabel";
        tokensLabel.TabIndex = 9;
        tokensLabel.Text = "{name} {n} {parent} {date} {ext}";
        tokensLabel.UseMnemonic = false;
        //
        // numbersLabel
        //
        numbersLabel.Anchor = AnchorStyles.Left;
        numbersLabel.AutoSize = true;
        numbersLabel.Name = "numbersLabel";
        numbersLabel.TabIndex = 10;
        numbersLabel.Text = "{n}:";
        numbersLabel.UseMnemonic = false;
        //
        // numbersPanel
        //
        numbersPanel.AutoSize = true;
        numbersPanel.Controls.Add(startLabel);
        numbersPanel.Controls.Add(startBox);
        numbersPanel.Controls.Add(stepLabel);
        numbersPanel.Controls.Add(stepBox);
        numbersPanel.Controls.Add(digitsLabel);
        numbersPanel.Controls.Add(digitsBox);
        numbersPanel.Margin = new Padding(0);
        numbersPanel.Name = "numbersPanel";
        numbersPanel.TabIndex = 11;
        numbersPanel.WrapContents = false;
        //
        // startLabel
        //
        startLabel.Anchor = AnchorStyles.Left;
        startLabel.AutoSize = true;
        startLabel.Name = "startLabel";
        startLabel.TabIndex = 0;
        startLabel.Text = "from";
        //
        // startBox
        //
        startBox.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
        startBox.Name = "startBox";
        startBox.Size = new Size(70, 23);
        startBox.TabIndex = 1;
        startBox.Value = new decimal(new int[] { 1, 0, 0, 0 });
        startBox.ValueChanged += OnRulesChanged;
        //
        // stepLabel
        //
        stepLabel.Anchor = AnchorStyles.Left;
        stepLabel.AutoSize = true;
        stepLabel.Name = "stepLabel";
        stepLabel.TabIndex = 2;
        stepLabel.Text = "step";
        //
        // stepBox
        //
        stepBox.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        stepBox.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        stepBox.Name = "stepBox";
        stepBox.Size = new Size(55, 23);
        stepBox.TabIndex = 3;
        stepBox.Value = new decimal(new int[] { 1, 0, 0, 0 });
        stepBox.ValueChanged += OnRulesChanged;
        //
        // digitsLabel
        //
        digitsLabel.Anchor = AnchorStyles.Left;
        digitsLabel.AutoSize = true;
        digitsLabel.Name = "digitsLabel";
        digitsLabel.TabIndex = 4;
        digitsLabel.Text = "digits";
        //
        // digitsBox
        //
        digitsBox.Maximum = new decimal(new int[] { 9, 0, 0, 0 });
        digitsBox.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        digitsBox.Name = "digitsBox";
        digitsBox.Size = new Size(45, 23);
        digitsBox.TabIndex = 5;
        digitsBox.Value = new decimal(new int[] { 1, 0, 0, 0 });
        digitsBox.ValueChanged += OnRulesChanged;
        //
        // caseLabel
        //
        caseLabel.Anchor = AnchorStyles.Left;
        caseLabel.AutoSize = true;
        caseLabel.Name = "caseLabel";
        caseLabel.TabIndex = 12;
        caseLabel.Text = "Case:";
        //
        // caseCombo
        //
        caseCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        caseCombo.Name = "caseCombo";
        caseCombo.Size = new Size(160, 23);
        caseCombo.TabIndex = 13;
        caseCombo.SelectedIndexChanged += OnRulesChanged;
        //
        // previewList
        //
        previewList.Columns.AddRange(new ColumnHeader[] { oldColumn, newColumn, problemColumn });
        previewList.Dock = DockStyle.Fill;
        previewList.FullRowSelect = true;
        previewList.Name = "previewList";
        previewList.TabIndex = 2;
        previewList.UseCompatibleStateImageBehavior = false;
        previewList.View = View.Details;
        //
        // oldColumn
        //
        oldColumn.Text = "Name";
        oldColumn.Width = 260;
        //
        // newColumn
        //
        newColumn.Text = "New name";
        newColumn.Width = 260;
        //
        // problemColumn
        //
        problemColumn.Text = "Problem";
        problemColumn.Width = 200;
        //
        // statusLabel
        //
        statusLabel.AutoSize = true;
        statusLabel.Margin = new Padding(3, 6, 3, 6);
        statusLabel.Name = "statusLabel";
        statusLabel.TabIndex = 3;
        statusLabel.UseMnemonic = false;
        //
        // buttonPanel
        //
        buttonPanel.AutoSize = true;
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(renameButton);
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
        // renameButton
        //
        renameButton.AutoSize = true;
        renameButton.Enabled = false;
        renameButton.Name = "renameButton";
        renameButton.TabIndex = 0;
        renameButton.Text = "Rename";
        renameButton.UseVisualStyleBackColor = true;
        renameButton.Click += OnRename;
        //
        // refreshTimer
        //
        refreshTimer.Interval = 250;
        refreshTimer.Tick += OnRefreshTimer;
        //
        // AdvancedRenameForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(760, 580);
        Controls.Add(layout);
        MinimumSize = new Size(600, 440);
        Name = "AdvancedRenameForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Advanced rename";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        rulesPanel.ResumeLayout(false);
        rulesPanel.PerformLayout();
        searchOptionsPanel.ResumeLayout(false);
        searchOptionsPanel.PerformLayout();
        numbersPanel.ResumeLayout(false);
        numbersPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)startBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)stepBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)digitsBox).EndInit();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel layout;
    private Label selectionLabel;
    private TableLayoutPanel rulesPanel;
    private Label findLabel;
    private TextBox findBox;
    private Label replaceLabel;
    private TextBox replaceBox;
    private FlowLayoutPanel searchOptionsPanel;
    private CheckBox matchCaseCheck;
    private CheckBox regexCheck;
    private Label nameLabel;
    private TextBox nameBox;
    private Label extensionLabel;
    private TextBox extensionBox;
    private Label tokensLabel;
    private Label numbersLabel;
    private FlowLayoutPanel numbersPanel;
    private Label startLabel;
    private NumericUpDown startBox;
    private Label stepLabel;
    private NumericUpDown stepBox;
    private Label digitsLabel;
    private NumericUpDown digitsBox;
    private Label caseLabel;
    private ComboBox caseCombo;
    private ListView previewList;
    private ColumnHeader oldColumn;
    private ColumnHeader newColumn;
    private ColumnHeader problemColumn;
    private Label statusLabel;
    private FlowLayoutPanel buttonPanel;
    private Button cancelButton;
    private Button renameButton;
    private System.Windows.Forms.Timer refreshTimer;
}
