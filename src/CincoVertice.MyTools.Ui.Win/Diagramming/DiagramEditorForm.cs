using System.ComponentModel;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.Diagramming.Services;

namespace CincoVertice.MyTools.Ui.Win.Diagramming;

/// <summary>
///     Diagram editor view: the canvas, with the shapes on a bar at the left and the style and file commands on a bar
///     at the top. <see cref="Ui.Diagramming.Presenters.DiagramEditorPresenter"/> does the work; the form only shows
///     what it's given and passes on what the user does.
/// </summary>
public partial class DiagramEditorForm : Form, IDiagramEditorView
{
    private const string WindowTitle = "Diagram editor";

    // How many pixels the arrow keys move the selection; with Shift, one
    private const int NudgeStep = 10;

    private readonly int _iconSize;
    private readonly List<Image> _images = [];

    // The color buttons' images: the current colors, replaced when they change
    private readonly Dictionary<ToolStripDropDownButton, Image> _swatches = [];

    private DiagramSceneModel? _scene;
    private IReadOnlyList<int> _lineWidths = [];
    private IReadOnlyList<int> _fontSizes = [];
    private IReadOnlyList<int> _textOutlineWidths = [];

    // Set while the toolbar is filled from the presenter, so that isn't taken as the user's choice
    private bool _isShowingChoices;

    // True while the text box holds a text the presenter is waiting for
    private bool _isEditingText;

    // The text box's font, made here for the size asked for
    private Font? _textFont;

    // Held with a drag to move the view instead
    private bool _isSpaceDown;

    public DiagramEditorForm()
    {
        InitializeComponent();

        // 16 pixels at 100% scaling, drawn at the screen's size rather than stretched, so they stay sharp
        _iconSize = (int)Math.Round(16 * DeviceDpi / 96.0);
        ToolsStrip.ImageScalingSize = new Size(_iconSize, _iconSize);
        StyleStrip.ImageScalingSize = new Size(_iconSize, _iconSize);

        foreach (ToolStripButton button in ToolButtons())
        {
            button.Image = Keep(DiagramToolIcons.Create(ToolOf(button), _iconSize));
        }

        foreach (ToolStripDropDownButton button in ColorButtons())
        {
            ((ToolStripDropDownMenu)button.DropDown).ShowImageMargin = false;
        }

        string[] routings = ["Elbow", "Straight"];
        string[] arrows = ["Arrow at end", "Arrow at start", "Arrows at both", "No arrows"];

        RoutingComboBox.Items.AddRange(routings);
        ArrowHeadsComboBox.Items.AddRange(arrows);
        RoutingPropertyBox.Items.AddRange(routings);
        ArrowsPropertyBox.Items.AddRange(arrows);
        DirectionPropertyBox.Items.AddRange(["Free", "Horizontal", "Vertical"]);
        FontFamilyBox.Items.AddRange([.. DiagramFonts.Names()]);

        // Windows takes drops only on a single-threaded apartment, as the app's UI thread is; elsewhere, such as a
        // test, it would show an error when the canvas is created
        Canvas.AllowDrop = Thread.CurrentThread.GetApartmentState() == ApartmentState.STA;

        FormExtensions.CenterOnCursorScreen(this);
        FormExtensions.ActivateWhenShown(this);
    }

    public event EventHandler<DiagramPointerModel>? PointerPressed;

    public event EventHandler<DiagramPointerModel>? PointerMoved;

    public event EventHandler<DiagramPointerModel>? PointerReleased;

    public event EventHandler<DiagramPointerModel>? PointerDoubleClicked;

    public event EventHandler<Point>? PointerRightClicked;

    public event EventHandler? PointerLeft;

    public event EventHandler<DiagramWheelModel>? WheelTurned;

    public event EventHandler<DiagramTool>? ToolSelected;

    public event EventHandler<DiagramCommand>? CommandRequested;

    public event EventHandler<Size>? NudgeRequested;

    public event EventHandler<DiagramColorChoiceModel>? ColorSelected;

    public event EventHandler<DiagramColorTarget>? MoreColorsRequested;

    public event EventHandler<int>? LineWidthSelected;

    public event EventHandler<bool>? DashedSelected;

    public event EventHandler<int>? FontSizeSelected;

    public event EventHandler<string>? FontFamilySelected;

    public event EventHandler<int>? TextOutlineWidthSelected;

    public event EventHandler<DiagramRouting>? RoutingSelected;

    public event EventHandler<DiagramArrowHeads>? ArrowHeadsSelected;

    public event EventHandler<DiagramLineDirection>? LineDirectionSelected;

    public event EventHandler<DiagramFilesDropModel>? FilesDropped;

    public event EventHandler<Rectangle>? BoundsEdited;

    public event EventHandler<DiagramLockChoiceModel>? LockToggled;

    public event EventHandler<int>? CornerRadiusEdited;

    public event EventHandler<string>? TextEdited;

    public event EventHandler<string>? TextCommitted;

    public event EventHandler<bool>? PropertiesPanelToggled;

    public event EventHandler<DiagramWindowSizeModel>? WindowClosed;

    event EventHandler<CancelEventArgs>? IDiagramEditorView.Closing
    {
        add => _closing += value;
        remove => _closing -= value;
    }

    private event EventHandler<CancelEventArgs>? _closing;

    public void ShowScene(DiagramSceneModel scene)
    {
        _scene = scene;
        Canvas.Invalidate();
    }

    public void ShowTool(DiagramTool tool)
    {
        foreach (ToolStripButton button in ToolButtons())
        {
            button.Checked = ToolOf(button) == tool;
        }
    }

    public void ShowPalette(IReadOnlyList<Color> colors)
    {
        FillPalette.Colors = colors;
        LinePalette.Colors = colors;
        TextPalette.Colors = colors;
        TextOutlinePalette.Colors = colors;
    }

    public void ShowSizes(IReadOnlyList<int> lineWidths, IReadOnlyList<int> fontSizes, IReadOnlyList<int> textOutlineWidths)
    {
        _lineWidths = lineWidths;
        _fontSizes = fontSizes;
        _textOutlineWidths = textOutlineWidths;
        _isShowingChoices = true;
        LineWidthComboBox.Items.AddRange([.. lineWidths.Select(width => $"{width} px")]);
        FontSizeComboBox.Items.AddRange([.. fontSizes.Select(size => $"{size} px")]);
        TextOutlineWidthComboBox.Items.AddRange(
            [.. textOutlineWidths.Select(width => width == 0 ? "No edge" : $"{width} px")]);
        _isShowingChoices = false;
    }

    public void ShowStyle(DiagramStyleModel style, DiagramRouting routing, DiagramArrowHeads heads)
    {
        _isShowingChoices = true;

        FillPalette.SelectedColor = style.Fill;
        LinePalette.SelectedColor = style.Line;
        TextPalette.SelectedColor = style.TextColor;
        // With no edge, its swatch shows no color, as no fill does; the color is kept for when it's turned on
        Color textOutline = style.TextOutlineWidth == 0 ? Color.Empty : style.TextOutlineColor;
        TextOutlinePalette.SelectedColor = textOutline;
        ShowSwatch(FillDropDownButton, style.Fill);
        ShowSwatch(LineDropDownButton, style.Line);
        ShowSwatch(TextColorDropDownButton, style.TextColor);
        ShowSwatch(TextOutlineDropDownButton, textOutline);

        LineWidthComboBox.SelectedIndex = _lineWidths.ToList().IndexOf(style.LineWidth);
        FontSizeComboBox.SelectedIndex = _fontSizes.ToList().IndexOf(style.FontSize);
        TextOutlineWidthComboBox.SelectedIndex = _textOutlineWidths.ToList().IndexOf(style.TextOutlineWidth);
        DashedButton.Checked = style.Dashed;
        RoutingComboBox.SelectedIndex = routing == DiagramRouting.Elbow ? 0 : 1;
        ArrowHeadsComboBox.SelectedIndex = (int)heads;

        _isShowingChoices = false;
    }

    public void ShowPropertiesPanel(bool isVisible)
    {
        PropertiesPanel.Visible = isVisible;
        PropertiesButton.Checked = isVisible;
    }

    public void ShowWindowSize(DiagramWindowSizeModel size)
    {
        // It may have been saved on a larger screen than this one
        Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;

        Size = new Size(Math.Min(size.Size.Width, area.Width), Math.Min(size.Size.Height, area.Height));
        FormExtensions.CenterOnCursorScreen(this);
        WindowState = size.IsMaximized ? FormWindowState.Maximized : FormWindowState.Normal;
    }

    public void ShowProperties(DiagramPropertiesModel properties)
    {
        _isShowingChoices = true;
        PropertiesTable.SuspendLayout();

        PropertiesTitleLabel.Text = properties.Title;

        bool hasBounds = properties.Bounds is not null;
        Rectangle bounds = properties.Bounds ?? Rectangle.Empty;

        ShowNumber(XLabel, XBox, hasBounds ? bounds.X : null);
        ShowNumber(YLabel, YBox, hasBounds ? bounds.Y : null);
        ShowNumber(WidthLabel, WidthBox, hasBounds ? bounds.Width : null);
        ShowNumber(HeightLabel, HeightBox, hasBounds ? bounds.Height : null);
        ShowLock(XLockCheckBox, XBox, properties.Locks, DiagramLock.X);
        ShowLock(YLockCheckBox, YBox, properties.Locks, DiagramLock.Y);
        ShowLock(WidthLockCheckBox, WidthBox, properties.Locks, DiagramLock.Width);
        ShowLock(HeightLockCheckBox, HeightBox, properties.Locks, DiagramLock.Height);
        ProportionsCheckBox.Visible = properties.Locks is not null;
        ProportionsCheckBox.Checked = properties.Locks is DiagramLock locks && locks.HasFlag(DiagramLock.AspectRatio);
        ResetSizeButton.Visible = properties.HasImages;
        ShowNumber(CornerRadiusLabel, CornerRadiusBox, properties.CornerRadius);
        ShowNumber(LineWidthLabel, LineWidthBox, properties.LineWidth);
        ShowNumber(FontSizeLabel, FontSizeBox, properties.FontSize);
        ShowNumber(TextOutlineWidthLabel, TextOutlineWidthBox, properties.TextOutlineWidth);

        ShowColor(FillLabel, FillColorButton, properties.Fill);
        ShowColor(LineLabel, LineColorButton, properties.Line);
        ShowColor(TextColorLabel, TextColorButton, properties.TextColor);
        ShowColor(
            TextOutlineLabel,
            TextOutlineColorButton,
            properties.TextOutlineWidth == 0 ? Color.Empty : properties.TextOutlineColor);

        // A connector has a line where a shape has a border
        LineLabel.Text = hasBounds ? "Border" : "Line";
        LineWidthLabel.Text = hasBounds ? "Border width" : "Line width";

        DashedCheckBox.Visible = properties.Dashed is not null;
        DashedCheckBox.Checked = properties.Dashed ?? false;

        TextLabel.Visible = properties.Text is not null;
        TextPropertyBox.Visible = properties.Text is not null;
        TextLabel.Text = hasBounds ? "Text" : "Label";

        if (!TextPropertyBox.Focused)
        {
            TextPropertyBox.Text = properties.Text?.ReplaceLineEndings(Environment.NewLine) ?? string.Empty;
        }

        FontFamilyLabel.Visible = properties.FontFamily is not null;
        FontFamilyBox.Visible = properties.FontFamily is not null;

        if (properties.FontFamily is string fontFamily)
        {
            // A diagram from another computer can have a font this one doesn't
            int index = FontFamilyBox.FindStringExact(fontFamily);

            FontFamilyBox.SelectedIndex = index >= 0 ? index : FontFamilyBox.Items.Add(fontFamily);
        }

        RoutingLabel.Visible = properties.Routing is not null;
        RoutingPropertyBox.Visible = properties.Routing is not null;
        RoutingPropertyBox.SelectedIndex = properties.Routing == DiagramRouting.Straight ? 1 : 0;

        ArrowsLabel.Visible = properties.Heads is not null;
        ArrowsPropertyBox.Visible = properties.Heads is not null;
        ArrowsPropertyBox.SelectedIndex = (int)(properties.Heads ?? DiagramArrowHeads.End);

        DirectionLabel.Visible = properties.LineDirection is not null;
        DirectionPropertyBox.Visible = properties.LineDirection is not null;
        DirectionPropertyBox.SelectedIndex = (int)(properties.LineDirection ?? DiagramLineDirection.Free);

        PropertiesTable.ResumeLayout();
        _isShowingChoices = false;
    }

    public void ShowContextMenu(Point location, DiagramContextMenuModel menu)
    {
        PasteStyleContextMenuItem.Enabled = menu.CanPasteStyle;
        GroupContextMenuItem.Enabled = menu.CanGroup;
        UngroupContextMenuItem.Enabled = menu.CanUngroup;
        SelectionMenu.Show(Canvas, location);
    }

    public void ShowUndoRedo(bool canUndo, bool canRedo)
    {
        UndoMenuItem.Enabled = canUndo;
        RedoMenuItem.Enabled = canRedo;
    }

    public void ShowDocument(string name, bool hasChanges)
    {
        Text = $"{name}{(hasChanges ? "*" : string.Empty)} - {WindowTitle}";
    }

    public void ShowZoom(int percent)
    {
        ZoomLabel.Text = $"{percent}%";
    }

    public void ShowCursor(DiagramCursor cursor)
    {
        Canvas.Cursor = cursor switch
        {
            DiagramCursor.Cross => Cursors.Cross,
            DiagramCursor.Move => Cursors.SizeAll,
            DiagramCursor.Pan => Cursors.Hand,
            DiagramCursor.SizeVertical => Cursors.SizeNS,
            DiagramCursor.SizeHorizontal => Cursors.SizeWE,
            DiagramCursor.SizeTopLeftBottomRight => Cursors.SizeNWSE,
            DiagramCursor.SizeTopRightBottomLeft => Cursors.SizeNESW,
            _ => Cursors.Default,
        };
    }

    public void BeginTextEdit(Rectangle bounds, string text, int fontSize, string fontFamily, Color color)
    {
        Font? oldFont = _textFont;

        _textFont = DiagramFonts.Create(fontFamily, fontSize);
        TextInputBox.Font = _textFont;
        oldFont?.Dispose();
        TextInputBox.ForeColor = color;

        // A light text on a light box couldn't be read while typing
        TextInputBox.BackColor = color.GetBrightness() > 0.7f ? Color.FromArgb(64, 64, 64) : Color.White;
        TextInputBox.Text = text.ReplaceLineEndings(Environment.NewLine);

        // A small shape still gets a box large enough to type in
        TextInputBox.Bounds = new Rectangle(
            bounds.Location,
            new Size(Math.Max(bounds.Width, fontSize * 6), Math.Max(bounds.Height, fontSize * 2)));
        TextInputBox.Visible = true;
        TextInputBox.SelectAll();
        TextInputBox.Focus();
        _isEditingText = true;
    }

    public Color? AskColor(Color current)
    {
        using var dialog = new ColorDialog { AnyColor = true, Color = current, FullOpen = true };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.Color : null;
    }

    public string? AskSavePath(string suggestedFileName)
    {
        using var dialog = new SaveFileDialog
        {
            DefaultExt = "svg",
            FileName = suggestedFileName,
            Filter = "SVG image (*.svg)|*.svg",
            Title = "Save diagram",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public string? AskExportPdfPath(string suggestedFileName)
    {
        using var dialog = new SaveFileDialog
        {
            DefaultExt = "pdf",
            FileName = suggestedFileName,
            Filter = "PDF document (*.pdf)|*.pdf",
            Title = "Export as PDF",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public string? AskOpenPath()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "SVG image (*.svg)|*.svg",
            Title = "Open diagram",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public IReadOnlyList<string> AskImagePaths()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "SVG image (*.svg)|*.svg",
            Multiselect = true,
            Title = "Insert image",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileNames : [];
    }

    public bool AskOverwrite(string name)
    {
        DialogResult answer = MessageBox.Show(
            this,
            $"{name} was changed elsewhere since it was opened here. Replace it with this diagram?",
            WindowTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        return answer == DialogResult.Yes;
    }

    public DiagramSaveChoice AskSaveChanges(string name)
    {
        DialogResult answer = MessageBox.Show(
            this,
            $"Save the changes to {name}?",
            WindowTitle,
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        return answer switch
        {
            DialogResult.Yes => DiagramSaveChoice.Save,
            DialogResult.No => DiagramSaveChoice.Discard,
            _ => DiagramSaveChoice.Cancel,
        };
    }

    public void ShowError(string message)
    {
        MessageBox.Show(this, message, WindowTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public void SetClipboardText(string text)
    {
        Clipboard.SetText(text);
    }

    public string? GetClipboardText() => Clipboard.ContainsText() ? Clipboard.GetText() : null;

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // An open list closes itself on Esc
        if (keyData != Keys.Escape
            || LineWidthComboBox.DroppedDown
            || FontSizeComboBox.DroppedDown
            || TextOutlineWidthComboBox.DroppedDown
            || RoutingComboBox.DroppedDown
            || ArrowHeadsComboBox.DroppedDown
            || RoutingPropertyBox.DroppedDown
            || ArrowsPropertyBox.DroppedDown
            || DirectionPropertyBox.DroppedDown
            || FontFamilyBox.DroppedDown)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Esc never reaches the text box's KeyDown: it is a dialog key. While typing, it only drops the text
        if (_isEditingText)
        {
            CancelText();
        }
        else if (PropertiesPanel.ContainsFocus)
        {
            // Back to the canvas, which also takes what was typed in the field
            Canvas.Focus();
        }
        else
        {
            CommandRequested?.Invoke(this, DiagramCommand.Cancel);
        }

        return true;
    }

    private static DiagramTool ToolOf(ToolStripButton button) => Enum.Parse<DiagramTool>((string)button.Tag!);

    private static DiagramCommand CommandOf(object? button) =>
        Enum.Parse<DiagramCommand>((string)((ToolStripItem)button!).Tag!);

    private static Bitmap CreateSwatch(Color color, int size)
    {
        var swatch = new Bitmap(size, size);
        int side = size - 2;

        using var graphics = Graphics.FromImage(swatch);

        if (color.IsEmpty)
        {
            // No fill: a red line through an empty square
            graphics.FillRectangle(Brushes.White, 1, 1, side, side);
            graphics.DrawLine(Pens.Red, 1, side, side, 1);
        }
        else
        {
            using var brush = new SolidBrush(color);

            graphics.FillRectangle(brush, 1, 1, side, side);
        }

        graphics.DrawRectangle(Pens.Gray, 1, 1, side - 1, side - 1);

        return swatch;
    }

    private ToolStripButton[] ToolButtons() =>
    [
        SelectButton,
        ConnectorButton,
        LineButton,
        RectangleButton,
        RoundedRectangleButton,
        EllipseButton,
        DiamondButton,
        ParallelogramButton,
        HexagonButton,
        CylinderButton,
        DocumentButton,
        TextButton,
    ];

    private ToolStripDropDownButton[] ColorButtons() =>
        [FillDropDownButton, LineDropDownButton, TextColorDropDownButton, TextOutlineDropDownButton];

    private Image Keep(Image image)
    {
        _images.Add(image);

        return image;
    }

    private void ShowSwatch(ToolStripDropDownButton button, Color color)
    {
        _swatches.TryGetValue(button, out Image? old);

        Image swatch = CreateSwatch(color, _iconSize);

        _swatches[button] = swatch;
        button.Image = swatch;
        old?.Dispose();
    }

    private DiagramPointerModel PointerOf(MouseEventArgs e) => new(
        e.Location,
        (ModifierKeys & Keys.Shift) != 0,
        (ModifierKeys & Keys.Control) != 0,
        (ModifierKeys & Keys.Alt) != 0,
        e.Button == MouseButtons.Middle || _isSpaceDown);

    /// <summary>A number, kept in the box's range, which would otherwise throw; null hides the field.</summary>
    private static void ShowNumber(Label label, NumericUpDown box, int? value)
    {
        label.Visible = value is not null;
        box.Visible = value is not null;

        if (value is int number)
        {
            box.Value = Math.Clamp(number, box.Minimum, box.Maximum);
        }
    }

    /// <summary>
    ///     A lock beside its field, which can't be typed in while it's locked; null, with no shape selected, hides it.
    /// </summary>
    private static void ShowLock(CheckBox lockBox, NumericUpDown box, DiagramLock? locks, DiagramLock which)
    {
        bool isLocked = locks is DiagramLock shown && shown.HasFlag(which);

        lockBox.Visible = locks is not null;
        lockBox.Checked = isLocked;
        lockBox.Text = LockGlyph(isLocked);
        box.Enabled = !isLocked;
    }

    private static string LockGlyph(bool isLocked) => isLocked ? "\U0001F512" : "\U0001F513";

    /// <summary>A color on the button; null hides the field, and an empty color reads as none.</summary>
    private static void ShowColor(Label label, Button button, Color? color)
    {
        label.Visible = color is not null;
        button.Visible = color is not null;

        if (color is Color shown)
        {
            button.BackColor = shown.IsEmpty ? SystemColors.Control : shown;
            button.Text = shown.IsEmpty ? "None" : string.Empty;
        }
    }

    /// <summary>
    ///     The same choices as the top bar's color, below the panel's button. The menu belongs to the top bar's button,
    ///     and a menu with an owner opens under it, so it's lent without its owner until it closes.
    /// </summary>
    private static void ShowColorMenu(ToolStripDropDownButton colors, Control button)
    {
        ToolStripDropDown menu = colors.DropDown;

        if (menu.Visible)
        {
            menu.Close();
        }

        void GiveBack(object? sender, ToolStripDropDownClosedEventArgs e)
        {
            menu.Closed -= GiveBack;
            menu.OwnerItem = colors;
        }

        menu.OwnerItem = null;
        menu.Closed += GiveBack;
        menu.Show(button, new Point(0, button.Height));
    }

    private void PropertiesButton_Click(object? sender, EventArgs e)
    {
        ToggleProperties();
    }

    private void ToggleProperties()
    {
        ShowPropertiesPanel(!PropertiesPanel.Visible);
        PropertiesPanelToggled?.Invoke(this, PropertiesPanel.Visible);
    }

    private void BoundsBox_ValueChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            BoundsEdited?.Invoke(
                this,
                new Rectangle((int)XBox.Value, (int)YBox.Value, (int)WidthBox.Value, (int)HeightBox.Value));
        }
    }

    private void LockCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        var lockBox = (CheckBox)sender!;

        lockBox.Text = LockGlyph(lockBox.Checked);

        if (!_isShowingChoices)
        {
            LockToggled?.Invoke(
                this,
                new DiagramLockChoiceModel(Enum.Parse<DiagramLock>((string)lockBox.Tag!), lockBox.Checked));
        }
    }

    private void ProportionsCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            LockToggled?.Invoke(
                this,
                new DiagramLockChoiceModel(DiagramLock.AspectRatio, ProportionsCheckBox.Checked));
        }
    }

    private void ResetSizeButton_Click(object? sender, EventArgs e)
    {
        Canvas.Focus();
        CommandRequested?.Invoke(this, DiagramCommand.ResetSize);
    }

    private void CornerRadiusBox_ValueChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            CornerRadiusEdited?.Invoke(this, (int)CornerRadiusBox.Value);
        }
    }

    private void LineWidthBox_ValueChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            LineWidthSelected?.Invoke(this, (int)LineWidthBox.Value);
        }
    }

    private void FontSizeBox_ValueChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            FontSizeSelected?.Invoke(this, (int)FontSizeBox.Value);
        }
    }

    private void TextOutlineWidthBox_ValueChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            TextOutlineWidthSelected?.Invoke(this, (int)TextOutlineWidthBox.Value);
        }
    }

    private void FontFamilyBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && FontFamilyBox.SelectedItem is string fontFamily)
        {
            FontFamilySelected?.Invoke(this, fontFamily);
        }
    }

    private void DashedCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            DashedSelected?.Invoke(this, DashedCheckBox.Checked);
        }
    }

    private void RoutingPropertyBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && RoutingPropertyBox.SelectedIndex >= 0)
        {
            RoutingSelected?.Invoke(
                this,
                RoutingPropertyBox.SelectedIndex == 0 ? DiagramRouting.Elbow : DiagramRouting.Straight);
        }
    }

    private void ArrowsPropertyBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && ArrowsPropertyBox.SelectedIndex >= 0)
        {
            ArrowHeadsSelected?.Invoke(this, (DiagramArrowHeads)ArrowsPropertyBox.SelectedIndex);
        }
    }

    private void DirectionPropertyBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && DirectionPropertyBox.SelectedIndex >= 0)
        {
            LineDirectionSelected?.Invoke(this, (DiagramLineDirection)DirectionPropertyBox.SelectedIndex);
        }
    }

    private void FillColorButton_Click(object? sender, EventArgs e)
    {
        ShowColorMenu(FillDropDownButton, FillColorButton);
    }

    private void LineColorButton_Click(object? sender, EventArgs e)
    {
        ShowColorMenu(LineDropDownButton, LineColorButton);
    }

    private void TextColorButton_Click(object? sender, EventArgs e)
    {
        ShowColorMenu(TextColorDropDownButton, TextColorButton);
    }

    private void TextOutlineColorButton_Click(object? sender, EventArgs e)
    {
        ShowColorMenu(TextOutlineDropDownButton, TextOutlineColorButton);
    }

    private void TextPropertyBox_KeyDown(object? sender, KeyEventArgs e)
    {
        // Enter ends the text; Shift+Enter starts a new line
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            TextEdited?.Invoke(this, TextPropertyBox.Text);
        }
    }

    private void TextPropertyBox_Leave(object? sender, EventArgs e)
    {
        if (!_isShowingChoices)
        {
            TextEdited?.Invoke(this, TextPropertyBox.Text);
        }
    }

    private void CommitText()
    {
        if (!_isEditingText)
        {
            return;
        }

        // Cleared first: hiding the focused box raises Leave, which commits again
        _isEditingText = false;
        string text = TextInputBox.Text;
        TextInputBox.Visible = false;
        Canvas.Focus();

        TextCommitted?.Invoke(this, text);
    }

    private void CancelText()
    {
        _isEditingText = false;
        TextInputBox.Visible = false;
        Canvas.Focus();
    }

    private void ToolButton_Click(object? sender, EventArgs e)
    {
        CommitText();
        ToolSelected?.Invoke(this, ToolOf((ToolStripButton)sender!));
        Canvas.Focus();
    }

    private void CommandButton_Click(object? sender, EventArgs e)
    {
        CommitText();

        // First: a toolbar button doesn't take the focus, and leaving a panel field is what applies what was typed
        Canvas.Focus();
        CommandRequested?.Invoke(this, CommandOf(sender));
    }

    // Closing asks about unsaved changes, as the window's X does
    private void ExitMenuItem_Click(object? sender, EventArgs e) => Close();

    private void ZoomLabel_Click(object? sender, EventArgs e)
    {
        CommitText();
        Canvas.Focus();
        CommandRequested?.Invoke(this, DiagramCommand.ZoomReset);
    }

    private void FillPalette_ColorPicked(object? sender, Color color)
    {
        // Not HideDropDown: the panel's color buttons show this same menu themselves
        FillDropDownButton.DropDown.Close();
        Canvas.Focus();
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.Fill, color));
    }

    private void LinePalette_ColorPicked(object? sender, Color color)
    {
        LineDropDownButton.DropDown.Close();
        Canvas.Focus();
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.Line, color));
    }

    private void TextPalette_ColorPicked(object? sender, Color color)
    {
        TextColorDropDownButton.DropDown.Close();
        Canvas.Focus();
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.Text, color));
    }

    private void TextOutlinePalette_ColorPicked(object? sender, Color color)
    {
        TextOutlineDropDownButton.DropDown.Close();
        Canvas.Focus();
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.TextOutline, color));
    }

    private void NoFillMenuItem_Click(object? sender, EventArgs e)
    {
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.Fill, Color.Empty));
    }

    private void NoTextOutlineMenuItem_Click(object? sender, EventArgs e)
    {
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.TextOutline, Color.Empty));
    }

    private void MoreFillColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreColorsRequested?.Invoke(this, DiagramColorTarget.Fill);
    }

    private void MoreLineColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreColorsRequested?.Invoke(this, DiagramColorTarget.Line);
    }

    private void MoreTextColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreColorsRequested?.Invoke(this, DiagramColorTarget.Text);
    }

    private void MoreTextOutlineColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreColorsRequested?.Invoke(this, DiagramColorTarget.TextOutline);
    }

    private void TextOutlineWidthComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && TextOutlineWidthComboBox.SelectedIndex >= 0)
        {
            TextOutlineWidthSelected?.Invoke(this, _textOutlineWidths[TextOutlineWidthComboBox.SelectedIndex]);
            Canvas.Focus();
        }
    }

    private void LineWidthComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && LineWidthComboBox.SelectedIndex >= 0)
        {
            LineWidthSelected?.Invoke(this, _lineWidths[LineWidthComboBox.SelectedIndex]);
            Canvas.Focus();
        }
    }

    private void FontSizeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && FontSizeComboBox.SelectedIndex >= 0)
        {
            FontSizeSelected?.Invoke(this, _fontSizes[FontSizeComboBox.SelectedIndex]);
            Canvas.Focus();
        }
    }

    private void RoutingComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && RoutingComboBox.SelectedIndex >= 0)
        {
            RoutingSelected?.Invoke(
                this,
                RoutingComboBox.SelectedIndex == 0 ? DiagramRouting.Elbow : DiagramRouting.Straight);
            Canvas.Focus();
        }
    }

    private void ArrowHeadsComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && ArrowHeadsComboBox.SelectedIndex >= 0)
        {
            ArrowHeadsSelected?.Invoke(this, (DiagramArrowHeads)ArrowHeadsComboBox.SelectedIndex);
            Canvas.Focus();
        }
    }

    private void DashedButton_Click(object? sender, EventArgs e)
    {
        DashedSelected?.Invoke(this, !DashedButton.Checked);
        Canvas.Focus();
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        if (_scene is not null)
        {
            DiagramRenderer.Draw(e.Graphics, _scene, Canvas.ClientSize);
        }
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        // Clicking the canvas doesn't take the focus from the text box by itself
        CommitText();
        Canvas.Focus();

        if (e.Button is MouseButtons.Left or MouseButtons.Middle)
        {
            PointerPressed?.Invoke(this, PointerOf(e));
        }
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        PointerMoved?.Invoke(this, PointerOf(e));
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button is MouseButtons.Left or MouseButtons.Middle)
        {
            PointerReleased?.Invoke(this, PointerOf(e));
        }
        else if (e.Button == MouseButtons.Right)
        {
            PointerRightClicked?.Invoke(this, e.Location);
        }
    }

    private void Canvas_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            PointerDoubleClicked?.Invoke(this, PointerOf(e));
        }
    }

    /// <summary>Files can be dropped; what they are is up to the presenter.</summary>
    private void Canvas_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void Canvas_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths)
        {
            // From another program, which keeps waiting while a message box is shown
            Point location = Canvas.PointToClient(new Point(e.X, e.Y));

            BeginInvoke(() => FilesDropped?.Invoke(this, new DiagramFilesDropModel(paths, location)));
            Activate();
        }
    }

    private void Canvas_MouseLeave(object? sender, EventArgs e)
    {
        PointerLeft?.Invoke(this, EventArgs.Empty);
    }

    private void Canvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        WheelTurned?.Invoke(
            this,
            new DiagramWheelModel(
                e.Location,
                e.Delta / (float)SystemInformation.MouseWheelScrollDelta,
                (ModifierKeys & Keys.Control) != 0,
                (ModifierKeys & Keys.Shift) != 0));
    }

    private void TextInputBox_KeyDown(object? sender, KeyEventArgs e)
    {
        // Enter ends the text; Shift+Enter starts a new line
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            CommitText();
        }
    }

    private void TextInputBox_Leave(object? sender, EventArgs e)
    {
        CommitText();
    }

    private void DiagramEditorForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        CommitText();

        // Applies what was typed in a panel field, so it counts as a change to ask about
        Canvas.Focus();

        var closing = new CancelEventArgs();

        _closing?.Invoke(this, closing);
        e.Cancel = closing.Cancel;

        if (!e.Cancel)
        {
            // Its normal size, also while it's maximized or minimized
            Size size = WindowState == FormWindowState.Normal ? Size : RestoreBounds.Size;

            WindowClosed?.Invoke(this, new DiagramWindowSizeModel(size, WindowState == FormWindowState.Maximized));
        }
    }

    private void DiagramEditorForm_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            _isSpaceDown = false;
        }
    }

    private void DiagramEditorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        // Not with Alt, which closes the window
        if (e.KeyData == Keys.F4)
        {
            ToggleProperties();
            e.Handled = true;

            return;
        }

        // While typing a text or in the panel's fields, keys are theirs
        if (_isEditingText || PropertiesPanel.ContainsFocus)
        {
            return;
        }

        if (e.KeyCode == Keys.Space)
        {
            _isSpaceDown = true;
        }

        Action? action = KeyAction(e);

        if (action is null)
        {
            return;
        }

        action();
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private Action? KeyAction(KeyEventArgs e)
    {
        int step = e.Shift ? 1 : NudgeStep;

        if (e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down && !e.Control && !e.Alt)
        {
            Size delta = e.KeyCode switch
            {
                Keys.Left => new Size(-step, 0),
                Keys.Right => new Size(step, 0),
                Keys.Up => new Size(0, -step),
                _ => new Size(0, step),
            };

            return () => NudgeRequested?.Invoke(this, delta);
        }

        DiagramCommand? command = (e.Modifiers, e.KeyCode) switch
        {
            (Keys.Control, Keys.N) => DiagramCommand.New,
            (Keys.Control, Keys.O) => DiagramCommand.Open,
            (Keys.Control, Keys.S) => DiagramCommand.Save,
            (Keys.Control | Keys.Shift, Keys.S) => DiagramCommand.SaveAs,
            (Keys.Control, Keys.E) => DiagramCommand.ExportPdf,
            (Keys.Control, Keys.Z) => DiagramCommand.Undo,
            (Keys.Control, Keys.Y) or (Keys.Control | Keys.Shift, Keys.Z) => DiagramCommand.Redo,
            (Keys.Control, Keys.X) => DiagramCommand.Cut,
            (Keys.Control, Keys.C) => DiagramCommand.Copy,
            (Keys.Control, Keys.V) => DiagramCommand.Paste,
            (Keys.Control, Keys.D) => DiagramCommand.Duplicate,
            (Keys.Control, Keys.I) => DiagramCommand.InsertImage,
            (Keys.Control | Keys.Shift, Keys.C) => DiagramCommand.CopyStyle,
            (Keys.Control | Keys.Shift, Keys.V) => DiagramCommand.PasteStyle,
            (Keys.Control, Keys.A) => DiagramCommand.SelectAll,
            (Keys.Control, Keys.G) => DiagramCommand.Group,
            (Keys.Control | Keys.Shift, Keys.G) => DiagramCommand.Ungroup,
            (Keys.Control, Keys.OemCloseBrackets) => DiagramCommand.BringToFront,
            (Keys.Control, Keys.OemOpenBrackets) => DiagramCommand.SendToBack,
            (Keys.Control, Keys.Oemplus or Keys.Add) => DiagramCommand.ZoomIn,
            (Keys.Control, Keys.OemMinus or Keys.Subtract) => DiagramCommand.ZoomOut,
            (Keys.Control, Keys.D0 or Keys.NumPad0) => DiagramCommand.ZoomReset,
            (Keys.None, Keys.Delete or Keys.Back) => DiagramCommand.Delete,
            _ => null,
        };

        if (command is DiagramCommand requested)
        {
            return () => CommandRequested?.Invoke(this, requested);
        }

        DiagramTool? tool = (e.Modifiers, e.KeyCode) switch
        {
            (Keys.None, Keys.V) => DiagramTool.Select,
            (Keys.None, Keys.C) => DiagramTool.Connector,
            (Keys.None, Keys.L) => DiagramTool.Line,
            (Keys.None, Keys.R) => DiagramTool.Rectangle,
            (Keys.None, Keys.U) => DiagramTool.RoundedRectangle,
            (Keys.None, Keys.O) => DiagramTool.Ellipse,
            (Keys.None, Keys.D) => DiagramTool.Diamond,
            (Keys.None, Keys.P) => DiagramTool.Parallelogram,
            (Keys.None, Keys.H) => DiagramTool.Hexagon,
            (Keys.None, Keys.Y) => DiagramTool.Cylinder,
            (Keys.None, Keys.M) => DiagramTool.Document,
            (Keys.None, Keys.T) => DiagramTool.Text,
            _ => null,
        };

        return tool is DiagramTool selected ? () => ToolSelected?.Invoke(this, selected) : null;
    }
}
