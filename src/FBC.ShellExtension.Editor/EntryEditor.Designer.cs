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
        matchHeader = new Label();
        matchEditor = new MatchRuleEditor();
        commandHeader = new Label();
        commandEditor = new CommandEditor();
        layout.SuspendLayout();
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
        layout.Controls.Add(matchHeader, 0, 1);
        layout.Controls.Add(matchEditor, 0, 2);
        layout.Controls.Add(commandHeader, 0, 3);
        layout.Controls.Add(commandEditor, 0, 4);
        layout.Dock = DockStyle.Top;
        layout.Location = new Point(0, 0);
        layout.Name = "layout";
        layout.Padding = new Padding(8);
        layout.RowCount = 5;
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
        // matchHeader
        //
        matchHeader.AutoSize = true;
        matchHeader.Margin = new Padding(3, 12, 3, 3);
        matchHeader.Name = "matchHeader";
        matchHeader.TabIndex = 2;
        matchHeader.Text = "Show when";
        //
        // matchEditor
        //
        matchEditor.AutoSize = true;
        matchEditor.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        matchEditor.Dock = DockStyle.Fill;
        matchEditor.Name = "matchEditor";
        matchEditor.TabIndex = 3;
        //
        // commandHeader
        //
        commandHeader.AutoSize = true;
        commandHeader.Margin = new Padding(3, 12, 3, 3);
        commandHeader.Name = "commandHeader";
        commandHeader.TabIndex = 4;
        commandHeader.Text = "Command";
        //
        // commandEditor
        //
        commandEditor.AutoSize = true;
        commandEditor.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        commandEditor.Dock = DockStyle.Fill;
        commandEditor.Name = "commandEditor";
        commandEditor.TabIndex = 5;
        //
        // EntryEditor
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(layout);
        Name = "EntryEditor";
        Size = new Size(500, 700);
        layout.ResumeLayout(false);
        layout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel layout;
    private Label labelLabel;
    private TextBox labelBox;
    private Label matchHeader;
    private MatchRuleEditor matchEditor;
    private Label commandHeader;
    private CommandEditor commandEditor;
}
