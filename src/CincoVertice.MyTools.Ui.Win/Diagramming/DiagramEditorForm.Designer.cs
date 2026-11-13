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
            SelectionMenu.Dispose();
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
        SelectionMenu = new ContextMenuStrip();
        CopyStyleContextMenuItem = new ToolStripMenuItem();
        PasteStyleContextMenuItem = new ToolStripMenuItem();
        GroupContextSeparator = new ToolStripSeparator();
        GroupContextMenuItem = new ToolStripMenuItem();
        UngroupContextMenuItem = new ToolStripMenuItem();
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
        FileDropDownButton = new ToolStripDropDownButton();
        NewMenuItem = new ToolStripMenuItem();
        OpenMenuItem = new ToolStripMenuItem();
        SaveMenuItem = new ToolStripMenuItem();
        SaveAsMenuItem = new ToolStripMenuItem();
        ExportPdfMenuItem = new ToolStripMenuItem();
        ExitSeparator = new ToolStripSeparator();
        ExitMenuItem = new ToolStripMenuItem();
        FileSeparator = new ToolStripSeparator();
        EditDropDownButton = new ToolStripDropDownButton();
        GroupSeparator = new ToolStripSeparator();
        UndoMenuItem = new ToolStripMenuItem();
        RedoMenuItem = new ToolStripMenuItem();
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
        TextOutlineDropDownButton = new ToolStripDropDownButton();
        TextOutlinePalette = new Snipping.ColorPaletteControl();
        TextOutlinePaletteHost = new ToolStripControlHost(TextOutlinePalette);
        NoTextOutlineMenuItem = new ToolStripMenuItem();
        MoreTextOutlineColorsMenuItem = new ToolStripMenuItem();
        TextOutlineWidthComboBox = new ToolStripComboBox();
        CopyStyleMenuItem = new ToolStripMenuItem();
        PasteStyleMenuItem = new ToolStripMenuItem();
        StyleSeparator = new ToolStripSeparator();
        RoutingComboBox = new ToolStripComboBox();
        ArrowHeadsComboBox = new ToolStripComboBox();
        ResetRouteButton = new ToolStripButton();
        ConnectorsSeparator = new ToolStripSeparator();
        GroupMenuItem = new ToolStripMenuItem();
        UngroupMenuItem = new ToolStripMenuItem();
        FrontButton = new ToolStripButton();
        BackButton = new ToolStripButton();
        PropertiesButton = new ToolStripButton();
        ZoomLabel = new ToolStripLabel();
        Canvas = new DiagramCanvasControl();
        TextInputBox = new TextBox();
        PropertiesPanel = new Panel();
        PropertiesTable = new TableLayoutPanel();
        PropertiesTitleLabel = new Label();
        TextLabel = new Label();
        TextPropertyBox = new TextBox();
        XLabel = new Label();
        XBox = new NumericUpDown();
        YLabel = new Label();
        YBox = new NumericUpDown();
        WidthLabel = new Label();
        WidthBox = new NumericUpDown();
        HeightLabel = new Label();
        HeightBox = new NumericUpDown();
        CornerRadiusLabel = new Label();
        CornerRadiusBox = new NumericUpDown();
        FillLabel = new Label();
        FillColorButton = new Button();
        LineLabel = new Label();
        LineColorButton = new Button();
        LineWidthLabel = new Label();
        LineWidthBox = new NumericUpDown();
        DashedCheckBox = new CheckBox();
        TextColorLabel = new Label();
        TextColorButton = new Button();
        FontFamilyLabel = new Label();
        FontFamilyBox = new ComboBox();
        FontSizeLabel = new Label();
        FontSizeBox = new NumericUpDown();
        TextOutlineLabel = new Label();
        TextOutlineColorButton = new Button();
        TextOutlineWidthLabel = new Label();
        TextOutlineWidthBox = new NumericUpDown();
        RoutingLabel = new Label();
        RoutingPropertyBox = new ComboBox();
        ArrowsLabel = new Label();
        ArrowsPropertyBox = new ComboBox();
        ToolsStrip.SuspendLayout();
        StyleStrip.SuspendLayout();
        Canvas.SuspendLayout();
        PropertiesPanel.SuspendLayout();
        PropertiesTable.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)XBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)YBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)WidthBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)HeightBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)CornerRadiusBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)LineWidthBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)FontSizeBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)TextOutlineWidthBox).BeginInit();
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
        StyleStrip.Items.AddRange(new ToolStripItem[] { FileDropDownButton, EditDropDownButton, FileSeparator, FillDropDownButton, LineDropDownButton, LineWidthComboBox, DashedButton, TextColorDropDownButton, FontSizeComboBox, TextOutlineDropDownButton, TextOutlineWidthComboBox, StyleSeparator, RoutingComboBox, ArrowHeadsComboBox, ResetRouteButton, ConnectorsSeparator, FrontButton, BackButton, ZoomLabel, PropertiesButton });
        StyleStrip.Name = "StyleStrip";
        StyleStrip.Padding = new Padding(4, 2, 4, 2);
        StyleStrip.TabIndex = 1;
        //
        // EditDropDownButton
        //
        EditDropDownButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        EditDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { UndoMenuItem, RedoMenuItem, HistorySeparator, CopyStyleMenuItem, PasteStyleMenuItem, GroupSeparator, GroupMenuItem, UngroupMenuItem });
        EditDropDownButton.Name = "EditDropDownButton";
        EditDropDownButton.Text = "Edit";
        EditDropDownButton.ToolTipText = "Undo, redo, copy and paste a style, group";
        //
        // GroupSeparator
        //
        GroupSeparator.Name = "GroupSeparator";
        //
        // SelectionMenu
        //
        SelectionMenu.Items.AddRange(new ToolStripItem[] { CopyStyleContextMenuItem, PasteStyleContextMenuItem, GroupContextSeparator, GroupContextMenuItem, UngroupContextMenuItem });
        SelectionMenu.Name = "SelectionMenu";
        //
        // CopyStyleContextMenuItem
        //
        CopyStyleContextMenuItem.Name = "CopyStyleContextMenuItem";
        CopyStyleContextMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+C";
        CopyStyleContextMenuItem.Tag = "CopyStyle";
        CopyStyleContextMenuItem.Text = "Copy style";
        CopyStyleContextMenuItem.Click += CommandButton_Click;
        //
        // PasteStyleContextMenuItem
        //
        PasteStyleContextMenuItem.Name = "PasteStyleContextMenuItem";
        PasteStyleContextMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+V";
        PasteStyleContextMenuItem.Tag = "PasteStyle";
        PasteStyleContextMenuItem.Text = "Paste style";
        PasteStyleContextMenuItem.Click += CommandButton_Click;
        //
        // GroupContextSeparator
        //
        GroupContextSeparator.Name = "GroupContextSeparator";
        //
        // GroupContextMenuItem
        //
        GroupContextMenuItem.Name = "GroupContextMenuItem";
        GroupContextMenuItem.ShortcutKeyDisplayString = "Ctrl+G";
        GroupContextMenuItem.Tag = "Group";
        GroupContextMenuItem.Text = "Group";
        GroupContextMenuItem.Click += CommandButton_Click;
        //
        // UngroupContextMenuItem
        //
        UngroupContextMenuItem.Name = "UngroupContextMenuItem";
        UngroupContextMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+G";
        UngroupContextMenuItem.Tag = "Ungroup";
        UngroupContextMenuItem.Text = "Ungroup";
        UngroupContextMenuItem.Click += CommandButton_Click;
        //
        // FileDropDownButton
        //
        FileDropDownButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        FileDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { NewMenuItem, OpenMenuItem, SaveMenuItem, SaveAsMenuItem, ExportPdfMenuItem, ExitSeparator, ExitMenuItem });
        FileDropDownButton.Name = "FileDropDownButton";
        FileDropDownButton.Text = "File";
        FileDropDownButton.ToolTipText = "New, open, save, export and close";
        //
        // NewMenuItem
        //
        NewMenuItem.Name = "NewMenuItem";
        NewMenuItem.ShortcutKeyDisplayString = "Ctrl+N";
        NewMenuItem.Tag = "New";
        NewMenuItem.Text = "New";
        NewMenuItem.ToolTipText = "Start an empty diagram";
        NewMenuItem.Click += CommandButton_Click;
        //
        // OpenMenuItem
        //
        OpenMenuItem.Name = "OpenMenuItem";
        OpenMenuItem.ShortcutKeyDisplayString = "Ctrl+O";
        OpenMenuItem.Tag = "Open";
        OpenMenuItem.Text = "Open...";
        OpenMenuItem.ToolTipText = "Open a diagram saved here";
        OpenMenuItem.Click += CommandButton_Click;
        //
        // SaveMenuItem
        //
        SaveMenuItem.Name = "SaveMenuItem";
        SaveMenuItem.ShortcutKeyDisplayString = "Ctrl+S";
        SaveMenuItem.Tag = "Save";
        SaveMenuItem.Text = "Save";
        SaveMenuItem.ToolTipText = "Save as an SVG image that opens here again";
        SaveMenuItem.Click += CommandButton_Click;
        //
        // SaveAsMenuItem
        //
        SaveAsMenuItem.Name = "SaveAsMenuItem";
        SaveAsMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+S";
        SaveAsMenuItem.Tag = "SaveAs";
        SaveAsMenuItem.Text = "Save as...";
        SaveAsMenuItem.ToolTipText = "Save under another name";
        SaveAsMenuItem.Click += CommandButton_Click;
        //
        // ExportPdfMenuItem
        //
        ExportPdfMenuItem.Name = "ExportPdfMenuItem";
        ExportPdfMenuItem.ShortcutKeyDisplayString = "Ctrl+E";
        ExportPdfMenuItem.Tag = "ExportPdf";
        ExportPdfMenuItem.Text = "Export as PDF...";
        ExportPdfMenuItem.ToolTipText = "Save a copy as a PDF file, to share or print";
        ExportPdfMenuItem.Click += CommandButton_Click;
        //
        // ExitSeparator
        //
        ExitSeparator.Name = "ExitSeparator";
        //
        // ExitMenuItem
        //
        ExitMenuItem.Name = "ExitMenuItem";
        ExitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
        ExitMenuItem.Text = "Exit";
        ExitMenuItem.ToolTipText = "Close this window";
        ExitMenuItem.Click += ExitMenuItem_Click;
        //
        // CopyStyleMenuItem
        //
        CopyStyleMenuItem.Name = "CopyStyleMenuItem";
        CopyStyleMenuItem.Tag = "CopyStyle";
        CopyStyleMenuItem.Text = "Copy style";
        CopyStyleMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+C";
        CopyStyleMenuItem.ToolTipText = "Copy the style of what's selected";
        CopyStyleMenuItem.Click += CommandButton_Click;
        //
        // PasteStyleMenuItem
        //
        PasteStyleMenuItem.Name = "PasteStyleMenuItem";
        PasteStyleMenuItem.Tag = "PasteStyle";
        PasteStyleMenuItem.Text = "Paste style";
        PasteStyleMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+V";
        PasteStyleMenuItem.ToolTipText = "Give what's selected, and what you draw next, the copied style";
        PasteStyleMenuItem.Click += CommandButton_Click;
        //
        // FileSeparator
        //
        FileSeparator.Name = "FileSeparator";
        //
        // UndoMenuItem
        //
        UndoMenuItem.Name = "UndoMenuItem";
        UndoMenuItem.Tag = "Undo";
        UndoMenuItem.Text = "Undo";
        UndoMenuItem.ShortcutKeyDisplayString = "Ctrl+Z";
        UndoMenuItem.ToolTipText = "Undo the last change";
        UndoMenuItem.Click += CommandButton_Click;
        //
        // RedoMenuItem
        //
        RedoMenuItem.Name = "RedoMenuItem";
        RedoMenuItem.Tag = "Redo";
        RedoMenuItem.Text = "Redo";
        RedoMenuItem.ShortcutKeyDisplayString = "Ctrl+Y";
        RedoMenuItem.ToolTipText = "Redo what was undone";
        RedoMenuItem.Click += CommandButton_Click;
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
        // TextOutlineDropDownButton
        //
        TextOutlineDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { TextOutlinePaletteHost, NoTextOutlineMenuItem, MoreTextOutlineColorsMenuItem });
        TextOutlineDropDownButton.Name = "TextOutlineDropDownButton";
        TextOutlineDropDownButton.Text = "Edge";
        TextOutlineDropDownButton.ToolTipText = "Color of the edge around the text's letters";
        //
        // TextOutlinePalette
        //
        TextOutlinePalette.Name = "TextOutlinePalette";
        TextOutlinePalette.ColorPicked += TextOutlinePalette_ColorPicked;
        //
        // TextOutlinePaletteHost
        //
        TextOutlinePaletteHost.AutoSize = true;
        TextOutlinePaletteHost.Name = "TextOutlinePaletteHost";
        //
        // NoTextOutlineMenuItem
        //
        NoTextOutlineMenuItem.Name = "NoTextOutlineMenuItem";
        NoTextOutlineMenuItem.Text = "No edge";
        NoTextOutlineMenuItem.Click += NoTextOutlineMenuItem_Click;
        //
        // MoreTextOutlineColorsMenuItem
        //
        MoreTextOutlineColorsMenuItem.Name = "MoreTextOutlineColorsMenuItem";
        MoreTextOutlineColorsMenuItem.Text = "More colors...";
        MoreTextOutlineColorsMenuItem.Click += MoreTextOutlineColorsMenuItem_Click;
        //
        // TextOutlineWidthComboBox
        //
        TextOutlineWidthComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        TextOutlineWidthComboBox.Name = "TextOutlineWidthComboBox";
        TextOutlineWidthComboBox.Size = new Size(70, 23);
        TextOutlineWidthComboBox.ToolTipText = "Width of the edge around the text's letters";
        TextOutlineWidthComboBox.SelectedIndexChanged += TextOutlineWidthComboBox_SelectedIndexChanged;
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
        // GroupMenuItem
        //
        GroupMenuItem.Name = "GroupMenuItem";
        GroupMenuItem.Tag = "Group";
        GroupMenuItem.Text = "Group";
        GroupMenuItem.ShortcutKeyDisplayString = "Ctrl+G";
        GroupMenuItem.ToolTipText = "Group the selected shapes";
        GroupMenuItem.Click += CommandButton_Click;
        //
        // UngroupMenuItem
        //
        UngroupMenuItem.Name = "UngroupMenuItem";
        UngroupMenuItem.Tag = "Ungroup";
        UngroupMenuItem.Text = "Ungroup";
        UngroupMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+G";
        UngroupMenuItem.ToolTipText = "Ungroup the selected groups";
        UngroupMenuItem.Click += CommandButton_Click;
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
        // PropertiesButton
        //
        PropertiesButton.Alignment = ToolStripItemAlignment.Right;
        PropertiesButton.Checked = true;
        PropertiesButton.Name = "PropertiesButton";
        PropertiesButton.Overflow = ToolStripItemOverflow.Never;
        PropertiesButton.Text = "Properties";
        PropertiesButton.ToolTipText = "Show or hide the properties panel (F4)";
        PropertiesButton.Click += PropertiesButton_Click;
        //
        // PropertiesPanel
        //
        PropertiesPanel.AutoScroll = true;
        PropertiesPanel.BorderStyle = BorderStyle.FixedSingle;
        PropertiesPanel.Controls.Add(PropertiesTable);
        PropertiesPanel.Dock = DockStyle.Right;
        PropertiesPanel.Name = "PropertiesPanel";
        PropertiesPanel.Size = new Size(230, 700);
        PropertiesPanel.TabIndex = 3;
        //
        // PropertiesTable
        //
        PropertiesTable.AutoSize = true;
        PropertiesTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        PropertiesTable.ColumnCount = 2;
        PropertiesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        PropertiesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        PropertiesTable.Controls.Add(PropertiesTitleLabel, 0, 0);
        PropertiesTable.SetColumnSpan(PropertiesTitleLabel, 2);
        PropertiesTable.Controls.Add(TextLabel, 0, 1);
        PropertiesTable.Controls.Add(TextPropertyBox, 1, 1);
        PropertiesTable.Controls.Add(XLabel, 0, 2);
        PropertiesTable.Controls.Add(XBox, 1, 2);
        PropertiesTable.Controls.Add(YLabel, 0, 3);
        PropertiesTable.Controls.Add(YBox, 1, 3);
        PropertiesTable.Controls.Add(WidthLabel, 0, 4);
        PropertiesTable.Controls.Add(WidthBox, 1, 4);
        PropertiesTable.Controls.Add(HeightLabel, 0, 5);
        PropertiesTable.Controls.Add(HeightBox, 1, 5);
        PropertiesTable.Controls.Add(CornerRadiusLabel, 0, 6);
        PropertiesTable.Controls.Add(CornerRadiusBox, 1, 6);
        PropertiesTable.Controls.Add(FillLabel, 0, 7);
        PropertiesTable.Controls.Add(FillColorButton, 1, 7);
        PropertiesTable.Controls.Add(LineLabel, 0, 8);
        PropertiesTable.Controls.Add(LineColorButton, 1, 8);
        PropertiesTable.Controls.Add(LineWidthLabel, 0, 9);
        PropertiesTable.Controls.Add(LineWidthBox, 1, 9);
        PropertiesTable.Controls.Add(DashedCheckBox, 1, 10);
        PropertiesTable.Controls.Add(TextColorLabel, 0, 11);
        PropertiesTable.Controls.Add(TextColorButton, 1, 11);
        PropertiesTable.Controls.Add(FontFamilyLabel, 0, 12);
        PropertiesTable.Controls.Add(FontFamilyBox, 1, 12);
        PropertiesTable.Controls.Add(FontSizeLabel, 0, 13);
        PropertiesTable.Controls.Add(FontSizeBox, 1, 13);
        PropertiesTable.Controls.Add(TextOutlineLabel, 0, 14);
        PropertiesTable.Controls.Add(TextOutlineColorButton, 1, 14);
        PropertiesTable.Controls.Add(TextOutlineWidthLabel, 0, 15);
        PropertiesTable.Controls.Add(TextOutlineWidthBox, 1, 15);
        PropertiesTable.Controls.Add(RoutingLabel, 0, 16);
        PropertiesTable.Controls.Add(RoutingPropertyBox, 1, 16);
        PropertiesTable.Controls.Add(ArrowsLabel, 0, 17);
        PropertiesTable.Controls.Add(ArrowsPropertyBox, 1, 17);
        PropertiesTable.Dock = DockStyle.Top;
        PropertiesTable.Name = "PropertiesTable";
        PropertiesTable.Padding = new Padding(6);
        PropertiesTable.RowCount = 18;
        PropertiesTable.TabIndex = 0;
        //
        // PropertiesTitleLabel
        //
        PropertiesTitleLabel.AutoSize = true;
        PropertiesTitleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        PropertiesTitleLabel.Margin = new Padding(3, 3, 3, 9);
        PropertiesTitleLabel.Name = "PropertiesTitleLabel";
        //
        // TextLabel
        //
        TextLabel.Anchor = AnchorStyles.Left;
        TextLabel.AutoSize = true;
        TextLabel.Name = "TextLabel";
        TextLabel.Text = "Text";
        //
        // TextPropertyBox
        //
        TextPropertyBox.AcceptsReturn = true;
        TextPropertyBox.Dock = DockStyle.Fill;
        TextPropertyBox.Multiline = true;
        TextPropertyBox.Name = "TextPropertyBox";
        TextPropertyBox.Size = new Size(120, 60);
        TextPropertyBox.TabIndex = 0;
        TextPropertyBox.KeyDown += TextPropertyBox_KeyDown;
        TextPropertyBox.Leave += TextPropertyBox_Leave;
        //
        // XLabel
        //
        XLabel.Anchor = AnchorStyles.Left;
        XLabel.AutoSize = true;
        XLabel.Name = "XLabel";
        XLabel.Text = "X";
        //
        // XBox
        //
        XBox.Dock = DockStyle.Fill;
        XBox.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        XBox.Minimum = new decimal(new int[] { 100000, 0, 0, int.MinValue });
        XBox.Name = "XBox";
        XBox.TabIndex = 1;
        XBox.ValueChanged += BoundsBox_ValueChanged;
        //
        // YLabel
        //
        YLabel.Anchor = AnchorStyles.Left;
        YLabel.AutoSize = true;
        YLabel.Name = "YLabel";
        YLabel.Text = "Y";
        //
        // YBox
        //
        YBox.Dock = DockStyle.Fill;
        YBox.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        YBox.Minimum = new decimal(new int[] { 100000, 0, 0, int.MinValue });
        YBox.Name = "YBox";
        YBox.TabIndex = 2;
        YBox.ValueChanged += BoundsBox_ValueChanged;
        //
        // WidthLabel
        //
        WidthLabel.Anchor = AnchorStyles.Left;
        WidthLabel.AutoSize = true;
        WidthLabel.Name = "WidthLabel";
        WidthLabel.Text = "Width";
        //
        // WidthBox
        //
        WidthBox.Dock = DockStyle.Fill;
        WidthBox.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        WidthBox.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
        WidthBox.Name = "WidthBox";
        WidthBox.TabIndex = 3;
        WidthBox.Value = new decimal(new int[] { 10, 0, 0, 0 });
        WidthBox.ValueChanged += BoundsBox_ValueChanged;
        //
        // HeightLabel
        //
        HeightLabel.Anchor = AnchorStyles.Left;
        HeightLabel.AutoSize = true;
        HeightLabel.Name = "HeightLabel";
        HeightLabel.Text = "Height";
        //
        // HeightBox
        //
        HeightBox.Dock = DockStyle.Fill;
        HeightBox.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        HeightBox.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
        HeightBox.Name = "HeightBox";
        HeightBox.TabIndex = 4;
        HeightBox.Value = new decimal(new int[] { 10, 0, 0, 0 });
        HeightBox.ValueChanged += BoundsBox_ValueChanged;
        //
        // CornerRadiusLabel
        //
        CornerRadiusLabel.Anchor = AnchorStyles.Left;
        CornerRadiusLabel.AutoSize = true;
        CornerRadiusLabel.Name = "CornerRadiusLabel";
        CornerRadiusLabel.Text = "Corner radius";
        //
        // CornerRadiusBox
        //
        CornerRadiusBox.Dock = DockStyle.Fill;
        CornerRadiusBox.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        CornerRadiusBox.Name = "CornerRadiusBox";
        CornerRadiusBox.TabIndex = 5;
        CornerRadiusBox.ValueChanged += CornerRadiusBox_ValueChanged;
        //
        // FillLabel
        //
        FillLabel.Anchor = AnchorStyles.Left;
        FillLabel.AutoSize = true;
        FillLabel.Name = "FillLabel";
        FillLabel.Text = "Fill";
        //
        // FillColorButton
        //
        FillColorButton.Dock = DockStyle.Fill;
        FillColorButton.FlatStyle = FlatStyle.Flat;
        FillColorButton.Name = "FillColorButton";
        FillColorButton.TabIndex = 6;
        FillColorButton.Click += FillColorButton_Click;
        //
        // LineLabel
        //
        LineLabel.Anchor = AnchorStyles.Left;
        LineLabel.AutoSize = true;
        LineLabel.Name = "LineLabel";
        LineLabel.Text = "Border";
        //
        // LineColorButton
        //
        LineColorButton.Dock = DockStyle.Fill;
        LineColorButton.FlatStyle = FlatStyle.Flat;
        LineColorButton.Name = "LineColorButton";
        LineColorButton.TabIndex = 7;
        LineColorButton.Click += LineColorButton_Click;
        //
        // LineWidthLabel
        //
        LineWidthLabel.Anchor = AnchorStyles.Left;
        LineWidthLabel.AutoSize = true;
        LineWidthLabel.Name = "LineWidthLabel";
        LineWidthLabel.Text = "Border width";
        //
        // LineWidthBox
        //
        LineWidthBox.Dock = DockStyle.Fill;
        LineWidthBox.Name = "LineWidthBox";
        LineWidthBox.TabIndex = 8;
        LineWidthBox.ValueChanged += LineWidthBox_ValueChanged;
        //
        // DashedCheckBox
        //
        DashedCheckBox.AutoSize = true;
        DashedCheckBox.Name = "DashedCheckBox";
        DashedCheckBox.TabIndex = 9;
        DashedCheckBox.Text = "Dashed";
        DashedCheckBox.CheckedChanged += DashedCheckBox_CheckedChanged;
        //
        // TextColorLabel
        //
        TextColorLabel.Anchor = AnchorStyles.Left;
        TextColorLabel.AutoSize = true;
        TextColorLabel.Name = "TextColorLabel";
        TextColorLabel.Text = "Text color";
        //
        // TextColorButton
        //
        TextColorButton.Dock = DockStyle.Fill;
        TextColorButton.FlatStyle = FlatStyle.Flat;
        TextColorButton.Name = "TextColorButton";
        TextColorButton.TabIndex = 10;
        TextColorButton.Click += TextColorButton_Click;
        //
        // FontFamilyLabel
        //
        FontFamilyLabel.Anchor = AnchorStyles.Left;
        FontFamilyLabel.AutoSize = true;
        FontFamilyLabel.Name = "FontFamilyLabel";
        FontFamilyLabel.Text = "Font";
        //
        // FontFamilyBox
        //
        FontFamilyBox.Dock = DockStyle.Fill;
        FontFamilyBox.DropDownStyle = ComboBoxStyle.DropDownList;
        FontFamilyBox.DropDownWidth = 220;
        FontFamilyBox.Name = "FontFamilyBox";
        FontFamilyBox.TabIndex = 14;
        FontFamilyBox.SelectedIndexChanged += FontFamilyBox_SelectedIndexChanged;
        //
        // FontSizeLabel
        //
        FontSizeLabel.Anchor = AnchorStyles.Left;
        FontSizeLabel.AutoSize = true;
        FontSizeLabel.Name = "FontSizeLabel";
        FontSizeLabel.Text = "Text size";
        //
        // FontSizeBox
        //
        FontSizeBox.Dock = DockStyle.Fill;
        FontSizeBox.Maximum = new decimal(new int[] { 400, 0, 0, 0 });
        FontSizeBox.Minimum = new decimal(new int[] { 6, 0, 0, 0 });
        FontSizeBox.Name = "FontSizeBox";
        FontSizeBox.TabIndex = 11;
        FontSizeBox.Value = new decimal(new int[] { 6, 0, 0, 0 });
        FontSizeBox.ValueChanged += FontSizeBox_ValueChanged;
        //
        // TextOutlineLabel
        //
        TextOutlineLabel.Anchor = AnchorStyles.Left;
        TextOutlineLabel.AutoSize = true;
        TextOutlineLabel.Name = "TextOutlineLabel";
        TextOutlineLabel.Text = "Text edge";
        //
        // TextOutlineColorButton
        //
        TextOutlineColorButton.Dock = DockStyle.Fill;
        TextOutlineColorButton.FlatStyle = FlatStyle.Flat;
        TextOutlineColorButton.Name = "TextOutlineColorButton";
        TextOutlineColorButton.TabIndex = 15;
        TextOutlineColorButton.Click += TextOutlineColorButton_Click;
        //
        // TextOutlineWidthLabel
        //
        TextOutlineWidthLabel.Anchor = AnchorStyles.Left;
        TextOutlineWidthLabel.AutoSize = true;
        TextOutlineWidthLabel.Name = "TextOutlineWidthLabel";
        TextOutlineWidthLabel.Text = "Edge width";
        //
        // TextOutlineWidthBox
        //
        TextOutlineWidthBox.Dock = DockStyle.Fill;
        TextOutlineWidthBox.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
        TextOutlineWidthBox.Name = "TextOutlineWidthBox";
        TextOutlineWidthBox.TabIndex = 16;
        TextOutlineWidthBox.ValueChanged += TextOutlineWidthBox_ValueChanged;
        //
        // RoutingLabel
        //
        RoutingLabel.Anchor = AnchorStyles.Left;
        RoutingLabel.AutoSize = true;
        RoutingLabel.Name = "RoutingLabel";
        RoutingLabel.Text = "Route";
        //
        // RoutingPropertyBox
        //
        RoutingPropertyBox.Dock = DockStyle.Fill;
        RoutingPropertyBox.DropDownStyle = ComboBoxStyle.DropDownList;
        RoutingPropertyBox.Name = "RoutingPropertyBox";
        RoutingPropertyBox.TabIndex = 12;
        RoutingPropertyBox.SelectedIndexChanged += RoutingPropertyBox_SelectedIndexChanged;
        //
        // ArrowsLabel
        //
        ArrowsLabel.Anchor = AnchorStyles.Left;
        ArrowsLabel.AutoSize = true;
        ArrowsLabel.Name = "ArrowsLabel";
        ArrowsLabel.Text = "Arrows";
        //
        // ArrowsPropertyBox
        //
        ArrowsPropertyBox.Dock = DockStyle.Fill;
        ArrowsPropertyBox.DropDownStyle = ComboBoxStyle.DropDownList;
        ArrowsPropertyBox.Name = "ArrowsPropertyBox";
        ArrowsPropertyBox.TabIndex = 13;
        ArrowsPropertyBox.SelectedIndexChanged += ArrowsPropertyBox_SelectedIndexChanged;
        //
        // ZoomLabel
        //
        ZoomLabel.Alignment = ToolStripItemAlignment.Right;
        ZoomLabel.IsLink = true;
        ZoomLabel.LinkBehavior = LinkBehavior.HoverUnderline;
        ZoomLabel.LinkColor = SystemColors.ControlText;
        ZoomLabel.Name = "ZoomLabel";
        ZoomLabel.Overflow = ToolStripItemOverflow.Never;
        ZoomLabel.ToolTipText = "Ctrl+wheel to zoom. Click, or Ctrl+0, for 100%";
        ZoomLabel.Click += ZoomLabel_Click;
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
        ClientSize = new Size(1280, 760);
        Controls.Add(Canvas);
        Controls.Add(PropertiesPanel);
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
        ((System.ComponentModel.ISupportInitialize)XBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)YBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)WidthBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)HeightBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)CornerRadiusBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)LineWidthBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)FontSizeBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)TextOutlineWidthBox).EndInit();
        PropertiesTable.ResumeLayout(false);
        PropertiesTable.PerformLayout();
        PropertiesPanel.ResumeLayout(false);
        PropertiesPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ContextMenuStrip SelectionMenu;
    private ToolStripMenuItem CopyStyleContextMenuItem;
    private ToolStripMenuItem PasteStyleContextMenuItem;
    private ToolStripSeparator GroupContextSeparator;
    private ToolStripMenuItem GroupContextMenuItem;
    private ToolStripMenuItem UngroupContextMenuItem;
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
    private ToolStripDropDownButton FileDropDownButton;
    private ToolStripMenuItem NewMenuItem;
    private ToolStripMenuItem OpenMenuItem;
    private ToolStripMenuItem SaveMenuItem;
    private ToolStripMenuItem SaveAsMenuItem;
    private ToolStripMenuItem ExportPdfMenuItem;
    private ToolStripSeparator ExitSeparator;
    private ToolStripMenuItem ExitMenuItem;
    private ToolStripSeparator FileSeparator;
    private ToolStripDropDownButton EditDropDownButton;
    private ToolStripSeparator GroupSeparator;
    private ToolStripMenuItem UndoMenuItem;
    private ToolStripMenuItem RedoMenuItem;
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
    private ToolStripDropDownButton TextOutlineDropDownButton;
    private Snipping.ColorPaletteControl TextOutlinePalette;
    private ToolStripControlHost TextOutlinePaletteHost;
    private ToolStripMenuItem NoTextOutlineMenuItem;
    private ToolStripMenuItem MoreTextOutlineColorsMenuItem;
    private ToolStripComboBox TextOutlineWidthComboBox;
    private ToolStripMenuItem CopyStyleMenuItem;
    private ToolStripMenuItem PasteStyleMenuItem;
    private ToolStripSeparator StyleSeparator;
    private ToolStripComboBox RoutingComboBox;
    private ToolStripComboBox ArrowHeadsComboBox;
    private ToolStripButton ResetRouteButton;
    private ToolStripSeparator ConnectorsSeparator;
    private ToolStripMenuItem GroupMenuItem;
    private ToolStripMenuItem UngroupMenuItem;
    private ToolStripButton FrontButton;
    private ToolStripButton BackButton;
    private ToolStripButton PropertiesButton;
    private ToolStripLabel ZoomLabel;
    private DiagramCanvasControl Canvas;
    private TextBox TextInputBox;
    private Panel PropertiesPanel;
    private TableLayoutPanel PropertiesTable;
    private Label PropertiesTitleLabel;
    private Label TextLabel;
    private TextBox TextPropertyBox;
    private Label XLabel;
    private NumericUpDown XBox;
    private Label YLabel;
    private NumericUpDown YBox;
    private Label WidthLabel;
    private NumericUpDown WidthBox;
    private Label HeightLabel;
    private NumericUpDown HeightBox;
    private Label CornerRadiusLabel;
    private NumericUpDown CornerRadiusBox;
    private Label FillLabel;
    private Button FillColorButton;
    private Label LineLabel;
    private Button LineColorButton;
    private Label LineWidthLabel;
    private NumericUpDown LineWidthBox;
    private CheckBox DashedCheckBox;
    private Label TextColorLabel;
    private Button TextColorButton;
    private Label FontFamilyLabel;
    private ComboBox FontFamilyBox;
    private Label FontSizeLabel;
    private NumericUpDown FontSizeBox;
    private Label TextOutlineLabel;
    private Button TextOutlineColorButton;
    private Label TextOutlineWidthLabel;
    private NumericUpDown TextOutlineWidthBox;
    private Label RoutingLabel;
    private ComboBox RoutingPropertyBox;
    private Label ArrowsLabel;
    private ComboBox ArrowsPropertyBox;
}
