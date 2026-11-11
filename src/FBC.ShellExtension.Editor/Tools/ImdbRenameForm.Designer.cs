#nullable disable

namespace FBC.ShellExtension.Editor.Tools;

partial class ImdbRenameForm
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
        formatPanel = new TableLayoutPanel();
        formatLabel = new Label();
        formatBox = new TextBox();
        searchButton = new Button();
        keyLink = new LinkLabel();
        tokensLabel = new Label();
        subfoldersCheck = new CheckBox();
        previewList = new ListView();
        fileColumn = new ColumnHeader();
        filmColumn = new ColumnHeader();
        newColumn = new ColumnHeader();
        stateColumn = new ColumnHeader();
        listMenu = new ContextMenuStrip(components);
        chooseItem = new ToolStripMenuItem();
        leaveItem = new ToolStripMenuItem();
        openItem = new ToolStripMenuItem();
        statusLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        closeButton = new Button();
        renameButton = new Button();
        refreshTimer = new System.Windows.Forms.Timer(components);
        layout.SuspendLayout();
        formatPanel.SuspendLayout();
        listMenu.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // layout
        //
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(selectionLabel, 0, 0);
        layout.Controls.Add(formatPanel, 0, 1);
        layout.Controls.Add(tokensLabel, 0, 2);
        layout.Controls.Add(subfoldersCheck, 0, 3);
        layout.Controls.Add(previewList, 0, 4);
        layout.Controls.Add(statusLabel, 0, 5);
        layout.Controls.Add(buttonPanel, 0, 6);
        layout.Dock = DockStyle.Fill;
        layout.Location = new Point(0, 0);
        layout.Name = "layout";
        layout.Padding = new Padding(8);
        layout.RowCount = 7;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
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
        // formatPanel
        //
        formatPanel.AutoSize = true;
        formatPanel.ColumnCount = 4;
        formatPanel.ColumnStyles.Add(new ColumnStyle());
        formatPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        formatPanel.ColumnStyles.Add(new ColumnStyle());
        formatPanel.ColumnStyles.Add(new ColumnStyle());
        formatPanel.Controls.Add(formatLabel, 0, 0);
        formatPanel.Controls.Add(formatBox, 1, 0);
        formatPanel.Controls.Add(searchButton, 2, 0);
        formatPanel.Controls.Add(keyLink, 3, 0);
        formatPanel.Dock = DockStyle.Fill;
        formatPanel.Margin = new Padding(0);
        formatPanel.Name = "formatPanel";
        formatPanel.RowCount = 1;
        formatPanel.RowStyles.Add(new RowStyle());
        formatPanel.TabIndex = 1;
        //
        // formatLabel
        //
        formatLabel.Anchor = AnchorStyles.Left;
        formatLabel.AutoSize = true;
        formatLabel.Name = "formatLabel";
        formatLabel.TabIndex = 0;
        formatLabel.Text = "&Name:";
        //
        // formatBox
        //
        formatBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        formatBox.Name = "formatBox";
        formatBox.TabIndex = 1;
        formatBox.TextChanged += OnFormatChanged;
        //
        // searchButton
        //
        searchButton.AutoSize = true;
        searchButton.Enabled = false;
        searchButton.MinimumSize = new Size(90, 0);
        searchButton.Name = "searchButton";
        searchButton.TabIndex = 2;
        searchButton.Text = "&Search";
        searchButton.UseVisualStyleBackColor = true;
        searchButton.Click += OnSearch;
        //
        // keyLink
        //
        keyLink.Anchor = AnchorStyles.Left;
        keyLink.AutoSize = true;
        keyLink.Margin = new Padding(9, 0, 3, 0);
        keyLink.Name = "keyLink";
        keyLink.TabIndex = 3;
        keyLink.TabStop = true;
        keyLink.Text = "OMDb API key…";
        keyLink.LinkClicked += OnKeyLink;
        //
        // tokensLabel
        //
        tokensLabel.AutoSize = true;
        tokensLabel.ForeColor = SystemColors.GrayText;
        tokensLabel.Margin = new Padding(3, 3, 3, 3);
        tokensLabel.Name = "tokensLabel";
        tokensLabel.TabIndex = 2;
        tokensLabel.UseMnemonic = false;
        //
        // subfoldersCheck
        //
        subfoldersCheck.AutoSize = true;
        subfoldersCheck.Margin = new Padding(3, 3, 3, 6);
        subfoldersCheck.Name = "subfoldersCheck";
        subfoldersCheck.TabIndex = 3;
        subfoldersCheck.Text = "Also in sub&folders";
        subfoldersCheck.UseVisualStyleBackColor = true;
        subfoldersCheck.CheckedChanged += OnSubfoldersChanged;
        //
        // previewList
        //
        previewList.Columns.AddRange(new ColumnHeader[] { fileColumn, filmColumn, newColumn, stateColumn });
        previewList.ContextMenuStrip = listMenu;
        previewList.Dock = DockStyle.Fill;
        previewList.FullRowSelect = true;
        previewList.HideSelection = false;
        previewList.Name = "previewList";
        previewList.TabIndex = 4;
        previewList.UseCompatibleStateImageBehavior = false;
        previewList.View = View.Details;
        previewList.DoubleClick += OnListDoubleClick;
        //
        // fileColumn
        //
        fileColumn.Text = "File";
        fileColumn.Width = 260;
        //
        // filmColumn
        //
        filmColumn.Text = "Film";
        filmColumn.Width = 220;
        //
        // newColumn
        //
        newColumn.Text = "New name";
        newColumn.Width = 300;
        //
        // stateColumn
        //
        stateColumn.Text = "State";
        stateColumn.Width = 200;
        //
        // listMenu
        //
        listMenu.Items.AddRange(new ToolStripItem[] { chooseItem, leaveItem, openItem });
        listMenu.Name = "listMenu";
        listMenu.Opening += OnMenuOpening;
        //
        // chooseItem
        //
        chooseItem.Name = "chooseItem";
        chooseItem.Text = "&Choose the film…";
        chooseItem.Click += OnChoose;
        //
        // leaveItem
        //
        leaveItem.Name = "leaveItem";
        leaveItem.Text = "&Leave the name as it is";
        leaveItem.Click += OnLeave;
        //
        // openItem
        //
        openItem.Name = "openItem";
        openItem.Text = "&Open on IMDb";
        openItem.Click += OnOpen;
        //
        // statusLabel
        //
        statusLabel.AutoSize = true;
        statusLabel.Margin = new Padding(3, 6, 3, 6);
        statusLabel.Name = "statusLabel";
        statusLabel.TabIndex = 5;
        statusLabel.UseMnemonic = false;
        //
        // buttonPanel
        //
        buttonPanel.AutoSize = true;
        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(renameButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Name = "buttonPanel";
        buttonPanel.TabIndex = 6;
        //
        // closeButton
        //
        closeButton.AutoSize = true;
        closeButton.Name = "closeButton";
        closeButton.TabIndex = 1;
        closeButton.Text = "Close";
        closeButton.UseVisualStyleBackColor = true;
        closeButton.Click += OnClose;
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
        // ImdbRenameForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = closeButton;
        ClientSize = new Size(1000, 580);
        Controls.Add(layout);
        MinimumSize = new Size(640, 420);
        Name = "ImdbRenameForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Rename from IMDb";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        formatPanel.ResumeLayout(false);
        formatPanel.PerformLayout();
        listMenu.ResumeLayout(false);
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel layout;
    private Label selectionLabel;
    private TableLayoutPanel formatPanel;
    private Label formatLabel;
    private TextBox formatBox;
    private Button searchButton;
    private LinkLabel keyLink;
    private Label tokensLabel;
    private CheckBox subfoldersCheck;
    private ListView previewList;
    private ColumnHeader fileColumn;
    private ColumnHeader filmColumn;
    private ColumnHeader newColumn;
    private ColumnHeader stateColumn;
    private ContextMenuStrip listMenu;
    private ToolStripMenuItem chooseItem;
    private ToolStripMenuItem leaveItem;
    private ToolStripMenuItem openItem;
    private Label statusLabel;
    private FlowLayoutPanel buttonPanel;
    private Button closeButton;
    private Button renameButton;
    private System.Windows.Forms.Timer refreshTimer;
}
