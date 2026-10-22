#nullable disable

namespace FBC.ShellExtension.Editor.Tools;

partial class OrganizeForm
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
        byPanel = new FlowLayoutPanel();
        byDateRadio = new RadioButton();
        byExtensionRadio = new RadioButton();
        datePanel = new FlowLayoutPanel();
        dateLabel = new Label();
        dateCombo = new ComboBox();
        foldersLabel = new Label();
        foldersCombo = new ComboBox();
        planList = new ListView();
        fileColumn = new ColumnHeader();
        folderColumn = new ColumnHeader();
        statusLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        moveButton = new Button();
        layout.SuspendLayout();
        byPanel.SuspendLayout();
        datePanel.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // layout
        //
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(selectionLabel, 0, 0);
        layout.Controls.Add(byPanel, 0, 1);
        layout.Controls.Add(datePanel, 0, 2);
        layout.Controls.Add(planList, 0, 3);
        layout.Controls.Add(statusLabel, 0, 4);
        layout.Controls.Add(buttonPanel, 0, 5);
        layout.Dock = DockStyle.Fill;
        layout.Location = new Point(0, 0);
        layout.Name = "layout";
        layout.Padding = new Padding(8);
        layout.RowCount = 6;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.Size = new Size(640, 480);
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
        // byPanel
        //
        byPanel.AutoSize = true;
        byPanel.Controls.Add(byDateRadio);
        byPanel.Controls.Add(byExtensionRadio);
        byPanel.Dock = DockStyle.Fill;
        byPanel.Name = "byPanel";
        byPanel.TabIndex = 1;
        byPanel.WrapContents = false;
        //
        // byDateRadio
        //
        byDateRadio.AutoSize = true;
        byDateRadio.Name = "byDateRadio";
        byDateRadio.TabIndex = 0;
        byDateRadio.TabStop = true;
        byDateRadio.Text = "By date";
        byDateRadio.UseVisualStyleBackColor = true;
        byDateRadio.CheckedChanged += OnOptionsChanged;
        //
        // byExtensionRadio
        //
        byExtensionRadio.AutoSize = true;
        byExtensionRadio.Name = "byExtensionRadio";
        byExtensionRadio.TabIndex = 1;
        byExtensionRadio.Text = "By extension";
        byExtensionRadio.UseVisualStyleBackColor = true;
        byExtensionRadio.CheckedChanged += OnOptionsChanged;
        //
        // datePanel
        //
        datePanel.AutoSize = true;
        datePanel.Controls.Add(dateLabel);
        datePanel.Controls.Add(dateCombo);
        datePanel.Controls.Add(foldersLabel);
        datePanel.Controls.Add(foldersCombo);
        datePanel.Dock = DockStyle.Fill;
        datePanel.Margin = new Padding(20, 0, 3, 6);
        datePanel.Name = "datePanel";
        datePanel.TabIndex = 2;
        //
        // dateLabel
        //
        dateLabel.Anchor = AnchorStyles.Left;
        dateLabel.AutoSize = true;
        dateLabel.Name = "dateLabel";
        dateLabel.TabIndex = 0;
        dateLabel.Text = "Date:";
        //
        // dateCombo
        //
        dateCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        dateCombo.Name = "dateCombo";
        dateCombo.Size = new Size(260, 23);
        dateCombo.TabIndex = 1;
        dateCombo.SelectedIndexChanged += OnOptionsChanged;
        //
        // foldersLabel
        //
        foldersLabel.Anchor = AnchorStyles.Left;
        foldersLabel.AutoSize = true;
        foldersLabel.Margin = new Padding(12, 0, 3, 0);
        foldersLabel.Name = "foldersLabel";
        foldersLabel.TabIndex = 2;
        foldersLabel.Text = "Folders:";
        //
        // foldersCombo
        //
        foldersCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        foldersCombo.Name = "foldersCombo";
        foldersCombo.Size = new Size(200, 23);
        foldersCombo.TabIndex = 3;
        foldersCombo.SelectedIndexChanged += OnOptionsChanged;
        //
        // planList
        //
        planList.Columns.AddRange(new ColumnHeader[] { fileColumn, folderColumn });
        planList.Dock = DockStyle.Fill;
        planList.FullRowSelect = true;
        planList.Name = "planList";
        planList.TabIndex = 3;
        planList.UseCompatibleStateImageBehavior = false;
        planList.View = View.Details;
        //
        // fileColumn
        //
        fileColumn.Text = "File";
        fileColumn.Width = 380;
        //
        // folderColumn
        //
        folderColumn.Text = "Goes into";
        folderColumn.Width = 200;
        //
        // statusLabel
        //
        statusLabel.AutoSize = true;
        statusLabel.Margin = new Padding(3, 6, 3, 6);
        statusLabel.Name = "statusLabel";
        statusLabel.TabIndex = 4;
        statusLabel.UseMnemonic = false;
        //
        // buttonPanel
        //
        buttonPanel.AutoSize = true;
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(moveButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Name = "buttonPanel";
        buttonPanel.TabIndex = 5;
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
        // moveButton
        //
        moveButton.AutoSize = true;
        moveButton.Enabled = false;
        moveButton.Name = "moveButton";
        moveButton.TabIndex = 0;
        moveButton.Text = "Move";
        moveButton.UseVisualStyleBackColor = true;
        moveButton.Click += OnMove;
        //
        // OrganizeForm
        //
        AcceptButton = moveButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(640, 480);
        Controls.Add(layout);
        MinimumSize = new Size(480, 360);
        Name = "OrganizeForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Organize into folders";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        byPanel.ResumeLayout(false);
        byPanel.PerformLayout();
        datePanel.ResumeLayout(false);
        datePanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel layout;
    private Label selectionLabel;
    private FlowLayoutPanel byPanel;
    private RadioButton byDateRadio;
    private RadioButton byExtensionRadio;
    private FlowLayoutPanel datePanel;
    private Label dateLabel;
    private ComboBox dateCombo;
    private Label foldersLabel;
    private ComboBox foldersCombo;
    private ListView planList;
    private ColumnHeader fileColumn;
    private ColumnHeader folderColumn;
    private Label statusLabel;
    private FlowLayoutPanel buttonPanel;
    private Button cancelButton;
    private Button moveButton;
}
