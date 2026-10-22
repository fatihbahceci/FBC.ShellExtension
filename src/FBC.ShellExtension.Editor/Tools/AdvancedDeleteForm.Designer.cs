#nullable disable

namespace FBC.ShellExtension.Editor.Tools;

partial class AdvancedDeleteForm
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
        patternsPanel = new TableLayoutPanel();
        patternsLabel = new Label();
        patternsCombo = new ComboBox();
        findButton = new Button();
        optionsPanel = new FlowLayoutPanel();
        filesCheck = new CheckBox();
        foldersCheck = new CheckBox();
        subfoldersCheck = new CheckBox();
        hintLabel = new Label();
        foundList = new ListView();
        pathColumn = new ColumnHeader();
        sizeColumn = new ColumnHeader();
        modifiedColumn = new ColumnHeader();
        statusLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        closeButton = new Button();
        recycleButton = new Button();
        layout.SuspendLayout();
        patternsPanel.SuspendLayout();
        optionsPanel.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // layout
        //
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(selectionLabel, 0, 0);
        layout.Controls.Add(patternsPanel, 0, 1);
        layout.Controls.Add(optionsPanel, 0, 2);
        layout.Controls.Add(hintLabel, 0, 3);
        layout.Controls.Add(foundList, 0, 4);
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
        layout.Size = new Size(720, 520);
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
        // patternsPanel
        //
        patternsPanel.AutoSize = true;
        patternsPanel.ColumnCount = 3;
        patternsPanel.ColumnStyles.Add(new ColumnStyle());
        patternsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        patternsPanel.ColumnStyles.Add(new ColumnStyle());
        patternsPanel.Controls.Add(patternsLabel, 0, 0);
        patternsPanel.Controls.Add(patternsCombo, 1, 0);
        patternsPanel.Controls.Add(findButton, 2, 0);
        patternsPanel.Dock = DockStyle.Fill;
        patternsPanel.Margin = new Padding(0);
        patternsPanel.Name = "patternsPanel";
        patternsPanel.RowCount = 1;
        patternsPanel.RowStyles.Add(new RowStyle());
        patternsPanel.TabIndex = 1;
        //
        // patternsLabel
        //
        patternsLabel.Anchor = AnchorStyles.Left;
        patternsLabel.AutoSize = true;
        patternsLabel.Name = "patternsLabel";
        patternsLabel.TabIndex = 0;
        patternsLabel.Text = "Names:";
        //
        // patternsCombo
        //
        patternsCombo.Dock = DockStyle.Fill;
        patternsCombo.Name = "patternsCombo";
        patternsCombo.TabIndex = 1;
        //
        // findButton
        //
        findButton.AutoSize = true;
        findButton.Name = "findButton";
        findButton.TabIndex = 2;
        findButton.Text = "Find";
        findButton.UseVisualStyleBackColor = true;
        findButton.Click += OnFind;
        //
        // optionsPanel
        //
        optionsPanel.AutoSize = true;
        optionsPanel.Controls.Add(filesCheck);
        optionsPanel.Controls.Add(foldersCheck);
        optionsPanel.Controls.Add(subfoldersCheck);
        optionsPanel.Dock = DockStyle.Fill;
        optionsPanel.Name = "optionsPanel";
        optionsPanel.TabIndex = 2;
        //
        // filesCheck
        //
        filesCheck.AutoSize = true;
        filesCheck.Checked = true;
        filesCheck.CheckState = CheckState.Checked;
        filesCheck.Name = "filesCheck";
        filesCheck.TabIndex = 0;
        filesCheck.Text = "Files";
        filesCheck.UseVisualStyleBackColor = true;
        //
        // foldersCheck
        //
        foldersCheck.AutoSize = true;
        foldersCheck.Name = "foldersCheck";
        foldersCheck.TabIndex = 1;
        foldersCheck.Text = "Folders";
        foldersCheck.UseVisualStyleBackColor = true;
        //
        // subfoldersCheck
        //
        subfoldersCheck.AutoSize = true;
        subfoldersCheck.Checked = true;
        subfoldersCheck.CheckState = CheckState.Checked;
        subfoldersCheck.Margin = new Padding(12, 3, 3, 3);
        subfoldersCheck.Name = "subfoldersCheck";
        subfoldersCheck.TabIndex = 2;
        subfoldersCheck.Text = "In subfolders too";
        subfoldersCheck.UseVisualStyleBackColor = true;
        //
        // hintLabel
        //
        hintLabel.AutoSize = true;
        hintLabel.ForeColor = SystemColors.GrayText;
        hintLabel.Margin = new Padding(3, 3, 3, 6);
        hintLabel.Name = "hintLabel";
        hintLabel.TabIndex = 3;
        hintLabel.Text = "Names with * and ?, separated by ;";
        //
        // foundList
        //
        foundList.CheckBoxes = true;
        foundList.Columns.AddRange(new ColumnHeader[] { pathColumn, sizeColumn, modifiedColumn });
        foundList.Dock = DockStyle.Fill;
        foundList.FullRowSelect = true;
        foundList.Name = "foundList";
        foundList.TabIndex = 4;
        foundList.UseCompatibleStateImageBehavior = false;
        foundList.View = View.Details;
        foundList.ItemChecked += OnItemChecked;
        foundList.Enter += OnListEnter;
        foundList.Leave += OnListLeave;
        //
        // pathColumn
        //
        pathColumn.Text = "Path";
        pathColumn.Width = 420;
        //
        // sizeColumn
        //
        sizeColumn.Text = "Size";
        sizeColumn.TextAlign = HorizontalAlignment.Right;
        sizeColumn.Width = 100;
        //
        // modifiedColumn
        //
        modifiedColumn.Text = "Modified";
        modifiedColumn.Width = 140;
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
        buttonPanel.Controls.Add(recycleButton);
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
        // recycleButton
        //
        recycleButton.AutoSize = true;
        recycleButton.Enabled = false;
        recycleButton.Name = "recycleButton";
        recycleButton.TabIndex = 0;
        recycleButton.Text = "Send to Recycle Bin";
        recycleButton.UseVisualStyleBackColor = true;
        recycleButton.Click += OnRecycle;
        //
        // AdvancedDeleteForm
        //
        AcceptButton = findButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = closeButton;
        ClientSize = new Size(720, 520);
        Controls.Add(layout);
        MinimumSize = new Size(520, 400);
        Name = "AdvancedDeleteForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Advanced delete";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        patternsPanel.ResumeLayout(false);
        patternsPanel.PerformLayout();
        optionsPanel.ResumeLayout(false);
        optionsPanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel layout;
    private Label selectionLabel;
    private TableLayoutPanel patternsPanel;
    private Label patternsLabel;
    private ComboBox patternsCombo;
    private Button findButton;
    private FlowLayoutPanel optionsPanel;
    private CheckBox filesCheck;
    private CheckBox foldersCheck;
    private CheckBox subfoldersCheck;
    private Label hintLabel;
    private ListView foundList;
    private ColumnHeader pathColumn;
    private ColumnHeader sizeColumn;
    private ColumnHeader modifiedColumn;
    private Label statusLabel;
    private FlowLayoutPanel buttonPanel;
    private Button closeButton;
    private Button recycleButton;
}
