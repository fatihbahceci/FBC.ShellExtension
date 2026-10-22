#nullable disable

namespace FBC.ShellExtension.Editor.Tools;

partial class FindReplaceForm
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
        searchPanel = new TableLayoutPanel();
        findLabel = new Label();
        findBox = new TextBox();
        replaceLabel = new Label();
        replaceBox = new TextBox();
        optionsPanel = new FlowLayoutPanel();
        matchCaseCheck = new CheckBox();
        wholeWordCheck = new CheckBox();
        regexCheck = new CheckBox();
        filesPanel = new FlowLayoutPanel();
        filesLabel = new Label();
        filesBox = new TextBox();
        subfoldersCheck = new CheckBox();
        backupCheck = new CheckBox();
        hintLabel = new Label();
        resultList = new ListView();
        fileColumn = new ColumnHeader();
        matchesColumn = new ColumnHeader();
        noteColumn = new ColumnHeader();
        statusLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        closeButton = new Button();
        replaceButton = new Button();
        previewButton = new Button();
        layout.SuspendLayout();
        searchPanel.SuspendLayout();
        optionsPanel.SuspendLayout();
        filesPanel.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // layout
        //
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(selectionLabel, 0, 0);
        layout.Controls.Add(searchPanel, 0, 1);
        layout.Controls.Add(optionsPanel, 0, 2);
        layout.Controls.Add(filesPanel, 0, 3);
        layout.Controls.Add(backupCheck, 0, 4);
        layout.Controls.Add(hintLabel, 0, 5);
        layout.Controls.Add(resultList, 0, 6);
        layout.Controls.Add(statusLabel, 0, 7);
        layout.Controls.Add(buttonPanel, 0, 8);
        layout.Dock = DockStyle.Fill;
        layout.Location = new Point(0, 0);
        layout.Name = "layout";
        layout.Padding = new Padding(8);
        layout.RowCount = 9;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
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
        // searchPanel
        //
        searchPanel.AutoSize = true;
        searchPanel.ColumnCount = 2;
        searchPanel.ColumnStyles.Add(new ColumnStyle());
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        searchPanel.Controls.Add(findLabel, 0, 0);
        searchPanel.Controls.Add(findBox, 1, 0);
        searchPanel.Controls.Add(replaceLabel, 0, 1);
        searchPanel.Controls.Add(replaceBox, 1, 1);
        searchPanel.Dock = DockStyle.Fill;
        searchPanel.Margin = new Padding(0);
        searchPanel.Name = "searchPanel";
        searchPanel.RowCount = 2;
        searchPanel.RowStyles.Add(new RowStyle());
        searchPanel.RowStyles.Add(new RowStyle());
        searchPanel.TabIndex = 1;
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
        findBox.TextChanged += OnOptionsChanged;
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
        replaceBox.TextChanged += OnOptionsChanged;
        //
        // optionsPanel
        //
        optionsPanel.AutoSize = true;
        optionsPanel.Controls.Add(matchCaseCheck);
        optionsPanel.Controls.Add(wholeWordCheck);
        optionsPanel.Controls.Add(regexCheck);
        optionsPanel.Dock = DockStyle.Fill;
        optionsPanel.Name = "optionsPanel";
        optionsPanel.TabIndex = 2;
        //
        // matchCaseCheck
        //
        matchCaseCheck.AutoSize = true;
        matchCaseCheck.Name = "matchCaseCheck";
        matchCaseCheck.TabIndex = 0;
        matchCaseCheck.Text = "Match case";
        matchCaseCheck.UseVisualStyleBackColor = true;
        matchCaseCheck.CheckedChanged += OnOptionsChanged;
        //
        // wholeWordCheck
        //
        wholeWordCheck.AutoSize = true;
        wholeWordCheck.Name = "wholeWordCheck";
        wholeWordCheck.TabIndex = 1;
        wholeWordCheck.Text = "Whole words";
        wholeWordCheck.UseVisualStyleBackColor = true;
        wholeWordCheck.CheckedChanged += OnOptionsChanged;
        //
        // regexCheck
        //
        regexCheck.AutoSize = true;
        regexCheck.Name = "regexCheck";
        regexCheck.TabIndex = 2;
        regexCheck.Text = "Regular expression";
        regexCheck.UseVisualStyleBackColor = true;
        regexCheck.CheckedChanged += OnOptionsChanged;
        //
        // filesPanel
        //
        filesPanel.AutoSize = true;
        filesPanel.Controls.Add(filesLabel);
        filesPanel.Controls.Add(filesBox);
        filesPanel.Controls.Add(subfoldersCheck);
        filesPanel.Dock = DockStyle.Fill;
        filesPanel.Name = "filesPanel";
        filesPanel.TabIndex = 3;
        filesPanel.WrapContents = false;
        //
        // filesLabel
        //
        filesLabel.Anchor = AnchorStyles.Left;
        filesLabel.AutoSize = true;
        filesLabel.Name = "filesLabel";
        filesLabel.TabIndex = 0;
        filesLabel.Text = "File names:";
        //
        // filesBox
        //
        filesBox.Name = "filesBox";
        filesBox.Size = new Size(260, 23);
        filesBox.TabIndex = 1;
        filesBox.Text = "*.*";
        filesBox.TextChanged += OnOptionsChanged;
        //
        // subfoldersCheck
        //
        subfoldersCheck.Anchor = AnchorStyles.Left;
        subfoldersCheck.AutoSize = true;
        subfoldersCheck.Checked = true;
        subfoldersCheck.CheckState = CheckState.Checked;
        subfoldersCheck.Margin = new Padding(12, 3, 3, 3);
        subfoldersCheck.Name = "subfoldersCheck";
        subfoldersCheck.TabIndex = 2;
        subfoldersCheck.Text = "In subfolders too";
        subfoldersCheck.UseVisualStyleBackColor = true;
        subfoldersCheck.CheckedChanged += OnOptionsChanged;
        //
        // backupCheck
        //
        backupCheck.AutoSize = true;
        backupCheck.Checked = true;
        backupCheck.CheckState = CheckState.Checked;
        backupCheck.Name = "backupCheck";
        backupCheck.TabIndex = 4;
        backupCheck.Text = "Keep the old file as name.bak";
        backupCheck.UseVisualStyleBackColor = true;
        //
        // hintLabel
        //
        hintLabel.AutoSize = true;
        hintLabel.ForeColor = SystemColors.GrayText;
        hintLabel.Margin = new Padding(3, 3, 3, 6);
        hintLabel.Name = "hintLabel";
        hintLabel.TabIndex = 5;
        hintLabel.Text = "Hidden folders such as .git are skipped.";
        //
        // resultList
        //
        resultList.Columns.AddRange(new ColumnHeader[] { fileColumn, matchesColumn, noteColumn });
        resultList.Dock = DockStyle.Fill;
        resultList.FullRowSelect = true;
        resultList.Name = "resultList";
        resultList.TabIndex = 6;
        resultList.UseCompatibleStateImageBehavior = false;
        resultList.View = View.Details;
        //
        // fileColumn
        //
        fileColumn.Text = "File";
        fileColumn.Width = 440;
        //
        // matchesColumn
        //
        matchesColumn.Text = "Matches";
        matchesColumn.TextAlign = HorizontalAlignment.Right;
        matchesColumn.Width = 80;
        //
        // noteColumn
        //
        noteColumn.Text = "Note";
        noteColumn.Width = 200;
        //
        // statusLabel
        //
        statusLabel.AutoSize = true;
        statusLabel.Margin = new Padding(3, 6, 3, 6);
        statusLabel.Name = "statusLabel";
        statusLabel.TabIndex = 7;
        statusLabel.UseMnemonic = false;
        //
        // buttonPanel
        //
        buttonPanel.AutoSize = true;
        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(replaceButton);
        buttonPanel.Controls.Add(previewButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Name = "buttonPanel";
        buttonPanel.TabIndex = 8;
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
        // replaceButton
        //
        replaceButton.AutoSize = true;
        replaceButton.Enabled = false;
        replaceButton.Name = "replaceButton";
        replaceButton.TabIndex = 1;
        replaceButton.Text = "Replace";
        replaceButton.UseVisualStyleBackColor = true;
        replaceButton.Click += OnReplace;
        //
        // previewButton
        //
        previewButton.AutoSize = true;
        previewButton.Name = "previewButton";
        previewButton.TabIndex = 0;
        previewButton.Text = "Preview";
        previewButton.UseVisualStyleBackColor = true;
        previewButton.Click += OnPreview;
        //
        // FindReplaceForm
        //
        AcceptButton = previewButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = closeButton;
        ClientSize = new Size(760, 580);
        Controls.Add(layout);
        MinimumSize = new Size(600, 440);
        Name = "FindReplaceForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Find and replace";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        searchPanel.ResumeLayout(false);
        searchPanel.PerformLayout();
        optionsPanel.ResumeLayout(false);
        optionsPanel.PerformLayout();
        filesPanel.ResumeLayout(false);
        filesPanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel layout;
    private Label selectionLabel;
    private TableLayoutPanel searchPanel;
    private Label findLabel;
    private TextBox findBox;
    private Label replaceLabel;
    private TextBox replaceBox;
    private FlowLayoutPanel optionsPanel;
    private CheckBox matchCaseCheck;
    private CheckBox wholeWordCheck;
    private CheckBox regexCheck;
    private FlowLayoutPanel filesPanel;
    private Label filesLabel;
    private TextBox filesBox;
    private CheckBox subfoldersCheck;
    private CheckBox backupCheck;
    private Label hintLabel;
    private ListView resultList;
    private ColumnHeader fileColumn;
    private ColumnHeader matchesColumn;
    private ColumnHeader noteColumn;
    private Label statusLabel;
    private FlowLayoutPanel buttonPanel;
    private Button closeButton;
    private Button replaceButton;
    private Button previewButton;
}
