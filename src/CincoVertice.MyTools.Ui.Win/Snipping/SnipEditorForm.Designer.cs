namespace CincoVertice.MyTools.Ui.Win.Snipping;

partial class SnipEditorForm
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

            foreach (Bitmap snip in _snips)
            {
                snip.Dispose();
            }

            _textFont?.Dispose();
            _colorSwatch?.Dispose();
            _textOutlineSwatch?.Dispose();

            foreach (Image image in _images)
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
        Tools = new ToolStrip();
        SelectButton = new ToolStripButton();
        PenButton = new ToolStripButton();
        HighlighterButton = new ToolStripButton();
        ArrowButton = new ToolStripButton();
        RectangleButton = new ToolStripButton();
        TextButton = new ToolStripButton();
        BlurButton = new ToolStripButton();
        ToolsSeparator = new ToolStripSeparator();
        ColorDropDownButton = new ToolStripDropDownButton();
        Palette = new ColorPaletteControl();
        PaletteHost = new ToolStripControlHost(Palette);
        MoreColorsMenuItem = new ToolStripMenuItem();
        SizeComboBox = new ToolStripComboBox();
        TransparencyComboBox = new ToolStripComboBox();
        ArrowHeadEndButton = new ToolStripButton();
        ArrowHeadStartButton = new ToolStripButton();
        ArrowHeadsBothButton = new ToolStripButton();
        CornerRadiusComboBox = new ToolStripComboBox();
        TextOutlineDropDownButton = new ToolStripDropDownButton();
        TextOutlinePalette = new ColorPaletteControl();
        TextOutlinePaletteHost = new ToolStripControlHost(TextOutlinePalette);
        MoreTextOutlineColorsMenuItem = new ToolStripMenuItem();
        TextOutlineWidthComboBox = new ToolStripComboBox();
        ColorsSeparator = new ToolStripSeparator();
        UndoButton = new ToolStripButton();
        RedoButton = new ToolStripButton();
        HistorySeparator = new ToolStripSeparator();
        CopyButton = new ToolStripButton();
        SaveButton = new ToolStripButton();
        SnipsSeparator = new ToolStripSeparator();
        PreviousSnipButton = new ToolStripButton();
        SnipPositionLabel = new ToolStripLabel();
        NextSnipButton = new ToolStripButton();
        OptionsDropDownButton = new ToolStripDropDownButton();
        HideWhileSnippingMenuItem = new ToolStripMenuItem();
        SelectionMenu = new ContextMenuStrip();
        ResetMenuItem = new ToolStripMenuItem();
        DeleteMenuItem = new ToolStripMenuItem();
        CanvasPanel = new Panel();
        Canvas = new PictureBox();
        TextInputBox = new TextBox();
        Tools.SuspendLayout();
        CanvasPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)Canvas).BeginInit();
        Canvas.SuspendLayout();
        SuspendLayout();
        //
        // Tools
        //
        Tools.GripStyle = ToolStripGripStyle.Hidden;
        Tools.Items.AddRange(new ToolStripItem[] { SelectButton, PenButton, HighlighterButton, ArrowButton, RectangleButton, TextButton, BlurButton, ToolsSeparator, ColorDropDownButton, SizeComboBox, TransparencyComboBox, ArrowHeadEndButton, ArrowHeadStartButton, ArrowHeadsBothButton, CornerRadiusComboBox, TextOutlineDropDownButton, TextOutlineWidthComboBox, ColorsSeparator, UndoButton, RedoButton, HistorySeparator, CopyButton, SaveButton, SnipsSeparator, PreviousSnipButton, SnipPositionLabel, NextSnipButton, OptionsDropDownButton });
        Tools.Location = new Point(0, 0);
        Tools.Name = "Tools";
        Tools.Padding = new Padding(4, 2, 4, 2);
        Tools.Size = new Size(800, 27);
        Tools.TabIndex = 0;
        //
        // SelectButton
        //
        SelectButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        SelectButton.Name = "SelectButton";
        SelectButton.Tag = "Select";
        SelectButton.Text = "Select";
        SelectButton.ToolTipText = "Select (V): click what's drawn, then drag it or its handles, arrow keys to move it, Delete to remove it, double-click a text to edit it";
        SelectButton.Click += ToolButton_Click;
        //
        // PenButton
        //
        PenButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        PenButton.Name = "PenButton";
        PenButton.Tag = "Pen";
        PenButton.Text = "Pen";
        PenButton.ToolTipText = "Pen (P)";
        PenButton.Click += ToolButton_Click;
        //
        // HighlighterButton
        //
        HighlighterButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        HighlighterButton.Name = "HighlighterButton";
        HighlighterButton.Tag = "Highlighter";
        HighlighterButton.Text = "Highlighter";
        HighlighterButton.ToolTipText = "Highlighter (H)";
        HighlighterButton.Click += ToolButton_Click;
        //
        // ArrowButton
        //
        ArrowButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        ArrowButton.Name = "ArrowButton";
        ArrowButton.Tag = "Arrow";
        ArrowButton.Text = "Arrow";
        ArrowButton.ToolTipText = "Arrow (A)";
        ArrowButton.Click += ToolButton_Click;
        //
        // RectangleButton
        //
        RectangleButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        RectangleButton.Name = "RectangleButton";
        RectangleButton.Tag = "Rectangle";
        RectangleButton.Text = "Rectangle";
        RectangleButton.ToolTipText = "Rounded rectangle (R)";
        RectangleButton.Click += ToolButton_Click;
        //
        // TextButton
        //
        TextButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        TextButton.Name = "TextButton";
        TextButton.Tag = "Text";
        TextButton.Text = "Text";
        TextButton.ToolTipText = "Text (T): click where it goes, Enter to end, Esc to end and select it, Shift+Enter for a new line";
        TextButton.Click += ToolButton_Click;
        //
        // BlurButton
        //
        BlurButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        BlurButton.Name = "BlurButton";
        BlurButton.Tag = "Blur";
        BlurButton.Text = "Blur";
        BlurButton.ToolTipText = "Blur (B): drag over what to hide. It can't be recovered from the copied or saved image";
        BlurButton.Click += ToolButton_Click;
        //
        // ToolsSeparator
        //
        ToolsSeparator.Name = "ToolsSeparator";
        //
        // ColorDropDownButton
        //
        ColorDropDownButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        ColorDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { PaletteHost, MoreColorsMenuItem });
        ColorDropDownButton.Name = "ColorDropDownButton";
        ColorDropDownButton.ToolTipText = "Color of this tool";
        //
        // Palette
        //
        Palette.Name = "Palette";
        Palette.ColorPicked += Palette_ColorPicked;
        //
        // PaletteHost
        //
        PaletteHost.AutoSize = true;
        PaletteHost.Name = "PaletteHost";
        //
        // MoreColorsMenuItem
        //
        MoreColorsMenuItem.Name = "MoreColorsMenuItem";
        MoreColorsMenuItem.Text = "More colors...";
        MoreColorsMenuItem.Click += MoreColorsMenuItem_Click;
        //
        // SizeComboBox
        //
        SizeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        SizeComboBox.Name = "SizeComboBox";
        SizeComboBox.Size = new Size(70, 23);
        SizeComboBox.ToolTipText = "Line width, or text size, in pixels (- and +)";
        SizeComboBox.SelectedIndexChanged += SizeComboBox_SelectedIndexChanged;
        // 
        // TransparencyComboBox
        // 
        TransparencyComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        TransparencyComboBox.Name = "TransparencyComboBox";
        TransparencyComboBox.Size = new Size(60, 23);
        TransparencyComboBox.ToolTipText = "Transparency";
        TransparencyComboBox.SelectedIndexChanged += TransparencyComboBox_SelectedIndexChanged;
        //
        // ArrowHeadEndButton
        //
        ArrowHeadEndButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        ArrowHeadEndButton.Name = "ArrowHeadEndButton";
        ArrowHeadEndButton.Tag = "End";
        ArrowHeadEndButton.Text = "Head at the end";
        ArrowHeadEndButton.ToolTipText = "Arrow head where you end the drag";
        ArrowHeadEndButton.Click += ArrowHeadsButton_Click;
        //
        // ArrowHeadStartButton
        //
        ArrowHeadStartButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        ArrowHeadStartButton.Name = "ArrowHeadStartButton";
        ArrowHeadStartButton.Tag = "Start";
        ArrowHeadStartButton.Text = "Head at the start";
        ArrowHeadStartButton.ToolTipText = "Arrow head where you start the drag";
        ArrowHeadStartButton.Click += ArrowHeadsButton_Click;
        //
        // ArrowHeadsBothButton
        //
        ArrowHeadsBothButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        ArrowHeadsBothButton.Name = "ArrowHeadsBothButton";
        ArrowHeadsBothButton.Tag = "Both";
        ArrowHeadsBothButton.Text = "Heads at both ends";
        ArrowHeadsBothButton.ToolTipText = "Arrow heads at both ends";
        ArrowHeadsBothButton.Click += ArrowHeadsButton_Click;
        //
        // CornerRadiusComboBox
        //
        CornerRadiusComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        CornerRadiusComboBox.Name = "CornerRadiusComboBox";
        CornerRadiusComboBox.Size = new Size(70, 23);
        CornerRadiusComboBox.ToolTipText = "How round the corners are";
        CornerRadiusComboBox.SelectedIndexChanged += CornerRadiusComboBox_SelectedIndexChanged;
        //
        // TextOutlineDropDownButton
        //
        TextOutlineDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { TextOutlinePaletteHost, MoreTextOutlineColorsMenuItem });
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
        // ColorsSeparator
        //
        ColorsSeparator.Name = "ColorsSeparator";
        //
        // UndoButton
        //
        UndoButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        UndoButton.Name = "UndoButton";
        UndoButton.Text = "Undo";
        UndoButton.ToolTipText = "Undo (Ctrl+Z)";
        UndoButton.Click += UndoButton_Click;
        //
        // RedoButton
        //
        RedoButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        RedoButton.Name = "RedoButton";
        RedoButton.Text = "Redo";
        RedoButton.ToolTipText = "Redo (Ctrl+Y)";
        RedoButton.Click += RedoButton_Click;
        //
        // HistorySeparator
        //
        HistorySeparator.Name = "HistorySeparator";
        //
        // CopyButton
        //
        CopyButton.Name = "CopyButton";
        CopyButton.Text = "Copy";
        CopyButton.ToolTipText = "Copy (Ctrl+C); every change is copied too";
        CopyButton.Click += CopyButton_Click;
        //
        // SaveButton
        //
        SaveButton.Name = "SaveButton";
        SaveButton.Text = "Save";
        SaveButton.ToolTipText = "Save as an image file (Ctrl+S)";
        SaveButton.Click += SaveButton_Click;
        //
        // SnipsSeparator
        //
        SnipsSeparator.Name = "SnipsSeparator";
        SnipsSeparator.Visible = false;
        //
        // PreviousSnipButton
        //
        PreviousSnipButton.Name = "PreviousSnipButton";
        PreviousSnipButton.Text = "<";
        PreviousSnipButton.ToolTipText = "Previous snip of this window (Alt+Left)";
        PreviousSnipButton.Visible = false;
        PreviousSnipButton.Click += PreviousSnipButton_Click;
        //
        // SnipPositionLabel
        //
        SnipPositionLabel.Name = "SnipPositionLabel";
        SnipPositionLabel.ToolTipText = "The snips stay here until this window closes";
        SnipPositionLabel.Visible = false;
        //
        // NextSnipButton
        //
        NextSnipButton.Name = "NextSnipButton";
        NextSnipButton.Text = ">";
        NextSnipButton.ToolTipText = "Next snip of this window (Alt+Right)";
        NextSnipButton.Visible = false;
        NextSnipButton.Click += NextSnipButton_Click;
        //
        // OptionsDropDownButton
        //
        OptionsDropDownButton.Alignment = ToolStripItemAlignment.Right;
        OptionsDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { HideWhileSnippingMenuItem });
        OptionsDropDownButton.Name = "OptionsDropDownButton";
        OptionsDropDownButton.Text = "Options";
        //
        // HideWhileSnippingMenuItem
        //
        HideWhileSnippingMenuItem.Name = "HideWhileSnippingMenuItem";
        HideWhileSnippingMenuItem.Text = "Leave this window out of new snips";
        HideWhileSnippingMenuItem.ToolTipText = "It stays on the screen, but isn't in the screenshot. Turn it off to snip the editor itself";
        HideWhileSnippingMenuItem.Click += HideWhileSnippingMenuItem_Click;
        //
        // SelectionMenu
        //
        SelectionMenu.Items.AddRange(new ToolStripItem[] { ResetMenuItem, DeleteMenuItem });
        SelectionMenu.Name = "SelectionMenu";
        //
        // ResetMenuItem
        //
        ResetMenuItem.Name = "ResetMenuItem";
        ResetMenuItem.Text = "Reset to original";
        ResetMenuItem.Click += ResetMenuItem_Click;
        //
        // DeleteMenuItem
        //
        DeleteMenuItem.Name = "DeleteMenuItem";
        DeleteMenuItem.ShortcutKeyDisplayString = "Del";
        DeleteMenuItem.Text = "Delete";
        DeleteMenuItem.Click += DeleteMenuItem_Click;
        //
        // CanvasPanel
        //
        CanvasPanel.AutoScroll = true;
        CanvasPanel.BackColor = Color.FromArgb(45, 45, 48);
        CanvasPanel.Controls.Add(Canvas);
        CanvasPanel.Dock = DockStyle.Fill;
        CanvasPanel.Location = new Point(0, 27);
        CanvasPanel.Name = "CanvasPanel";
        CanvasPanel.Size = new Size(800, 423);
        CanvasPanel.TabIndex = 1;
        //
        // Canvas
        //
        Canvas.Controls.Add(TextInputBox);
        Canvas.Cursor = Cursors.Cross;
        Canvas.Location = new Point(0, 0);
        Canvas.Name = "Canvas";
        Canvas.Size = new Size(100, 50);
        Canvas.SizeMode = PictureBoxSizeMode.AutoSize;
        Canvas.TabIndex = 0;
        Canvas.TabStop = false;
        Canvas.Paint += Canvas_Paint;
        Canvas.MouseDown += Canvas_MouseDown;
        Canvas.MouseMove += Canvas_MouseMove;
        Canvas.MouseUp += Canvas_MouseUp;
        Canvas.MouseDoubleClick += Canvas_MouseDoubleClick;
        //
        // TextInputBox
        //
        TextInputBox.AcceptsReturn = true;
        TextInputBox.BorderStyle = BorderStyle.None;
        TextInputBox.Cursor = Cursors.IBeam;
        TextInputBox.Location = new Point(0, 0);
        TextInputBox.Multiline = true;
        TextInputBox.Name = "TextInputBox";
        TextInputBox.Size = new Size(100, 23);
        TextInputBox.TabIndex = 0;
        TextInputBox.Visible = false;
        TextInputBox.WordWrap = false;
        TextInputBox.TextChanged += TextInputBox_TextChanged;
        TextInputBox.KeyDown += TextInputBox_KeyDown;
        TextInputBox.Leave += TextInputBox_Leave;
        //
        // SnipEditorForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Controls.Add(CanvasPanel);
        Controls.Add(Tools);
        KeyPreview = true;
        MinimumSize = new Size(480, 200);
        Name = "SnipEditorForm";
        StartPosition = FormStartPosition.Manual;
        Text = "Snip editor";
        FormClosing += SnipEditorForm_FormClosing;
        KeyDown += SnipEditorForm_KeyDown;
        Tools.ResumeLayout(false);
        Tools.PerformLayout();
        CanvasPanel.ResumeLayout(false);
        CanvasPanel.PerformLayout();
        Canvas.ResumeLayout(false);
        Canvas.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)Canvas).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ToolStrip Tools;
    private ToolStripButton SelectButton;
    private ToolStripButton PenButton;
    private ToolStripButton HighlighterButton;
    private ToolStripButton ArrowButton;
    private ToolStripButton RectangleButton;
    private ToolStripButton TextButton;
    private ToolStripButton BlurButton;
    private ToolStripSeparator ToolsSeparator;
    private ToolStripDropDownButton ColorDropDownButton;
    private ColorPaletteControl Palette;
    private ToolStripControlHost PaletteHost;
    private ToolStripMenuItem MoreColorsMenuItem;
    private ToolStripComboBox SizeComboBox;
    private ToolStripComboBox TransparencyComboBox;
    private ToolStripButton ArrowHeadEndButton;
    private ToolStripButton ArrowHeadStartButton;
    private ToolStripButton ArrowHeadsBothButton;
    private ToolStripComboBox CornerRadiusComboBox;
    private ToolStripDropDownButton TextOutlineDropDownButton;
    private ColorPaletteControl TextOutlinePalette;
    private ToolStripControlHost TextOutlinePaletteHost;
    private ToolStripMenuItem MoreTextOutlineColorsMenuItem;
    private ToolStripComboBox TextOutlineWidthComboBox;
    private ToolStripSeparator ColorsSeparator;
    private ToolStripButton UndoButton;
    private ToolStripButton RedoButton;
    private ToolStripSeparator HistorySeparator;
    private ToolStripButton CopyButton;
    private ToolStripButton SaveButton;
    private ToolStripSeparator SnipsSeparator;
    private ToolStripButton PreviousSnipButton;
    private ToolStripLabel SnipPositionLabel;
    private ToolStripButton NextSnipButton;
    private ToolStripDropDownButton OptionsDropDownButton;
    private ToolStripMenuItem HideWhileSnippingMenuItem;
    private ContextMenuStrip SelectionMenu;
    private ToolStripMenuItem ResetMenuItem;
    private ToolStripMenuItem DeleteMenuItem;
    private Panel CanvasPanel;
    private PictureBox Canvas;
    private TextBox TextInputBox;
}
