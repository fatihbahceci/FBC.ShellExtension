#nullable disable

namespace FBC.ShellExtension.Editor;

partial class PreviewForm
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
        modePanel = new FlowLayoutPanel();
        rightClickLabel = new Label();
        itemsRadio = new RadioButton();
        backgroundRadio = new RadioButton();
        selectionPanel = new TableLayoutPanel();
        selectionList = new ListView();
        pathColumn = new ColumnHeader();
        kindColumn = new ColumnHeader();
        selectionButtons = new FlowLayoutPanel();
        addFilesButton = new Button();
        addFolderButton = new Button();
        removeButton = new Button();
        clearButton = new Button();
        hintLabel = new Label();
        split = new SplitContainer();
        menuTree = new TreeView();
        processList = new ListView();
        programColumn = new ColumnHeader();
        argumentsColumn = new ColumnHeader();
        folderColumn = new ColumnHeader();
        noteLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        closeButton = new Button();
        runButton = new Button();
        filesDialog = new OpenFileDialog();
        folderDialog = new FolderBrowserDialog();
        layout.SuspendLayout();
        modePanel.SuspendLayout();
        selectionPanel.SuspendLayout();
        selectionButtons.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)split).BeginInit();
        split.Panel1.SuspendLayout();
        split.Panel2.SuspendLayout();
        split.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // layout
        //
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(modePanel, 0, 0);
        layout.Controls.Add(selectionPanel, 0, 1);
        layout.Controls.Add(hintLabel, 0, 2);
        layout.Controls.Add(split, 0, 3);
        layout.Controls.Add(buttonPanel, 0, 4);
        layout.Dock = DockStyle.Fill;
        layout.Location = new Point(0, 0);
        layout.Name = "layout";
        layout.Padding = new Padding(8);
        layout.RowCount = 5;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle());
        layout.Size = new Size(900, 640);
        layout.TabIndex = 0;
        //
        // modePanel
        //
        modePanel.AutoSize = true;
        modePanel.Controls.Add(rightClickLabel);
        modePanel.Controls.Add(itemsRadio);
        modePanel.Controls.Add(backgroundRadio);
        modePanel.Dock = DockStyle.Fill;
        modePanel.Name = "modePanel";
        modePanel.TabIndex = 0;
        modePanel.WrapContents = false;
        //
        // rightClickLabel
        //
        rightClickLabel.Anchor = AnchorStyles.Left;
        rightClickLabel.AutoSize = true;
        rightClickLabel.Name = "rightClickLabel";
        rightClickLabel.TabIndex = 0;
        rightClickLabel.Text = "Right-click on:";
        //
        // itemsRadio
        //
        itemsRadio.AutoSize = true;
        itemsRadio.Checked = true;
        itemsRadio.Name = "itemsRadio";
        itemsRadio.TabIndex = 1;
        itemsRadio.TabStop = true;
        itemsRadio.Text = "the selected items";
        itemsRadio.CheckedChanged += OnModeChanged;
        //
        // backgroundRadio
        //
        backgroundRadio.AutoSize = true;
        backgroundRadio.Name = "backgroundRadio";
        backgroundRadio.TabIndex = 2;
        backgroundRadio.Text = "the empty area of a folder";
        backgroundRadio.CheckedChanged += OnModeChanged;
        //
        // selectionPanel
        //
        selectionPanel.ColumnCount = 2;
        selectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        selectionPanel.ColumnStyles.Add(new ColumnStyle());
        selectionPanel.Controls.Add(selectionList, 0, 0);
        selectionPanel.Controls.Add(selectionButtons, 1, 0);
        selectionPanel.Dock = DockStyle.Fill;
        selectionPanel.Margin = new Padding(0);
        selectionPanel.Name = "selectionPanel";
        selectionPanel.RowCount = 1;
        selectionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        selectionPanel.TabIndex = 1;
        //
        // selectionList
        //
        selectionList.AllowDrop = true;
        selectionList.Columns.AddRange(new ColumnHeader[] { pathColumn, kindColumn });
        selectionList.Dock = DockStyle.Fill;
        selectionList.FullRowSelect = true;
        selectionList.Name = "selectionList";
        selectionList.TabIndex = 0;
        selectionList.UseCompatibleStateImageBehavior = false;
        selectionList.View = View.Details;
        selectionList.DragDrop += OnSelectionDragDrop;
        selectionList.DragEnter += OnSelectionDragEnter;
        selectionList.KeyDown += OnSelectionKeyDown;
        //
        // pathColumn
        //
        pathColumn.Text = "Path";
        pathColumn.Width = 560;
        //
        // kindColumn
        //
        kindColumn.Text = "Type";
        kindColumn.Width = 140;
        //
        // selectionButtons
        //
        selectionButtons.AutoSize = true;
        selectionButtons.Controls.Add(addFilesButton);
        selectionButtons.Controls.Add(addFolderButton);
        selectionButtons.Controls.Add(removeButton);
        selectionButtons.Controls.Add(clearButton);
        selectionButtons.Dock = DockStyle.Fill;
        selectionButtons.FlowDirection = FlowDirection.TopDown;
        selectionButtons.Name = "selectionButtons";
        selectionButtons.TabIndex = 1;
        selectionButtons.WrapContents = false;
        //
        // addFilesButton
        //
        addFilesButton.AutoSize = true;
        addFilesButton.Name = "addFilesButton";
        addFilesButton.TabIndex = 0;
        addFilesButton.Text = "Add files…";
        addFilesButton.UseVisualStyleBackColor = true;
        addFilesButton.Click += OnAddFiles;
        //
        // addFolderButton
        //
        addFolderButton.AutoSize = true;
        addFolderButton.Name = "addFolderButton";
        addFolderButton.TabIndex = 1;
        addFolderButton.Text = "Add folder…";
        addFolderButton.UseVisualStyleBackColor = true;
        addFolderButton.Click += OnAddFolder;
        //
        // removeButton
        //
        removeButton.AutoSize = true;
        removeButton.Name = "removeButton";
        removeButton.TabIndex = 2;
        removeButton.Text = "Remove";
        removeButton.UseVisualStyleBackColor = true;
        removeButton.Click += OnRemove;
        //
        // clearButton
        //
        clearButton.AutoSize = true;
        clearButton.Name = "clearButton";
        clearButton.TabIndex = 3;
        clearButton.Text = "Clear";
        clearButton.UseVisualStyleBackColor = true;
        clearButton.Click += OnClear;
        //
        // hintLabel
        //
        hintLabel.AutoSize = true;
        hintLabel.ForeColor = SystemColors.GrayText;
        hintLabel.Margin = new Padding(3, 6, 3, 6);
        hintLabel.Name = "hintLabel";
        hintLabel.TabIndex = 2;
        hintLabel.Text = "Add files or folders, or drag them here from Explorer.";
        //
        // split
        //
        split.Dock = DockStyle.Fill;
        split.Name = "split";
        split.Panel1.Controls.Add(menuTree);
        split.Panel2.Controls.Add(processList);
        split.Panel2.Controls.Add(noteLabel);
        split.Size = new Size(884, 330);
        split.SplitterDistance = 300;
        split.TabIndex = 3;
        //
        // menuTree
        //
        menuTree.Dock = DockStyle.Fill;
        menuTree.HideSelection = false;
        menuTree.Name = "menuTree";
        menuTree.TabIndex = 0;
        menuTree.AfterSelect += OnMenuAfterSelect;
        //
        // processList
        //
        processList.Columns.AddRange(new ColumnHeader[] { programColumn, argumentsColumn, folderColumn });
        processList.Dock = DockStyle.Fill;
        processList.FullRowSelect = true;
        processList.Name = "processList";
        processList.TabIndex = 1;
        processList.UseCompatibleStateImageBehavior = false;
        processList.View = View.Details;
        //
        // programColumn
        //
        programColumn.Text = "Program";
        programColumn.Width = 160;
        //
        // argumentsColumn
        //
        argumentsColumn.Text = "Arguments";
        argumentsColumn.Width = 260;
        //
        // folderColumn
        //
        folderColumn.Text = "Working folder";
        folderColumn.Width = 160;
        //
        // noteLabel
        //
        noteLabel.AutoSize = true;
        noteLabel.Dock = DockStyle.Top;
        noteLabel.Name = "noteLabel";
        noteLabel.Padding = new Padding(0, 0, 0, 6);
        noteLabel.TabIndex = 0;
        noteLabel.Text = "Select a command to see the processes it would start.";
        //
        // buttonPanel
        //
        buttonPanel.AutoSize = true;
        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(runButton);
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
        closeButton.TabIndex = 1;
        closeButton.Text = "Close";
        closeButton.UseVisualStyleBackColor = true;
        closeButton.Click += OnClose;
        //
        // runButton
        //
        runButton.AutoSize = true;
        runButton.Enabled = false;
        runButton.Name = "runButton";
        runButton.TabIndex = 0;
        runButton.Text = "Run";
        runButton.UseVisualStyleBackColor = true;
        runButton.Click += OnRun;
        //
        // filesDialog
        //
        filesDialog.Multiselect = true;
        filesDialog.RestoreDirectory = true;
        //
        // PreviewForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = closeButton;
        ClientSize = new Size(900, 640);
        Controls.Add(layout);
        MinimumSize = new Size(600, 450);
        Name = "PreviewForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Preview";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        modePanel.ResumeLayout(false);
        modePanel.PerformLayout();
        selectionPanel.ResumeLayout(false);
        selectionPanel.PerformLayout();
        selectionButtons.ResumeLayout(false);
        selectionButtons.PerformLayout();
        split.Panel1.ResumeLayout(false);
        split.Panel2.ResumeLayout(false);
        split.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)split).EndInit();
        split.ResumeLayout(false);
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel layout;
    private FlowLayoutPanel modePanel;
    private Label rightClickLabel;
    private RadioButton itemsRadio;
    private RadioButton backgroundRadio;
    private TableLayoutPanel selectionPanel;
    private ListView selectionList;
    private ColumnHeader pathColumn;
    private ColumnHeader kindColumn;
    private FlowLayoutPanel selectionButtons;
    private Button addFilesButton;
    private Button addFolderButton;
    private Button removeButton;
    private Button clearButton;
    private Label hintLabel;
    private SplitContainer split;
    private TreeView menuTree;
    private ListView processList;
    private ColumnHeader programColumn;
    private ColumnHeader argumentsColumn;
    private ColumnHeader folderColumn;
    private Label noteLabel;
    private FlowLayoutPanel buttonPanel;
    private Button closeButton;
    private Button runButton;
    private OpenFileDialog filesDialog;
    private FolderBrowserDialog folderDialog;
}
