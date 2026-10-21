#nullable disable

namespace FBC.ShellExtension.Editor;

partial class CommandEditor
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
        components = new System.ComponentModel.Container();
        layout = new TableLayoutPanel();
        kindLabel = new Label();
        kindCombo = new ComboBox();
        toolNote = new Label();
        programLabel = new Label();
        fileNameBox = new TextBox();
        browseButton = new Button();
        argumentsLabel = new Label();
        argumentsBox = new TextBox();
        variablesButton = new Button();
        workingLabel = new Label();
        workingDirectoryBox = new TextBox();
        workingHint = new Label();
        runModeLabel = new Label();
        runModeCombo = new ComboBox();
        multiLabel = new Label();
        multiCombo = new ComboBox();
        parallelPanel = new FlowLayoutPanel();
        parallelCheck = new CheckBox();
        parallelCount = new NumericUpDown();
        windowLabel = new Label();
        windowCombo = new ComboBox();
        encodingLabel = new Label();
        encodingCombo = new ComboBox();
        optionsPanel = new FlowLayoutPanel();
        runAsCheck = new CheckBox();
        noWindowCheck = new CheckBox();
        shellExecuteCheck = new CheckBox();
        closeOnSuccessCheck = new CheckBox();
        showOutputCheck = new CheckBox();
        verbLabel = new Label();
        verbBox = new TextBox();
        variablesMenu = new ContextMenuStrip(components);
        browseDialog = new OpenFileDialog();
        layout.SuspendLayout();
        parallelPanel.SuspendLayout();
        optionsPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)parallelCount).BeginInit();
        SuspendLayout();
        //
        // layout
        //
        layout.AutoSize = true;
        layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        layout.ColumnCount = 3;
        layout.ColumnStyles.Add(new ColumnStyle());
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle());
        layout.Controls.Add(kindLabel, 0, 0);
        layout.Controls.Add(kindCombo, 1, 0);
        layout.Controls.Add(toolNote, 1, 1);
        layout.Controls.Add(programLabel, 0, 2);
        layout.Controls.Add(fileNameBox, 1, 2);
        layout.Controls.Add(browseButton, 2, 2);
        layout.Controls.Add(argumentsLabel, 0, 3);
        layout.Controls.Add(argumentsBox, 1, 3);
        layout.Controls.Add(variablesButton, 2, 3);
        layout.Controls.Add(workingLabel, 0, 4);
        layout.Controls.Add(workingDirectoryBox, 1, 4);
        layout.Controls.Add(workingHint, 1, 5);
        layout.Controls.Add(runModeLabel, 0, 6);
        layout.Controls.Add(runModeCombo, 1, 6);
        layout.Controls.Add(multiLabel, 0, 7);
        layout.Controls.Add(multiCombo, 1, 7);
        layout.Controls.Add(parallelPanel, 1, 8);
        layout.Controls.Add(windowLabel, 0, 9);
        layout.Controls.Add(windowCombo, 1, 9);
        layout.Controls.Add(encodingLabel, 0, 10);
        layout.Controls.Add(encodingCombo, 1, 10);
        layout.Controls.Add(optionsPanel, 1, 11);
        layout.Controls.Add(verbLabel, 0, 12);
        layout.Controls.Add(verbBox, 1, 12);
        layout.Dock = DockStyle.Top;
        layout.Location = new Point(0, 0);
        layout.Name = "layout";
        layout.RowCount = 13;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.SetColumnSpan(kindCombo, 2);
        layout.SetColumnSpan(toolNote, 2);
        layout.SetColumnSpan(workingDirectoryBox, 2);
        layout.SetColumnSpan(workingHint, 2);
        layout.SetColumnSpan(runModeCombo, 2);
        layout.SetColumnSpan(multiCombo, 2);
        layout.SetColumnSpan(parallelPanel, 2);
        layout.SetColumnSpan(windowCombo, 2);
        layout.SetColumnSpan(encodingCombo, 2);
        layout.SetColumnSpan(optionsPanel, 2);
        layout.SetColumnSpan(verbBox, 2);
        layout.Size = new Size(480, 420);
        layout.TabIndex = 0;
        //
        // kindLabel
        //
        kindLabel.Anchor = AnchorStyles.Left;
        kindLabel.AutoSize = true;
        kindLabel.Name = "kindLabel";
        kindLabel.TabIndex = 0;
        kindLabel.Text = "Kind:";
        //
        // kindCombo
        //
        kindCombo.Dock = DockStyle.Fill;
        kindCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        kindCombo.Name = "kindCombo";
        kindCombo.TabIndex = 1;
        kindCombo.SelectedIndexChanged += OnKindChanged;
        //
        // toolNote
        //
        toolNote.AutoSize = true;
        toolNote.ForeColor = SystemColors.GrayText;
        toolNote.Name = "toolNote";
        toolNote.TabIndex = 2;
        toolNote.Text = "Copies the full path of every item, one per line.";
        toolNote.UseMnemonic = false;
        toolNote.Visible = false;
        //
        // programLabel
        //
        programLabel.Anchor = AnchorStyles.Left;
        programLabel.AutoSize = true;
        programLabel.Name = "programLabel";
        programLabel.TabIndex = 3;
        programLabel.Text = "Program:";
        //
        // fileNameBox
        //
        fileNameBox.Dock = DockStyle.Fill;
        fileNameBox.Name = "fileNameBox";
        fileNameBox.TabIndex = 4;
        fileNameBox.TextChanged += OnFieldChanged;
        //
        // browseButton
        //
        browseButton.AutoSize = true;
        browseButton.Name = "browseButton";
        browseButton.TabIndex = 5;
        browseButton.Text = "Browse…";
        browseButton.UseVisualStyleBackColor = true;
        browseButton.Click += OnBrowse;
        //
        // argumentsLabel
        //
        argumentsLabel.Anchor = AnchorStyles.Left;
        argumentsLabel.AutoSize = true;
        argumentsLabel.Name = "argumentsLabel";
        argumentsLabel.TabIndex = 6;
        argumentsLabel.Text = "Arguments:";
        //
        // argumentsBox
        //
        argumentsBox.Dock = DockStyle.Fill;
        argumentsBox.Name = "argumentsBox";
        argumentsBox.TabIndex = 7;
        argumentsBox.TextChanged += OnFieldChanged;
        //
        // variablesButton
        //
        variablesButton.AutoSize = true;
        variablesButton.Name = "variablesButton";
        variablesButton.TabIndex = 8;
        variablesButton.Text = "Insert";
        variablesButton.UseVisualStyleBackColor = true;
        variablesButton.Click += OnShowVariables;
        //
        // workingLabel
        //
        workingLabel.Anchor = AnchorStyles.Left;
        workingLabel.AutoSize = true;
        workingLabel.Name = "workingLabel";
        workingLabel.TabIndex = 9;
        workingLabel.Text = "Working folder:";
        //
        // workingDirectoryBox
        //
        workingDirectoryBox.Dock = DockStyle.Fill;
        workingDirectoryBox.Name = "workingDirectoryBox";
        workingDirectoryBox.TabIndex = 10;
        workingDirectoryBox.TextChanged += OnFieldChanged;
        //
        // workingHint
        //
        workingHint.AutoSize = true;
        workingHint.ForeColor = SystemColors.GrayText;
        workingHint.Name = "workingHint";
        workingHint.TabIndex = 11;
        workingHint.Text = "Empty: the folder of the item";
        //
        // runModeLabel
        //
        runModeLabel.Anchor = AnchorStyles.Left;
        runModeLabel.AutoSize = true;
        runModeLabel.Name = "runModeLabel";
        runModeLabel.TabIndex = 12;
        runModeLabel.Text = "Run:";
        //
        // runModeCombo
        //
        runModeCombo.Dock = DockStyle.Fill;
        runModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        runModeCombo.Name = "runModeCombo";
        runModeCombo.TabIndex = 13;
        runModeCombo.SelectedIndexChanged += OnFieldChanged;
        //
        // multiLabel
        //
        multiLabel.Anchor = AnchorStyles.Left;
        multiLabel.AutoSize = true;
        multiLabel.Name = "multiLabel";
        multiLabel.TabIndex = 14;
        multiLabel.Text = "Several items:";
        //
        // multiCombo
        //
        multiCombo.Dock = DockStyle.Fill;
        multiCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        multiCombo.Name = "multiCombo";
        multiCombo.TabIndex = 15;
        multiCombo.SelectedIndexChanged += OnFieldChanged;
        //
        // parallelPanel
        //
        parallelPanel.AutoSize = true;
        parallelPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        parallelPanel.Controls.Add(parallelCheck);
        parallelPanel.Controls.Add(parallelCount);
        parallelPanel.Dock = DockStyle.Fill;
        parallelPanel.Margin = new Padding(0);
        parallelPanel.Name = "parallelPanel";
        parallelPanel.TabIndex = 16;
        parallelPanel.WrapContents = false;
        //
        // parallelCheck
        //
        parallelCheck.Anchor = AnchorStyles.Left;
        parallelCheck.AutoSize = true;
        parallelCheck.Name = "parallelCheck";
        parallelCheck.TabIndex = 0;
        parallelCheck.Text = "At most at once:";
        parallelCheck.CheckedChanged += OnFieldChanged;
        //
        // parallelCount
        //
        parallelCount.Enabled = false;
        parallelCount.Maximum = new decimal(new int[] { 2147483647, 0, 0, 0 });
        parallelCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        parallelCount.Name = "parallelCount";
        parallelCount.Size = new Size(60, 23);
        parallelCount.TabIndex = 1;
        parallelCount.Value = new decimal(new int[] { 2, 0, 0, 0 });
        parallelCount.ValueChanged += OnFieldChanged;
        //
        // windowLabel
        //
        windowLabel.Anchor = AnchorStyles.Left;
        windowLabel.AutoSize = true;
        windowLabel.Name = "windowLabel";
        windowLabel.TabIndex = 17;
        windowLabel.Text = "Window:";
        //
        // windowCombo
        //
        windowCombo.Dock = DockStyle.Fill;
        windowCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        windowCombo.Name = "windowCombo";
        windowCombo.TabIndex = 18;
        windowCombo.SelectedIndexChanged += OnFieldChanged;
        //
        // encodingLabel
        //
        encodingLabel.Anchor = AnchorStyles.Left;
        encodingLabel.AutoSize = true;
        encodingLabel.Name = "encodingLabel";
        encodingLabel.TabIndex = 19;
        encodingLabel.Text = "Output encoding:";
        //
        // encodingCombo
        //
        encodingCombo.Dock = DockStyle.Fill;
        encodingCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        encodingCombo.Name = "encodingCombo";
        encodingCombo.TabIndex = 20;
        encodingCombo.SelectedIndexChanged += OnFieldChanged;
        //
        // optionsPanel
        //
        optionsPanel.AutoSize = true;
        optionsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        optionsPanel.Controls.Add(runAsCheck);
        optionsPanel.Controls.Add(noWindowCheck);
        optionsPanel.Controls.Add(shellExecuteCheck);
        optionsPanel.Controls.Add(closeOnSuccessCheck);
        optionsPanel.Controls.Add(showOutputCheck);
        optionsPanel.Dock = DockStyle.Fill;
        optionsPanel.FlowDirection = FlowDirection.TopDown;
        optionsPanel.Margin = new Padding(0);
        optionsPanel.Name = "optionsPanel";
        optionsPanel.TabIndex = 21;
        optionsPanel.WrapContents = false;
        //
        // runAsCheck
        //
        runAsCheck.AutoSize = true;
        runAsCheck.Name = "runAsCheck";
        runAsCheck.TabIndex = 0;
        runAsCheck.Text = "Run as administrator";
        runAsCheck.CheckedChanged += OnFieldChanged;
        //
        // noWindowCheck
        //
        noWindowCheck.AutoSize = true;
        noWindowCheck.Name = "noWindowCheck";
        noWindowCheck.TabIndex = 1;
        noWindowCheck.Text = "No console window";
        noWindowCheck.CheckedChanged += OnFieldChanged;
        //
        // shellExecuteCheck
        //
        shellExecuteCheck.AutoSize = true;
        shellExecuteCheck.Name = "shellExecuteCheck";
        shellExecuteCheck.TabIndex = 2;
        shellExecuteCheck.Text = "Start through the shell";
        shellExecuteCheck.CheckedChanged += OnFieldChanged;
        //
        // closeOnSuccessCheck
        //
        closeOnSuccessCheck.AutoSize = true;
        closeOnSuccessCheck.Name = "closeOnSuccessCheck";
        closeOnSuccessCheck.TabIndex = 3;
        closeOnSuccessCheck.Text = "Close the result window when all succeed";
        closeOnSuccessCheck.CheckedChanged += OnFieldChanged;
        //
        // showOutputCheck
        //
        showOutputCheck.AutoSize = true;
        showOutputCheck.Name = "showOutputCheck";
        showOutputCheck.TabIndex = 4;
        showOutputCheck.Text = "Show the program's output";
        showOutputCheck.CheckedChanged += OnFieldChanged;
        //
        // verbLabel
        //
        verbLabel.Anchor = AnchorStyles.Left;
        verbLabel.AutoSize = true;
        verbLabel.Name = "verbLabel";
        verbLabel.TabIndex = 22;
        verbLabel.Text = "Shell verb:";
        //
        // verbBox
        //
        verbBox.Dock = DockStyle.Fill;
        verbBox.Name = "verbBox";
        verbBox.TabIndex = 23;
        verbBox.TextChanged += OnFieldChanged;
        //
        // variablesMenu
        //
        variablesMenu.Name = "variablesMenu";
        //
        // browseDialog
        //
        browseDialog.RestoreDirectory = true;
        //
        // CommandEditor
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(layout);
        Name = "CommandEditor";
        Size = new Size(480, 420);
        layout.ResumeLayout(false);
        layout.PerformLayout();
        parallelPanel.ResumeLayout(false);
        parallelPanel.PerformLayout();
        optionsPanel.ResumeLayout(false);
        optionsPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)parallelCount).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel layout;
    private Label kindLabel;
    private ComboBox kindCombo;
    private Label toolNote;
    private Label programLabel;
    private TextBox fileNameBox;
    private Button browseButton;
    private Label argumentsLabel;
    private TextBox argumentsBox;
    private Button variablesButton;
    private Label workingLabel;
    private TextBox workingDirectoryBox;
    private Label workingHint;
    private Label runModeLabel;
    private ComboBox runModeCombo;
    private Label multiLabel;
    private ComboBox multiCombo;
    private FlowLayoutPanel parallelPanel;
    private CheckBox parallelCheck;
    private NumericUpDown parallelCount;
    private Label windowLabel;
    private ComboBox windowCombo;
    private Label encodingLabel;
    private ComboBox encodingCombo;
    private FlowLayoutPanel optionsPanel;
    private CheckBox runAsCheck;
    private CheckBox noWindowCheck;
    private CheckBox shellExecuteCheck;
    private CheckBox closeOnSuccessCheck;
    private CheckBox showOutputCheck;
    private Label verbLabel;
    private TextBox verbBox;
    private ContextMenuStrip variablesMenu;
    private OpenFileDialog browseDialog;
}
