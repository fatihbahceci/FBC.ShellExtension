#nullable disable

namespace FBC.ShellExtension.Editor;

partial class MainForm
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
        menuStrip = new MenuStrip();
        fileMenu = new ToolStripMenuItem();
        saveItem = new ToolStripMenuItem();
        reloadItem = new ToolStripMenuItem();
        openFolderItem = new ToolStripMenuItem();
        fileSeparator = new ToolStripSeparator();
        exitItem = new ToolStripMenuItem();
        editMenu = new ToolStripMenuItem();
        undoItem = new ToolStripMenuItem();
        redoItem = new ToolStripMenuItem();
        editSeparator1 = new ToolStripSeparator();
        addCommandItem = new ToolStripMenuItem();
        addSubmenuItem = new ToolStripMenuItem();
        addSeparatorItem = new ToolStripMenuItem();
        duplicateItem = new ToolStripMenuItem();
        deleteItem = new ToolStripMenuItem();
        editSeparator2 = new ToolStripSeparator();
        moveUpItem = new ToolStripMenuItem();
        moveDownItem = new ToolStripMenuItem();
        moveInItem = new ToolStripMenuItem();
        moveOutItem = new ToolStripMenuItem();
        testMenu = new ToolStripMenuItem();
        toolsMenu = new ToolStripMenuItem();
        checkItem = new ToolStripMenuItem();
        toolsSeparator = new ToolStripSeparator();
        backupItem = new ToolStripMenuItem();
        restoreItem = new ToolStripMenuItem();
        resetItem = new ToolStripMenuItem();
        ffmpegSeparator = new ToolStripSeparator();
        ffmpegItem = new ToolStripMenuItem();
        previewItem = new ToolStripMenuItem();
        helpMenu = new ToolStripMenuItem();
        aboutItem = new ToolStripMenuItem();
        toolStrip = new ToolStrip();
        addCommandButton = new ToolStripButton();
        addSubmenuButton = new ToolStripButton();
        addSeparatorButton = new ToolStripButton();
        toolSeparator1 = new ToolStripSeparator();
        deleteButton = new ToolStripButton();
        moveUpButton = new ToolStripButton();
        moveDownButton = new ToolStripButton();
        toolSeparator2 = new ToolStripSeparator();
        undoButton = new ToolStripButton();
        redoButton = new ToolStripButton();
        toolSeparator3 = new ToolStripSeparator();
        previewButton = new ToolStripButton();
        saveButton = new ToolStripButton();
        outerSplit = new SplitContainer();
        mainSplit = new SplitContainer();
        tree = new CheckBoxTreeView();
        detailHost = new Panel();
        entryEditor = new EntryEditor();
        messageLabel = new Label();
        issuesList = new ListView();
        issueLocationColumn = new ColumnHeader();
        issueProblemColumn = new ColumnHeader();
        statusStrip = new StatusStrip();
        pathStatus = new ToolStripStatusLabel();
        stateStatus = new ToolStripStatusLabel();
        issuesStatus = new ToolStripStatusLabel();
        treeMenu = new ContextMenuStrip(components);
        ctxAddCommandItem = new ToolStripMenuItem();
        ctxAddSubmenuItem = new ToolStripMenuItem();
        ctxAddSeparatorItem = new ToolStripMenuItem();
        ctxSeparator1 = new ToolStripSeparator();
        ctxDuplicateItem = new ToolStripMenuItem();
        ctxDeleteItem = new ToolStripMenuItem();
        ctxSeparator2 = new ToolStripSeparator();
        ctxMoveUpItem = new ToolStripMenuItem();
        ctxMoveDownItem = new ToolStripMenuItem();
        ctxMoveInItem = new ToolStripMenuItem();
        ctxMoveOutItem = new ToolStripMenuItem();
        menuStrip.SuspendLayout();
        toolStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)outerSplit).BeginInit();
        outerSplit.Panel1.SuspendLayout();
        outerSplit.Panel2.SuspendLayout();
        outerSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)mainSplit).BeginInit();
        mainSplit.Panel1.SuspendLayout();
        mainSplit.Panel2.SuspendLayout();
        mainSplit.SuspendLayout();
        detailHost.SuspendLayout();
        statusStrip.SuspendLayout();
        treeMenu.SuspendLayout();
        SuspendLayout();
        //
        // menuStrip
        //
        menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, testMenu, toolsMenu, helpMenu });
        menuStrip.Location = new Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Size = new Size(1000, 24);
        menuStrip.TabIndex = 0;
        //
        // fileMenu
        //
        fileMenu.DropDownItems.AddRange(new ToolStripItem[] { saveItem, reloadItem, openFolderItem, fileSeparator, exitItem });
        fileMenu.Name = "fileMenu";
        fileMenu.Text = "&File";
        //
        // saveItem
        //
        saveItem.Name = "saveItem";
        saveItem.ShortcutKeys = Keys.Control | Keys.S;
        saveItem.Text = "&Save";
        saveItem.Click += OnSave;
        //
        // reloadItem
        //
        reloadItem.Name = "reloadItem";
        reloadItem.Text = "&Reload from disk";
        reloadItem.Click += OnReload;
        //
        // openFolderItem
        //
        openFolderItem.Name = "openFolderItem";
        openFolderItem.Text = "Open the config &folder";
        openFolderItem.Click += OnOpenFolder;
        //
        // fileSeparator
        //
        fileSeparator.Name = "fileSeparator";
        //
        // exitItem
        //
        exitItem.Name = "exitItem";
        exitItem.Text = "E&xit";
        exitItem.Click += OnExit;
        //
        // editMenu
        //
        editMenu.DropDownItems.AddRange(new ToolStripItem[] { undoItem, redoItem, editSeparator1, addCommandItem, addSubmenuItem, addSeparatorItem, duplicateItem, deleteItem, editSeparator2, moveUpItem, moveDownItem, moveInItem, moveOutItem });
        editMenu.Name = "editMenu";
        editMenu.Text = "&Edit";
        //
        // undoItem
        //
        undoItem.Name = "undoItem";
        undoItem.ShortcutKeys = Keys.Control | Keys.Z;
        undoItem.Text = "&Undo";
        undoItem.Click += OnUndo;
        //
        // redoItem
        //
        redoItem.Name = "redoItem";
        redoItem.ShortcutKeys = Keys.Control | Keys.Y;
        redoItem.Text = "&Redo";
        redoItem.Click += OnRedo;
        //
        // editSeparator1
        //
        editSeparator1.Name = "editSeparator1";
        //
        // addCommandItem
        //
        addCommandItem.Name = "addCommandItem";
        addCommandItem.ShortcutKeys = Keys.Control | Keys.N;
        addCommandItem.Text = "Add &command";
        addCommandItem.Click += OnAddCommand;
        //
        // addSubmenuItem
        //
        addSubmenuItem.Name = "addSubmenuItem";
        addSubmenuItem.Text = "Add sub&menu";
        addSubmenuItem.Click += OnAddSubmenu;
        //
        // addSeparatorItem
        //
        addSeparatorItem.Name = "addSeparatorItem";
        addSeparatorItem.Text = "Add se&parator";
        addSeparatorItem.Click += OnAddSeparator;
        //
        // duplicateItem
        //
        duplicateItem.Name = "duplicateItem";
        duplicateItem.ShortcutKeys = Keys.Control | Keys.D;
        duplicateItem.Text = "D&uplicate";
        duplicateItem.Click += OnDuplicate;
        //
        // deleteItem
        //
        deleteItem.Name = "deleteItem";
        deleteItem.ShortcutKeyDisplayString = "Del";
        deleteItem.Text = "&Delete";
        deleteItem.Click += OnDelete;
        //
        // editSeparator2
        //
        editSeparator2.Name = "editSeparator2";
        //
        // moveUpItem
        //
        moveUpItem.Name = "moveUpItem";
        moveUpItem.Text = "Move u&p";
        moveUpItem.Click += OnMoveUp;
        //
        // moveDownItem
        //
        moveDownItem.Name = "moveDownItem";
        moveDownItem.Text = "Move dow&n";
        moveDownItem.Click += OnMoveDown;
        //
        // moveInItem
        //
        moveInItem.Name = "moveInItem";
        moveInItem.Text = "Move &into the submenu above";
        moveInItem.Click += OnMoveIn;
        //
        // moveOutItem
        //
        moveOutItem.Name = "moveOutItem";
        moveOutItem.Text = "Move &out of the submenu";
        moveOutItem.Click += OnMoveOut;
        //
        // testMenu
        //
        testMenu.DropDownItems.AddRange(new ToolStripItem[] { previewItem });
        testMenu.Name = "testMenu";
        testMenu.Text = "&Test";
        //
        // previewItem
        //
        previewItem.Name = "previewItem";
        previewItem.ShortcutKeys = Keys.F5;
        previewItem.Text = "&Preview the menu…";
        previewItem.Click += OnPreview;
        //
        // toolsMenu
        //
        toolsMenu.DropDownItems.AddRange(new ToolStripItem[] { checkItem, toolsSeparator, backupItem, restoreItem, resetItem, ffmpegSeparator, ffmpegItem });
        toolsMenu.Name = "toolsMenu";
        toolsMenu.Text = "Tools";
        //
        // checkItem
        //
        checkItem.Name = "checkItem";
        checkItem.Text = "Check the installation";
        checkItem.Click += OnCheckInstallation;
        //
        // toolsSeparator
        //
        toolsSeparator.Name = "toolsSeparator";
        //
        // backupItem
        //
        backupItem.Name = "backupItem";
        backupItem.Text = "Back up the configuration…";
        backupItem.Click += OnBackup;
        //
        // restoreItem
        //
        restoreItem.Name = "restoreItem";
        restoreItem.Text = "Restore a configuration…";
        restoreItem.Click += OnRestore;
        //
        // resetItem
        //
        resetItem.Name = "resetItem";
        resetItem.Text = "Reset to the template…";
        resetItem.Click += OnReset;
        //
        // ffmpegSeparator
        //
        ffmpegSeparator.Name = "ffmpegSeparator";
        //
        // ffmpegItem
        //
        ffmpegItem.Name = "ffmpegItem";
        ffmpegItem.Text = "ffmpeg…";
        ffmpegItem.Click += OnFfmpeg;
        //
        // helpMenu
        //
        helpMenu.DropDownItems.AddRange(new ToolStripItem[] { aboutItem });
        helpMenu.Name = "helpMenu";
        helpMenu.Text = "&Help";
        //
        // aboutItem
        //
        aboutItem.Name = "aboutItem";
        aboutItem.Text = "&About";
        aboutItem.Click += OnAbout;
        //
        // toolStrip
        //
        toolStrip.Items.AddRange(new ToolStripItem[] { addCommandButton, addSubmenuButton, addSeparatorButton, toolSeparator1, deleteButton, moveUpButton, moveDownButton, toolSeparator2, undoButton, redoButton, toolSeparator3, previewButton, saveButton });
        toolStrip.Location = new Point(0, 24);
        toolStrip.Name = "toolStrip";
        toolStrip.Size = new Size(1000, 25);
        toolStrip.TabIndex = 1;
        //
        // addCommandButton
        //
        addCommandButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        addCommandButton.Name = "addCommandButton";
        addCommandButton.Text = "Add command";
        addCommandButton.Click += OnAddCommand;
        //
        // addSubmenuButton
        //
        addSubmenuButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        addSubmenuButton.Name = "addSubmenuButton";
        addSubmenuButton.Text = "Add submenu";
        addSubmenuButton.Click += OnAddSubmenu;
        //
        // addSeparatorButton
        //
        addSeparatorButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        addSeparatorButton.Name = "addSeparatorButton";
        addSeparatorButton.Text = "Add separator";
        addSeparatorButton.Click += OnAddSeparator;
        //
        // toolSeparator1
        //
        toolSeparator1.Name = "toolSeparator1";
        //
        // deleteButton
        //
        deleteButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        deleteButton.Name = "deleteButton";
        deleteButton.Text = "Delete";
        deleteButton.Click += OnDelete;
        //
        // moveUpButton
        //
        moveUpButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        moveUpButton.Name = "moveUpButton";
        moveUpButton.Text = "Move up";
        moveUpButton.Click += OnMoveUp;
        //
        // moveDownButton
        //
        moveDownButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        moveDownButton.Name = "moveDownButton";
        moveDownButton.Text = "Move down";
        moveDownButton.Click += OnMoveDown;
        //
        // toolSeparator2
        //
        toolSeparator2.Name = "toolSeparator2";
        //
        // undoButton
        //
        undoButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        undoButton.Name = "undoButton";
        undoButton.Text = "Undo";
        undoButton.Click += OnUndo;
        //
        // redoButton
        //
        redoButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        redoButton.Name = "redoButton";
        redoButton.Text = "Redo";
        redoButton.Click += OnRedo;
        //
        // toolSeparator3
        //
        toolSeparator3.Name = "toolSeparator3";
        //
        // previewButton
        //
        previewButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        previewButton.Name = "previewButton";
        previewButton.Text = "Preview";
        previewButton.Click += OnPreview;
        //
        // saveButton
        //
        saveButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        saveButton.Name = "saveButton";
        saveButton.Text = "Save";
        saveButton.Click += OnSave;
        //
        // outerSplit
        //
        outerSplit.Dock = DockStyle.Fill;
        outerSplit.Location = new Point(0, 49);
        outerSplit.Name = "outerSplit";
        outerSplit.Orientation = Orientation.Horizontal;
        outerSplit.Panel1.Controls.Add(mainSplit);
        outerSplit.Panel2.Controls.Add(issuesList);
        outerSplit.Size = new Size(1000, 629);
        outerSplit.SplitterDistance = 500;
        outerSplit.TabIndex = 2;
        //
        // mainSplit
        //
        mainSplit.Dock = DockStyle.Fill;
        mainSplit.Location = new Point(0, 0);
        mainSplit.Name = "mainSplit";
        mainSplit.Panel1.Controls.Add(tree);
        mainSplit.Panel2.Controls.Add(detailHost);
        mainSplit.Size = new Size(1000, 500);
        mainSplit.SplitterDistance = 330;
        mainSplit.TabIndex = 0;
        //
        // tree
        //
        tree.AllowDrop = true;
        tree.CheckBoxes = true;
        tree.ContextMenuStrip = treeMenu;
        tree.Dock = DockStyle.Fill;
        tree.HideSelection = false;
        tree.Location = new Point(0, 0);
        tree.Name = "tree";
        tree.Size = new Size(330, 500);
        tree.TabIndex = 0;
        tree.AfterCheck += OnTreeAfterCheck;
        tree.ItemDrag += OnTreeItemDrag;
        tree.AfterSelect += OnTreeAfterSelect;
        tree.NodeMouseClick += OnTreeNodeMouseClick;
        tree.DragDrop += OnTreeDragDrop;
        tree.DragOver += OnTreeDragOver;
        tree.KeyDown += OnTreeKeyDown;
        //
        // detailHost
        //
        detailHost.AutoScroll = true;
        detailHost.Controls.Add(entryEditor);
        detailHost.Controls.Add(messageLabel);
        detailHost.Dock = DockStyle.Fill;
        detailHost.Location = new Point(0, 0);
        detailHost.Name = "detailHost";
        detailHost.Size = new Size(666, 500);
        detailHost.TabIndex = 0;
        //
        // entryEditor
        //
        entryEditor.AutoSize = true;
        entryEditor.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        entryEditor.Dock = DockStyle.Top;
        entryEditor.Location = new Point(0, 60);
        entryEditor.Name = "entryEditor";
        entryEditor.Size = new Size(666, 400);
        entryEditor.TabIndex = 1;
        entryEditor.Visible = false;
        //
        // messageLabel
        //
        messageLabel.Dock = DockStyle.Top;
        messageLabel.ForeColor = SystemColors.GrayText;
        messageLabel.Location = new Point(0, 0);
        messageLabel.Name = "messageLabel";
        messageLabel.Padding = new Padding(12);
        messageLabel.Size = new Size(666, 60);
        messageLabel.TabIndex = 0;
        messageLabel.Text = "Select an entry on the left, or add one.";
        //
        // issuesList
        //
        issuesList.Columns.AddRange(new ColumnHeader[] { issueLocationColumn, issueProblemColumn });
        issuesList.Dock = DockStyle.Fill;
        issuesList.FullRowSelect = true;
        issuesList.Location = new Point(0, 0);
        issuesList.MultiSelect = false;
        issuesList.Name = "issuesList";
        issuesList.Size = new Size(1000, 125);
        issuesList.TabIndex = 0;
        issuesList.UseCompatibleStateImageBehavior = false;
        issuesList.View = View.Details;
        issuesList.ItemActivate += OnIssueActivate;
        //
        // issueLocationColumn
        //
        issueLocationColumn.Text = "Where";
        issueLocationColumn.Width = 320;
        //
        // issueProblemColumn
        //
        issueProblemColumn.Text = "Problem";
        issueProblemColumn.Width = 640;
        //
        // statusStrip
        //
        statusStrip.Items.AddRange(new ToolStripItem[] { pathStatus, stateStatus, issuesStatus });
        statusStrip.Location = new Point(0, 678);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1000, 22);
        statusStrip.TabIndex = 3;
        //
        // pathStatus
        //
        pathStatus.IsLink = true;
        pathStatus.Name = "pathStatus";
        pathStatus.Spring = true;
        pathStatus.TextAlign = ContentAlignment.MiddleLeft;
        pathStatus.Click += OnPathStatusClick;
        //
        // stateStatus
        //
        stateStatus.Name = "stateStatus";
        //
        // issuesStatus
        //
        issuesStatus.Name = "issuesStatus";
        //
        // treeMenu
        //
        treeMenu.Items.AddRange(new ToolStripItem[] { ctxAddCommandItem, ctxAddSubmenuItem, ctxAddSeparatorItem, ctxSeparator1, ctxDuplicateItem, ctxDeleteItem, ctxSeparator2, ctxMoveUpItem, ctxMoveDownItem, ctxMoveInItem, ctxMoveOutItem });
        treeMenu.Name = "treeMenu";
        treeMenu.Opening += OnTreeMenuOpening;
        //
        // ctxAddCommandItem
        //
        ctxAddCommandItem.Name = "ctxAddCommandItem";
        ctxAddCommandItem.Text = "Add command";
        ctxAddCommandItem.Click += OnAddCommand;
        //
        // ctxAddSubmenuItem
        //
        ctxAddSubmenuItem.Name = "ctxAddSubmenuItem";
        ctxAddSubmenuItem.Text = "Add submenu";
        ctxAddSubmenuItem.Click += OnAddSubmenu;
        //
        // ctxAddSeparatorItem
        //
        ctxAddSeparatorItem.Name = "ctxAddSeparatorItem";
        ctxAddSeparatorItem.Text = "Add separator";
        ctxAddSeparatorItem.Click += OnAddSeparator;
        //
        // ctxSeparator1
        //
        ctxSeparator1.Name = "ctxSeparator1";
        //
        // ctxDuplicateItem
        //
        ctxDuplicateItem.Name = "ctxDuplicateItem";
        ctxDuplicateItem.Text = "Duplicate";
        ctxDuplicateItem.Click += OnDuplicate;
        //
        // ctxDeleteItem
        //
        ctxDeleteItem.Name = "ctxDeleteItem";
        ctxDeleteItem.Text = "Delete";
        ctxDeleteItem.Click += OnDelete;
        //
        // ctxSeparator2
        //
        ctxSeparator2.Name = "ctxSeparator2";
        //
        // ctxMoveUpItem
        //
        ctxMoveUpItem.Name = "ctxMoveUpItem";
        ctxMoveUpItem.Text = "Move up";
        ctxMoveUpItem.Click += OnMoveUp;
        //
        // ctxMoveDownItem
        //
        ctxMoveDownItem.Name = "ctxMoveDownItem";
        ctxMoveDownItem.Text = "Move down";
        ctxMoveDownItem.Click += OnMoveDown;
        //
        // ctxMoveInItem
        //
        ctxMoveInItem.Name = "ctxMoveInItem";
        ctxMoveInItem.Text = "Move into the submenu above";
        ctxMoveInItem.Click += OnMoveIn;
        //
        // ctxMoveOutItem
        //
        ctxMoveOutItem.Name = "ctxMoveOutItem";
        ctxMoveOutItem.Text = "Move out of the submenu";
        ctxMoveOutItem.Click += OnMoveOut;
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 700);
        Controls.Add(outerSplit);
        Controls.Add(toolStrip);
        Controls.Add(menuStrip);
        Controls.Add(statusStrip);
        MainMenuStrip = menuStrip;
        MinimumSize = new Size(640, 420);
        Name = "MainForm";
        StartPosition = FormStartPosition.WindowsDefaultBounds;
        Text = "FBCTools Editor";
        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        toolStrip.ResumeLayout(false);
        toolStrip.PerformLayout();
        outerSplit.Panel1.ResumeLayout(false);
        outerSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)outerSplit).EndInit();
        outerSplit.ResumeLayout(false);
        mainSplit.Panel1.ResumeLayout(false);
        mainSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)mainSplit).EndInit();
        mainSplit.ResumeLayout(false);
        detailHost.ResumeLayout(false);
        detailHost.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        treeMenu.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private MenuStrip menuStrip;
    private ToolStripMenuItem fileMenu;
    private ToolStripMenuItem saveItem;
    private ToolStripMenuItem reloadItem;
    private ToolStripMenuItem openFolderItem;
    private ToolStripSeparator fileSeparator;
    private ToolStripMenuItem exitItem;
    private ToolStripMenuItem editMenu;
    private ToolStripMenuItem undoItem;
    private ToolStripMenuItem redoItem;
    private ToolStripSeparator editSeparator1;
    private ToolStripMenuItem addCommandItem;
    private ToolStripMenuItem addSubmenuItem;
    private ToolStripMenuItem addSeparatorItem;
    private ToolStripMenuItem duplicateItem;
    private ToolStripMenuItem deleteItem;
    private ToolStripSeparator editSeparator2;
    private ToolStripMenuItem moveUpItem;
    private ToolStripMenuItem moveDownItem;
    private ToolStripMenuItem moveInItem;
    private ToolStripMenuItem moveOutItem;
    private ToolStripMenuItem testMenu;
    private ToolStripMenuItem toolsMenu;
    private ToolStripMenuItem checkItem;
    private ToolStripSeparator toolsSeparator;
    private ToolStripMenuItem backupItem;
    private ToolStripMenuItem restoreItem;
    private ToolStripMenuItem resetItem;
    private ToolStripSeparator ffmpegSeparator;
    private ToolStripMenuItem ffmpegItem;
    private ToolStripMenuItem previewItem;
    private ToolStripMenuItem helpMenu;
    private ToolStripMenuItem aboutItem;
    private ToolStrip toolStrip;
    private ToolStripButton addCommandButton;
    private ToolStripButton addSubmenuButton;
    private ToolStripButton addSeparatorButton;
    private ToolStripSeparator toolSeparator1;
    private ToolStripButton deleteButton;
    private ToolStripButton moveUpButton;
    private ToolStripButton moveDownButton;
    private ToolStripSeparator toolSeparator2;
    private ToolStripButton undoButton;
    private ToolStripButton redoButton;
    private ToolStripSeparator toolSeparator3;
    private ToolStripButton previewButton;
    private ToolStripButton saveButton;
    private SplitContainer outerSplit;
    private SplitContainer mainSplit;
    private CheckBoxTreeView tree;
    private Panel detailHost;
    private EntryEditor entryEditor;
    private Label messageLabel;
    private ListView issuesList;
    private ColumnHeader issueLocationColumn;
    private ColumnHeader issueProblemColumn;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel pathStatus;
    private ToolStripStatusLabel stateStatus;
    private ToolStripStatusLabel issuesStatus;
    private ContextMenuStrip treeMenu;
    private ToolStripMenuItem ctxAddCommandItem;
    private ToolStripMenuItem ctxAddSubmenuItem;
    private ToolStripMenuItem ctxAddSeparatorItem;
    private ToolStripSeparator ctxSeparator1;
    private ToolStripMenuItem ctxDuplicateItem;
    private ToolStripMenuItem ctxDeleteItem;
    private ToolStripSeparator ctxSeparator2;
    private ToolStripMenuItem ctxMoveUpItem;
    private ToolStripMenuItem ctxMoveDownItem;
    private ToolStripMenuItem ctxMoveInItem;
    private ToolStripMenuItem ctxMoveOutItem;
}
