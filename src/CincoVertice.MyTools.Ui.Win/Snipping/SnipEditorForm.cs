using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Services;
using CincoVertice.MyTools.Ui.Snipping.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;

namespace CincoVertice.MyTools.Ui.Win.Snipping;

/// <summary>
///     Snip editor view: the snip at its real size, with the annotations from
///     <see cref="Ui.Snipping.Presenters.SnipEditorPresenter"/> drawn over it by <see cref="AnnotationRenderer"/>.
///     The form only shows what it's given and passes on what the user does.
/// </summary>
public partial class SnipEditorForm : Form, ISnipEditorView
{
    // How many pixels the arrow keys move what's selected; with Shift, one
    private const int NudgeStep = 10;

    // How long a notice such as "Copied 2 lines of text." stays
    private const int NoticeMilliseconds = 3000;

    // Every snip of this window, oldest first, which the form owns and disposes
    private readonly List<Bitmap> _snips = [];

    // The toolbar's icons, made here, so disposed with the form
    private readonly List<Image> _images = [];
    private readonly int _iconSize;

    // The color button's image: the current tool's color, replaced when it changes
    private Image? _colorSwatch;

    // The text outline button's image, likewise
    private Image? _textOutlineSwatch;

    private IReadOnlyList<AnnotationModel> _annotations = [];

    // Which of the snips is shown
    private int _shownSnip;

    // How large it's shown: 1 is pixel for pixel
    private float _zoom = 1;

    // The part of the snip shown that's kept, null for all of it; and the part a crop being dragged would keep
    private Rectangle? _crop;
    private Rectangle? _cropPreview;

    // Where on the canvas's panel the mouse was when it asked to zoom, to keep what's there under it
    private Point? _zoomAnchor;

    // What shows the selected annotation; null when nothing is selected
    private SnipSelectionModel? _selection;

    // The box being dragged to select what's in it, in snip pixels
    private Rectangle? _selectionBand;

    // False while the choices of color, size and transparency are hidden
    private bool _showsDrawingChoices = true;

    // The values behind the size, transparency and text outline lists, which show them as text
    private IReadOnlyList<int> _sizes = [];
    private IReadOnlyList<int> _transparencies = [];
    private IReadOnlyList<int> _textOutlineWidths = [];
    private IReadOnlyList<int> _cornerRadii = [];

    // Set while the lists are filled or selected from the presenter, so that isn't taken as the user's choice
    private bool _isShowingChoices;

    // The text being typed: where it goes and its font; null when no text is being typed
    private Point? _textLocation;
    private Font? _textFont;

    /// <param name="snip">The image to edit; the form owns it and disposes it.</param>
    public SnipEditorForm(Bitmap snip)
    {
        InitializeComponent();

        _snips.Add(snip);
        SizeCanvas();

        // 16 pixels at 100% scaling, drawn at the screen's size rather than stretched, so they stay sharp
        _iconSize = (int)Math.Round(16 * DeviceDpi / 96.0);
        Tools.ImageScalingSize = new Size(_iconSize, _iconSize);

        foreach (ToolStripButton button in ToolButtons())
        {
            button.Image = Keep(ToolIcons.Create(ToolOf(button), _iconSize));
        }

        foreach (ToolStripButton button in ArrowHeadsButtons())
        {
            button.Image = Keep(ToolIcons.CreateArrowHeads(ArrowHeadsOf(button), _iconSize));
        }

        UndoButton.Image = Keep(ToolIcons.CreateUndo(_iconSize));
        RedoButton.Image = Keep(ToolIcons.CreateRedo(_iconSize));

        // The palette and More colors only, without the menu's empty image column
        ((ToolStripDropDownMenu)ColorDropDownButton.DropDown).ShowImageMargin = false;
        ((ToolStripDropDownMenu)TextOutlineDropDownButton.DropDown).ShowImageMargin = false;

        FitToSnip();
        FormExtensions.CenterOnCursorScreen(this);
        FormExtensions.ActivateWhenShown(this);
    }

    public event EventHandler<SnipPointerModel>? PointerPressed;

    public event EventHandler? SelectAllRequested;

    public event EventHandler<Point>? PointerMoved;

    public event EventHandler<Point>? PointerReleased;

    public event EventHandler<Point>? PointerDoubleClicked;

    public event EventHandler<Point>? PointerHovered;

    public event EventHandler<Point>? PointerRightClicked;

    public event EventHandler? DeleteRequested;

    public event EventHandler? ResetRequested;

    public event EventHandler? DuplicateRequested;

    public event EventHandler? BringToFrontRequested;

    public event EventHandler? SendToBackRequested;

    public event EventHandler<Size>? NudgeRequested;

    public event EventHandler? DeselectRequested;

    public event EventHandler<TextInputModel>? TextEscaped;

    public event EventHandler<SnipTool>? ToolSelected;

    public event EventHandler<Color>? ColorSelected;

    public event EventHandler? MoreColorsRequested;

    public event EventHandler<int>? SizeSelected;

    public event EventHandler<int>? TransparencySelected;

    public event EventHandler<SnipArrowHeads>? ArrowHeadsSelected;

    public event EventHandler<int>? CornerRadiusSelected;

    public event EventHandler<int>? TextOutlineWidthSelected;

    public event EventHandler<Color>? TextOutlineColorSelected;

    public event EventHandler? MoreTextOutlineColorsRequested;

    public event EventHandler<TextInputModel>? TextCommitted;

    public event EventHandler? SnipAdded;

    public event EventHandler<int>? SnipStepRequested;

    public event EventHandler<CancelEventArgs>? CloseRequested;

    public event EventHandler<bool>? HideWhileSnippingSelected;

    public event EventHandler<int>? ZoomRequested;

    public event EventHandler? CropApplyRequested;

    public event EventHandler? CropCancelRequested;

    public event EventHandler? UndoRequested;

    public event EventHandler? RedoRequested;

    public event EventHandler? CopyRequested;

    public event EventHandler? CopyTextRequested;

    public event EventHandler? SaveRequested;

    /// <summary>Takes another snip to edit, and fits the window to it as a new window would be.</summary>
    /// <param name="snip">The image; the form owns it and disposes it.</param>
    public void AddSnip(Bitmap snip)
    {
        // Into the snip it was typed over
        CommitText();

        _snips.Add(snip);
        SnipAdded?.Invoke(this, EventArgs.Empty);

        // A maximized window is already as large as it gets
        if (WindowState == FormWindowState.Normal)
        {
            FitToSnip();
            FormExtensions.CenterOnCursorScreen(this);
        }
    }

    public void ShowTool(SnipTool tool)
    {
        foreach (ToolStripButton button in ToolButtons())
        {
            button.Checked = ToolOf(button) == tool;
        }
    }

    public Size GetSnipSize(int index) => _snips[index].Size;

    public void ShowCrop(Rectangle? crop)
    {
        if (crop == _crop)
        {
            return;
        }

        _crop = crop;
        SizeCanvas();
        CanvasPanel.AutoScrollPosition = Point.Empty;
        Canvas.Invalidate();

        // As going to another snip does: the window fits what's left, keeping its upper left corner
        if (WindowState == FormWindowState.Normal)
        {
            FitToSnipFromUpperLeft();
        }
    }

    public void ShowCropPreview(Rectangle? area)
    {
        _cropPreview = area;
        ApplyCropButton.Visible = area is not null;
        CancelCropButton.Visible = area is not null;
        Canvas.Invalidate();
    }

    public void ShowSnip(int index, int count)
    {
        bool isAnotherSnip = index != _shownSnip;

        _shownSnip = index;
        SizeCanvas();
        Canvas.Invalidate();
        CanvasPanel.AutoScrollPosition = Point.Empty;

        // Nothing to go back to with only one
        SnipsSeparator.Visible = count > 1;
        PreviousSnipButton.Visible = count > 1;
        SnipPositionLabel.Visible = count > 1;
        NextSnipButton.Visible = count > 1;

        PreviousSnipButton.Enabled = index > 0;
        NextSnipButton.Enabled = index < count - 1;
        SnipPositionLabel.Text = $"{index + 1} / {count}";

        // Last, since the toolbar's width counts. Not for the same snip: that would undo the user's resizing
        if (isAnotherSnip && WindowState == FormWindowState.Normal)
        {
            FitToSnipFromUpperLeft();
        }
    }

    public void ShowPalette(IReadOnlyList<Color> colors, IReadOnlyList<Color> customColors)
    {
        Palette.Colors = colors;
        Palette.CustomColors = customColors;
        TextOutlinePalette.Colors = colors;
        TextOutlinePalette.CustomColors = customColors;
    }

    public void ShowColor(Color color)
    {
        Palette.SelectedColor = color;

        Image? old = _colorSwatch;
        _colorSwatch = CreateSwatch(color, _iconSize);
        ColorDropDownButton.Image = _colorSwatch;
        old?.Dispose();
    }

    public Color? AskColor(Color current, IReadOnlyList<Color> customColors)
    {
        // The dialog's custom colors are 0x00BBGGRR, as ToOle writes them
        using var dialog = new ColorDialog
        {
            AnyColor = true,
            Color = current,
            CustomColors = [.. customColors.Select(ColorTranslator.ToOle)],
            FullOpen = true,
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.Color : null;
    }

    public void ShowSizes(IReadOnlyList<int> sizes, int selected)
    {
        _sizes = ShowChoices(SizeComboBox, _sizes, sizes, selected, size => $"{size} px");
        SizeComboBox.Visible = _showsDrawingChoices && sizes.Count > 0;
    }

    public void ShowTransparencies(IReadOnlyList<int> transparencies, int selected)
    {
        _transparencies = ShowChoices(
            TransparencyComboBox,
            _transparencies,
            transparencies,
            selected,
            transparency => $"{transparency}%");
    }

    public void ShowDrawingChoices(bool isVisible)
    {
        _showsDrawingChoices = isVisible;
        ColorDropDownButton.Visible = isVisible;
        SizeComboBox.Visible = isVisible;
        TransparencyComboBox.Visible = isVisible;
    }

    public void ShowArrowHeads(SnipArrowHeads? selected)
    {
        foreach (ToolStripButton button in ArrowHeadsButtons())
        {
            button.Visible = selected is not null;
            button.Checked = ArrowHeadsOf(button) == selected;
        }
    }

    public void ShowCornerRadius(IReadOnlyList<int> radii, int? selected)
    {
        CornerRadiusComboBox.Visible = selected is not null;

        if (selected is int radius)
        {
            _cornerRadii = ShowChoices(
                CornerRadiusComboBox,
                _cornerRadii,
                radii,
                radius,
                value => value == 0 ? "Square" : $"{value} px");
        }
    }

    public void ShowTextOutline(IReadOnlyList<int> widths, SnipTextOutlineModel? outline)
    {
        TextOutlineDropDownButton.Visible = outline is not null;
        TextOutlineWidthComboBox.Visible = outline is not null;

        if (outline is null)
        {
            return;
        }

        _textOutlineWidths = ShowChoices(
            TextOutlineWidthComboBox,
            _textOutlineWidths,
            widths,
            outline.Width,
            width => width == 0 ? "No edge" : $"{width} px");
        TextOutlinePalette.SelectedColor = outline.Color;

        Image? old = _textOutlineSwatch;
        _textOutlineSwatch = CreateSwatch(outline.Color, _iconSize);
        TextOutlineDropDownButton.Image = _textOutlineSwatch;
        old?.Dispose();
    }

    public void ShowAnnotations(IReadOnlyList<AnnotationModel> annotations)
    {
        _annotations = annotations;
        Canvas.Invalidate();
    }

    public void ShowSelection(SnipSelectionModel? selection)
    {
        _selection = selection;
        Canvas.Invalidate();
    }

    public void ShowSelectionBand(Rectangle? band)
    {
        if (band != _selectionBand)
        {
            _selectionBand = band;
            Canvas.Invalidate();
        }
    }

    public void ShowCursor(SnipCursor cursor)
    {
        Canvas.Cursor = cursor switch
        {
            SnipCursor.Cross => Cursors.Cross,
            SnipCursor.Move => Cursors.SizeAll,
            SnipCursor.SizeVertical => Cursors.SizeNS,
            SnipCursor.SizeHorizontal => Cursors.SizeWE,
            SnipCursor.SizeTopLeftBottomRight => Cursors.SizeNWSE,
            SnipCursor.SizeTopRightBottomLeft => Cursors.SizeNESW,
            _ => Cursors.Default,
        };
    }

    public void ShowContextMenu(Point location, SnipContextMenuModel menu)
    {
        ResetMenuItem.Enabled = menu.CanReset;
        BringToFrontMenuItem.Enabled = menu.CanBringToFront;
        SendToBackMenuItem.Enabled = menu.CanSendToBack;
        SelectionMenu.Show(Canvas, ToCanvas(location));
    }

    public Size MeasureText(string text, int fontSize) => AnnotationRenderer.MeasureText(text, fontSize);

    public void ShowUndoRedo(bool canUndo, bool canRedo)
    {
        UndoButton.Enabled = canUndo;
        RedoButton.Enabled = canRedo;
    }

    public void ShowHideWhileSnipping(bool isHidden)
    {
        HideWhileSnippingMenuItem.Checked = isHidden;
    }

    public void ShowZoom(int percent)
    {
        float zoom = percent / 100f;

        ZoomLabel.Text = $"{percent}%";

        if (zoom == _zoom)
        {
            return;
        }

        // The box is at the old size, over the old place
        CommitText();

        // What's at the mouse stays there, or for the keys, what's in the middle of the snip as far as it shows
        Rectangle shown = Rectangle.Intersect(CanvasPanel.ClientRectangle, Canvas.Bounds);
        Point anchor = _zoomAnchor ?? new Point(shown.Left + shown.Width / 2, shown.Top + shown.Height / 2);
        var snipPoint = new PointF(
            (anchor.X - CanvasPanel.AutoScrollPosition.X) / _zoom,
            (anchor.Y - CanvasPanel.AutoScrollPosition.Y) / _zoom);

        _zoomAnchor = null;
        _zoom = zoom;
        SizeCanvas();
        CanvasPanel.AutoScrollPosition = new Point(
            Math.Max(0, (int)Math.Round(snipPoint.X * zoom - anchor.X)),
            Math.Max(0, (int)Math.Round(snipPoint.Y * zoom - anchor.Y)));
        Canvas.Invalidate();
    }

    public void BeginTextInput(Point location, Color color, int fontSize, string text)
    {
        CommitText();

        // As large as the text will be on the screen at this zoom
        _textFont?.Dispose();
        _textFont = AnnotationRenderer.CreateFont(Math.Max(1, (int)Math.Round(fontSize * _zoom)));
        _textLocation = location;

        TextInputBox.Font = _textFont;
        TextInputBox.ForeColor = color;

        // A light text on a light box couldn't be read while typing
        TextInputBox.BackColor = color.GetBrightness() > 0.7f ? Color.FromArgb(64, 64, 64) : Color.White;
        TextInputBox.Text = text.ReplaceLineEndings(Environment.NewLine);
        TextInputBox.Location = ToCanvas(location);
        FitTextInput();
        TextInputBox.Visible = true;
        TextInputBox.Focus();
        TextInputBox.Select(TextInputBox.TextLength, 0);
    }

    public void CopyImageToClipboard()
    {
        using Bitmap image = RenderImage();

        Clipboard.SetImage(image);
    }

    public byte[] RenderImagePng()
    {
        using Bitmap image = RenderImage();

        return PngEncoder.Encode(image);
    }

    public void CopyTextToClipboard(string text)
    {
        Clipboard.SetText(text);
    }

    public void ShowNotice(string message)
    {
        // The text is read after a wait, so the window may be closed by now
        if (IsDisposed)
        {
            return;
        }

        // Under the button, where the user looks after clicking it
        Rectangle button = CopyTextButton.Bounds;
        NoticeToolTip.Show(message, Tools, button.Left, Tools.Height, NoticeMilliseconds);
    }

    public string? AskSavePath(string suggestedFileName)
    {
        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        string screenshots = Path.Combine(pictures, "Screenshots");

        using var dialog = new SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = "PNG image (*.png)|*.png|JPEG image (*.jpg)|*.jpg|Bitmap (*.bmp)|*.bmp",
            InitialDirectory = Directory.Exists(screenshots) ? screenshots : pictures,
            Title = "Save snip",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public void SaveImage(string path)
    {
        ImageFormat format = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".bmp" => ImageFormat.Bmp,
            _ => ImageFormat.Png,
        };
        using Bitmap image = RenderImage();

        image.Save(path, format);
    }

    public bool AskDiscardSnips(int count)
    {
        DialogResult answer = MessageBox.Show(
            this,
            $"{count} snips weren't saved. Close the snip editor and lose them?",
            Text,
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning,

            // Enter or Esc keeps them, as a quick key press shouldn't lose work
            MessageBoxDefaultButton.Button2);

        return answer == DialogResult.OK;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Enter applies a crop being adjusted; while typing, it's the text's
        if (keyData == Keys.Enter && _cropPreview is not null && _textLocation is null)
        {
            CropApplyRequested?.Invoke(this, EventArgs.Empty);

            return true;
        }

        // An open list closes itself on Esc; the window stays
        if (keyData != Keys.Escape
            || SizeComboBox.DroppedDown
            || TransparencyComboBox.DroppedDown
            || TextOutlineWidthComboBox.DroppedDown
            || CornerRadiusComboBox.DroppedDown)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Esc never reaches the text box's KeyDown: it is a dialog key. While typing, it only ends the text
        if (_textLocation is not null)
        {
            EscapeText();
        }
        else if (_cropPreview is not null)
        {
            CropCancelRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (_selection is not null)
        {
            DeselectRequested?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Close();
        }

        return true;
    }

    private static SnipTool ToolOf(ToolStripButton button) => Enum.Parse<SnipTool>((string)button.Tag!);

    private static SnipArrowHeads ArrowHeadsOf(ToolStripButton button) =>
        Enum.Parse<SnipArrowHeads>((string)button.Tag!);

    private static Bitmap CreateSwatch(Color color, int size)
    {
        var swatch = new Bitmap(size, size);
        int side = size - 2;

        using var graphics = Graphics.FromImage(swatch);
        using var brush = new SolidBrush(color);

        graphics.FillRectangle(brush, 1, 1, side, side);
        graphics.DrawRectangle(Pens.Gray, 1, 1, side - 1, side - 1);

        return swatch;
    }

    private Image Keep(Image image)
    {
        _images.Add(image);

        return image;
    }

    /// <summary>
    ///     Fills the list with the values, unless it shows them already, and selects one.
    /// </summary>
    /// <returns>The values the list shows now.</returns>
    private IReadOnlyList<int> ShowChoices(
        ToolStripComboBox list,
        IReadOnlyList<int> shown,
        IReadOnlyList<int> values,
        int selected,
        Func<int, string> format)
    {
        _isShowingChoices = true;

        try
        {
            if (!shown.SequenceEqual(values))
            {
                list.Items.Clear();
                list.Items.AddRange([.. values.Select(format)]);
            }

            list.SelectedIndex = values.ToList().IndexOf(selected);
        }
        finally
        {
            _isShowingChoices = false;
        }

        return values;
    }

    /// <summary>Selects the next smaller or larger size, if there is one; the presenter hears of it.</summary>
    private void StepSize(int step)
    {
        int index = SizeComboBox.SelectedIndex + step;

        // None is selected on a text scaled to a size that isn't in the list
        if (SizeComboBox.SelectedIndex >= 0 && index >= 0 && index < SizeComboBox.Items.Count)
        {
            SizeComboBox.SelectedIndex = index;
        }
    }

    private ToolStripButton[] ToolButtons() =>
        [SelectButton, PenButton, SmoothPenButton, HighlighterButton, ArrowButton, RectangleButton, TextButton, StepButton, BlurButton, CropButton];

    private ToolStripButton[] ArrowHeadsButtons() => [ArrowHeadEndButton, ArrowHeadStartButton, ArrowHeadsBothButton];

    /// <summary>
    ///     Makes the window as large as the snip, up to most of the screen; a larger snip scrolls.
    /// </summary>
    private void FitToSnip()
    {
        Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;

        FitToSnip(new Size(area.Width * 9 / 10, area.Height * 9 / 10));
    }

    /// <summary>
    ///     Makes the window as large as the snip without moving its upper left corner, up to the edges of its screen; a
    ///     larger snip scrolls.
    /// </summary>
    private void FitToSnipFromUpperLeft()
    {
        Rectangle area = Screen.FromControl(this).WorkingArea;
        Size frame = Size - ClientSize;

        FitToSnip(new Size(area.Right - Left - frame.Width, area.Bottom - Top - frame.Height));
    }

    private void FitToSnip(Size largest)
    {
        Size canvas = Canvas.Size;
        int width = Math.Max(canvas.Width, Tools.PreferredSize.Width);
        int height = canvas.Height + Tools.Height;

        ClientSize = new Size(Math.Min(width, largest.Width), Math.Min(height, largest.Height));
    }

    /// <summary>The part of the snip shown that's kept: its crop, or all of it.</summary>
    private Rectangle ShownArea => _crop ?? new Rectangle(Point.Empty, _snips[_shownSnip].Size);

    /// <summary>
    ///     The canvas as large as the part of the snip shown, at the zoom, with room for the last pixel even when it's
    ///     partly shown.
    /// </summary>
    private void SizeCanvas()
    {
        Rectangle shown = ShownArea;

        Canvas.Size = new Size(
            Math.Max(1, (int)Math.Ceiling(shown.Width * _zoom)),
            Math.Max(1, (int)Math.Ceiling(shown.Height * _zoom)));
    }

    /// <summary>The snip pixel under a point of the canvas, in the pixels of the whole snip.</summary>
    private Point ToSnip(Point canvas) => new(
        (int)Math.Floor(canvas.X / _zoom) + ShownArea.X,
        (int)Math.Floor(canvas.Y / _zoom) + ShownArea.Y);

    /// <summary>Where a snip pixel's corner is on the canvas.</summary>
    private Point ToCanvas(Point snip) => new(
        (int)Math.Round((snip.X - ShownArea.X) * _zoom),
        (int)Math.Round((snip.Y - ShownArea.Y) * _zoom));

    /// <summary>The snip with the annotations, as it is copied or saved; the caller disposes it.</summary>
    private Bitmap RenderImage()
    {
        Bitmap snip = _snips[_shownSnip];
        Rectangle shown = ShownArea;
        var image = new Bitmap(shown.Width, shown.Height, PixelFormat.Format24bppRgb);

        using var graphics = Graphics.FromImage(image);

        // GraphicsUnit.Pixel: pixel for pixel, never scaled by the bitmap's resolution. In the pixels of the whole snip,
        // with the crop's corner at the image's
        graphics.TranslateTransform(-shown.X, -shown.Y);
        graphics.DrawImage(snip, shown, shown, GraphicsUnit.Pixel);
        AnnotationRenderer.Draw(graphics, _annotations, snip, shown);

        return image;
    }

    /// <summary>Sizes the text box to its text, with room for the next character.</summary>
    private void FitTextInput()
    {
        Size text = TextRenderer.MeasureText(TextInputBox.Text + " ", TextInputBox.Font);

        TextInputBox.Size = new Size(Math.Max(text.Width + 8, 24), text.Height + 4);
    }

    private void CommitText()
    {
        if (_textLocation is not Point location)
        {
            return;
        }

        // Cleared first: hiding the focused box raises Leave, which commits again
        _textLocation = null;
        string text = TextInputBox.Text;
        HideTextInput();

        TextCommitted?.Invoke(this, new TextInputModel(location, text));
    }

    /// <summary>
    ///     Gives the focus back to the window: nothing else on it can take it, so the hidden box would keep it, and the
    ///     keys with it.
    /// </summary>
    private void HideTextInput()
    {
        TextInputBox.Visible = false;
        ActiveControl = null;
    }

    private void EscapeText()
    {
        if (_textLocation is not Point location)
        {
            return;
        }

        // Cleared first: hiding the focused box raises Leave, which commits
        _textLocation = null;
        string text = TextInputBox.Text;
        HideTextInput();

        TextEscaped?.Invoke(this, new TextInputModel(location, text));
    }

    private void ToolButton_Click(object? sender, EventArgs e)
    {
        CommitText();
        ToolSelected?.Invoke(this, ToolOf((ToolStripButton)sender!));
    }

    private void Palette_ColorPicked(object? sender, Color color)
    {
        ColorDropDownButton.HideDropDown();
        ColorSelected?.Invoke(this, color);
    }

    private void MoreColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreColorsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SizeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && SizeComboBox.SelectedIndex >= 0)
        {
            SizeSelected?.Invoke(this, _sizes[SizeComboBox.SelectedIndex]);
        }
    }

    private void TransparencyComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && TransparencyComboBox.SelectedIndex >= 0)
        {
            TransparencySelected?.Invoke(this, _transparencies[TransparencyComboBox.SelectedIndex]);
        }
    }

    private void ArrowHeadsButton_Click(object? sender, EventArgs e)
    {
        ArrowHeadsSelected?.Invoke(this, ArrowHeadsOf((ToolStripButton)sender!));
    }

    private void TextOutlinePalette_ColorPicked(object? sender, Color color)
    {
        TextOutlineDropDownButton.HideDropDown();
        TextOutlineColorSelected?.Invoke(this, color);
    }

    private void MoreTextOutlineColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreTextOutlineColorsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CornerRadiusComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && CornerRadiusComboBox.SelectedIndex >= 0)
        {
            CornerRadiusSelected?.Invoke(this, _cornerRadii[CornerRadiusComboBox.SelectedIndex]);
        }
    }

    private void TextOutlineWidthComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && TextOutlineWidthComboBox.SelectedIndex >= 0)
        {
            TextOutlineWidthSelected?.Invoke(this, _textOutlineWidths[TextOutlineWidthComboBox.SelectedIndex]);
        }
    }

    private void HideWhileSnippingMenuItem_Click(object? sender, EventArgs e)
    {
        HideWhileSnippingSelected?.Invoke(this, !HideWhileSnippingMenuItem.Checked);
    }

    private void ResetMenuItem_Click(object? sender, EventArgs e)
    {
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    private void DeleteMenuItem_Click(object? sender, EventArgs e)
    {
        DeleteRequested?.Invoke(this, EventArgs.Empty);
    }

    private void DuplicateMenuItem_Click(object? sender, EventArgs e)
    {
        DuplicateRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BringToFrontMenuItem_Click(object? sender, EventArgs e)
    {
        BringToFrontRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SendToBackMenuItem_Click(object? sender, EventArgs e)
    {
        SendToBackRequested?.Invoke(this, EventArgs.Empty);
    }

    private void PreviousSnipButton_Click(object? sender, EventArgs e)
    {
        CommitText();
        SnipStepRequested?.Invoke(this, -1);
    }

    private void NextSnipButton_Click(object? sender, EventArgs e)
    {
        CommitText();
        SnipStepRequested?.Invoke(this, 1);
    }

    private void UndoButton_Click(object? sender, EventArgs e)
    {
        CommitText();
        UndoRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RedoButton_Click(object? sender, EventArgs e)
    {
        CommitText();
        RedoRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CopyButton_Click(object? sender, EventArgs e)
    {
        CommitText();
        CopyRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CopyTextButton_Click(object? sender, EventArgs e)
    {
        CommitText();
        CopyTextRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        CommitText();
        SaveRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        Bitmap snip = _snips[_shownSnip];
        Rectangle shown = ShownArea;
        GraphicsState state = graphics.Save();

        // Larger, each pixel a sharp square, for close work; smaller, smoothed, so it can still be read
        graphics.InterpolationMode = _zoom >= 1 ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.DrawImage(
            snip,
            new RectangleF(0, 0, shown.Width * _zoom, shown.Height * _zoom),
            shown,
            GraphicsUnit.Pixel);
        graphics.Restore(state);

        // In snip pixels, as in the copied or saved image, with the crop's corner at the canvas's
        state = graphics.Save();
        graphics.ScaleTransform(_zoom, _zoom);
        graphics.TranslateTransform(-shown.X, -shown.Y);
        AnnotationRenderer.Draw(graphics, _annotations, snip, shown);
        graphics.Restore(state);

        // Not scaled, so the frame and handles stay as thin and as small; only moved to the crop's corner
        state = graphics.Save();
        graphics.TranslateTransform(-shown.X * _zoom, -shown.Y * _zoom);

        if (_selection is not null)
        {
            AnnotationRenderer.DrawSelection(graphics, _selection, _zoom);
        }

        if (_cropPreview is Rectangle crop)
        {
            DrawCropPreview(graphics, crop, shown);
        }

        if (_selectionBand is Rectangle band)
        {
            AnnotationRenderer.DrawSelectionBand(graphics, band, _zoom);
        }

        graphics.Restore(state);
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        // The canvas can't take the focus, so clicking it doesn't end the text by itself
        CommitText();

        if (e.Button == MouseButtons.Left)
        {
            PointerPressed?.Invoke(this, new SnipPointerModel(ToSnip(e.Location), (ModifierKeys & Keys.Shift) != 0));
        }
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            PointerMoved?.Invoke(this, ToSnip(e.Location));
        }
        else if (e.Button == MouseButtons.None)
        {
            PointerHovered?.Invoke(this, ToSnip(e.Location));
        }
    }

    private void Canvas_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            PointerDoubleClicked?.Invoke(this, ToSnip(e.Location));
        }
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            PointerReleased?.Invoke(this, ToSnip(e.Location));
        }
        else if (e.Button == MouseButtons.Right)
        {
            PointerRightClicked?.Invoke(this, ToSnip(e.Location));
        }
    }

    /// <summary>What would be cut away dimmed, and the part kept framed, in the pixels of the canvas at the zoom.</summary>
    private void DrawCropPreview(Graphics graphics, Rectangle crop, Rectangle shown)
    {
        RectangleF Scaled(Rectangle area) => new(area.X * _zoom, area.Y * _zoom, area.Width * _zoom, area.Height * _zoom);

        using var outside = new Region(Scaled(shown));
        using var shade = new SolidBrush(Color.FromArgb(140, Color.Black));
        using var frame = new Pen(Color.White) { DashStyle = DashStyle.Dash };
        RectangleF kept = Scaled(crop);

        outside.Exclude(kept);
        graphics.FillRegion(shade, outside);
        graphics.DrawRectangle(frame, kept.X, kept.Y, kept.Width - 1, kept.Height - 1);

        // The handles only: the dashed frame is the crop's own
        AnnotationRenderer.DrawSelection(graphics, AnnotationGeometry.FrameOf(crop) with { Box = null }, _zoom);
    }

    /// <summary>With Ctrl, zooms around the mouse; without, the panel scrolls as usual.</summary>
    private void Canvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        if ((ModifierKeys & Keys.Control) == 0 || e.Delta == 0)
        {
            return;
        }

        if (e is HandledMouseEventArgs handled)
        {
            handled.Handled = true;
        }

        _zoomAnchor = CanvasPanel.PointToClient(Canvas.PointToScreen(e.Location));
        ZoomRequested?.Invoke(this, Math.Sign(e.Delta));
    }

    private void ApplyCropButton_Click(object? sender, EventArgs e)
    {
        CropApplyRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CancelCropButton_Click(object? sender, EventArgs e)
    {
        CropCancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ZoomLabel_Click(object? sender, EventArgs e)
    {
        ZoomRequested?.Invoke(this, 0);
    }

    private void TextInputBox_TextChanged(object? sender, EventArgs e)
    {
        FitTextInput();
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

    /// <returns>What a key does to what's selected: delete it, or move it; null without either.</returns>
    private Action? SelectionKeyCommand(KeyEventArgs e)
    {
        if (_selection is null)
        {
            return null;
        }

        // As in the diagram editor
        Action? arrange = e.KeyData switch
        {
            Keys.Control | Keys.D => () => DuplicateRequested?.Invoke(this, EventArgs.Empty),
            Keys.Control | Keys.OemCloseBrackets => () => BringToFrontRequested?.Invoke(this, EventArgs.Empty),
            Keys.Control | Keys.OemOpenBrackets => () => SendToBackRequested?.Invoke(this, EventArgs.Empty),
            _ => null,
        };

        if (arrange is not null)
        {
            return arrange;
        }

        // Far with the arrows alone, and by a pixel with Shift, as in the diagram editor
        int step = e.Shift ? 1 : NudgeStep;
        Size? delta = (e.Control || e.Alt ? Keys.None : e.KeyCode) switch
        {
            Keys.Left => new Size(-step, 0),
            Keys.Right => new Size(step, 0),
            Keys.Up => new Size(0, -step),
            Keys.Down => new Size(0, step),
            _ => null,
        };

        if (delta is Size nudge)
        {
            return () => NudgeRequested?.Invoke(this, nudge);
        }

        return e.KeyData is Keys.Delete or Keys.Back ? () => DeleteRequested?.Invoke(this, EventArgs.Empty) : null;
    }

    /// <summary>
    ///     Asks only when the user closes it: on Windows shutting down or the app exiting, a question could stop either.
    /// </summary>
    private void SnipEditorForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing)
        {
            return;
        }

        // Into the snip it was typed over, so it counts
        CommitText();

        var closing = new CancelEventArgs();

        CloseRequested?.Invoke(this, closing);
        e.Cancel = closing.Cancel;
    }

    private void SnipEditorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        // While typing a text, keys are the text's
        if (_textLocation is not null)
        {
            return;
        }

        Action? command = SelectionKeyCommand(e) ?? (e.Modifiers, e.KeyCode) switch
        {
            (Keys.Control, Keys.Z) => () => UndoRequested?.Invoke(this, EventArgs.Empty),
            (Keys.Control | Keys.Shift, Keys.Z) or (Keys.Control, Keys.Y) =>
                () => RedoRequested?.Invoke(this, EventArgs.Empty),
            (Keys.Control, Keys.C) => () => CopyRequested?.Invoke(this, EventArgs.Empty),
            (Keys.Control | Keys.Shift, Keys.C) => () => CopyTextRequested?.Invoke(this, EventArgs.Empty),
            (Keys.Control, Keys.S) => () => SaveRequested?.Invoke(this, EventArgs.Empty),
            (Keys.Control, Keys.A) => () => SelectAllRequested?.Invoke(this, EventArgs.Empty),
            (Keys.Control, Keys.Oemplus or Keys.Add) => () => ZoomRequested?.Invoke(this, 1),
            (Keys.Control, Keys.OemMinus or Keys.Subtract) => () => ZoomRequested?.Invoke(this, -1),
            (Keys.Control, Keys.D0 or Keys.NumPad0) => () => ZoomRequested?.Invoke(this, 0),
            (Keys.Alt, Keys.Left) => () => SnipStepRequested?.Invoke(this, -1),
            (Keys.Alt, Keys.Right) => () => SnipStepRequested?.Invoke(this, 1),
            (Keys.None, Keys.V) => () => ToolSelected?.Invoke(this, SnipTool.Select),
            (Keys.None, Keys.P) => () => ToolSelected?.Invoke(this, SnipTool.Pen),
            (Keys.None, Keys.S) => () => ToolSelected?.Invoke(this, SnipTool.SmoothPen),
            (Keys.None, Keys.H) => () => ToolSelected?.Invoke(this, SnipTool.Highlighter),
            (Keys.None, Keys.A) => () => ToolSelected?.Invoke(this, SnipTool.Arrow),
            (Keys.None, Keys.R) => () => ToolSelected?.Invoke(this, SnipTool.Rectangle),
            (Keys.None, Keys.T) => () => ToolSelected?.Invoke(this, SnipTool.Text),
            (Keys.None, Keys.B) => () => ToolSelected?.Invoke(this, SnipTool.Blur),
            (Keys.None, Keys.N) => () => ToolSelected?.Invoke(this, SnipTool.Step),
            (Keys.None, Keys.C) => () => ToolSelected?.Invoke(this, SnipTool.Crop),
            // - and + where the keyboard has them, [ and ] as in other drawing programs
            (Keys.None, Keys.OemMinus or Keys.Subtract or Keys.OemOpenBrackets) => () => StepSize(-1),
            (Keys.None or Keys.Shift, Keys.Oemplus or Keys.Add) or (Keys.None, Keys.OemCloseBrackets) =>
                () => StepSize(1),
            _ => null,
        };

        if (command is null)
        {
            return;
        }

        command();
        e.Handled = true;
        e.SuppressKeyPress = true;
    }
}
