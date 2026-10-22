#nullable disable

namespace FBC.ShellExtension.Editor.Tools;

partial class DeleteLockedForm
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
        statusLabel = new Label();
        holderList = new ListView();
        programColumn = new ColumnHeader();
        idColumn = new ColumnHeader();
        kindColumn = new ColumnHeader();
        hintLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        restartButton = new Button();
        deleteButton = new Button();
        layout.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // layout
        //
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(selectionLabel, 0, 0);
        layout.Controls.Add(statusLabel, 0, 1);
        layout.Controls.Add(holderList, 0, 2);
        layout.Controls.Add(hintLabel, 0, 3);
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
        layout.Size = new Size(640, 420);
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
        // statusLabel
        //
        statusLabel.AutoSize = true;
        statusLabel.Margin = new Padding(3, 3, 3, 6);
        statusLabel.MaximumSize = new Size(600, 0);
        statusLabel.Name = "statusLabel";
        statusLabel.TabIndex = 1;
        statusLabel.UseMnemonic = false;
        //
        // holderList
        //
        holderList.Columns.AddRange(new ColumnHeader[] { programColumn, idColumn, kindColumn });
        holderList.Dock = DockStyle.Fill;
        holderList.FullRowSelect = true;
        holderList.Name = "holderList";
        holderList.TabIndex = 2;
        holderList.UseCompatibleStateImageBehavior = false;
        holderList.View = View.Details;
        //
        // programColumn
        //
        programColumn.Text = "Program";
        programColumn.Width = 280;
        //
        // idColumn
        //
        idColumn.Text = "Process";
        idColumn.TextAlign = HorizontalAlignment.Right;
        idColumn.Width = 80;
        //
        // kindColumn
        //
        kindColumn.Text = "Kind";
        kindColumn.Width = 220;
        //
        // hintLabel
        //
        hintLabel.AutoSize = true;
        hintLabel.ForeColor = SystemColors.GrayText;
        hintLabel.Margin = new Padding(3, 6, 3, 6);
        hintLabel.MaximumSize = new Size(600, 0);
        hintLabel.Name = "hintLabel";
        hintLabel.TabIndex = 3;
        hintLabel.Text = "Close asks a program to close as you would.";
        //
        // buttonPanel
        //
        buttonPanel.AutoSize = true;
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(restartButton);
        buttonPanel.Controls.Add(deleteButton);
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
        cancelButton.TabIndex = 2;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += OnClose;
        //
        // restartButton
        //
        restartButton.AutoSize = true;
        restartButton.Enabled = false;
        restartButton.Name = "restartButton";
        restartButton.TabIndex = 1;
        restartButton.Text = "Delete at next restart";
        restartButton.UseVisualStyleBackColor = true;
        restartButton.Click += OnRestart;
        //
        // deleteButton
        //
        deleteButton.AutoSize = true;
        deleteButton.Enabled = false;
        deleteButton.Name = "deleteButton";
        deleteButton.TabIndex = 0;
        deleteButton.Text = "Close them and delete";
        deleteButton.UseVisualStyleBackColor = true;
        deleteButton.Click += OnDelete;
        //
        // DeleteLockedForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(640, 420);
        Controls.Add(layout);
        MinimumSize = new Size(480, 320);
        Name = "DeleteLockedForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Unlock and delete";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel layout;
    private Label selectionLabel;
    private Label statusLabel;
    private ListView holderList;
    private ColumnHeader programColumn;
    private ColumnHeader idColumn;
    private ColumnHeader kindColumn;
    private Label hintLabel;
    private FlowLayoutPanel buttonPanel;
    private Button cancelButton;
    private Button restartButton;
    private Button deleteButton;
}
