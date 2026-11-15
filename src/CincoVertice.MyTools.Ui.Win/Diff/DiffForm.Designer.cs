namespace CincoVertice.MyTools.Ui.Win.Diff;

partial class DiffForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        Toolbar = new ToolStrip();
        LeftFileButton = new ToolStripButton();
        RightFileButton = new ToolStripButton();
        FileSeparator = new ToolStripSeparator();
        PasteLeftButton = new ToolStripButton();
        PasteRightButton = new ToolStripButton();
        PasteSeparator = new ToolStripSeparator();
        SwapButton = new ToolStripButton();
        ReloadButton = new ToolStripButton();
        ReloadSeparator = new ToolStripSeparator();
        PreviousButton = new ToolStripButton();
        NextButton = new ToolStripButton();
        NavigationSeparator = new ToolStripSeparator();
        IgnoreWhitespaceButton = new ToolStripButton();
        IgnoreCaseButton = new ToolStripButton();
        HeaderTable = new TableLayoutPanel();
        LeftTitleLabel = new Label();
        RightTitleLabel = new Label();
        DiffText = new DiffTextControl();
        StatusBar = new StatusStrip();
        SummaryLabel = new ToolStripStatusLabel();
        PositionLabel = new ToolStripStatusLabel();
        OpenDialog = new OpenFileDialog();
        Toolbar.SuspendLayout();
        HeaderTable.SuspendLayout();
        StatusBar.SuspendLayout();
        SuspendLayout();
        //
        // Toolbar
        //
        Toolbar.GripStyle = ToolStripGripStyle.Hidden;
        Toolbar.Items.AddRange(new ToolStripItem[] { LeftFileButton, RightFileButton, FileSeparator, PasteLeftButton, PasteRightButton, PasteSeparator, SwapButton, ReloadButton, ReloadSeparator, PreviousButton, NextButton, NavigationSeparator, IgnoreWhitespaceButton, IgnoreCaseButton });
        Toolbar.Location = new Point(0, 0);
        Toolbar.Name = "Toolbar";
        Toolbar.Size = new Size(1100, 25);
        Toolbar.TabIndex = 0;
        //
        // LeftFileButton
        //
        LeftFileButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        LeftFileButton.Name = "LeftFileButton";
        LeftFileButton.Text = "Left file...";
        LeftFileButton.ToolTipText = "Choose the file on the left, or two files to compare (Ctrl+O)";
        LeftFileButton.Click += LeftFileButton_Click;
        //
        // RightFileButton
        //
        RightFileButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        RightFileButton.Name = "RightFileButton";
        RightFileButton.Text = "Right file...";
        RightFileButton.ToolTipText = "Choose the file on the right";
        RightFileButton.Click += RightFileButton_Click;
        //
        // FileSeparator
        //
        FileSeparator.Name = "FileSeparator";
        //
        // PasteLeftButton
        //
        PasteLeftButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        PasteLeftButton.Name = "PasteLeftButton";
        PasteLeftButton.Text = "Paste left";
        PasteLeftButton.ToolTipText = "Compare the clipboard's text, on the left";
        PasteLeftButton.Click += PasteLeftButton_Click;
        //
        // PasteRightButton
        //
        PasteRightButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        PasteRightButton.Name = "PasteRightButton";
        PasteRightButton.Text = "Paste right";
        PasteRightButton.ToolTipText = "Compare the clipboard's text, on the right";
        PasteRightButton.Click += PasteRightButton_Click;
        //
        // PasteSeparator
        //
        PasteSeparator.Name = "PasteSeparator";
        //
        // SwapButton
        //
        SwapButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        SwapButton.Name = "SwapButton";
        SwapButton.Text = "Swap";
        SwapButton.ToolTipText = "Put the left side on the right, and the right on the left";
        SwapButton.Click += SwapButton_Click;
        //
        // ReloadButton
        //
        ReloadButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        ReloadButton.Name = "ReloadButton";
        ReloadButton.Text = "Reload";
        ReloadButton.ToolTipText = "Read the files again (F5)";
        ReloadButton.Click += ReloadButton_Click;
        //
        // ReloadSeparator
        //
        ReloadSeparator.Name = "ReloadSeparator";
        //
        // PreviousButton
        //
        PreviousButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        PreviousButton.Enabled = false;
        PreviousButton.Name = "PreviousButton";
        PreviousButton.Text = "Previous change";
        PreviousButton.ToolTipText = "Go to the change before (Shift+F7)";
        PreviousButton.Click += PreviousButton_Click;
        //
        // NextButton
        //
        NextButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        NextButton.Enabled = false;
        NextButton.Name = "NextButton";
        NextButton.Text = "Next change";
        NextButton.ToolTipText = "Go to the next change (F7)";
        NextButton.Click += NextButton_Click;
        //
        // NavigationSeparator
        //
        NavigationSeparator.Name = "NavigationSeparator";
        //
        // IgnoreWhitespaceButton
        //
        IgnoreWhitespaceButton.CheckOnClick = true;
        IgnoreWhitespaceButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        IgnoreWhitespaceButton.Name = "IgnoreWhitespaceButton";
        IgnoreWhitespaceButton.Text = "Ignore whitespace";
        IgnoreWhitespaceButton.ToolTipText = "Lines differing only in spaces and tabs count as the same";
        IgnoreWhitespaceButton.CheckedChanged += IgnoreButton_CheckedChanged;
        //
        // IgnoreCaseButton
        //
        IgnoreCaseButton.CheckOnClick = true;
        IgnoreCaseButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        IgnoreCaseButton.Name = "IgnoreCaseButton";
        IgnoreCaseButton.Text = "Ignore case";
        IgnoreCaseButton.ToolTipText = "Lines differing only in upper and lower case count as the same";
        IgnoreCaseButton.CheckedChanged += IgnoreButton_CheckedChanged;
        //
        // HeaderTable
        //
        HeaderTable.ColumnCount = 2;
        HeaderTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        HeaderTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        HeaderTable.Controls.Add(LeftTitleLabel, 0, 0);
        HeaderTable.Controls.Add(RightTitleLabel, 1, 0);
        HeaderTable.Dock = DockStyle.Top;
        HeaderTable.Location = new Point(0, 25);
        HeaderTable.Name = "HeaderTable";
        HeaderTable.RowCount = 1;
        HeaderTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        HeaderTable.Size = new Size(1100, 24);
        HeaderTable.TabIndex = 1;
        //
        // LeftTitleLabel
        //
        LeftTitleLabel.AutoEllipsis = true;
        LeftTitleLabel.Dock = DockStyle.Fill;
        LeftTitleLabel.Name = "LeftTitleLabel";
        LeftTitleLabel.TabIndex = 0;
        LeftTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // RightTitleLabel
        //
        RightTitleLabel.AutoEllipsis = true;
        RightTitleLabel.Dock = DockStyle.Fill;
        RightTitleLabel.Name = "RightTitleLabel";
        RightTitleLabel.TabIndex = 1;
        RightTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // DiffText
        //
        DiffText.AllowDrop = true;
        DiffText.Dock = DockStyle.Fill;
        DiffText.Location = new Point(0, 49);
        DiffText.Name = "DiffText";
        DiffText.Size = new Size(1100, 629);
        DiffText.TabIndex = 2;
        DiffText.DragDrop += DiffText_DragDrop;
        DiffText.DragEnter += DiffText_DragEnter;
        //
        // StatusBar
        //
        StatusBar.Items.AddRange(new ToolStripItem[] { SummaryLabel, PositionLabel });
        StatusBar.Location = new Point(0, 678);
        StatusBar.Name = "StatusBar";
        StatusBar.Size = new Size(1100, 22);
        StatusBar.TabIndex = 3;
        //
        // SummaryLabel
        //
        SummaryLabel.Name = "SummaryLabel";
        SummaryLabel.Spring = true;
        SummaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // PositionLabel
        //
        PositionLabel.Name = "PositionLabel";
        //
        // OpenDialog
        //
        OpenDialog.Filter = "All files (*.*)|*.*";
        OpenDialog.Multiselect = true;
        //
        // DiffForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 700);
        Controls.Add(DiffText);
        Controls.Add(HeaderTable);
        Controls.Add(Toolbar);
        Controls.Add(StatusBar);
        MinimumSize = new Size(600, 300);
        Name = "DiffForm";
        StartPosition = FormStartPosition.Manual;
        Text = "Compare files";
        Activated += DiffForm_Activated;
        Toolbar.ResumeLayout(false);
        Toolbar.PerformLayout();
        HeaderTable.ResumeLayout(false);
        StatusBar.ResumeLayout(false);
        StatusBar.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ToolStrip Toolbar;
    private ToolStripButton LeftFileButton;
    private ToolStripButton RightFileButton;
    private ToolStripSeparator FileSeparator;
    private ToolStripButton PasteLeftButton;
    private ToolStripButton PasteRightButton;
    private ToolStripSeparator PasteSeparator;
    private ToolStripButton SwapButton;
    private ToolStripButton ReloadButton;
    private ToolStripSeparator ReloadSeparator;
    private ToolStripButton PreviousButton;
    private ToolStripButton NextButton;
    private ToolStripSeparator NavigationSeparator;
    private ToolStripButton IgnoreWhitespaceButton;
    private ToolStripButton IgnoreCaseButton;
    private TableLayoutPanel HeaderTable;
    private Label LeftTitleLabel;
    private Label RightTitleLabel;
    private DiffTextControl DiffText;
    private StatusStrip StatusBar;
    private ToolStripStatusLabel SummaryLabel;
    private ToolStripStatusLabel PositionLabel;
    private OpenFileDialog OpenDialog;
}
