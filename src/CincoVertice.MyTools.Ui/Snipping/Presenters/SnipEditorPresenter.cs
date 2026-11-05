using System.Drawing;
using System.Globalization;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Views;

namespace CincoVertice.MyTools.Ui.Snipping.Presenters;

/// <summary>
///     One snip editor: turns drags on the view into annotations with the current tool, and keeps them with undo and
///     redo. It can hold several snips, each with its own annotations and undo; the view keeps their images. Each tool
///     keeps its own size, transparency and color; these and the tool are saved
///     (<see cref="ISnipEditorSettingsStore"/>), so the next editor starts with them. After every change, and when
///     another snip is shown, the snip shown is copied to the clipboard again, so it can be pasted right away.
/// </summary>
public sealed class SnipEditorPresenter
{
    /// <summary>
    ///     The colors offered in the palette, by rows of 8: bright, light (e.g. for the highlighter), dark and
    ///     neutral. Any other color can be picked with More colors, and is then kept in the palette as a custom
    ///     color.
    /// </summary>
    public static IReadOnlyList<Color> Palette { get; } =
    [
        Rgb(0xFF0000), Rgb(0xFF8C00), Rgb(0xFFFF00), Rgb(0x00CC00),
        Rgb(0x00B7EB), Rgb(0x0063B1), Rgb(0x8E44AD), Rgb(0xFF00FF),
        Rgb(0xFF9999), Rgb(0xFFCC80), Rgb(0xFFF59D), Rgb(0xA5D6A7),
        Rgb(0x81D4FA), Rgb(0x90CAF9), Rgb(0xCE93D8), Rgb(0xF48FB1),
        Rgb(0x000000), Rgb(0x404040), Rgb(0x808080), Rgb(0xC0C0C0),
        Rgb(0xFFFFFF), Rgb(0x8B4513), Rgb(0x006400), Rgb(0x000080),
    ];

    /// <summary>How many colors picked with More colors the palette keeps, newest first.</summary>
    public const int MaxCustomColors = 8;

    /// <summary>The line widths to choose from, in pixels.</summary>
    public static IReadOnlyList<int> LineSizes { get; } = [1, 2, 3, 4, 5, 6, 8, 10, 12, 16, 20, 24, 32, 40, 48];

    /// <summary>The font sizes to choose from, in pixels.</summary>
    public static IReadOnlyList<int> FontSizes { get; } = [10, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 64, 72];

    /// <summary>The transparencies to choose from, in percent; 100 would be invisible.</summary>
    public static IReadOnlyList<int> Transparencies { get; } = [0, 10, 20, 30, 40, 50, 60, 70, 80, 90];

    /// <summary>The widths of the edge around a text's letters to choose from, in pixels; 0 is no edge.</summary>
    public static IReadOnlyList<int> TextOutlineWidths { get; } = [0, 1, 2, 3, 4, 5, 6, 8, 10, 12];

    private readonly ISnipEditorView _view;
    private readonly TimeProvider _timeProvider;
    private readonly ISnipEditorSettingsStore _settingsStore;

    // Every snip of the window, oldest first; starts with the one it opened with
    private readonly List<SnipPage> _pages = [new SnipPage()];

    private int _current;

    private readonly Dictionary<SnipTool, SnipToolSettingsModel> _settings = new()
    {
        [SnipTool.Pen] = new SnipToolSettingsModel(4, 0, Palette[0]),
        [SnipTool.Highlighter] = new SnipToolSettingsModel(16, 50, Palette[2]),
        [SnipTool.Arrow] = new SnipToolSettingsModel(4, 0, Palette[0]),
        [SnipTool.Rectangle] = new SnipToolSettingsModel(4, 0, Palette[0]),
        [SnipTool.Text] = new SnipToolSettingsModel(24, 0, Palette[0]),

        // Not used: a blur has no color, size or transparency
        [SnipTool.Blur] = new SnipToolSettingsModel(4, 0, Palette[0]),
    };

    // Colors picked with More colors that aren't in the palette, newest first, at most MaxCustomColors
    private readonly List<Color> _customColors = [];

    private SnipTool _tool = SnipTool.Pen;
    private SnipArrowHeads _arrowHeads = SnipArrowHeads.End;

    // Off until a width is chosen; white shows on most of what a text is written over
    private SnipTextOutlineModel _textOutline = new(0, Rgb(0xFFFFFF));

    // The drag in progress: where it started, the points of a freehand line, and what it would add
    private Point? _start;
    private List<Point> _points = [];
    private AnnotationModel? _drawing;

    /// <param name="settingsStore">
    ///     Where the tool, each tool's settings and the custom colors are kept: the editor starts with the ones saved
    ///     last, and saves them on every change.
    /// </param>
    public SnipEditorPresenter(
        ISnipEditorView view,
        TimeProvider timeProvider,
        ISnipEditorSettingsStore settingsStore)
    {
        _view = view;
        _timeProvider = timeProvider;
        _settingsStore = settingsStore;

        Apply(settingsStore.Load());

        _view.PointerPressed += OnPointerPressed;
        _view.PointerMoved += OnPointerMoved;
        _view.PointerReleased += OnPointerReleased;
        _view.ToolSelected += OnToolSelected;
        _view.ColorSelected += OnColorSelected;
        _view.MoreColorsRequested += OnMoreColorsRequested;
        _view.SizeSelected += OnSizeSelected;
        _view.TransparencySelected += OnTransparencySelected;
        _view.ArrowHeadsSelected += OnArrowHeadsSelected;
        _view.TextOutlineWidthSelected += OnTextOutlineWidthSelected;
        _view.TextOutlineColorSelected += OnTextOutlineColorSelected;
        _view.MoreTextOutlineColorsRequested += OnMoreTextOutlineColorsRequested;
        _view.SnipAdded += OnSnipAdded;
        _view.SnipStepRequested += OnSnipStepRequested;
        _view.TextCommitted += OnTextCommitted;
        _view.UndoRequested += OnUndoRequested;
        _view.RedoRequested += OnRedoRequested;
        _view.CopyRequested += OnCopyRequested;
        _view.SaveRequested += OnSaveRequested;
    }

    /// <summary>The annotations kept on the snip shown, oldest first; without the one being drawn.</summary>
    public IReadOnlyList<AnnotationModel> Annotations => Page.Annotations;

    private SnipPage Page => _pages[_current];

    private SnipToolSettingsModel Settings => _settings[_tool];

    /// <summary>The current tool's color, as see-through as its transparency (halves round up).</summary>
    private Color DrawingColor
    {
        get
        {
            double alpha = 255 * (100 - Settings.Transparency) / 100.0;

            return Color.FromArgb((int)Math.Round(alpha, MidpointRounding.AwayFromZero), Settings.Color);
        }
    }

    /// <summary>Shows the palette, the starting tool and its settings, and copies the snip to the clipboard.</summary>
    public void Start()
    {
        _view.ShowPalette(Palette, [.. _customColors]);
        ShowTool();
        ShowPage(0);
    }

    /// <summary>The sizes a tool can use: font sizes for the text tool, line widths for the others.</summary>
    public static IReadOnlyList<int> SizesOf(SnipTool tool) => tool == SnipTool.Text ? FontSizes : LineSizes;

    /// <summary>
    ///     Takes the saved settings that can still be used: a tool, size, transparency or color the editor doesn't
    ///     offer (e.g. from a file edited by hand) keeps its default. Any color is fine, made solid.
    /// </summary>
    private void Apply(SnipEditorSettingsModel? saved)
    {
        if (saved is null)
        {
            return;
        }

        if (Enum.IsDefined(saved.Tool))
        {
            _tool = saved.Tool;
        }

        if (Enum.IsDefined(saved.ArrowHeads))
        {
            _arrowHeads = saved.ArrowHeads;
        }

        if (saved.TextOutline is { } outline)
        {
            _textOutline = new SnipTextOutlineModel(
                TextOutlineWidths.Contains(outline.Width) ? outline.Width : _textOutline.Width,
                outline.Color.IsEmpty ? _textOutline.Color : Solid(outline.Color));
        }

        foreach (var (tool, settings) in saved.Tools)
        {
            if (!_settings.TryGetValue(tool, out SnipToolSettingsModel? defaults))
            {
                continue;
            }

            _settings[tool] = new SnipToolSettingsModel(
                SizesOf(tool).Contains(settings.Size) ? settings.Size : defaults.Size,
                Transparencies.Contains(settings.Transparency) ? settings.Transparency : defaults.Transparency,
                settings.Color.IsEmpty ? defaults.Color : Solid(settings.Color));
        }

        // Oldest last, so adding each in turn at the front keeps the saved order
        foreach (Color color in saved.CustomColors.Reverse())
        {
            AddCustomColor(color);
        }
    }

    private void SaveSettings()
    {
        // Copies: the store may keep them, and the editor goes on changing its own
        _settingsStore.Save(new SnipEditorSettingsModel(
            _tool,
            _settings.ToDictionary(),
            [.. _customColors],
            _arrowHeads,
            _textOutline));
    }

    /// <summary>
    ///     Puts a color first among the custom colors, unless the palette has it already; the oldest beyond
    ///     <see cref="MaxCustomColors"/> is dropped.
    /// </summary>
    /// <returns>True when the custom colors changed.</returns>
    private bool AddCustomColor(Color color)
    {
        Color solid = Solid(color);

        if (color.IsEmpty || Palette.Contains(solid) || _customColors.FirstOrDefault() == solid)
        {
            return false;
        }

        _customColors.Remove(solid);
        _customColors.Insert(0, solid);

        if (_customColors.Count > MaxCustomColors)
        {
            _customColors.RemoveAt(MaxCustomColors);
        }

        return true;
    }

    private void ShowTool()
    {
        _view.ShowTool(_tool);
        _view.ShowDrawingChoices(_tool != SnipTool.Blur);
        _view.ShowColor(Settings.Color);
        _view.ShowSizes(SizesOf(_tool), Settings.Size);
        _view.ShowTransparencies(Transparencies, Settings.Transparency);
        _view.ShowArrowHeads(_tool == SnipTool.Arrow ? _arrowHeads : null);
        ShowTextOutline();
    }

    private void ShowTextOutline()
    {
        _view.ShowTextOutline(TextOutlineWidths, _tool == SnipTool.Text ? _textOutline : null);
    }

    /// <summary>The color without transparency, compared by its value only, as palette colors are.</summary>
    private static Color Solid(Color color) => Color.FromArgb(255, color.R, color.G, color.B);

    private static Color Rgb(int rgb) => Solid(Color.FromArgb(rgb));

    private void OnPointerPressed(object? sender, Point point)
    {
        if (_tool == SnipTool.Text)
        {
            // The text box can't show transparency; the text is drawn see-through once it's added
            _view.BeginTextInput(point, Settings.Color, Settings.Size);

            return;
        }

        _start = point;
        _points = [point];
        _drawing = CreateAnnotation(point, point);
        Refresh();
    }

    private void OnPointerMoved(object? sender, Point point)
    {
        if (_start is not Point start)
        {
            return;
        }

        if (_points[^1] != point)
        {
            _points.Add(point);
        }

        _drawing = CreateAnnotation(start, point);
        Refresh();
    }

    private void OnPointerReleased(object? sender, Point point)
    {
        if (_start is null)
        {
            return;
        }

        OnPointerMoved(sender, point);

        AnnotationModel? drawn = _drawing;
        _start = null;
        _drawing = null;

        if (drawn is not null && IsVisible(drawn))
        {
            Add(drawn);
        }
        else
        {
            Refresh();
        }
    }

    private void OnToolSelected(object? sender, SnipTool tool)
    {
        _tool = tool;
        ShowTool();
        SaveSettings();
    }

    private void OnColorSelected(object? sender, Color color)
    {
        // Only the current tool's: the pen can stay red while the highlighter is yellow
        _settings[_tool] = Settings with { Color = Solid(color) };
        _view.ShowColor(Settings.Color);
        SaveSettings();
    }

    private void OnMoreColorsRequested(object? sender, EventArgs e)
    {
        if (AskColor(Settings.Color) is Color color)
        {
            OnColorSelected(sender, color);
        }
    }

    /// <returns>The color picked, now kept among the custom colors; null when the user cancels.</returns>
    private Color? AskColor(Color current)
    {
        if (_view.AskColor(current, [.. _customColors]) is not Color color)
        {
            return null;
        }

        // Kept in the palette, so it's one click away next time, in this editor and the next ones
        if (AddCustomColor(color))
        {
            _view.ShowPalette(Palette, [.. _customColors]);
        }

        return color;
    }

    private void OnSizeSelected(object? sender, int size)
    {
        if (size <= 0)
        {
            return;
        }

        _settings[_tool] = Settings with { Size = size };
        _view.ShowSizes(SizesOf(_tool), size);
        SaveSettings();
    }

    private void OnTransparencySelected(object? sender, int transparency)
    {
        // At 100% nothing would be drawn
        int clamped = Math.Clamp(transparency, 0, Transparencies[^1]);

        _settings[_tool] = Settings with { Transparency = clamped };
        _view.ShowTransparencies(Transparencies, clamped);
        SaveSettings();
    }

    private void OnArrowHeadsSelected(object? sender, SnipArrowHeads heads)
    {
        _arrowHeads = heads;
        _view.ShowArrowHeads(heads);
        SaveSettings();
    }

    private void OnTextOutlineWidthSelected(object? sender, int width)
    {
        if (!TextOutlineWidths.Contains(width))
        {
            return;
        }

        _textOutline = _textOutline with { Width = width };
        ShowTextOutline();
        SaveSettings();
    }

    private void OnTextOutlineColorSelected(object? sender, Color color)
    {
        _textOutline = _textOutline with { Color = Solid(color) };
        ShowTextOutline();
        SaveSettings();
    }

    private void OnMoreTextOutlineColorsRequested(object? sender, EventArgs e)
    {
        if (AskColor(_textOutline.Color) is Color color)
        {
            OnTextOutlineColorSelected(sender, color);
        }
    }

    private void OnTextCommitted(object? sender, TextInputModel input)
    {
        // Spaces at the end would only make the text wider; an empty text adds nothing
        string text = input.Text.ReplaceLineEndings("\n").TrimEnd();

        if (text.Length > 0)
        {
            Add(new TextAnnotationModel(
                input.Location,
                text,
                DrawingColor,
                Settings.Size,
                _textOutline.Width > 0 ? _textOutline : null));
        }
    }

    private void OnUndoRequested(object? sender, EventArgs e)
    {
        if (Page.Annotations.Count == 0)
        {
            return;
        }

        Page.Undone.Push(Page.Annotations[^1]);
        Page.Annotations.RemoveAt(Page.Annotations.Count - 1);
        Changed();
    }

    private void OnRedoRequested(object? sender, EventArgs e)
    {
        if (Page.Undone.Count == 0)
        {
            return;
        }

        Page.Annotations.Add(Page.Undone.Pop());
        Changed();
    }

    private void OnSnipAdded(object? sender, EventArgs e)
    {
        _pages.Add(new SnipPage());
        ShowPage(_pages.Count - 1);
    }

    private void OnSnipStepRequested(object? sender, int step)
    {
        int page = _current + step;

        if (page >= 0 && page < _pages.Count && page != _current)
        {
            ShowPage(page);
        }
    }

    private void ShowPage(int page)
    {
        // A drag can't go on over another snip
        _start = null;
        _points = [];
        _drawing = null;

        _current = page;
        _view.ShowSnip(page, _pages.Count);
        Changed();
    }

    private void OnCopyRequested(object? sender, EventArgs e)
    {
        _view.CopyImageToClipboard();
    }

    private void OnSaveRequested(object? sender, EventArgs e)
    {
        DateTimeOffset now = _timeProvider.GetLocalNow();
        int page = _current;
        string? path = _view.AskSavePath(
            $"Snip {now.ToString("yyyy-MM-dd HHmmss", CultureInfo.InvariantCulture)}.png");

        if (path is null)
        {
            return;
        }

        // A new snip can arrive while the dialog is open, from a global hotkey; the one asked about is what's saved
        if (_current != page)
        {
            ShowPage(page);
        }

        _view.SaveImage(path);
    }

    private AnnotationModel CreateAnnotation(Point start, Point end)
    {
        int width = Settings.Size;
        Color color = DrawingColor;

        // The pen and the highlighter differ only in their settings
        return _tool switch
        {
            SnipTool.Arrow => new ArrowAnnotationModel(start, end, color, width, _arrowHeads),
            SnipTool.Rectangle => new RectangleAnnotationModel(FromCorners(start, end), color, width),
            SnipTool.Blur => new BlurAnnotationModel(FromCorners(start, end)),
            _ => new StrokeAnnotationModel([.. _points], color, width),
        };
    }

    /// <summary>
    ///     A click with the pen or the highlighter leaves a dot; with the arrow, the rectangle or the blur it leaves
    ///     nothing.
    /// </summary>
    private static bool IsVisible(AnnotationModel annotation) => annotation switch
    {
        ArrowAnnotationModel arrow => Math.Abs(arrow.To.X - arrow.From.X) + Math.Abs(arrow.To.Y - arrow.From.Y) >= 4,
        RectangleAnnotationModel rectangle => rectangle.Bounds.Width >= 2 && rectangle.Bounds.Height >= 2,
        BlurAnnotationModel blur => blur.Bounds.Width >= 2 && blur.Bounds.Height >= 2,
        _ => true,
    };

    /// <summary>Keeps a new annotation; what was undone can't be redone after it.</summary>
    private void Add(AnnotationModel annotation)
    {
        Page.Annotations.Add(annotation);
        Page.Undone.Clear();
        Changed();
    }

    private void Changed()
    {
        Refresh();
        _view.CopyImageToClipboard();
    }

    private void Refresh()
    {
        _view.ShowAnnotations(_drawing is null ? [.. Page.Annotations] : [.. Page.Annotations, _drawing]);
        _view.ShowUndoRedo(Page.Annotations.Count > 0, Page.Undone.Count > 0);
    }

    private static Rectangle FromCorners(Point a, Point b) => Rectangle.FromLTRB(
        Math.Min(a.X, b.X),
        Math.Min(a.Y, b.Y),
        Math.Max(a.X, b.X),
        Math.Max(a.Y, b.Y));

    /// <summary>What a snip of the window has drawn over it.</summary>
    private sealed class SnipPage
    {
        public List<AnnotationModel> Annotations { get; } = [];

        public Stack<AnnotationModel> Undone { get; } = new();
    }
}
