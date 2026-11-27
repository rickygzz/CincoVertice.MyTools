namespace CincoVertice.MyTools.Ui.Win.RegexTester;

partial class RegexTesterForm
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
        if (disposing && (components != null))
        {
            components.Dispose();
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
        PatternPanel = new TableLayoutPanel();
        PatternLabel = new Label();
        PatternBox = new TextBox();
        AddButton = new Button();
        ExamplesButton = new Button();
        ReferenceButton = new CheckBox();
        ReferenceList = new ListView();
        ReferencePatternColumn = new ColumnHeader();
        ReferenceDescriptionColumn = new ColumnHeader();
        ExampleMenu = new ContextMenuStrip(components);
        PieceMenu = new ContextMenuStrip(components);
        OptionsPanel = new FlowLayoutPanel();
        IgnoreCaseCheckBox = new CheckBox();
        MultilineCheckBox = new CheckBox();
        DotAllCheckBox = new CheckBox();
        MainSplit = new SplitContainer();
        SampleBox = new CincoVertice.Common.Ui.Win.Controls.Rtf.CodeTextControl();
        TextLabel = new Label();
        MatchesList = new ListView();
        NameColumn = new ColumnHeader();
        ValueColumn = new ColumnHeader();
        PlaceColumn = new ColumnHeader();
        MatchesLabel = new Label();
        ReplacePanel = new TableLayoutPanel();
        ReplaceLabel = new Label();
        ReplacementBox = new TextBox();
        ResultLabel = new Label();
        ResultBox = new TextBox();
        StatusBar = new StatusStrip();
        StatusLabel = new ToolStripStatusLabel();
        InputTimer = new System.Windows.Forms.Timer(components);
        PatternPanel.SuspendLayout();
        OptionsPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)MainSplit).BeginInit();
        MainSplit.Panel1.SuspendLayout();
        MainSplit.Panel2.SuspendLayout();
        MainSplit.SuspendLayout();
        ReplacePanel.SuspendLayout();
        StatusBar.SuspendLayout();
        SuspendLayout();
        //
        // PatternPanel
        //
        PatternPanel.ColumnCount = 5;
        PatternPanel.ColumnStyles.Add(new ColumnStyle());
        PatternPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        PatternPanel.ColumnStyles.Add(new ColumnStyle());
        PatternPanel.ColumnStyles.Add(new ColumnStyle());
        PatternPanel.ColumnStyles.Add(new ColumnStyle());
        PatternPanel.Controls.Add(PatternLabel, 0, 0);
        PatternPanel.Controls.Add(PatternBox, 1, 0);
        PatternPanel.Controls.Add(AddButton, 2, 0);
        PatternPanel.Controls.Add(ExamplesButton, 3, 0);
        PatternPanel.Controls.Add(ReferenceButton, 4, 0);
        PatternPanel.Controls.Add(OptionsPanel, 1, 1);
        PatternPanel.SetColumnSpan(OptionsPanel, 4);
        PatternPanel.Dock = DockStyle.Top;
        PatternPanel.Name = "PatternPanel";
        PatternPanel.Padding = new Padding(6, 6, 6, 0);
        PatternPanel.RowCount = 2;
        PatternPanel.RowStyles.Add(new RowStyle());
        PatternPanel.RowStyles.Add(new RowStyle());
        PatternPanel.Size = new Size(900, 66);
        PatternPanel.TabIndex = 0;
        //
        // PatternLabel
        //
        PatternLabel.Anchor = AnchorStyles.Left;
        PatternLabel.AutoSize = true;
        PatternLabel.Name = "PatternLabel";
        PatternLabel.TabIndex = 0;
        PatternLabel.Text = "&Pattern";
        //
        // PatternBox
        //
        PatternBox.Dock = DockStyle.Fill;
        PatternBox.Font = new Font("Consolas", 11F);
        PatternBox.Name = "PatternBox";
        PatternBox.PlaceholderText = "A .NET regular expression, as (\\w+)@(\\w+)\\.com";
        PatternBox.TabIndex = 1;
        PatternBox.TextChanged += Input_Changed;
        //
        // AddButton
        //
        AddButton.Anchor = AnchorStyles.Left;
        AddButton.AutoSize = true;
        AddButton.Name = "AddButton";
        AddButton.TabIndex = 2;
        AddButton.Text = "&Add ▾";
        AddButton.UseVisualStyleBackColor = true;
        AddButton.Click += AddButton_Click;
        //
        // PieceMenu
        //
        PieceMenu.Name = "PieceMenu";
        PieceMenu.ShowItemToolTips = true;
        PieceMenu.Opening += PieceMenu_Opening;
        //
        // ExamplesButton
        //
        ExamplesButton.Anchor = AnchorStyles.Left;
        ExamplesButton.AutoSize = true;
        ExamplesButton.Name = "ExamplesButton";
        ExamplesButton.TabIndex = 3;
        ExamplesButton.Text = "&Examples ▾";
        ExamplesButton.UseVisualStyleBackColor = true;
        ExamplesButton.Click += ExamplesButton_Click;
        //
        // ExampleMenu
        //
        ExampleMenu.Name = "ExampleMenu";
        ExampleMenu.ShowItemToolTips = true;
        //
        // ReferenceButton
        //
        ReferenceButton.Anchor = AnchorStyles.Left;
        ReferenceButton.Appearance = Appearance.Button;
        ReferenceButton.AutoSize = true;
        ReferenceButton.Name = "ReferenceButton";
        ReferenceButton.TabIndex = 4;
        ReferenceButton.Text = "Quick re&ference";
        ReferenceButton.UseVisualStyleBackColor = true;
        ReferenceButton.CheckedChanged += ReferenceButton_CheckedChanged;
        //
        // ReferenceList
        //
        ReferenceList.Columns.AddRange(new ColumnHeader[] { ReferencePatternColumn, ReferenceDescriptionColumn });
        ReferenceList.Dock = DockStyle.Right;
        ReferenceList.FullRowSelect = true;
        ReferenceList.HeaderStyle = ColumnHeaderStyle.None;
        ReferenceList.MultiSelect = false;
        ReferenceList.ShowItemToolTips = true;
        ReferenceList.Name = "ReferenceList";
        ReferenceList.Size = new Size(380, 380);
        ReferenceList.TabIndex = 5;
        ReferenceList.UseCompatibleStateImageBehavior = false;
        ReferenceList.View = View.Details;
        ReferenceList.Visible = false;
        //
        // ReferencePatternColumn
        //
        ReferencePatternColumn.Text = "Pattern";
        ReferencePatternColumn.Width = 110;
        //
        // ReferenceDescriptionColumn
        //
        ReferenceDescriptionColumn.Text = "Description";
        ReferenceDescriptionColumn.Width = 245;
        //
        // OptionsPanel
        //
        OptionsPanel.AutoSize = true;
        OptionsPanel.Controls.Add(IgnoreCaseCheckBox);
        OptionsPanel.Controls.Add(MultilineCheckBox);
        OptionsPanel.Controls.Add(DotAllCheckBox);
        OptionsPanel.Dock = DockStyle.Fill;
        OptionsPanel.Name = "OptionsPanel";
        OptionsPanel.TabIndex = 5;
        //
        // IgnoreCaseCheckBox
        //
        IgnoreCaseCheckBox.AutoSize = true;
        IgnoreCaseCheckBox.Name = "IgnoreCaseCheckBox";
        IgnoreCaseCheckBox.TabIndex = 0;
        IgnoreCaseCheckBox.Text = "&Ignore case";
        IgnoreCaseCheckBox.CheckedChanged += Input_Changed;
        //
        // MultilineCheckBox
        //
        MultilineCheckBox.AutoSize = true;
        MultilineCheckBox.Name = "MultilineCheckBox";
        MultilineCheckBox.TabIndex = 1;
        MultilineCheckBox.Text = "^ and $ at each &line";
        MultilineCheckBox.CheckedChanged += Input_Changed;
        //
        // DotAllCheckBox
        //
        DotAllCheckBox.AutoSize = true;
        DotAllCheckBox.Name = "DotAllCheckBox";
        DotAllCheckBox.TabIndex = 2;
        DotAllCheckBox.Text = ". matches &new lines too";
        DotAllCheckBox.CheckedChanged += Input_Changed;
        //
        // MainSplit
        //
        MainSplit.Dock = DockStyle.Fill;
        MainSplit.Name = "MainSplit";
        MainSplit.Panel1.Controls.Add(SampleBox);
        MainSplit.Panel1.Controls.Add(TextLabel);
        MainSplit.Panel2.Controls.Add(MatchesList);
        MainSplit.Panel2.Controls.Add(MatchesLabel);
        MainSplit.Size = new Size(900, 380);
        MainSplit.SplitterDistance = 520;
        MainSplit.TabIndex = 1;
        //
        // SampleBox
        //
        SampleBox.BorderStyle = BorderStyle.None;
        SampleBox.Dock = DockStyle.Fill;
        SampleBox.HideSelection = false;
        SampleBox.Name = "SampleBox";
        SampleBox.TabIndex = 1;
        SampleBox.Text = "";
        SampleBox.TextChanged += Input_Changed;
        //
        // TextLabel
        //
        TextLabel.AutoSize = true;
        TextLabel.Dock = DockStyle.Top;
        TextLabel.Name = "TextLabel";
        TextLabel.Padding = new Padding(3, 6, 0, 3);
        TextLabel.TabIndex = 0;
        TextLabel.Text = "&Text to try it on: each match colored, every other one in another color, its groups over it";
        //
        // MatchesList
        //
        MatchesList.Columns.AddRange(new ColumnHeader[] { NameColumn, ValueColumn, PlaceColumn });
        MatchesList.Dock = DockStyle.Fill;
        MatchesList.FullRowSelect = true;
        MatchesList.HideSelection = false;
        MatchesList.MultiSelect = false;
        MatchesList.Name = "MatchesList";
        MatchesList.TabIndex = 1;
        MatchesList.UseCompatibleStateImageBehavior = false;
        MatchesList.View = View.Details;
        MatchesList.SelectedIndexChanged += MatchesList_SelectedIndexChanged;
        //
        // NameColumn
        //
        NameColumn.Text = "Match or group";
        NameColumn.Width = 110;
        //
        // ValueColumn
        //
        ValueColumn.Text = "Value";
        ValueColumn.Width = 150;
        //
        // PlaceColumn
        //
        PlaceColumn.Text = "Where";
        PlaceColumn.Width = 100;
        //
        // MatchesLabel
        //
        MatchesLabel.AutoSize = true;
        MatchesLabel.Dock = DockStyle.Top;
        MatchesLabel.Name = "MatchesLabel";
        MatchesLabel.Padding = new Padding(3, 6, 0, 3);
        MatchesLabel.TabIndex = 0;
        MatchesLabel.Text = "&Matches: choose one to select it in the text";
        //
        // ReplacePanel
        //
        ReplacePanel.ColumnCount = 2;
        ReplacePanel.ColumnStyles.Add(new ColumnStyle());
        ReplacePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        ReplacePanel.Controls.Add(ReplaceLabel, 0, 0);
        ReplacePanel.Controls.Add(ReplacementBox, 1, 0);
        ReplacePanel.Controls.Add(ResultLabel, 0, 1);
        ReplacePanel.Controls.Add(ResultBox, 1, 1);
        ReplacePanel.Dock = DockStyle.Bottom;
        ReplacePanel.Name = "ReplacePanel";
        ReplacePanel.Padding = new Padding(6);
        ReplacePanel.RowCount = 2;
        ReplacePanel.RowStyles.Add(new RowStyle());
        ReplacePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        ReplacePanel.Size = new Size(900, 130);
        ReplacePanel.TabIndex = 2;
        //
        // ReplaceLabel
        //
        ReplaceLabel.Anchor = AnchorStyles.Left;
        ReplaceLabel.AutoSize = true;
        ReplaceLabel.Name = "ReplaceLabel";
        ReplaceLabel.TabIndex = 0;
        ReplaceLabel.Text = "&Replace with";
        //
        // ReplacementBox
        //
        ReplacementBox.Dock = DockStyle.Fill;
        ReplacementBox.Font = new Font("Consolas", 11F);
        ReplacementBox.Name = "ReplacementBox";
        ReplacementBox.PlaceholderText = "What each match becomes: $1 or ${name} for a group, $0 for the whole match";
        ReplacementBox.TabIndex = 1;
        ReplacementBox.TextChanged += Input_Changed;
        //
        // ResultLabel
        //
        ResultLabel.AutoSize = true;
        ResultLabel.Name = "ResultLabel";
        ResultLabel.Padding = new Padding(0, 4, 0, 0);
        ResultLabel.TabIndex = 2;
        ResultLabel.Text = "Result";
        //
        // ResultBox
        //
        ResultBox.Dock = DockStyle.Fill;
        ResultBox.Font = new Font("Consolas", 11F);
        ResultBox.Multiline = true;
        ResultBox.Name = "ResultBox";
        ResultBox.ReadOnly = true;
        ResultBox.ScrollBars = ScrollBars.Both;
        ResultBox.TabIndex = 3;
        ResultBox.WordWrap = false;
        //
        // StatusBar
        //
        StatusBar.Items.AddRange(new ToolStripItem[] { StatusLabel });
        StatusBar.Name = "StatusBar";
        StatusBar.TabIndex = 3;
        //
        // StatusLabel
        //
        StatusLabel.Name = "StatusLabel";
        StatusLabel.Spring = true;
        StatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // InputTimer
        //
        InputTimer.Interval = 250;
        InputTimer.Tick += InputTimer_Tick;
        //
        // RegexTesterForm
        //
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(900, 620);
        Controls.Add(MainSplit);
        Controls.Add(ReferenceList);
        Controls.Add(ReplacePanel);
        Controls.Add(PatternPanel);
        Controls.Add(StatusBar);
        MinimumSize = new Size(600, 400);
        Name = "RegexTesterForm";
        Text = "Regex tester";
        PatternPanel.ResumeLayout(false);
        PatternPanel.PerformLayout();
        OptionsPanel.ResumeLayout(false);
        OptionsPanel.PerformLayout();
        MainSplit.Panel1.ResumeLayout(false);
        MainSplit.Panel1.PerformLayout();
        MainSplit.Panel2.ResumeLayout(false);
        MainSplit.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)MainSplit).EndInit();
        MainSplit.ResumeLayout(false);
        ReplacePanel.ResumeLayout(false);
        ReplacePanel.PerformLayout();
        StatusBar.ResumeLayout(false);
        StatusBar.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel PatternPanel;
    private Label PatternLabel;
    private TextBox PatternBox;
    private Button AddButton;
    private ContextMenuStrip PieceMenu;
    private Button ExamplesButton;
    private ContextMenuStrip ExampleMenu;
    private CheckBox ReferenceButton;
    private ListView ReferenceList;
    private ColumnHeader ReferencePatternColumn;
    private ColumnHeader ReferenceDescriptionColumn;
    private FlowLayoutPanel OptionsPanel;
    private CheckBox IgnoreCaseCheckBox;
    private CheckBox MultilineCheckBox;
    private CheckBox DotAllCheckBox;
    private SplitContainer MainSplit;
    private CincoVertice.Common.Ui.Win.Controls.Rtf.CodeTextControl SampleBox;
    private Label TextLabel;
    private ListView MatchesList;
    private ColumnHeader NameColumn;
    private ColumnHeader ValueColumn;
    private ColumnHeader PlaceColumn;
    private Label MatchesLabel;
    private TableLayoutPanel ReplacePanel;
    private Label ReplaceLabel;
    private TextBox ReplacementBox;
    private Label ResultLabel;
    private TextBox ResultBox;
    private StatusStrip StatusBar;
    private ToolStripStatusLabel StatusLabel;
    private System.Windows.Forms.Timer InputTimer;
}
