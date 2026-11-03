namespace CincoVertice.MyTools.Ui.Win.Diagramming;

partial class DiagramEditorForm
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
            _textFont?.Dispose();

            foreach (Image image in _images.Concat(_swatches.Values))
            {
                image.Dispose();
            }
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
        ToolsStrip = new ToolStrip();
        SelectButton = new ToolStripButton();
        ConnectorButton = new ToolStripButton();
        ToolsSeparator = new ToolStripSeparator();
        RectangleButton = new ToolStripButton();
        RoundedRectangleButton = new ToolStripButton();
        EllipseButton = new ToolStripButton();
        DiamondButton = new ToolStripButton();
        ParallelogramButton = new ToolStripButton();
        HexagonButton = new ToolStripButton();
        CylinderButton = new ToolStripButton();
        DocumentButton = new ToolStripButton();
        TextButton = new ToolStripButton();
        StyleStrip = new ToolStrip();
        NewButton = new ToolStripButton();
        OpenButton = new ToolStripButton();
        SaveButton = new ToolStripButton();
        FileSeparator = new ToolStripSeparator();
        UndoButton = new ToolStripButton();
        RedoButton = new ToolStripButton();
        HistorySeparator = new ToolStripSeparator();
        FillDropDownButton = new ToolStripDropDownButton();
        FillPalette = new Snipping.ColorPaletteControl();
        FillPaletteHost = new ToolStripControlHost(FillPalette);
        NoFillMenuItem = new ToolStripMenuItem();
        MoreFillColorsMenuItem = new ToolStripMenuItem();
        LineDropDownButton = new ToolStripDropDownButton();
        LinePalette = new Snipping.ColorPaletteControl();
        LinePaletteHost = new ToolStripControlHost(LinePalette);
        MoreLineColorsMenuItem = new ToolStripMenuItem();
        LineWidthComboBox = new ToolStripComboBox();
        DashedButton = new ToolStripButton();
        TextColorDropDownButton = new ToolStripDropDownButton();
        TextPalette = new Snipping.ColorPaletteControl();
        TextPaletteHost = new ToolStripControlHost(TextPalette);
        MoreTextColorsMenuItem = new ToolStripMenuItem();
        FontSizeComboBox = new ToolStripComboBox();
        StyleSeparator = new ToolStripSeparator();
        RoutingComboBox = new ToolStripComboBox();
        ArrowHeadsComboBox = new ToolStripComboBox();
        ResetRouteButton = new ToolStripButton();
        ConnectorsSeparator = new ToolStripSeparator();
        GroupButton = new ToolStripButton();
        UngroupButton = new ToolStripButton();
        FrontButton = new ToolStripButton();
        BackButton = new ToolStripButton();
        ZoomLabel = new ToolStripLabel();
        Canvas = new DiagramCanvasControl();
        TextInputBox = new TextBox();
        ToolsStrip.SuspendLayout();
        StyleStrip.SuspendLayout();
        Canvas.SuspendLayout();
        SuspendLayout();
        //
        // ToolsStrip
        //
        ToolsStrip.Dock = DockStyle.Left;
        ToolsStrip.GripStyle = ToolStripGripStyle.Hidden;
        ToolsStrip.Items.AddRange(new ToolStripItem[] { SelectButton, ConnectorButton, ToolsSeparator, RectangleButton, RoundedRectangleButton, EllipseButton, DiamondButton, ParallelogramButton, HexagonButton, CylinderButton, DocumentButton, TextButton });
        ToolsStrip.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
        ToolsStrip.Name = "ToolsStrip";
        ToolsStrip.TabIndex = 2;
        //
        // SelectButton
        //
        SelectButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        SelectButton.Name = "SelectButton";
        SelectButton.Tag = "Select";
        SelectButton.Text = "Select";
        SelectButton.ToolTipText = "Select, move and scale (V). Space or the middle button drags the view";
        SelectButton.Click += ToolButton_Click;
        //
        // ConnectorButton
        //
        ConnectorButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        ConnectorButton.Name = "ConnectorButton";
        ConnectorButton.Tag = "Connector";
        ConnectorButton.Text = "Connector";
        ConnectorButton.ToolTipText = "Connector (C): drag from one shape to another. Double-click it for a label";
        ConnectorButton.Click += ToolButton_Click;
        //
        // ToolsSeparator
        //
        ToolsSeparator.Name = "ToolsSeparator";
        //
        // RectangleButton
        //
        RectangleButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        RectangleButton.Name = "RectangleButton";
        RectangleButton.Tag = "Rectangle";
        RectangleButton.Text = "Rectangle";
        RectangleButton.ToolTipText = "Rectangle, or process (R)";
        RectangleButton.Click += ToolButton_Click;
        //
        // RoundedRectangleButton
        //
        RoundedRectangleButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        RoundedRectangleButton.Name = "RoundedRectangleButton";
        RoundedRectangleButton.Tag = "RoundedRectangle";
        RoundedRectangleButton.Text = "Rounded rectangle";
        RoundedRectangleButton.ToolTipText = "Rounded rectangle, or start and end (U)";
        RoundedRectangleButton.Click += ToolButton_Click;
        //
        // EllipseButton
        //
        EllipseButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        EllipseButton.Name = "EllipseButton";
        EllipseButton.Tag = "Ellipse";
        EllipseButton.Text = "Ellipse";
        EllipseButton.ToolTipText = "Ellipse (O)";
        EllipseButton.Click += ToolButton_Click;
        //
        // DiamondButton
        //
        DiamondButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        DiamondButton.Name = "DiamondButton";
        DiamondButton.Tag = "Diamond";
        DiamondButton.Text = "Diamond";
        DiamondButton.ToolTipText = "Diamond, or decision (D)";
        DiamondButton.Click += ToolButton_Click;
        //
        // ParallelogramButton
        //
        ParallelogramButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        ParallelogramButton.Name = "ParallelogramButton";
        ParallelogramButton.Tag = "Parallelogram";
        ParallelogramButton.Text = "Parallelogram";
        ParallelogramButton.ToolTipText = "Parallelogram, or input and output (P)";
        ParallelogramButton.Click += ToolButton_Click;
        //
        // HexagonButton
        //
        HexagonButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        HexagonButton.Name = "HexagonButton";
        HexagonButton.Tag = "Hexagon";
        HexagonButton.Text = "Hexagon";
        HexagonButton.ToolTipText = "Hexagon, or preparation (H)";
        HexagonButton.Click += ToolButton_Click;
        //
        // CylinderButton
        //
        CylinderButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        CylinderButton.Name = "CylinderButton";
        CylinderButton.Tag = "Cylinder";
        CylinderButton.Text = "Cylinder";
        CylinderButton.ToolTipText = "Cylinder, or database (Y)";
        CylinderButton.Click += ToolButton_Click;
        //
        // DocumentButton
        //
        DocumentButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        DocumentButton.Name = "DocumentButton";
        DocumentButton.Tag = "Document";
        DocumentButton.Text = "Document";
        DocumentButton.ToolTipText = "Document (M)";
        DocumentButton.Click += ToolButton_Click;
        //
        // TextButton
        //
        TextButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        TextButton.Name = "TextButton";
        TextButton.Tag = "Text";
        TextButton.Text = "Text";
        TextButton.ToolTipText = "Text (T): click where it goes, Enter to end, Shift+Enter for a new line";
        TextButton.Click += ToolButton_Click;
        //
        // StyleStrip
        //
        StyleStrip.GripStyle = ToolStripGripStyle.Hidden;
        StyleStrip.Items.AddRange(new ToolStripItem[] { NewButton, OpenButton, SaveButton, FileSeparator, UndoButton, RedoButton, HistorySeparator, FillDropDownButton, LineDropDownButton, LineWidthComboBox, DashedButton, TextColorDropDownButton, FontSizeComboBox, StyleSeparator, RoutingComboBox, ArrowHeadsComboBox, ResetRouteButton, ConnectorsSeparator, GroupButton, UngroupButton, FrontButton, BackButton, ZoomLabel });
        StyleStrip.Name = "StyleStrip";
        StyleStrip.Padding = new Padding(4, 2, 4, 2);
        StyleStrip.TabIndex = 1;
        //
        // NewButton
        //
        NewButton.Name = "NewButton";
        NewButton.Tag = "New";
        NewButton.Text = "New";
        NewButton.ToolTipText = "New diagram (Ctrl+N)";
        NewButton.Click += CommandButton_Click;
        //
        // OpenButton
        //
        OpenButton.Name = "OpenButton";
        OpenButton.Tag = "Open";
        OpenButton.Text = "Open";
        OpenButton.ToolTipText = "Open a diagram saved here (Ctrl+O)";
        OpenButton.Click += CommandButton_Click;
        //
        // SaveButton
        //
        SaveButton.Name = "SaveButton";
        SaveButton.Tag = "Save";
        SaveButton.Text = "Save";
        SaveButton.ToolTipText = "Save as an SVG image that opens here again (Ctrl+S; Ctrl+Shift+S to save as)";
        SaveButton.Click += CommandButton_Click;
        //
        // FileSeparator
        //
        FileSeparator.Name = "FileSeparator";
        //
        // UndoButton
        //
        UndoButton.Name = "UndoButton";
        UndoButton.Tag = "Undo";
        UndoButton.Text = "Undo";
        UndoButton.ToolTipText = "Undo (Ctrl+Z)";
        UndoButton.Click += CommandButton_Click;
        //
        // RedoButton
        //
        RedoButton.Name = "RedoButton";
        RedoButton.Tag = "Redo";
        RedoButton.Text = "Redo";
        RedoButton.ToolTipText = "Redo (Ctrl+Y)";
        RedoButton.Click += CommandButton_Click;
        //
        // HistorySeparator
        //
        HistorySeparator.Name = "HistorySeparator";
        //
        // FillDropDownButton
        //
        FillDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { FillPaletteHost, NoFillMenuItem, MoreFillColorsMenuItem });
        FillDropDownButton.Name = "FillDropDownButton";
        FillDropDownButton.Text = "Fill";
        FillDropDownButton.ToolTipText = "Fill color";
        //
        // FillPalette
        //
        FillPalette.Name = "FillPalette";
        FillPalette.ColorPicked += FillPalette_ColorPicked;
        //
        // FillPaletteHost
        //
        FillPaletteHost.AutoSize = true;
        FillPaletteHost.Name = "FillPaletteHost";
        //
        // NoFillMenuItem
        //
        NoFillMenuItem.Name = "NoFillMenuItem";
        NoFillMenuItem.Text = "No fill";
        NoFillMenuItem.Click += NoFillMenuItem_Click;
        //
        // MoreFillColorsMenuItem
        //
        MoreFillColorsMenuItem.Name = "MoreFillColorsMenuItem";
        MoreFillColorsMenuItem.Text = "More colors...";
        MoreFillColorsMenuItem.Click += MoreFillColorsMenuItem_Click;
        //
        // LineDropDownButton
        //
        LineDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { LinePaletteHost, MoreLineColorsMenuItem });
        LineDropDownButton.Name = "LineDropDownButton";
        LineDropDownButton.Text = "Line";
        LineDropDownButton.ToolTipText = "Line color";
        //
        // LinePalette
        //
        LinePalette.Name = "LinePalette";
        LinePalette.ColorPicked += LinePalette_ColorPicked;
        //
        // LinePaletteHost
        //
        LinePaletteHost.AutoSize = true;
        LinePaletteHost.Name = "LinePaletteHost";
        //
        // MoreLineColorsMenuItem
        //
        MoreLineColorsMenuItem.Name = "MoreLineColorsMenuItem";
        MoreLineColorsMenuItem.Text = "More colors...";
        MoreLineColorsMenuItem.Click += MoreLineColorsMenuItem_Click;
        //
        // LineWidthComboBox
        //
        LineWidthComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        LineWidthComboBox.Name = "LineWidthComboBox";
        LineWidthComboBox.Size = new Size(60, 23);
        LineWidthComboBox.ToolTipText = "Line width";
        LineWidthComboBox.SelectedIndexChanged += LineWidthComboBox_SelectedIndexChanged;
        //
        // DashedButton
        //
        DashedButton.Name = "DashedButton";
        DashedButton.Text = "Dashed";
        DashedButton.ToolTipText = "Dashed line";
        DashedButton.Click += DashedButton_Click;
        //
        // TextColorDropDownButton
        //
        TextColorDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { TextPaletteHost, MoreTextColorsMenuItem });
        TextColorDropDownButton.Name = "TextColorDropDownButton";
        TextColorDropDownButton.Text = "Text";
        TextColorDropDownButton.ToolTipText = "Text color";
        //
        // TextPalette
        //
        TextPalette.Name = "TextPalette";
        TextPalette.ColorPicked += TextPalette_ColorPicked;
        //
        // TextPaletteHost
        //
        TextPaletteHost.AutoSize = true;
        TextPaletteHost.Name = "TextPaletteHost";
        //
        // MoreTextColorsMenuItem
        //
        MoreTextColorsMenuItem.Name = "MoreTextColorsMenuItem";
        MoreTextColorsMenuItem.Text = "More colors...";
        MoreTextColorsMenuItem.Click += MoreTextColorsMenuItem_Click;
        //
        // FontSizeComboBox
        //
        FontSizeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        FontSizeComboBox.Name = "FontSizeComboBox";
        FontSizeComboBox.Size = new Size(60, 23);
        FontSizeComboBox.ToolTipText = "Text size";
        FontSizeComboBox.SelectedIndexChanged += FontSizeComboBox_SelectedIndexChanged;
        //
        // StyleSeparator
        //
        StyleSeparator.Name = "StyleSeparator";
        //
        // RoutingComboBox
        //
        RoutingComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        RoutingComboBox.Name = "RoutingComboBox";
        RoutingComboBox.Size = new Size(70, 23);
        RoutingComboBox.ToolTipText = "How connectors go between shapes";
        RoutingComboBox.SelectedIndexChanged += RoutingComboBox_SelectedIndexChanged;
        //
        // ArrowHeadsComboBox
        //
        ArrowHeadsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        ArrowHeadsComboBox.Name = "ArrowHeadsComboBox";
        ArrowHeadsComboBox.Size = new Size(105, 23);
        ArrowHeadsComboBox.ToolTipText = "Which ends of a connector have an arrow";
        ArrowHeadsComboBox.SelectedIndexChanged += ArrowHeadsComboBox_SelectedIndexChanged;
        //
        // ResetRouteButton
        //
        ResetRouteButton.Name = "ResetRouteButton";
        ResetRouteButton.Tag = "ResetRoute";
        ResetRouteButton.Text = "Reset line";
        ResetRouteButton.ToolTipText = "Let the selected connectors work out their own path again";
        ResetRouteButton.Click += CommandButton_Click;
        //
        // ConnectorsSeparator
        //
        ConnectorsSeparator.Name = "ConnectorsSeparator";
        //
        // GroupButton
        //
        GroupButton.Name = "GroupButton";
        GroupButton.Tag = "Group";
        GroupButton.Text = "Group";
        GroupButton.ToolTipText = "Group the selected shapes (Ctrl+G)";
        GroupButton.Click += CommandButton_Click;
        //
        // UngroupButton
        //
        UngroupButton.Name = "UngroupButton";
        UngroupButton.Tag = "Ungroup";
        UngroupButton.Text = "Ungroup";
        UngroupButton.ToolTipText = "Ungroup (Ctrl+Shift+G)";
        UngroupButton.Click += CommandButton_Click;
        //
        // FrontButton
        //
        FrontButton.Name = "FrontButton";
        FrontButton.Tag = "BringToFront";
        FrontButton.Text = "Front";
        FrontButton.ToolTipText = "Bring to front (Ctrl+])";
        FrontButton.Click += CommandButton_Click;
        //
        // BackButton
        //
        BackButton.Name = "BackButton";
        BackButton.Tag = "SendToBack";
        BackButton.Text = "Back";
        BackButton.ToolTipText = "Send to back (Ctrl+[)";
        BackButton.Click += CommandButton_Click;
        //
        // ZoomLabel
        //
        ZoomLabel.Alignment = ToolStripItemAlignment.Right;
        ZoomLabel.Name = "ZoomLabel";
        ZoomLabel.ToolTipText = "Ctrl+wheel to zoom, Ctrl+0 for 100%";
        //
        // Canvas
        //
        Canvas.Controls.Add(TextInputBox);
        Canvas.Dock = DockStyle.Fill;
        Canvas.Name = "Canvas";
        Canvas.TabIndex = 0;
        Canvas.Paint += Canvas_Paint;
        Canvas.MouseDown += Canvas_MouseDown;
        Canvas.MouseMove += Canvas_MouseMove;
        Canvas.MouseUp += Canvas_MouseUp;
        Canvas.MouseDoubleClick += Canvas_MouseDoubleClick;
        Canvas.MouseLeave += Canvas_MouseLeave;
        Canvas.MouseWheel += Canvas_MouseWheel;
        //
        // TextInputBox
        //
        TextInputBox.AcceptsReturn = true;
        TextInputBox.BorderStyle = BorderStyle.FixedSingle;
        TextInputBox.Multiline = true;
        TextInputBox.Name = "TextInputBox";
        TextInputBox.TabIndex = 0;
        TextInputBox.TextAlign = HorizontalAlignment.Center;
        TextInputBox.Visible = false;
        TextInputBox.KeyDown += TextInputBox_KeyDown;
        TextInputBox.Leave += TextInputBox_Leave;
        //
        // DiagramEditorForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 720);
        Controls.Add(Canvas);
        Controls.Add(ToolsStrip);
        Controls.Add(StyleStrip);
        KeyPreview = true;
        MinimumSize = new Size(640, 360);
        Name = "DiagramEditorForm";
        StartPosition = FormStartPosition.Manual;
        Text = "Diagram editor";
        FormClosing += DiagramEditorForm_FormClosing;
        KeyDown += DiagramEditorForm_KeyDown;
        KeyUp += DiagramEditorForm_KeyUp;
        ToolsStrip.ResumeLayout(false);
        ToolsStrip.PerformLayout();
        StyleStrip.ResumeLayout(false);
        StyleStrip.PerformLayout();
        Canvas.ResumeLayout(false);
        Canvas.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ToolStrip ToolsStrip;
    private ToolStripButton SelectButton;
    private ToolStripButton ConnectorButton;
    private ToolStripSeparator ToolsSeparator;
    private ToolStripButton RectangleButton;
    private ToolStripButton RoundedRectangleButton;
    private ToolStripButton EllipseButton;
    private ToolStripButton DiamondButton;
    private ToolStripButton ParallelogramButton;
    private ToolStripButton HexagonButton;
    private ToolStripButton CylinderButton;
    private ToolStripButton DocumentButton;
    private ToolStripButton TextButton;
    private ToolStrip StyleStrip;
    private ToolStripButton NewButton;
    private ToolStripButton OpenButton;
    private ToolStripButton SaveButton;
    private ToolStripSeparator FileSeparator;
    private ToolStripButton UndoButton;
    private ToolStripButton RedoButton;
    private ToolStripSeparator HistorySeparator;
    private ToolStripDropDownButton FillDropDownButton;
    private Snipping.ColorPaletteControl FillPalette;
    private ToolStripControlHost FillPaletteHost;
    private ToolStripMenuItem NoFillMenuItem;
    private ToolStripMenuItem MoreFillColorsMenuItem;
    private ToolStripDropDownButton LineDropDownButton;
    private Snipping.ColorPaletteControl LinePalette;
    private ToolStripControlHost LinePaletteHost;
    private ToolStripMenuItem MoreLineColorsMenuItem;
    private ToolStripComboBox LineWidthComboBox;
    private ToolStripButton DashedButton;
    private ToolStripDropDownButton TextColorDropDownButton;
    private Snipping.ColorPaletteControl TextPalette;
    private ToolStripControlHost TextPaletteHost;
    private ToolStripMenuItem MoreTextColorsMenuItem;
    private ToolStripComboBox FontSizeComboBox;
    private ToolStripSeparator StyleSeparator;
    private ToolStripComboBox RoutingComboBox;
    private ToolStripComboBox ArrowHeadsComboBox;
    private ToolStripButton ResetRouteButton;
    private ToolStripSeparator ConnectorsSeparator;
    private ToolStripButton GroupButton;
    private ToolStripButton UngroupButton;
    private ToolStripButton FrontButton;
    private ToolStripButton BackButton;
    private ToolStripLabel ZoomLabel;
    private DiagramCanvasControl Canvas;
    private TextBox TextInputBox;
}
