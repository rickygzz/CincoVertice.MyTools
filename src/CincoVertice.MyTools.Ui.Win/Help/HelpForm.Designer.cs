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
        FindLabel = new ToolStripLabel();
        FindTextBox = new ToolStripTextBox();
        FindNextButton = new ToolStripButton();
        Split = new SplitContainer();
        PagesListBox = new ListBox();
        PagePanel = new Panel();
        PageTextBox = new RichTextBox();
        ResizeTimer = new System.Windows.Forms.Timer(components);
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
            BackButton, ForwardButton, HistorySeparator, FindLabel, FindTextBox, FindNextButton,
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
        // Split
        //
        Split.Dock = DockStyle.Fill;
        Split.FixedPanel = FixedPanel.Panel1;
        Split.Location = new Point(0, 25);
        Split.Name = "Split";
        Split.Panel1.BackColor = SystemColors.Window;
        Split.Panel1.Controls.Add(PagesListBox);
        Split.Panel1.Padding = new Padding(8, 8, 0, 0);
        Split.Panel2.Controls.Add(PagePanel);
        Split.Size = new Size(1000, 675);
        Split.SplitterDistance = 240;
        Split.TabIndex = 1;
        //
        // PagesListBox
        //
        PagesListBox.BorderStyle = BorderStyle.None;
        PagesListBox.DisplayMember = "Title";
        PagesListBox.Dock = DockStyle.Fill;
        PagesListBox.IntegralHeight = false;
        PagesListBox.ItemHeight = 15;
        PagesListBox.Location = new Point(8, 8);
        PagesListBox.Name = "PagesListBox";
        PagesListBox.Size = new Size(240, 675);
        PagesListBox.TabIndex = 0;
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
        PagePanel.Size = new Size(756, 675);
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
        PageTextBox.Size = new Size(740, 667);
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
        KeyDown += HelpForm_KeyDown;
        Tools.ResumeLayout(false);
        Tools.PerformLayout();
        Split.Panel1.ResumeLayout(false);
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
    private ToolStripLabel FindLabel;
    private ToolStripTextBox FindTextBox;
    private ToolStripButton FindNextButton;
    private SplitContainer Split;
    private ListBox PagesListBox;
    private Panel PagePanel;
    private RichTextBox PageTextBox;
    private System.Windows.Forms.Timer ResizeTimer;
}
