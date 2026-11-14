namespace CincoVertice.MyTools.Ui.Win.Help;

partial class HelpForm
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
            _boldFont.Dispose();
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
        Tools = new ToolStrip();
        BackButton = new ToolStripButton();
        ForwardButton = new ToolStripButton();
        HistorySeparator = new ToolStripSeparator();
        OutlineButton = new ToolStripDropDownButton();
        OutlineSeparator = new ToolStripSeparator();
        FindLabel = new ToolStripLabel();
        FindTextBox = new ToolStripTextBox();
        FindNextButton = new ToolStripButton();
        ThemeButton = new ToolStripButton();
        Split = new SplitContainer();
        PagesListBox = new ListBox();
        SearchTextBox = new TextBox();
        PagePanel = new Panel();
        PageTextBox = new RichTextBox();
        ResizeTimer = new System.Windows.Forms.Timer(components);
        SearchTimer = new System.Windows.Forms.Timer(components);
        Tools.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)Split).BeginInit();
        Split.Panel1.SuspendLayout();
        Split.Panel2.SuspendLayout();
        Split.SuspendLayout();
        PagePanel.SuspendLayout();
        SuspendLayout();
        //
        // Tools
        //
        Tools.GripStyle = ToolStripGripStyle.Hidden;
        Tools.Items.AddRange(new ToolStripItem[]
        {
            BackButton, ForwardButton, HistorySeparator, OutlineButton, OutlineSeparator, FindLabel, FindTextBox,
            FindNextButton, ThemeButton,
        });
        Tools.Location = new Point(0, 0);
        Tools.Name = "Tools";
        Tools.Size = new Size(1000, 25);
        Tools.TabIndex = 0;
        //
        // BackButton
        //
        BackButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        BackButton.Enabled = false;
        BackButton.Name = "BackButton";
        BackButton.Text = "◀ &Back";
        BackButton.ToolTipText = "Back (Alt+Left)";
        BackButton.Click += BackButton_Click;
        //
        // ForwardButton
        //
        ForwardButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        ForwardButton.Enabled = false;
        ForwardButton.Name = "ForwardButton";
        ForwardButton.Text = "&Forward ▶";
        ForwardButton.ToolTipText = "Forward (Alt+Right)";
        ForwardButton.Click += ForwardButton_Click;
        //
        // HistorySeparator
        //
        HistorySeparator.Name = "HistorySeparator";
        //
        // OutlineButton
        //
        OutlineButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        OutlineButton.Enabled = false;
        OutlineButton.Name = "OutlineButton";
        OutlineButton.Text = "&Outline";
        OutlineButton.ToolTipText = "Go to a heading of this page";
        //
        // OutlineSeparator
        //
        OutlineSeparator.Name = "OutlineSeparator";
        //
        // FindLabel
        //
        FindLabel.Name = "FindLabel";
        FindLabel.Text = "Find:";
        //
        // FindTextBox
        //
        FindTextBox.Name = "FindTextBox";
        FindTextBox.Size = new Size(200, 25);
        FindTextBox.ToolTipText = "Find in this page (Ctrl+F); Enter or F3 finds the next";
        FindTextBox.KeyDown += FindTextBox_KeyDown;
        FindTextBox.TextChanged += FindTextBox_TextChanged;
        //
        // FindNextButton
        //
        FindNextButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        FindNextButton.Name = "FindNextButton";
        FindNextButton.Text = "&Next";
        FindNextButton.ToolTipText = "Find the next (F3)";
        FindNextButton.Click += FindNextButton_Click;
        //
        // ThemeButton
        //
        ThemeButton.Alignment = ToolStripItemAlignment.Right;
        ThemeButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        ThemeButton.Name = "ThemeButton";
        ThemeButton.Text = "&Dark";
        ThemeButton.Click += ThemeButton_Click;
        //
        // Split
        //
        Split.Dock = DockStyle.Fill;
        Split.FixedPanel = FixedPanel.Panel1;
        Split.Location = new Point(0, 25);
        Split.Name = "Split";
        Split.Panel1.BackColor = SystemColors.Window;
        Split.Panel1.Controls.Add(PagesListBox);
        Split.Panel1.Controls.Add(SearchTextBox);
        Split.Panel1.Padding = new Padding(8, 8, 0, 0);
        Split.Panel2.Controls.Add(PagePanel);
        Split.Size = new Size(1000, 675);
        Split.SplitterDistance = 260;
        Split.TabIndex = 1;
        //
        // SearchTextBox
        //
        SearchTextBox.Dock = DockStyle.Top;
        SearchTextBox.Location = new Point(8, 8);
        SearchTextBox.Name = "SearchTextBox";
        SearchTextBox.PlaceholderText = "Search the guide (Ctrl+E)";
        SearchTextBox.Size = new Size(252, 23);
        SearchTextBox.TabIndex = 0;
        SearchTextBox.KeyDown += SearchTextBox_KeyDown;
        SearchTextBox.TextChanged += SearchTextBox_TextChanged;
        //
        // PagesListBox
        //
        PagesListBox.BorderStyle = BorderStyle.None;
        PagesListBox.Dock = DockStyle.Fill;
        PagesListBox.DrawMode = DrawMode.OwnerDrawVariable;
        PagesListBox.IntegralHeight = false;
        PagesListBox.Location = new Point(8, 31);
        PagesListBox.Name = "PagesListBox";
        PagesListBox.Size = new Size(252, 644);
        PagesListBox.TabIndex = 1;
        PagesListBox.DrawItem += PagesListBox_DrawItem;
        PagesListBox.MeasureItem += PagesListBox_MeasureItem;
        PagesListBox.SelectedIndexChanged += PagesListBox_SelectedIndexChanged;
        //
        // PagePanel
        //
        PagePanel.BackColor = SystemColors.Window;
        PagePanel.Controls.Add(PageTextBox);
        PagePanel.Dock = DockStyle.Fill;
        PagePanel.Location = new Point(0, 0);
        PagePanel.Name = "PagePanel";
        PagePanel.Padding = new Padding(16, 8, 0, 0);
        PagePanel.Size = new Size(736, 675);
        PagePanel.TabIndex = 0;
        //
        // PageTextBox
        //
        PageTextBox.BackColor = SystemColors.Window;
        PageTextBox.BorderStyle = BorderStyle.None;
        PageTextBox.DetectUrls = false;
        PageTextBox.Dock = DockStyle.Fill;
        PageTextBox.HideSelection = false;
        PageTextBox.Location = new Point(16, 8);
        PageTextBox.Name = "PageTextBox";
        PageTextBox.ReadOnly = true;
        PageTextBox.ScrollBars = RichTextBoxScrollBars.ForcedVertical;
        PageTextBox.Size = new Size(720, 667);
        PageTextBox.TabIndex = 0;
        PageTextBox.Text = "";
        PageTextBox.MouseMove += PageTextBox_MouseMove;
        PageTextBox.MouseUp += PageTextBox_MouseUp;
        PageTextBox.Resize += PageTextBox_Resize;
        //
        // ResizeTimer
        //
        ResizeTimer.Interval = 250;
        ResizeTimer.Tick += ResizeTimer_Tick;
        //
        // SearchTimer
        //
        SearchTimer.Interval = 300;
        SearchTimer.Tick += SearchTimer_Tick;
        //
        // HelpForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 700);
        Controls.Add(Split);
        Controls.Add(Tools);
        KeyPreview = true;
        MinimumSize = new Size(600, 360);
        Name = "HelpForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "My Tools help";
        FormClosing += HelpForm_FormClosing;
        KeyDown += HelpForm_KeyDown;
        Tools.ResumeLayout(false);
        Tools.PerformLayout();
        Split.Panel1.ResumeLayout(false);
        Split.Panel1.PerformLayout();
        Split.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)Split).EndInit();
        Split.ResumeLayout(false);
        PagePanel.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ToolStrip Tools;
    private ToolStripButton BackButton;
    private ToolStripButton ForwardButton;
    private ToolStripSeparator HistorySeparator;
    private ToolStripDropDownButton OutlineButton;
    private ToolStripSeparator OutlineSeparator;
    private ToolStripLabel FindLabel;
    private ToolStripTextBox FindTextBox;
    private ToolStripButton FindNextButton;
    private ToolStripButton ThemeButton;
    private SplitContainer Split;
    private ListBox PagesListBox;
    private TextBox SearchTextBox;
    private Panel PagePanel;
    private RichTextBox PageTextBox;
    private System.Windows.Forms.Timer ResizeTimer;
    private System.Windows.Forms.Timer SearchTimer;
}
