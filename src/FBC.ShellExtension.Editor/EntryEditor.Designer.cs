#nullable disable

namespace FBC.ShellExtension.Editor;

partial class EntryEditor
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
        labelLabel = new Label();
        labelBox = new TextBox();
        iconLabel = new Label();
        iconPanel = new FlowLayoutPanel();
        iconPicture = new PictureBox();
        chooseIconButton = new Button();
        refreshIconButton = new Button();
        removeIconButton = new Button();
        iconDialog = new OpenFileDialog();
        taskbarPanel = new FlowLayoutPanel();
        taskbarCheck = new CheckBox();
        taskbarNote = new Label();
        matchHeader = new Label();
        matchEditor = new MatchRuleEditor();
        commandHeader = new Label();
        commandEditor = new CommandEditor();
        layout.SuspendLayout();
        iconPanel.SuspendLayout();
        taskbarPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)iconPicture).BeginInit();
        SuspendLayout();
        //
        // layout
        //
        layout.AutoSize = true;
        layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        layout.ColumnCount = 2;
        layout.ColumnStyles.Add(new ColumnStyle());
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(labelLabel, 0, 0);
        layout.Controls.Add(labelBox, 1, 0);
        layout.Controls.Add(iconLabel, 0, 1);
        layout.Controls.Add(iconPanel, 1, 1);
        layout.Controls.Add(taskbarPanel, 1, 2);
        layout.Controls.Add(matchHeader, 0, 3);
        layout.Controls.Add(matchEditor, 0, 4);
        layout.Controls.Add(commandHeader, 0, 5);
        layout.Controls.Add(commandEditor, 0, 6);
        layout.Dock = DockStyle.Top;
        layout.Location = new Point(0, 0);
        layout.Name = "layout";
        layout.Padding = new Padding(8);
        layout.RowCount = 7;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.SetColumnSpan(matchHeader, 2);
        layout.SetColumnSpan(matchEditor, 2);
        layout.SetColumnSpan(commandHeader, 2);
        layout.SetColumnSpan(commandEditor, 2);
        layout.Size = new Size(500, 700);
        layout.TabIndex = 0;
        //
        // labelLabel
        //
        labelLabel.Anchor = AnchorStyles.Left;
        labelLabel.AutoSize = true;
        labelLabel.Name = "labelLabel";
        labelLabel.TabIndex = 0;
        labelLabel.Text = "Label:";
        //
        // labelBox
        //
        labelBox.Dock = DockStyle.Fill;
        labelBox.Name = "labelBox";
        labelBox.TabIndex = 1;
        labelBox.TextChanged += OnLabelChanged;
        //
        // iconLabel
        //
        iconLabel.Anchor = AnchorStyles.Left;
        iconLabel.AutoSize = true;
        iconLabel.Name = "iconLabel";
        iconLabel.TabIndex = 2;
        iconLabel.Text = "Icon:";
        //
        // iconPanel
        //
        iconPanel.AutoSize = true;
        iconPanel.Controls.Add(iconPicture);
        iconPanel.Controls.Add(chooseIconButton);
        iconPanel.Controls.Add(refreshIconButton);
        iconPanel.Controls.Add(removeIconButton);
        iconPanel.Margin = new Padding(0);
        iconPanel.Name = "iconPanel";
        iconPanel.TabIndex = 3;
        iconPanel.WrapContents = false;
        //
        // iconPicture
        //
        iconPicture.Anchor = AnchorStyles.Left;
        iconPicture.Name = "iconPicture";
        iconPicture.Size = new Size(32, 32);
        iconPicture.SizeMode = PictureBoxSizeMode.Zoom;
        iconPicture.TabIndex = 0;
        iconPicture.TabStop = false;
        //
        // chooseIconButton
        //
        chooseIconButton.Anchor = AnchorStyles.Left;
        chooseIconButton.AutoSize = true;
        chooseIconButton.Name = "chooseIconButton";
        chooseIconButton.TabIndex = 1;
        chooseIconButton.Text = "Choose icon…";
        chooseIconButton.UseVisualStyleBackColor = true;
        chooseIconButton.Click += OnChooseIcon;
        //
        // refreshIconButton
        //
        refreshIconButton.Anchor = AnchorStyles.Left;
        refreshIconButton.AutoSize = true;
        refreshIconButton.Name = "refreshIconButton";
        refreshIconButton.TabIndex = 2;
        refreshIconButton.Text = "Refresh icon";
        refreshIconButton.UseVisualStyleBackColor = true;
        refreshIconButton.Click += OnRefreshIcon;
        //
        // removeIconButton
        //
        removeIconButton.Anchor = AnchorStyles.Left;
        removeIconButton.AutoSize = true;
        removeIconButton.Name = "removeIconButton";
        removeIconButton.TabIndex = 3;
        removeIconButton.Text = "Remove icon";
        removeIconButton.UseVisualStyleBackColor = true;
        removeIconButton.Click += OnRemoveIcon;
        //
        // taskbarPanel
        //
        taskbarPanel.AutoSize = true;
        taskbarPanel.Controls.Add(taskbarCheck);
        taskbarPanel.Controls.Add(taskbarNote);
        taskbarPanel.Margin = new Padding(0);
        taskbarPanel.Name = "taskbarPanel";
        taskbarPanel.TabIndex = 4;
        taskbarPanel.WrapContents = false;
        //
        // taskbarCheck
        //
        taskbarCheck.Anchor = AnchorStyles.Left;
        taskbarCheck.AutoCheck = false;
        taskbarCheck.AutoSize = true;
        taskbarCheck.Name = "taskbarCheck";
        taskbarCheck.TabIndex = 0;
        taskbarCheck.Text = "Show in taskbar";
        taskbarCheck.UseVisualStyleBackColor = true;
        taskbarCheck.Click += OnTaskbarClick;
        taskbarCheck.CheckStateChanged += OnTaskbarStateChanged;
        //
        // taskbarNote
        //
        taskbarNote.Anchor = AnchorStyles.Left;
        taskbarNote.AutoSize = true;
        taskbarNote.ForeColor = SystemColors.GrayText;
        taskbarNote.MaximumSize = new Size(420, 0);
        taskbarNote.Name = "taskbarNote";
        taskbarNote.TabIndex = 1;
        taskbarNote.UseMnemonic = false;
        //
        // iconDialog
        //
        iconDialog.DereferenceLinks = false;
        //
        // matchHeader
        //
        matchHeader.AutoSize = true;
        matchHeader.Margin = new Padding(3, 12, 3, 3);
        matchHeader.Name = "matchHeader";
        matchHeader.TabIndex = 5;
        matchHeader.Text = "Show when";
        //
        // matchEditor
        //
        matchEditor.AutoSize = true;
        matchEditor.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        matchEditor.Dock = DockStyle.Fill;
        matchEditor.Name = "matchEditor";
        matchEditor.TabIndex = 6;
        //
        // commandHeader
        //
        commandHeader.AutoSize = true;
        commandHeader.Margin = new Padding(3, 12, 3, 3);
        commandHeader.Name = "commandHeader";
        commandHeader.TabIndex = 7;
        commandHeader.Text = "Command";
        //
        // commandEditor
        //
        commandEditor.AutoSize = true;
        commandEditor.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        commandEditor.Dock = DockStyle.Fill;
        commandEditor.Name = "commandEditor";
        commandEditor.TabIndex = 8;
        //
        // EntryEditor
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(layout);
        Name = "EntryEditor";
        Size = new Size(500, 700);
        ((System.ComponentModel.ISupportInitialize)iconPicture).EndInit();
        iconPanel.ResumeLayout(false);
        iconPanel.PerformLayout();
        taskbarPanel.ResumeLayout(false);
        taskbarPanel.PerformLayout();
        layout.ResumeLayout(false);
        layout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel layout;
    private Label labelLabel;
    private TextBox labelBox;
    private Label iconLabel;
    private FlowLayoutPanel iconPanel;
    private PictureBox iconPicture;
    private Button chooseIconButton;
    private Button refreshIconButton;
    private Button removeIconButton;
    private OpenFileDialog iconDialog;
    private FlowLayoutPanel taskbarPanel;
    private CheckBox taskbarCheck;
    private Label taskbarNote;
    private Label matchHeader;
    private MatchRuleEditor matchEditor;
    private Label commandHeader;
    private CommandEditor commandEditor;
}
