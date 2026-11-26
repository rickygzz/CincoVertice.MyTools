namespace CincoVertice.MyTools.Ui.Win.JsonEditor;

partial class JsonEditorForm
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
        NewButton = new ToolStripButton();
        OpenButton = new ToolStripButton();
        SaveButton = new ToolStripButton();
        SaveAsButton = new ToolStripButton();
        FileSeparator = new ToolStripSeparator();
        FormatButton = new ToolStripButton();
        MinifyButton = new ToolStripButton();
        FormatSeparator = new ToolStripSeparator();
        CollapseAllButton = new ToolStripButton();
        ExpandAllButton = new ToolStripButton();
        ThemeButton = new ToolStripButton();
        TreeButton = new ToolStripButton();
        ToolsSeparator = new ToolStripSeparator();
        SortKeysButton = new ToolStripButton();
        RepairButton = new ToolStripButton();
        CompareButton = new ToolStripDropDownButton();
        CompareClipboardItem = new ToolStripMenuItem();
        CompareFileItem = new ToolStripMenuItem();
        ConvertButton = new ToolStripDropDownButton();
        CopyYamlItem = new ToolStripMenuItem();
        CopyCsvItem = new ToolStripMenuItem();
        SaveYamlItem = new ToolStripMenuItem();
        SaveCsvItem = new ToolStripMenuItem();
        ConvertSeparator = new ToolStripSeparator();
        OpenCsvItem = new ToolStripMenuItem();
        PasteCsvItem = new ToolStripMenuItem();
        SchemaButton = new ToolStripDropDownButton();
        ChooseSchemaItem = new ToolStripMenuItem();
        NoSchemaItem = new ToolStripMenuItem();
        TreeSplit = new SplitContainer();
        JsonTree = new TreeView();
        SchemaLabel = new ToolStripStatusLabel();
        ResultMenu = new ContextMenuStrip(components);
        CopyResultJsonItem = new ToolStripMenuItem();
        CopyResultLinesItem = new ToolStripMenuItem();
        SearchLayout = new TableLayoutPanel();
        ModeComboBox = new ComboBox();
        SearchBox = new TextBox();
        PreviousButton = new Button();
        NextButton = new Button();
        SearchMessageLabel = new Label();
        Split = new SplitContainer();
        Editor = new JsonTextControl();
        ResultBox = new TextBox();
        ResultLabel = new Label();
        Status = new StatusStrip();
        PositionLabel = new ToolStripStatusLabel();
        PathLabel = new ToolStripStatusLabel();
        ValidityLabel = new ToolStripStatusLabel();
        SuggestionsList = new ListBox();
        Tips = new ToolTip(components);
        Tools.SuspendLayout();
        SearchLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)Split).BeginInit();
        Split.Panel1.SuspendLayout();
        Split.Panel2.SuspendLayout();
        Split.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)TreeSplit).BeginInit();
        TreeSplit.Panel1.SuspendLayout();
        TreeSplit.Panel2.SuspendLayout();
        TreeSplit.SuspendLayout();
        ResultMenu.SuspendLayout();
        Status.SuspendLayout();
        SuspendLayout();
        //
        // Tools
        //
        Tools.GripStyle = ToolStripGripStyle.Hidden;
        Tools.Items.AddRange(new ToolStripItem[] { NewButton, OpenButton, SaveButton, SaveAsButton, FileSeparator, FormatButton, MinifyButton, FormatSeparator, CollapseAllButton, ExpandAllButton, TreeButton, ToolsSeparator, SortKeysButton, RepairButton, CompareButton, ConvertButton, SchemaButton, ThemeButton });
        Tools.Location = new Point(0, 0);
        Tools.Name = "Tools";
        Tools.Size = new Size(900, 25);
        Tools.TabIndex = 0;
        //
        // NewButton
        //
        NewButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        NewButton.Name = "NewButton";
        NewButton.Text = "New";
        NewButton.ToolTipText = "A new, empty document (Ctrl+N)";
        NewButton.Click += NewButton_Click;
        //
        // OpenButton
        //
        OpenButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        OpenButton.Name = "OpenButton";
        OpenButton.Text = "Open...";
        OpenButton.ToolTipText = "Open a JSON file (Ctrl+O); a file dropped on the window opens too";
        OpenButton.Click += OpenButton_Click;
        //
        // SaveButton
        //
        SaveButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        SaveButton.Name = "SaveButton";
        SaveButton.Text = "Save";
        SaveButton.ToolTipText = "Save (Ctrl+S), with the encoding and line breaks the file had";
        SaveButton.Click += SaveButton_Click;
        //
        // SaveAsButton
        //
        SaveAsButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        SaveAsButton.Name = "SaveAsButton";
        SaveAsButton.Text = "Save as...";
        SaveAsButton.ToolTipText = "Save under another name (Ctrl+Shift+S)";
        SaveAsButton.Click += SaveAsButton_Click;
        //
        // FileSeparator
        //
        FileSeparator.Name = "FileSeparator";
        //
        // FormatButton
        //
        FormatButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        FormatButton.Name = "FormatButton";
        FormatButton.Text = "Format";
        FormatButton.ToolTipText = "Lay the JSON out on lines, indented by two spaces (Shift+Alt+F); Undo takes it back";
        FormatButton.Click += FormatButton_Click;
        //
        // MinifyButton
        //
        MinifyButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        MinifyButton.Name = "MinifyButton";
        MinifyButton.Text = "Minify";
        MinifyButton.ToolTipText = "Put the JSON on one line, without spaces; Undo takes it back";
        MinifyButton.Click += MinifyButton_Click;
        //
        // FormatSeparator
        //
        FormatSeparator.Name = "FormatSeparator";
        //
        // CollapseAllButton
        //
        CollapseAllButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        CollapseAllButton.Name = "CollapseAllButton";
        CollapseAllButton.Text = "Collapse all";
        CollapseAllButton.ToolTipText = "Collapse every section but the outermost (Ctrl+Shift+-); Ctrl+- collapses the one the caret is in";
        CollapseAllButton.Click += CollapseAllButton_Click;
        //
        // ExpandAllButton
        //
        ExpandAllButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        ExpandAllButton.Name = "ExpandAllButton";
        ExpandAllButton.Text = "Expand all";
        ExpandAllButton.ToolTipText = "Open every section (Ctrl+Shift++); Ctrl++ opens the one the caret is on";
        ExpandAllButton.Click += ExpandAllButton_Click;
        //
        // ThemeButton
        //
        ThemeButton.Alignment = ToolStripItemAlignment.Right;
        ThemeButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        ThemeButton.Name = "ThemeButton";
        ThemeButton.Text = "&Dark";
        ThemeButton.Click += ThemeButton_Click;
        //
        // TreeButton
        //
        TreeButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        TreeButton.Name = "TreeButton";
        TreeButton.Text = "&Tree";
        TreeButton.ToolTipText = "Show or hide the values as a tree, beside the text (Ctrl+T)";
        TreeButton.Click += TreeButton_Click;
        //
        // SortKeysButton
        //
        SortKeysButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        SortKeysButton.Name = "SortKeysButton";
        SortKeysButton.Text = "&Sort keys";
        SortKeysButton.ToolTipText = "Sort every object's keys and format; right-click an object to sort only it";
        SortKeysButton.Click += SortKeysButton_Click;
        //
        // RepairButton
        //
        RepairButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        RepairButton.Name = "RepairButton";
        RepairButton.Text = "&Repair";
        RepairButton.ToolTipText = "Fix single quotes, names without quotes, extra or missing commas, comments, and Python or JavaScript values";
        RepairButton.Click += RepairButton_Click;
        //
        // CompareButton
        //
        CompareButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        CompareButton.DropDownItems.AddRange(new ToolStripItem[] { CompareClipboardItem, CompareFileItem });
        CompareButton.Name = "CompareButton";
        CompareButton.Text = "&Compare";
        CompareButton.ToolTipText = "Compare with the clipboard or a file, both formatted the same way";
        //
        // CompareClipboardItem
        //
        CompareClipboardItem.Name = "CompareClipboardItem";
        CompareClipboardItem.Text = "With the &clipboard";
        CompareClipboardItem.Click += CompareClipboardItem_Click;
        //
        // CompareFileItem
        //
        CompareFileItem.Name = "CompareFileItem";
        CompareFileItem.Text = "With a &file...";
        CompareFileItem.Click += CompareFileItem_Click;
        //
        // ConvertButton
        //
        ConvertButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        ConvertButton.DropDownItems.AddRange(new ToolStripItem[] { CopyYamlItem, CopyCsvItem, SaveYamlItem, SaveCsvItem, ConvertSeparator, OpenCsvItem, PasteCsvItem });
        ConvertButton.Name = "ConvertButton";
        ConvertButton.Text = "Con&vert";
        ConvertButton.ToolTipText = "YAML or CSV of the document, or of the JMESPath result when there is one; CSV to JSON";
        //
        // CopyYamlItem
        //
        CopyYamlItem.Name = "CopyYamlItem";
        CopyYamlItem.Text = "Copy as &YAML";
        CopyYamlItem.Click += CopyYamlItem_Click;
        //
        // CopyCsvItem
        //
        CopyCsvItem.Name = "CopyCsvItem";
        CopyCsvItem.Text = "Copy as &CSV";
        CopyCsvItem.Click += CopyCsvItem_Click;
        //
        // SaveYamlItem
        //
        SaveYamlItem.Name = "SaveYamlItem";
        SaveYamlItem.Text = "Save as Y&AML...";
        SaveYamlItem.Click += SaveYamlItem_Click;
        //
        // SaveCsvItem
        //
        SaveCsvItem.Name = "SaveCsvItem";
        SaveCsvItem.Text = "Save as C&SV...";
        SaveCsvItem.Click += SaveCsvItem_Click;
        //
        // OpenCsvItem
        //
        OpenCsvItem.Name = "OpenCsvItem";
        OpenCsvItem.Text = "&Open CSV as JSON...";
        OpenCsvItem.Click += OpenCsvItem_Click;
        //
        // PasteCsvItem
        //
        PasteCsvItem.Name = "PasteCsvItem";
        PasteCsvItem.Text = "&Paste CSV as JSON";
        PasteCsvItem.Click += PasteCsvItem_Click;
        //
        // SchemaButton
        //
        SchemaButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        SchemaButton.DropDownItems.AddRange(new ToolStripItem[] { ChooseSchemaItem, NoSchemaItem });
        SchemaButton.Name = "SchemaButton";
        SchemaButton.Text = "Sc&hema";
        SchemaButton.ToolTipText = "Check the JSON against a JSON Schema file; one named in $schema is used by itself";
        //
        // ChooseSchemaItem
        //
        ChooseSchemaItem.Name = "ChooseSchemaItem";
        ChooseSchemaItem.Text = "&Choose a schema...";
        ChooseSchemaItem.Click += ChooseSchemaItem_Click;
        //
        // NoSchemaItem
        //
        NoSchemaItem.Name = "NoSchemaItem";
        NoSchemaItem.Text = "&No schema";
        NoSchemaItem.Click += NoSchemaItem_Click;
        //
        // SearchLayout
        //
        SearchLayout.AutoSize = true;
        SearchLayout.ColumnCount = 5;
        SearchLayout.ColumnStyles.Add(new ColumnStyle());
        SearchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        SearchLayout.ColumnStyles.Add(new ColumnStyle());
        SearchLayout.ColumnStyles.Add(new ColumnStyle());
        SearchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
        SearchLayout.Controls.Add(ModeComboBox, 0, 0);
        SearchLayout.Controls.Add(SearchBox, 1, 0);
        SearchLayout.Controls.Add(PreviousButton, 2, 0);
        SearchLayout.Controls.Add(NextButton, 3, 0);
        SearchLayout.Controls.Add(SearchMessageLabel, 4, 0);
        SearchLayout.Dock = DockStyle.Top;
        SearchLayout.Location = new Point(0, 25);
        SearchLayout.Name = "SearchLayout";
        SearchLayout.Padding = new Padding(3, 3, 3, 0);
        SearchLayout.RowCount = 1;
        SearchLayout.RowStyles.Add(new RowStyle());
        SearchLayout.Size = new Size(900, 32);
        SearchLayout.TabIndex = 1;
        //
        // ModeComboBox
        //
        ModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        ModeComboBox.Items.AddRange(new object[] { "Text", "JMESPath" });
        ModeComboBox.Name = "ModeComboBox";
        ModeComboBox.Size = new Size(95, 23);
        ModeComboBox.TabIndex = 0;
        Tips.SetToolTip(ModeComboBox, "Find text (Ctrl+F), or the values a JMESPath expression gives (Ctrl+J)");
        ModeComboBox.SelectedIndexChanged += ModeComboBox_SelectedIndexChanged;
        //
        // SearchBox
        //
        SearchBox.Dock = DockStyle.Fill;
        SearchBox.Font = new Font("Consolas", 10F);
        SearchBox.Name = "SearchBox";
        SearchBox.PlaceholderText = "Find (Ctrl+F), or a JMESPath expression such as people[?age > `30`].name (Ctrl+J)";
        SearchBox.TabIndex = 1;
        SearchBox.TextChanged += SearchBox_TextChanged;
        SearchBox.KeyDown += SearchBox_KeyDown;
        SearchBox.Leave += SearchBox_Leave;
        //
        // PreviousButton
        //
        PreviousButton.AutoSize = true;
        PreviousButton.Name = "PreviousButton";
        PreviousButton.Size = new Size(30, 25);
        PreviousButton.TabIndex = 2;
        PreviousButton.Text = "↑";
        Tips.SetToolTip(PreviousButton, "The match before (Shift+F3)");
        PreviousButton.Click += PreviousButton_Click;
        //
        // NextButton
        //
        NextButton.AutoSize = true;
        NextButton.Name = "NextButton";
        NextButton.Size = new Size(30, 25);
        NextButton.TabIndex = 3;
        NextButton.Text = "↓";
        Tips.SetToolTip(NextButton, "The next match (Enter, F3)");
        NextButton.Click += NextButton_Click;
        //
        // SearchMessageLabel
        //
        SearchMessageLabel.AutoEllipsis = true;
        SearchMessageLabel.Dock = DockStyle.Fill;
        SearchMessageLabel.Name = "SearchMessageLabel";
        SearchMessageLabel.TabIndex = 4;
        SearchMessageLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // Split
        //
        Split.Dock = DockStyle.Fill;
        Split.Location = new Point(0, 57);
        Split.Name = "Split";
        Split.Orientation = Orientation.Horizontal;
        Split.Panel2Collapsed = true;
        Split.Size = new Size(900, 521);
        Split.SplitterDistance = 380;
        Split.TabIndex = 2;
        //
        // Split.Panel1
        //
        Split.Panel1.Controls.Add(TreeSplit);
        //
        // TreeSplit
        //
        TreeSplit.Dock = DockStyle.Fill;
        TreeSplit.FixedPanel = FixedPanel.Panel1;
        TreeSplit.Name = "TreeSplit";
        TreeSplit.Panel1Collapsed = true;
        TreeSplit.Size = new Size(900, 521);
        TreeSplit.SplitterDistance = 260;
        TreeSplit.TabIndex = 0;
        //
        // TreeSplit.Panel1
        //
        TreeSplit.Panel1.Controls.Add(JsonTree);
        //
        // TreeSplit.Panel2
        //
        TreeSplit.Panel2.Controls.Add(Editor);
        //
        // JsonTree
        //
        JsonTree.BorderStyle = BorderStyle.None;
        JsonTree.Dock = DockStyle.Fill;
        JsonTree.HideSelection = false;
        JsonTree.Name = "JsonTree";
        JsonTree.TabIndex = 0;
        JsonTree.BeforeExpand += JsonTree_BeforeExpand;
        JsonTree.AfterSelect += JsonTree_AfterSelect;
        //
        // Split.Panel2
        //
        Split.Panel2.Controls.Add(ResultBox);
        Split.Panel2.Controls.Add(ResultLabel);
        //
        // Editor
        //
        Editor.Dock = DockStyle.Fill;
        Editor.Name = "Editor";
        Editor.TabIndex = 0;
        Editor.TextEdited += Editor_TextEdited;
        Editor.CaretMoved += Editor_CaretMoved;
        Editor.PathRequested += Editor_PathRequested;
        Editor.HoverRequested += Editor_HoverRequested;
        Editor.KeysRequested += Editor_KeysRequested;
        Editor.SortKeysHereRequested += Editor_SortKeysHereRequested;
        Editor.GoToPathRequested += Editor_GoToPathRequested;
        //
        // ResultBox
        //
        ResultBox.Dock = DockStyle.Fill;
        ResultBox.Font = new Font("Consolas", 10F);
        ResultBox.Multiline = true;
        ResultBox.Name = "ResultBox";
        ResultBox.ReadOnly = true;
        ResultBox.ScrollBars = ScrollBars.Both;
        ResultBox.TabIndex = 1;
        ResultBox.WordWrap = false;
        ResultBox.ContextMenuStrip = ResultMenu;
        //
        // ResultMenu
        //
        ResultMenu.Items.AddRange(new ToolStripItem[] { CopyResultJsonItem, CopyResultLinesItem });
        ResultMenu.Name = "ResultMenu";
        //
        // CopyResultJsonItem
        //
        CopyResultJsonItem.Name = "CopyResultJsonItem";
        CopyResultJsonItem.Text = "Copy as &JSON";
        CopyResultJsonItem.Click += CopyResultJsonItem_Click;
        //
        // CopyResultLinesItem
        //
        CopyResultLinesItem.Name = "CopyResultLinesItem";
        CopyResultLinesItem.Text = "Copy as &lines (values without quotes)";
        CopyResultLinesItem.Click += CopyResultLinesItem_Click;
        //
        // ResultLabel
        //
        ResultLabel.AutoSize = true;
        ResultLabel.Dock = DockStyle.Top;
        ResultLabel.Name = "ResultLabel";
        ResultLabel.Padding = new Padding(3);
        ResultLabel.TabIndex = 0;
        ResultLabel.Text = "Result of the expression";
        //
        // Status
        //
        Status.Items.AddRange(new ToolStripItem[] { PositionLabel, PathLabel, SchemaLabel, ValidityLabel });
        Status.Name = "Status";
        Status.TabIndex = 3;
        //
        // PositionLabel
        //
        PositionLabel.Name = "PositionLabel";
        PositionLabel.Size = new Size(90, 17);
        //
        // PathLabel
        //
        PathLabel.Name = "PathLabel";
        PathLabel.Spring = true;
        PathLabel.TextAlign = ContentAlignment.MiddleLeft;
        PathLabel.ToolTipText = "The JMESPath of the value at the caret; right-click a value to copy its path";
        //
        // SchemaLabel
        //
        SchemaLabel.IsLink = true;
        SchemaLabel.Name = "SchemaLabel";
        SchemaLabel.ToolTipText = "Go to the next schema problem (F8; Shift+F8 for the one before)";
        SchemaLabel.Visible = false;
        SchemaLabel.Click += SchemaLabel_Click;
        //
        // ValidityLabel
        //
        ValidityLabel.Name = "ValidityLabel";
        //
        // SuggestionsList
        //
        SuggestionsList.DrawMode = DrawMode.OwnerDrawFixed;
        SuggestionsList.IntegralHeight = false;
        SuggestionsList.ItemHeight = 20;
        SuggestionsList.Name = "SuggestionsList";
        SuggestionsList.TabStop = false;
        SuggestionsList.Visible = false;
        SuggestionsList.DrawItem += SuggestionsList_DrawItem;
        SuggestionsList.MouseClick += SuggestionsList_MouseClick;
        //
        // JsonEditorForm
        //
        AllowDrop = true;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(900, 600);
        Controls.Add(SuggestionsList);
        Controls.Add(Split);
        Controls.Add(Status);
        Controls.Add(SearchLayout);
        Controls.Add(Tools);
        KeyPreview = true;
        MinimumSize = new Size(500, 300);
        Name = "JsonEditorForm";
        StartPosition = FormStartPosition.Manual;
        Text = "JSON editor";
        FormClosing += JsonEditorForm_FormClosing;
        DragEnter += JsonEditorForm_DragEnter;
        DragDrop += JsonEditorForm_DragDrop;
        Tools.ResumeLayout(false);
        Tools.PerformLayout();
        SearchLayout.ResumeLayout(false);
        SearchLayout.PerformLayout();
        TreeSplit.Panel1.ResumeLayout(false);
        TreeSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)TreeSplit).EndInit();
        TreeSplit.ResumeLayout(false);
        ResultMenu.ResumeLayout(false);
        Split.Panel1.ResumeLayout(false);
        Split.Panel2.ResumeLayout(false);
        Split.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)Split).EndInit();
        Split.ResumeLayout(false);
        Status.ResumeLayout(false);
        Status.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ToolStrip Tools;
    private ToolStripButton NewButton;
    private ToolStripButton OpenButton;
    private ToolStripButton SaveButton;
    private ToolStripButton SaveAsButton;
    private ToolStripSeparator FileSeparator;
    private ToolStripButton FormatButton;
    private ToolStripButton MinifyButton;
    private ToolStripSeparator FormatSeparator;
    private ToolStripButton CollapseAllButton;
    private ToolStripButton ExpandAllButton;
    private ToolStripButton ThemeButton;
    private ToolStripButton TreeButton;
    private ToolStripSeparator ToolsSeparator;
    private ToolStripButton SortKeysButton;
    private ToolStripButton RepairButton;
    private ToolStripDropDownButton CompareButton;
    private ToolStripMenuItem CompareClipboardItem;
    private ToolStripMenuItem CompareFileItem;
    private ToolStripDropDownButton ConvertButton;
    private ToolStripMenuItem CopyYamlItem;
    private ToolStripMenuItem CopyCsvItem;
    private ToolStripMenuItem SaveYamlItem;
    private ToolStripMenuItem SaveCsvItem;
    private ToolStripSeparator ConvertSeparator;
    private ToolStripMenuItem OpenCsvItem;
    private ToolStripMenuItem PasteCsvItem;
    private ToolStripDropDownButton SchemaButton;
    private ToolStripMenuItem ChooseSchemaItem;
    private ToolStripMenuItem NoSchemaItem;
    private SplitContainer TreeSplit;
    private TreeView JsonTree;
    private ToolStripStatusLabel SchemaLabel;
    private ContextMenuStrip ResultMenu;
    private ToolStripMenuItem CopyResultJsonItem;
    private ToolStripMenuItem CopyResultLinesItem;
    private TableLayoutPanel SearchLayout;
    private ComboBox ModeComboBox;
    private TextBox SearchBox;
    private Button PreviousButton;
    private Button NextButton;
    private Label SearchMessageLabel;
    private SplitContainer Split;
    private JsonTextControl Editor;
    private TextBox ResultBox;
    private Label ResultLabel;
    private StatusStrip Status;
    private ToolStripStatusLabel PositionLabel;
    private ToolStripStatusLabel PathLabel;
    private ToolStripStatusLabel ValidityLabel;
    private ListBox SuggestionsList;
    private ToolTip Tips;
}
