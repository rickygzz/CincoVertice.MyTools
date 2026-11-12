namespace CincoVertice.MyTools.Ui.Win.Zones;

partial class ZoneEditorForm
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
            ZoneMenu.Dispose();
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
        Tools = new ToolStrip();
        LayoutDropDownButton = new ToolStripDropDownButton();
        Columns2MenuItem = new ToolStripMenuItem();
        Columns3MenuItem = new ToolStripMenuItem();
        Columns4MenuItem = new ToolStripMenuItem();
        WideCenterMenuItem = new ToolStripMenuItem();
        RowsSeparator = new ToolStripSeparator();
        Rows2MenuItem = new ToolStripMenuItem();
        Rows3MenuItem = new ToolStripMenuItem();
        GridsSeparator = new ToolStripSeparator();
        Grid2x2MenuItem = new ToolStripMenuItem();
        Grid3x2MenuItem = new ToolStripMenuItem();
        Grid3x3MenuItem = new ToolStripMenuItem();
        LayoutSeparator = new ToolStripSeparator();
        GapLabel = new ToolStripLabel();
        GapComboBox = new ToolStripComboBox();
        MonitorLabel = new ToolStripLabel();
        MonitorComboBox = new ToolStripComboBox();
        OwnLabel = new ToolStripLabel();
        UseDefaultButton = new ToolStripButton();
        MonitorSeparator = new ToolStripSeparator();
        UndoButton = new ToolStripButton();
        RedoButton = new ToolStripButton();
        HistorySeparator = new ToolStripSeparator();
        SaveButton = new ToolStripButton();
        EditAsTextButton = new ToolStripButton();
        Canvas = new ZoneCanvasControl();
        ZoneMenu = new ContextMenuStrip();
        SplitVerticallyMenuItem = new ToolStripMenuItem();
        SplitHorizontallyMenuItem = new ToolStripMenuItem();
        DeleteSeparator = new ToolStripSeparator();
        DeleteMenuItem = new ToolStripMenuItem();
        Tools.SuspendLayout();
        ZoneMenu.SuspendLayout();
        SuspendLayout();
        //
        // Tools
        //
        Tools.GripStyle = ToolStripGripStyle.Hidden;
        Tools.Items.AddRange(new ToolStripItem[]
        {
            LayoutDropDownButton, LayoutSeparator, GapLabel, GapComboBox, MonitorLabel, MonitorComboBox,
            OwnLabel, UseDefaultButton, MonitorSeparator, UndoButton, RedoButton, HistorySeparator, SaveButton,
            EditAsTextButton,
        });
        Tools.Location = new Point(0, 0);
        Tools.Name = "Tools";
        Tools.Size = new Size(900, 25);
        Tools.TabIndex = 0;
        //
        // LayoutDropDownButton
        //
        LayoutDropDownButton.DropDownItems.AddRange(new ToolStripItem[]
        {
            Columns2MenuItem, Columns3MenuItem, Columns4MenuItem, WideCenterMenuItem, RowsSeparator, Rows2MenuItem,
            Rows3MenuItem, GridsSeparator, Grid2x2MenuItem, Grid3x2MenuItem, Grid3x3MenuItem,
        });
        LayoutDropDownButton.Name = "LayoutDropDownButton";
        LayoutDropDownButton.Text = "&Layout";
        LayoutDropDownButton.ToolTipText = "Start from a layout: replaces the zones";
        //
        // Columns2MenuItem
        //
        Columns2MenuItem.Name = "Columns2MenuItem";
        Columns2MenuItem.Tag = "Columns2";
        Columns2MenuItem.Text = "2 columns";
        Columns2MenuItem.Click += TemplateMenuItem_Click;
        //
        // Columns3MenuItem
        //
        Columns3MenuItem.Name = "Columns3MenuItem";
        Columns3MenuItem.Tag = "Columns3";
        Columns3MenuItem.Text = "3 columns";
        Columns3MenuItem.Click += TemplateMenuItem_Click;
        //
        // Columns4MenuItem
        //
        Columns4MenuItem.Name = "Columns4MenuItem";
        Columns4MenuItem.Tag = "Columns4";
        Columns4MenuItem.Text = "4 columns";
        Columns4MenuItem.Click += TemplateMenuItem_Click;
        //
        // WideCenterMenuItem
        //
        WideCenterMenuItem.Name = "WideCenterMenuItem";
        WideCenterMenuItem.Tag = "WideCenter";
        WideCenterMenuItem.Text = "Wide center (25 / 50 / 25)";
        WideCenterMenuItem.Click += TemplateMenuItem_Click;
        //
        // RowsSeparator
        //
        RowsSeparator.Name = "RowsSeparator";
        //
        // Rows2MenuItem
        //
        Rows2MenuItem.Name = "Rows2MenuItem";
        Rows2MenuItem.Tag = "Rows2";
        Rows2MenuItem.Text = "2 rows";
        Rows2MenuItem.Click += TemplateMenuItem_Click;
        //
        // Rows3MenuItem
        //
        Rows3MenuItem.Name = "Rows3MenuItem";
        Rows3MenuItem.Tag = "Rows3";
        Rows3MenuItem.Text = "3 rows";
        Rows3MenuItem.Click += TemplateMenuItem_Click;
        //
        // GridsSeparator
        //
        GridsSeparator.Name = "GridsSeparator";
        //
        // Grid2x2MenuItem
        //
        Grid2x2MenuItem.Name = "Grid2x2MenuItem";
        Grid2x2MenuItem.Tag = "Grid2x2";
        Grid2x2MenuItem.Text = "Grid 2 x 2";
        Grid2x2MenuItem.Click += TemplateMenuItem_Click;
        //
        // Grid3x2MenuItem
        //
        Grid3x2MenuItem.Name = "Grid3x2MenuItem";
        Grid3x2MenuItem.Tag = "Grid3x2";
        Grid3x2MenuItem.Text = "Grid 3 x 2";
        Grid3x2MenuItem.Click += TemplateMenuItem_Click;
        //
        // Grid3x3MenuItem
        //
        Grid3x3MenuItem.Name = "Grid3x3MenuItem";
        Grid3x3MenuItem.Tag = "Grid3x3";
        Grid3x3MenuItem.Text = "Grid 3 x 3";
        Grid3x3MenuItem.Click += TemplateMenuItem_Click;
        //
        // LayoutSeparator
        //
        LayoutSeparator.Name = "LayoutSeparator";
        //
        // GapLabel
        //
        GapLabel.Name = "GapLabel";
        GapLabel.Text = "Gap";
        //
        // GapComboBox
        //
        GapComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        GapComboBox.Name = "GapComboBox";
        GapComboBox.Size = new Size(70, 25);
        GapComboBox.ToolTipText = "Space between zones and around them, in pixels of the screen";
        GapComboBox.SelectedIndexChanged += GapComboBox_SelectedIndexChanged;
        //
        // MonitorLabel
        //
        MonitorLabel.Name = "MonitorLabel";
        MonitorLabel.Text = "Edit layout of";
        //
        // MonitorComboBox
        //
        MonitorComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        MonitorComboBox.Name = "MonitorComboBox";
        MonitorComboBox.Size = new Size(260, 25);
        MonitorComboBox.ToolTipText = "The default layout, or a monitor's own";
        MonitorComboBox.SelectedIndexChanged += MonitorComboBox_SelectedIndexChanged;
        //
        // OwnLabel
        //
        OwnLabel.ForeColor = SystemColors.GrayText;
        OwnLabel.Name = "OwnLabel";
        //
        // UseDefaultButton
        //
        UseDefaultButton.Name = "UseDefaultButton";
        UseDefaultButton.Text = "Use default layout";
        UseDefaultButton.ToolTipText = "Drop this monitor's own layout, so it uses the default one";
        UseDefaultButton.Visible = false;
        UseDefaultButton.Click += UseDefaultButton_Click;
        //
        // MonitorSeparator
        //
        MonitorSeparator.Name = "MonitorSeparator";
        //
        // UndoButton
        //
        UndoButton.Enabled = false;
        UndoButton.Name = "UndoButton";
        UndoButton.Text = "Undo";
        UndoButton.ToolTipText = "Undo (Ctrl+Z)";
        UndoButton.Click += UndoButton_Click;
        //
        // RedoButton
        //
        RedoButton.Enabled = false;
        RedoButton.Name = "RedoButton";
        RedoButton.Text = "Redo";
        RedoButton.ToolTipText = "Redo (Ctrl+Y)";
        RedoButton.Click += RedoButton_Click;
        //
        // HistorySeparator
        //
        HistorySeparator.Name = "HistorySeparator";
        //
        // SaveButton
        //
        SaveButton.Name = "SaveButton";
        SaveButton.Text = "&Save";
        SaveButton.ToolTipText = "Save (Ctrl+S); the next drag uses the zones saved";
        SaveButton.Click += SaveButton_Click;
        //
        // EditAsTextButton
        //
        EditAsTextButton.Name = "EditAsTextButton";
        EditAsTextButton.Text = "Edit as &text...";
        EditAsTextButton.ToolTipText = "Open zones.cfg in the config editor";
        EditAsTextButton.Click += EditAsTextButton_Click;
        //
        // Canvas
        //
        Canvas.BackColor = Color.FromArgb(48, 48, 48);
        Canvas.Cursor = Cursors.Cross;
        Canvas.Dock = DockStyle.Fill;
        Canvas.Location = new Point(0, 25);
        Canvas.Name = "Canvas";
        Canvas.Size = new Size(900, 535);
        Canvas.TabIndex = 1;
        Canvas.Paint += Canvas_Paint;
        Canvas.MouseDown += Canvas_MouseDown;
        Canvas.MouseMove += Canvas_MouseMove;
        Canvas.MouseUp += Canvas_MouseUp;
        //
        // ZoneMenu
        //
        ZoneMenu.Items.AddRange(new ToolStripItem[]
        {
            SplitVerticallyMenuItem, SplitHorizontallyMenuItem, DeleteSeparator, DeleteMenuItem,
        });
        ZoneMenu.Name = "ZoneMenu";
        //
        // SplitVerticallyMenuItem
        //
        SplitVerticallyMenuItem.Name = "SplitVerticallyMenuItem";
        SplitVerticallyMenuItem.Text = "Split into &left and right here";
        SplitVerticallyMenuItem.Click += SplitVerticallyMenuItem_Click;
        //
        // SplitHorizontallyMenuItem
        //
        SplitHorizontallyMenuItem.Name = "SplitHorizontallyMenuItem";
        SplitHorizontallyMenuItem.Text = "Split into &top and bottom here";
        SplitHorizontallyMenuItem.Click += SplitHorizontallyMenuItem_Click;
        //
        // DeleteSeparator
        //
        DeleteSeparator.Name = "DeleteSeparator";
        //
        // DeleteMenuItem
        //
        DeleteMenuItem.Name = "DeleteMenuItem";
        DeleteMenuItem.ShortcutKeyDisplayString = "Delete";
        DeleteMenuItem.Text = "&Delete";
        DeleteMenuItem.Click += DeleteMenuItem_Click;
        //
        // ZoneEditorForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1080, 600);
        Controls.Add(Canvas);
        Controls.Add(Tools);
        KeyPreview = true;
        MinimumSize = new Size(720, 360);
        Name = "ZoneEditorForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Window zones";
        FormClosing += ZoneEditorForm_FormClosing;
        KeyDown += ZoneEditorForm_KeyDown;
        Tools.ResumeLayout(false);
        Tools.PerformLayout();
        ZoneMenu.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ToolStrip Tools;
    private ToolStripDropDownButton LayoutDropDownButton;
    private ToolStripMenuItem Columns2MenuItem;
    private ToolStripMenuItem Columns3MenuItem;
    private ToolStripMenuItem Columns4MenuItem;
    private ToolStripMenuItem WideCenterMenuItem;
    private ToolStripSeparator RowsSeparator;
    private ToolStripMenuItem Rows2MenuItem;
    private ToolStripMenuItem Rows3MenuItem;
    private ToolStripSeparator GridsSeparator;
    private ToolStripMenuItem Grid2x2MenuItem;
    private ToolStripMenuItem Grid3x2MenuItem;
    private ToolStripMenuItem Grid3x3MenuItem;
    private ToolStripSeparator LayoutSeparator;
    private ToolStripLabel GapLabel;
    private ToolStripComboBox GapComboBox;
    private ToolStripLabel MonitorLabel;
    private ToolStripComboBox MonitorComboBox;
    private ToolStripLabel OwnLabel;
    private ToolStripButton UseDefaultButton;
    private ToolStripSeparator MonitorSeparator;
    private ToolStripButton UndoButton;
    private ToolStripButton RedoButton;
    private ToolStripSeparator HistorySeparator;
    private ToolStripButton SaveButton;
    private ToolStripButton EditAsTextButton;
    private ZoneCanvasControl Canvas;
    private ContextMenuStrip ZoneMenu;
    private ToolStripMenuItem SplitVerticallyMenuItem;
    private ToolStripMenuItem SplitHorizontallyMenuItem;
    private ToolStripSeparator DeleteSeparator;
    private ToolStripMenuItem DeleteMenuItem;
}
