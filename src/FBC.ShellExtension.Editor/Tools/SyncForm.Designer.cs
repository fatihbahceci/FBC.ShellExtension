#nullable disable

namespace FBC.ShellExtension.Editor.Tools;

partial class SyncForm
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
        foldersPanel = new TableLayoutPanel();
        sourceLabel = new Label();
        sourceBox = new TextBox();
        swapButton = new Button();
        destinationLabel = new Label();
        destinationBox = new TextBox();
        browseButton = new Button();
        extrasCheck = new CheckBox();
        hintLabel = new Label();
        compareButton = new Button();
        planList = new ListView();
        pathColumn = new ColumnHeader();
        actionColumn = new ColumnHeader();
        sizeColumn = new ColumnHeader();
        modifiedColumn = new ColumnHeader();
        statusLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        closeButton = new Button();
        syncButton = new Button();
        folderDialog = new FolderBrowserDialog();
        layout.SuspendLayout();
        foldersPanel.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // layout
        //
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(foldersPanel, 0, 0);
        layout.Controls.Add(extrasCheck, 0, 1);
        layout.Controls.Add(hintLabel, 0, 2);
        layout.Controls.Add(compareButton, 0, 3);
        layout.Controls.Add(planList, 0, 4);
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
        layout.Size = new Size(760, 560);
        layout.TabIndex = 0;
        //
        // foldersPanel
        //
        foldersPanel.AutoSize = true;
        foldersPanel.ColumnCount = 3;
        foldersPanel.ColumnStyles.Add(new ColumnStyle());
        foldersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        foldersPanel.ColumnStyles.Add(new ColumnStyle());
        foldersPanel.Controls.Add(sourceLabel, 0, 0);
        foldersPanel.Controls.Add(sourceBox, 1, 0);
        foldersPanel.Controls.Add(swapButton, 2, 0);
        foldersPanel.Controls.Add(destinationLabel, 0, 1);
        foldersPanel.Controls.Add(destinationBox, 1, 1);
        foldersPanel.Controls.Add(browseButton, 2, 1);
        foldersPanel.Dock = DockStyle.Fill;
        foldersPanel.Margin = new Padding(0);
        foldersPanel.Name = "foldersPanel";
        foldersPanel.RowCount = 2;
        foldersPanel.RowStyles.Add(new RowStyle());
        foldersPanel.RowStyles.Add(new RowStyle());
        foldersPanel.TabIndex = 0;
        //
        // sourceLabel
        //
        sourceLabel.Anchor = AnchorStyles.Left;
        sourceLabel.AutoSize = true;
        sourceLabel.Name = "sourceLabel";
        sourceLabel.TabIndex = 0;
        sourceLabel.Text = "From:";
        //
        // sourceBox
        //
        sourceBox.Dock = DockStyle.Fill;
        sourceBox.Name = "sourceBox";
        sourceBox.ReadOnly = true;
        sourceBox.TabIndex = 1;
        sourceBox.TextChanged += OnFoldersChanged;
        //
        // swapButton
        //
        swapButton.AutoSize = true;
        swapButton.Enabled = false;
        swapButton.Name = "swapButton";
        swapButton.TabIndex = 2;
        swapButton.Text = "Swap";
        swapButton.UseVisualStyleBackColor = true;
        swapButton.Click += OnSwap;
        //
        // destinationLabel
        //
        destinationLabel.Anchor = AnchorStyles.Left;
        destinationLabel.AutoSize = true;
        destinationLabel.Name = "destinationLabel";
        destinationLabel.TabIndex = 3;
        destinationLabel.Text = "To:";
        //
        // destinationBox
        //
        destinationBox.Dock = DockStyle.Fill;
        destinationBox.Name = "destinationBox";
        destinationBox.TabIndex = 4;
        destinationBox.TextChanged += OnFoldersChanged;
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
        // extrasCheck
        //
        extrasCheck.AutoSize = true;
        extrasCheck.Name = "extrasCheck";
        extrasCheck.TabIndex = 1;
        extrasCheck.Text = "Send what is only in the destination to the Recycle Bin";
        extrasCheck.UseVisualStyleBackColor = true;
        extrasCheck.CheckedChanged += OnExtrasChanged;
        //
        // hintLabel
        //
        hintLabel.AutoSize = true;
        hintLabel.ForeColor = SystemColors.GrayText;
        hintLabel.Margin = new Padding(3, 3, 3, 6);
        hintLabel.Name = "hintLabel";
        hintLabel.TabIndex = 2;
        hintLabel.Text = "Files newer in the destination are listed but not ticked.";
        //
        // compareButton
        //
        compareButton.AutoSize = true;
        compareButton.Name = "compareButton";
        compareButton.TabIndex = 3;
        compareButton.Text = "Compare";
        compareButton.UseVisualStyleBackColor = true;
        compareButton.Click += OnCompare;
        //
        // planList
        //
        planList.CheckBoxes = true;
        planList.Columns.AddRange(new ColumnHeader[] { pathColumn, actionColumn, sizeColumn, modifiedColumn });
        planList.Dock = DockStyle.Fill;
        planList.FullRowSelect = true;
        planList.Name = "planList";
        planList.TabIndex = 4;
        planList.UseCompatibleStateImageBehavior = false;
        planList.View = View.Details;
        planList.ItemChecked += OnItemChecked;
        planList.Enter += OnListEnter;
        planList.Leave += OnListLeave;
        //
        // pathColumn
        //
        pathColumn.Text = "Path";
        pathColumn.Width = 360;
        //
        // actionColumn
        //
        actionColumn.Text = "What";
        actionColumn.Width = 160;
        //
        // sizeColumn
        //
        sizeColumn.Text = "Size";
        sizeColumn.TextAlign = HorizontalAlignment.Right;
        sizeColumn.Width = 90;
        //
        // modifiedColumn
        //
        modifiedColumn.Text = "Modified";
        modifiedColumn.Width = 130;
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
        buttonPanel.Controls.Add(syncButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Name = "buttonPanel";
        buttonPanel.TabIndex = 6;
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
        // syncButton
        //
        syncButton.AutoSize = true;
        syncButton.Enabled = false;
        syncButton.Name = "syncButton";
        syncButton.TabIndex = 0;
        syncButton.Text = "Synchronize";
        syncButton.UseVisualStyleBackColor = true;
        syncButton.Click += OnSync;
        //
        // SyncForm
        //
        AcceptButton = compareButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = closeButton;
        ClientSize = new Size(760, 560);
        Controls.Add(layout);
        MinimumSize = new Size(560, 420);
        Name = "SyncForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Synchronize folders";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        foldersPanel.ResumeLayout(false);
        foldersPanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel layout;
    private TableLayoutPanel foldersPanel;
    private Label sourceLabel;
    private TextBox sourceBox;
    private Button swapButton;
    private Label destinationLabel;
    private TextBox destinationBox;
    private Button browseButton;
    private CheckBox extrasCheck;
    private Label hintLabel;
    private Button compareButton;
    private ListView planList;
    private ColumnHeader pathColumn;
    private ColumnHeader actionColumn;
    private ColumnHeader sizeColumn;
    private ColumnHeader modifiedColumn;
    private Label statusLabel;
    private FlowLayoutPanel buttonPanel;
    private Button closeButton;
    private Button syncButton;
    private FolderBrowserDialog folderDialog;
}
