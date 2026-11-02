using System.Drawing.Imaging;
using CincoVertice.MyTools.Ui.Snipping.Models;
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
    private readonly Bitmap _snip;

    // The toolbar's icons, made here, so disposed with the form
    private readonly List<Image> _images = [];
    private readonly int _iconSize;

    // The color button's image: the current tool's color, replaced when it changes
    private Image? _colorSwatch;

    private IReadOnlyList<AnnotationModel> _annotations = [];

    // The values behind the size and transparency lists, which show them as text
    private IReadOnlyList<int> _sizes = [];
    private IReadOnlyList<int> _transparencies = [];

    // Set while the lists are filled or selected from the presenter, so that isn't taken as the user's choice
    private bool _isShowingChoices;

    // The text being typed: where it goes and its font; null when no text is being typed
    private Point? _textLocation;
    private Font? _textFont;

    /// <param name="snip">The image to edit; the form owns it and disposes it.</param>
    public SnipEditorForm(Bitmap snip)
    {
        InitializeComponent();

        _snip = snip;
        Canvas.Image = snip;

        // 16 pixels at 100% scaling, drawn at the screen's size rather than stretched, so they stay sharp
        _iconSize = (int)Math.Round(16 * DeviceDpi / 96.0);
        Tools.ImageScalingSize = new Size(_iconSize, _iconSize);

        foreach (ToolStripButton button in ToolButtons())
        {
            button.Image = Keep(ToolIcons.Create(ToolOf(button), _iconSize));
        }

        UndoButton.Image = Keep(ToolIcons.CreateUndo(_iconSize));
        RedoButton.Image = Keep(ToolIcons.CreateRedo(_iconSize));

        // The palette and More colors only, without the menu's empty image column
        ((ToolStripDropDownMenu)ColorDropDownButton.DropDown).ShowImageMargin = false;

        FitToSnip();
        FormExtensions.CenterOnCursorScreen(this);
        FormExtensions.ActivateWhenShown(this);
    }

    public event EventHandler<Point>? PointerPressed;

    public event EventHandler<Point>? PointerMoved;

    public event EventHandler<Point>? PointerReleased;

    public event EventHandler<SnipTool>? ToolSelected;

    public event EventHandler<Color>? ColorSelected;

    public event EventHandler? MoreColorsRequested;

    public event EventHandler<int>? SizeSelected;

    public event EventHandler<int>? TransparencySelected;

    public event EventHandler<TextInputModel>? TextCommitted;

    public event EventHandler? UndoRequested;

    public event EventHandler? RedoRequested;

    public event EventHandler? CopyRequested;

    public event EventHandler? SaveRequested;

    public void ShowTool(SnipTool tool)
    {
        foreach (ToolStripButton button in ToolButtons())
        {
            button.Checked = ToolOf(button) == tool;
        }
    }

    public void ShowPalette(IReadOnlyList<Color> colors, IReadOnlyList<Color> customColors)
    {
        Palette.Colors = colors;
        Palette.CustomColors = customColors;
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

    public void ShowAnnotations(IReadOnlyList<AnnotationModel> annotations)
    {
        _annotations = annotations;
        Canvas.Invalidate();
    }

    public void ShowUndoRedo(bool canUndo, bool canRedo)
    {
        UndoButton.Enabled = canUndo;
        RedoButton.Enabled = canRedo;
    }

    public void BeginTextInput(Point location, Color color, int fontSize)
    {
        CommitText();

        _textFont?.Dispose();
        _textFont = AnnotationRenderer.CreateFont(fontSize);
        _textLocation = location;

        TextInputBox.Font = _textFont;
        TextInputBox.ForeColor = color;

        // A light text on a light box couldn't be read while typing
        TextInputBox.BackColor = color.GetBrightness() > 0.7f ? Color.FromArgb(64, 64, 64) : Color.White;
        TextInputBox.Text = string.Empty;
        TextInputBox.Location = location;
        FitTextInput();
        TextInputBox.Visible = true;
        TextInputBox.Focus();
    }

    public void CopyImageToClipboard()
    {
        using Bitmap image = RenderImage();

        Clipboard.SetImage(image);
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

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Esc never reaches the text box's KeyDown: it is a dialog key
        if (keyData == Keys.Escape && _textLocation is not null)
        {
            CancelText();

            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static SnipTool ToolOf(ToolStripButton button) => Enum.Parse<SnipTool>((string)button.Tag!);

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

        if (index >= 0 && index < SizeComboBox.Items.Count)
        {
            SizeComboBox.SelectedIndex = index;
        }
    }

    private ToolStripButton[] ToolButtons() => [PenButton, HighlighterButton, ArrowButton, RectangleButton, TextButton];

    /// <summary>
    ///     Makes the window as large as the snip, up to most of the screen; a larger snip scrolls.
    /// </summary>
    private void FitToSnip()
    {
        Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
        int width = Math.Max(_snip.Width, Tools.PreferredSize.Width);
        int height = _snip.Height + Tools.Height;

        ClientSize = new Size(Math.Min(width, area.Width * 9 / 10), Math.Min(height, area.Height * 9 / 10));
    }

    /// <summary>The snip with the annotations, as it is copied or saved; the caller disposes it.</summary>
    private Bitmap RenderImage()
    {
        var image = new Bitmap(_snip.Width, _snip.Height, PixelFormat.Format24bppRgb);
        var whole = new Rectangle(Point.Empty, _snip.Size);

        using var graphics = Graphics.FromImage(image);

        // GraphicsUnit.Pixel: pixel for pixel, never scaled by the bitmap's resolution
        graphics.DrawImage(_snip, whole, whole, GraphicsUnit.Pixel);
        AnnotationRenderer.Draw(graphics, _annotations);

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
        TextInputBox.Visible = false;

        TextCommitted?.Invoke(this, new TextInputModel(location, text));
    }

    private void CancelText()
    {
        _textLocation = null;
        TextInputBox.Visible = false;
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

    private void UndoButton_Click(object? sender, EventArgs e)
    {
        UndoRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RedoButton_Click(object? sender, EventArgs e)
    {
        RedoRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CopyButton_Click(object? sender, EventArgs e)
    {
        CopyRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        CommitText();
        SaveRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        AnnotationRenderer.Draw(e.Graphics, _annotations);
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        // The canvas can't take the focus, so clicking it doesn't end the text by itself
        CommitText();
        PointerPressed?.Invoke(this, e.Location);
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            PointerMoved?.Invoke(this, e.Location);
        }
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            PointerReleased?.Invoke(this, e.Location);
        }
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

    private void SnipEditorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        // While typing a text, keys are the text's
        if (TextInputBox.Focused)
        {
            return;
        }

        Action? command = (e.Modifiers, e.KeyCode) switch
        {
            (Keys.Control, Keys.Z) => () => UndoRequested?.Invoke(this, EventArgs.Empty),
            (Keys.Control | Keys.Shift, Keys.Z) or (Keys.Control, Keys.Y) =>
                () => RedoRequested?.Invoke(this, EventArgs.Empty),
            (Keys.Control, Keys.C) => () => CopyRequested?.Invoke(this, EventArgs.Empty),
            (Keys.Control, Keys.S) => () => SaveRequested?.Invoke(this, EventArgs.Empty),
            (Keys.None, Keys.P) => () => ToolSelected?.Invoke(this, SnipTool.Pen),
            (Keys.None, Keys.H) => () => ToolSelected?.Invoke(this, SnipTool.Highlighter),
            (Keys.None, Keys.A) => () => ToolSelected?.Invoke(this, SnipTool.Arrow),
            (Keys.None, Keys.R) => () => ToolSelected?.Invoke(this, SnipTool.Rectangle),
            (Keys.None, Keys.T) => () => ToolSelected?.Invoke(this, SnipTool.Text),
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
