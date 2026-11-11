#nullable disable

namespace FBC.ShellExtension.Editor.Tools;

partial class MovieChoiceForm
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
        forLabel = new Label();
        searchPanel = new TableLayoutPanel();
        queryLabel = new Label();
        queryBox = new TextBox();
        yearLabel = new Label();
        yearBox = new TextBox();
        searchButton = new Button();
        filmList = new ListView();
        titleColumn = new ColumnHeader();
        yearColumn = new ColumnHeader();
        idColumn = new ColumnHeader();
        statusLabel = new Label();
        buttonPanel = new FlowLayoutPanel();
        skipButton = new Button();
        selectButton = new Button();
        stopButton = new Button();
        layout.SuspendLayout();
        searchPanel.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // layout
        //
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(forLabel, 0, 0);
        layout.Controls.Add(searchPanel, 0, 1);
        layout.Controls.Add(filmList, 0, 2);
        layout.Controls.Add(statusLabel, 0, 3);
        layout.Controls.Add(buttonPanel, 0, 4);
        layout.Dock = DockStyle.Fill;
        layout.Name = "layout";
        layout.Padding = new Padding(8);
        layout.RowCount = 5;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.TabIndex = 0;
        //
        // forLabel
        //
        forLabel.AutoEllipsis = true;
        forLabel.Dock = DockStyle.Fill;
        forLabel.Margin = new Padding(3, 0, 3, 6);
        forLabel.Name = "forLabel";
        forLabel.Size = new Size(100, 18);
        forLabel.TabIndex = 0;
        forLabel.UseMnemonic = false;
        //
        // searchPanel
        //
        searchPanel.AutoSize = true;
        searchPanel.ColumnCount = 5;
        searchPanel.ColumnStyles.Add(new ColumnStyle());
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        searchPanel.ColumnStyles.Add(new ColumnStyle());
        searchPanel.ColumnStyles.Add(new ColumnStyle());
        searchPanel.ColumnStyles.Add(new ColumnStyle());
        searchPanel.Controls.Add(queryLabel, 0, 0);
        searchPanel.Controls.Add(queryBox, 1, 0);
        searchPanel.Controls.Add(yearLabel, 2, 0);
        searchPanel.Controls.Add(yearBox, 3, 0);
        searchPanel.Controls.Add(searchButton, 4, 0);
        searchPanel.Dock = DockStyle.Fill;
        searchPanel.Margin = new Padding(0);
        searchPanel.Name = "searchPanel";
        searchPanel.RowCount = 1;
        searchPanel.RowStyles.Add(new RowStyle());
        searchPanel.TabIndex = 1;
        //
        // queryLabel
        //
        queryLabel.Anchor = AnchorStyles.Left;
        queryLabel.AutoSize = true;
        queryLabel.Name = "queryLabel";
        queryLabel.TabIndex = 0;
        queryLabel.Text = "&Title or IMDb address:";
        //
        // queryBox
        //
        queryBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        queryBox.Name = "queryBox";
        queryBox.TabIndex = 1;
        //
        // yearLabel
        //
        yearLabel.Anchor = AnchorStyles.Left;
        yearLabel.AutoSize = true;
        yearLabel.Name = "yearLabel";
        yearLabel.TabIndex = 2;
        yearLabel.Text = "&Year:";
        //
        // yearBox
        //
        yearBox.Anchor = AnchorStyles.Left;
        yearBox.MaxLength = 4;
        yearBox.Name = "yearBox";
        yearBox.Size = new Size(50, 23);
        yearBox.TabIndex = 3;
        //
        // searchButton
        //
        searchButton.AutoSize = true;
        searchButton.Name = "searchButton";
        searchButton.TabIndex = 4;
        searchButton.Text = "&Search";
        searchButton.UseVisualStyleBackColor = true;
        searchButton.Click += OnSearch;
        //
        // filmList
        //
        filmList.Columns.AddRange(new ColumnHeader[] { titleColumn, yearColumn, idColumn });
        filmList.Dock = DockStyle.Fill;
        filmList.FullRowSelect = true;
        filmList.HideSelection = false;
        filmList.MultiSelect = false;
        filmList.Name = "filmList";
        filmList.TabIndex = 2;
        filmList.UseCompatibleStateImageBehavior = false;
        filmList.View = View.Details;
        filmList.DoubleClick += OnSelect;
        //
        // titleColumn
        //
        titleColumn.Text = "Title";
        titleColumn.Width = 330;
        //
        // yearColumn
        //
        yearColumn.Text = "Year";
        yearColumn.Width = 60;
        //
        // idColumn
        //
        idColumn.Text = "IMDb ID";
        idColumn.Width = 100;
        //
        // statusLabel
        //
        statusLabel.AutoSize = true;
        statusLabel.Margin = new Padding(3, 6, 3, 3);
        statusLabel.MaximumSize = new Size(520, 0);
        statusLabel.Name = "statusLabel";
        statusLabel.TabIndex = 3;
        statusLabel.UseMnemonic = false;
        //
        // buttonPanel
        //
        buttonPanel.AutoSize = true;
        buttonPanel.Controls.Add(skipButton);
        buttonPanel.Controls.Add(selectButton);
        buttonPanel.Controls.Add(stopButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Name = "buttonPanel";
        buttonPanel.TabIndex = 4;
        //
        // skipButton
        //
        skipButton.AutoSize = true;
        skipButton.Name = "skipButton";
        skipButton.TabIndex = 1;
        skipButton.Text = "S&kip";
        skipButton.UseVisualStyleBackColor = true;
        skipButton.Click += OnSkip;
        //
        // selectButton
        //
        selectButton.AutoSize = true;
        selectButton.Name = "selectButton";
        selectButton.TabIndex = 0;
        selectButton.Text = "S&elect";
        selectButton.UseVisualStyleBackColor = true;
        selectButton.Click += OnSelect;
        //
        // stopButton
        //
        stopButton.AutoSize = true;
        stopButton.Name = "stopButton";
        stopButton.TabIndex = 2;
        stopButton.Text = "Stop &asking";
        stopButton.UseVisualStyleBackColor = true;
        stopButton.Click += OnStop;
        //
        // MovieChoiceForm
        //
        AcceptButton = selectButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = skipButton;
        ClientSize = new Size(560, 380);
        Controls.Add(layout);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(420, 300);
        Name = "MovieChoiceForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Choose the film";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        searchPanel.ResumeLayout(false);
        searchPanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel layout;
    private Label forLabel;
    private TableLayoutPanel searchPanel;
    private Label queryLabel;
    private TextBox queryBox;
    private Label yearLabel;
    private TextBox yearBox;
    private Button searchButton;
    private ListView filmList;
    private ColumnHeader titleColumn;
    private ColumnHeader yearColumn;
    private ColumnHeader idColumn;
    private Label statusLabel;
    private FlowLayoutPanel buttonPanel;
    private Button skipButton;
    private Button selectButton;
    private Button stopButton;
}
