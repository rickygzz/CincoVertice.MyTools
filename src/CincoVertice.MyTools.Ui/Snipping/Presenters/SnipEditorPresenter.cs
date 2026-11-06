using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Services;
using CincoVertice.MyTools.Ui.Snipping.Views;

namespace CincoVertice.MyTools.Ui.Snipping.Presenters;

/// <summary>
///     One snip editor: turns drags on the view into annotations with the current tool, and keeps them with undo and
///     redo. It can hold several snips, each with its own annotations and undo; the view keeps their images. Each tool
///     keeps its own size, transparency and color; these and the tool are saved
///     (<see cref="ISnipEditorSettingsStore"/>), so the next editor starts with them. After every change, and when
///     another snip is shown, the snip shown is copied to the clipboard again, so it can be pasted right away. The
///     select tool picks one annotation at a time, to move, scale, change or delete it, with the toolbar then showing
///     its color and size instead of a tool's.
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

    /// <summary>How round corners can be, in pixels; 0 is square.</summary>
    public static IReadOnlyList<int> CornerRadii { get; } = [0, 4, 8, 12, 16, 24, 32, 48];

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

        // Not used: a blur has no color, size or transparency, and the select tool doesn't draw
        [SnipTool.Blur] = new SnipToolSettingsModel(4, 0, Palette[0]),
        [SnipTool.Select] = new SnipToolSettingsModel(4, 0, Palette[0]),
    };

    // Colors picked with More colors that aren't in the palette, newest first, at most MaxCustomColors
    private readonly List<Color> _customColors = [];

    private SnipTool _tool = SnipTool.Pen;
    private SnipArrowHeads _arrowHeads = SnipArrowHeads.End;

    // What the rectangle tool's corners get
    private int _cornerRadius = 12;

    // Off until a width is chosen; white shows on most of what a text is written over
    private SnipTextOutlineModel _textOutline = new(0, Rgb(0xFFFFFF));

    // The drag in progress: where it started, the points of a freehand line, and what it would add
    private Point? _start;
    private List<Point> _points = [];
    private AnnotationModel? _drawing;

    // Which annotation of the snip shown is selected, and the drag of it in progress with what it would become
    private int? _selected;
    private SelectionDrag? _selectionDrag;
    private AnnotationModel? _preview;

    // The selected text while it's being typed over, when it isn't drawn since the text box stands in for it
    private bool _isEditingText;

    private SnipCursor _cursor = SnipCursor.Cross;

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
        _view.PointerDoubleClicked += OnPointerDoubleClicked;
        _view.PointerHovered += OnPointerHovered;
        _view.PointerRightClicked += OnPointerRightClicked;
        _view.DeleteRequested += OnDeleteRequested;
        _view.ResetRequested += OnResetRequested;
        _view.NudgeRequested += OnNudgeRequested;
        _view.DeselectRequested += OnDeselectRequested;
        _view.TextEscaped += OnTextEscaped;
        _view.ToolSelected += OnToolSelected;
        _view.ColorSelected += OnColorSelected;
        _view.MoreColorsRequested += OnMoreColorsRequested;
        _view.SizeSelected += OnSizeSelected;
        _view.TransparencySelected += OnTransparencySelected;
        _view.ArrowHeadsSelected += OnArrowHeadsSelected;
        _view.CornerRadiusSelected += OnCornerRadiusSelected;
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
        _view.CloseRequested += OnCloseRequested;
    }

    /// <summary>The annotations kept on the snip shown, oldest first; without the one being drawn.</summary>
    public IReadOnlyList<AnnotationModel> Annotations => Page.Annotations;

    private SnipPage Page => _pages[_current];

    private AnnotationModel? Selected => _selected is int index ? Page.Annotations[index] : null;

    private SnipToolSettingsModel Settings => _settings[_tool];

    /// <summary>A color, as see-through as this (halves round up).</summary>
    private static Color WithTransparency(Color color, int transparency)
    {
        double alpha = 255 * (100 - transparency) / 100.0;

        return Color.FromArgb((int)Math.Round(alpha, MidpointRounding.AwayFromZero), color);
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

        if (saved.CornerRadius is int radius && CornerRadii.Contains(radius))
        {
            _cornerRadius = radius;
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
            _textOutline,
            _cornerRadius));
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
        ShowCursor(_tool == SnipTool.Select ? SnipCursor.Default : SnipCursor.Cross);
        ShowChoices();
    }

    /// <summary>The toolbar's choices: those of what's selected, or else those of the tool.</summary>
    private void ShowChoices()
    {
        AnnotationModel? selected = Selected;

        _view.ShowDrawingChoices(selected is null
            ? _tool is not (SnipTool.Blur or SnipTool.Select)
            : selected is not BlurAnnotationModel);

        SnipToolSettingsModel settings = selected is null ? Settings : SettingsOf(selected);
        IReadOnlyList<int> sizes = selected switch
        {
            null => SizesOf(_tool),
            TextAnnotationModel => FontSizes,

            // Its frame is its size
            HighlightAnnotationModel => [],
            _ => LineSizes,
        };

        _view.ShowColor(settings.Color);
        _view.ShowSizes(sizes, settings.Size);
        _view.ShowTransparencies(Transparencies, settings.Transparency);

        if (selected is null)
        {
            _view.ShowArrowHeads(_tool == SnipTool.Arrow ? _arrowHeads : null);
            _view.ShowCornerRadius(CornerRadii, _tool == SnipTool.Rectangle ? _cornerRadius : null);
        }
        else
        {
            _view.ShowArrowHeads(selected is ArrowAnnotationModel arrow ? arrow.Heads : null);
            _view.ShowCornerRadius(CornerRadii, selected switch
            {
                RectangleAnnotationModel rectangle => rectangle.CornerRadius,
                HighlightAnnotationModel highlight => highlight.CornerRadius,
                _ => null,
            });
        }

        ShowTextOutline();
    }

    private void ShowTextOutline()
    {
        SnipTextOutlineModel? outline = _tool == SnipTool.Text ? _textOutline : null;

        if (Selected is { } selected)
        {
            outline = selected is TextAnnotationModel text ? text.Outline ?? _textOutline with { Width = 0 } : null;
        }

        _view.ShowTextOutline(TextOutlineWidths, outline);
    }

    /// <summary>What the toolbar shows for an annotation that's selected.</summary>
    private static SnipToolSettingsModel SettingsOf(AnnotationModel annotation)
    {
        int size = annotation switch
        {
            StrokeAnnotationModel stroke => stroke.Width,
            ArrowAnnotationModel arrow => arrow.Width,
            RectangleAnnotationModel rectangle => rectangle.Width,
            TextAnnotationModel text => text.FontSize,
            _ => 0,
        };
        int transparency = (int)Math.Round(100 - annotation.Color.A * 100 / 255.0, MidpointRounding.AwayFromZero);

        return new SnipToolSettingsModel(size, transparency, Solid(annotation.Color));
    }

    private void ShowCursor(SnipCursor cursor)
    {
        if (cursor != _cursor)
        {
            _cursor = cursor;
            _view.ShowCursor(cursor);
        }
    }

    private Size MeasureText(string text, int fontSize) => _view.MeasureText(text, fontSize);

    private void Deselect()
    {
        _selected = null;
        _selectionDrag = null;
        _preview = null;
        _isEditingText = false;
    }

    /// <summary>Keeps a change to the selected annotation; what was undone can't be redone after it.</summary>
    private void ChangeSelected(Func<AnnotationModel, AnnotationModel> change)
    {
        if (_selected is not int index)
        {
            return;
        }

        AnnotationModel current = Page.Annotations[index];
        AnnotationModel changed = change(current);

        if (changed != current)
        {
            Replace(index, changed with { Original = current.Original ?? current });
        }

        ShowChoices();
    }

    private void Replace(int index, AnnotationModel replacement)
    {
        Commit([.. Page.Annotations.Select((annotation, i) => i == index ? replacement : annotation)]);
    }

    /// <returns>What a click at the point is on: the one on top, or else what's selected, if it's inside its frame.</returns>
    private int? TargetAt(Point point)
    {
        // The one on top is the last one drawn
        for (int i = Page.Annotations.Count - 1; i >= 0; i--)
        {
            if (AnnotationGeometry.Hits(Page.Annotations[i], point, MeasureText))
            {
                return i;
            }
        }

        // Inside the frame also holds what's selected, as a rectangle has only its outline to take hold of
        return Selected is { } selected && IsInsideFrame(selected, point) ? _selected : null;
    }

    private void PressWithSelectTool(Point point)
    {
        if (Selected is { } selected)
        {
            SnipSelectionModel selection = AnnotationGeometry.SelectionOf(selected, MeasureText);

            if (AnnotationGeometry.HandleAt(selection, point) is int handle)
            {
                _selectionDrag = new SelectionDrag(point, handle, selected);

                return;
            }
        }

        if (TargetAt(point) is int target)
        {
            Select(target);
            _selectionDrag = new SelectionDrag(point, null, Page.Annotations[target]);

            return;
        }

        if (_selected is not null)
        {
            Deselect();
            ShowChoices();
            Refresh();
        }
    }

    private bool IsInsideFrame(AnnotationModel annotation, Point point) =>
        AnnotationGeometry.SelectionOf(annotation, MeasureText).Box?.Contains(point) == true;

    private void Select(int index)
    {
        _selected = index;
        ShowChoices();
        Refresh();
    }

    private void OnPointerPressed(object? sender, Point point)
    {
        if (_tool == SnipTool.Select)
        {
            PressWithSelectTool(point);

            return;
        }

        if (_tool == SnipTool.Text)
        {
            // The text box can't show transparency; the text is drawn see-through once it's added
            _view.BeginTextInput(point, Settings.Color, Settings.Size, string.Empty);

            return;
        }

        _start = point;
        _points = [point];
        _drawing = CreateAnnotation(point, point);
        Refresh();
    }

    private void OnPointerMoved(object? sender, Point point)
    {
        if (_selectionDrag is { } drag)
        {
            _preview = point == drag.Start ? null : Dragged(drag, point);
            Refresh();

            return;
        }

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
        if (_selectionDrag is { } drag)
        {
            _selectionDrag = null;
            _preview = null;

            if (point == drag.Start)
            {
                Refresh();
            }
            else
            {
                ChangeSelected(_ => Dragged(drag, point));
            }

            return;
        }

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

    private AnnotationModel Dragged(SelectionDrag drag, Point point) => drag.Handle is int handle
        ? AnnotationGeometry.Resized(drag.Original, handle, point, MeasureText)
        : AnnotationGeometry.Moved(drag.Original, new Size(point.X - drag.Start.X, point.Y - drag.Start.Y));

    /// <summary>On a selected text, lets the user type over it.</summary>
    private void OnPointerDoubleClicked(object? sender, Point point)
    {
        if (_tool != SnipTool.Select || Selected is not TextAnnotationModel text
            || !AnnotationGeometry.Hits(text, point, MeasureText))
        {
            return;
        }

        _selectionDrag = null;
        _preview = null;
        _isEditingText = true;
        Refresh();

        // Solid: the text box can't show transparency
        _view.BeginTextInput(text.Location, Solid(text.Color), text.FontSize, text.Text);
    }

    private void OnPointerHovered(object? sender, Point point)
    {
        if (_tool != SnipTool.Select)
        {
            return;
        }

        SnipCursor cursor = SnipCursor.Default;

        if (Selected is { } selected)
        {
            SnipSelectionModel selection = AnnotationGeometry.SelectionOf(selected, MeasureText);

            if (AnnotationGeometry.HandleAt(selection, point) is int handle)
            {
                ShowCursor(AnnotationGeometry.CursorOf(selection, handle));

                return;
            }

            if (IsInsideFrame(selected, point))
            {
                cursor = SnipCursor.Move;
            }
        }

        if (Page.Annotations.Any(annotation => AnnotationGeometry.Hits(annotation, point, MeasureText)))
        {
            cursor = SnipCursor.Move;
        }

        ShowCursor(cursor);
    }

    /// <summary>Over something with the select tool, selects it and offers what can be done to it.</summary>
    private void OnPointerRightClicked(object? sender, Point point)
    {
        if (_tool != SnipTool.Select || _selectionDrag is not null)
        {
            return;
        }

        bool isOnHandle = Selected is { } selected
            && AnnotationGeometry.HandleAt(AnnotationGeometry.SelectionOf(selected, MeasureText), point) is not null;

        if (!isOnHandle)
        {
            if (TargetAt(point) is not int target)
            {
                return;
            }

            Select(target);
        }

        _view.ShowContextMenu(point, Selected!.Original is not null);
    }

    private void OnResetRequested(object? sender, EventArgs e)
    {
        if (_selected is int index && Page.Annotations[index].Original is { } original)
        {
            Replace(index, original);
            ShowChoices();
        }
    }

    private void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (_selected is not int index)
        {
            return;
        }

        Deselect();
        Commit([.. Page.Annotations.Where((_, i) => i != index)]);
        ShowChoices();
    }

    private void OnNudgeRequested(object? sender, Size delta)
    {
        if (_selectionDrag is null)
        {
            ChangeSelected(annotation => AnnotationGeometry.Moved(annotation, delta));
        }
    }

    private void OnDeselectRequested(object? sender, EventArgs e)
    {
        Deselect();
        ShowChoices();
        Refresh();
    }

    /// <summary>
    ///     An empty text is gone, as with any other way of ending it. One with something in it is kept and selected, with
    ///     the select tool, ready to be moved.
    /// </summary>
    private void OnTextEscaped(object? sender, TextInputModel input)
    {
        int? editing = _isEditingText ? _selected : null;

        OnTextCommitted(sender, input);

        if (CleanText(input.Text).Length == 0)
        {
            return;
        }

        if (_tool != SnipTool.Select)
        {
            OnToolSelected(sender, SnipTool.Select);
        }

        Select(editing ?? Page.Annotations.Count - 1);
    }

    /// <summary>The color without transparency, compared by its value only, as palette colors are.</summary>
    private static Color Solid(Color color) => Color.FromArgb(255, color.R, color.G, color.B);

    private static Color Rgb(int rgb) => Solid(Color.FromArgb(rgb));

    private void OnToolSelected(object? sender, SnipTool tool)
    {
        _tool = tool;
        Deselect();
        ShowTool();
        Refresh();
        SaveSettings();
    }

    private void OnColorSelected(object? sender, Color color)
    {
        if (Selected is not null)
        {
            // As see-through as it was
            ChangeSelected(annotation => annotation with { Color = Color.FromArgb(annotation.Color.A, Solid(color)) });

            return;
        }

        // Only the current tool's: the pen can stay red while the highlighter is yellow
        _settings[_tool] = Settings with { Color = Solid(color) };
        _view.ShowColor(Settings.Color);
        SaveSettings();
    }

    private void OnMoreColorsRequested(object? sender, EventArgs e)
    {
        Color current = Selected is { } selected ? Solid(selected.Color) : Settings.Color;

        if (AskColor(current) is Color color)
        {
            OnColorSelected(sender, color);

            // A change to what's selected saves nothing by itself, and the custom color is to be kept
            if (Selected is not null)
            {
                SaveSettings();
            }
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

        if (Selected is not null)
        {
            ChangeSelected(annotation => annotation switch
            {
                StrokeAnnotationModel stroke => stroke with { Width = size },
                ArrowAnnotationModel arrow => arrow with { Width = size },
                RectangleAnnotationModel rectangle => rectangle with { Width = size },
                TextAnnotationModel text => text with { FontSize = size },
                _ => annotation,
            });

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

        if (Selected is not null)
        {
            ChangeSelected(annotation => annotation with { Color = WithTransparency(annotation.Color, clamped) });

            return;
        }

        _settings[_tool] = Settings with { Transparency = clamped };
        _view.ShowTransparencies(Transparencies, clamped);
        SaveSettings();
    }

    private void OnArrowHeadsSelected(object? sender, SnipArrowHeads heads)
    {
        if (Selected is not null)
        {
            ChangeSelected(annotation => annotation is ArrowAnnotationModel arrow ? arrow with { Heads = heads } : annotation);

            return;
        }

        _arrowHeads = heads;
        _view.ShowArrowHeads(heads);
        SaveSettings();
    }

    private void OnCornerRadiusSelected(object? sender, int radius)
    {
        if (!CornerRadii.Contains(radius))
        {
            return;
        }

        if (Selected is not null)
        {
            ChangeSelected(annotation => annotation switch
            {
                RectangleAnnotationModel rectangle => rectangle with { CornerRadius = radius },
                HighlightAnnotationModel highlight => highlight with { CornerRadius = radius },
                _ => annotation,
            });

            return;
        }

        _cornerRadius = radius;
        _view.ShowCornerRadius(CornerRadii, radius);
        SaveSettings();
    }

    private void OnTextOutlineWidthSelected(object? sender, int width)
    {
        if (!TextOutlineWidths.Contains(width))
        {
            return;
        }

        if (Selected is not null)
        {
            ChangeSelectedOutline(outline => outline with { Width = width });

            return;
        }

        _textOutline = _textOutline with { Width = width };
        ShowTextOutline();
        SaveSettings();
    }

    /// <summary>A text without an edge gets one of no width, which keeps its color for when a width is chosen.</summary>
    private void ChangeSelectedOutline(Func<SnipTextOutlineModel, SnipTextOutlineModel> change)
    {
        ChangeSelected(annotation => annotation is TextAnnotationModel text
            ? text with { Outline = change(text.Outline ?? _textOutline with { Width = 0 }) }
            : annotation);
    }

    private void OnTextOutlineColorSelected(object? sender, Color color)
    {
        if (Selected is not null)
        {
            ChangeSelectedOutline(outline => outline with { Color = Solid(color) });

            return;
        }

        _textOutline = _textOutline with { Color = Solid(color) };
        ShowTextOutline();
        SaveSettings();
    }

    private void OnMoreTextOutlineColorsRequested(object? sender, EventArgs e)
    {
        Color current = Selected is TextAnnotationModel { Outline: { } outline } ? outline.Color : _textOutline.Color;

        if (AskColor(current) is Color color)
        {
            OnTextOutlineColorSelected(sender, color);

            if (Selected is not null)
            {
                SaveSettings();
            }
        }
    }

    /// <summary>Spaces at the end would only make a text wider.</summary>
    private static string CleanText(string text) => text.ReplaceLineEndings("\n").TrimEnd();

    private void OnTextCommitted(object? sender, TextInputModel input)
    {
        // An empty text adds nothing
        string text = CleanText(input.Text);

        if (_isEditingText)
        {
            _isEditingText = false;

            // Emptied, it's gone, as a new text with nothing in it would be
            if (text.Length == 0)
            {
                OnDeleteRequested(sender, EventArgs.Empty);
            }
            else
            {
                ChangeSelected(annotation => annotation is TextAnnotationModel edited ? edited with { Text = text } : annotation);
                Refresh();
            }

            return;
        }

        if (text.Length > 0)
        {
            Add(new TextAnnotationModel(
                input.Location,
                text,
                WithTransparency(Settings.Color, Settings.Transparency),
                Settings.Size,
                _textOutline.Width > 0 ? _textOutline : null));
        }
    }

    private void OnUndoRequested(object? sender, EventArgs e)
    {
        if (Page.Undo.Count == 0)
        {
            return;
        }

        Page.Redo.Push(Page.Annotations);
        Page.Annotations = Page.Undo.Pop();
        Restored();
    }

    private void OnRedoRequested(object? sender, EventArgs e)
    {
        if (Page.Redo.Count == 0)
        {
            return;
        }

        Page.Undo.Push(Page.Annotations);
        Page.Annotations = Page.Redo.Pop();
        Restored();
    }

    /// <summary>After undo or redo the annotations are other ones, so what was selected may not be there.</summary>
    private void Restored()
    {
        Deselect();
        ShowChoices();
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

        bool hadSelection = _selected is not null;

        Deselect();
        _current = page;
        _view.ShowSnip(page, _pages.Count);

        if (hadSelection)
        {
            ShowChoices();
        }

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
        Page.Saved = Page.Annotations;
    }

    /// <summary>
    ///     With more than one snip that would be lost, asks first; one alone closes at once, as when a snip is taken,
    ///     pasted and done with.
    /// </summary>
    private void OnCloseRequested(object? sender, CancelEventArgs e)
    {
        int unsaved = _pages.Count(page => !ReferenceEquals(page.Saved, page.Annotations));

        if (unsaved > 1 && !_view.AskDiscardSnips(unsaved))
        {
            e.Cancel = true;
        }
    }

    private AnnotationModel CreateAnnotation(Point start, Point end)
    {
        int width = Settings.Size;
        Color color = WithTransparency(Settings.Color, Settings.Transparency);

        // The pen and the highlighter differ only in their settings
        return _tool switch
        {
            SnipTool.Arrow => new ArrowAnnotationModel(start, end, color, width, _arrowHeads),
            SnipTool.Rectangle => new RectangleAnnotationModel(FromCorners(start, end), color, width, _cornerRadius),
            SnipTool.Blur => new BlurAnnotationModel(FromCorners(start, end)),
            _ => new StrokeAnnotationModel([.. _points], color, width, _tool == SnipTool.Highlighter),
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

    private void Add(AnnotationModel annotation)
    {
        Commit([.. Page.Annotations, annotation]);
    }

    /// <summary>Keeps these annotations, with the earlier ones to undo to; what was undone can't be redone after it.</summary>
    private void Commit(IReadOnlyList<AnnotationModel> annotations)
    {
        Page.Undo.Push(Page.Annotations);
        Page.Redo.Clear();
        Page.Annotations = annotations;
        Changed();
    }

    private void Changed()
    {
        Refresh();
        _view.CopyImageToClipboard();
    }

    private void Refresh()
    {
        List<AnnotationModel> shown = [];

        for (int i = 0; i < Page.Annotations.Count; i++)
        {
            if (i != _selected)
            {
                shown.Add(Page.Annotations[i]);
            }
            else if (!_isEditingText)
            {
                shown.Add(_preview ?? Page.Annotations[i]);
            }
        }

        if (_drawing is not null)
        {
            shown.Add(_drawing);
        }

        _view.ShowAnnotations(shown);

        AnnotationModel? selected = _isEditingText ? null : _preview ?? Selected;

        _view.ShowSelection(selected is null ? null : AnnotationGeometry.SelectionOf(selected, MeasureText));
        _view.ShowUndoRedo(Page.Undo.Count > 0, Page.Redo.Count > 0);
    }

    private static Rectangle FromCorners(Point a, Point b) => Rectangle.FromLTRB(
        Math.Min(a.X, b.X),
        Math.Min(a.Y, b.Y),
        Math.Max(a.X, b.X),
        Math.Max(a.Y, b.Y));

    /// <summary>What a snip of the window has drawn over it.</summary>
    private sealed class SnipPage
    {
        public IReadOnlyList<AnnotationModel> Annotations { get; set; } = [];

        public Stack<IReadOnlyList<AnnotationModel>> Undo { get; } = new();

        public Stack<IReadOnlyList<AnnotationModel>> Redo { get; } = new();

        /// <summary>
        ///     The annotations it was last saved with; null when it never was. Undo can bring them back, which makes it
        ///     saved again: the lists are kept as they are, so the same list is the same snip.
        /// </summary>
        public IReadOnlyList<AnnotationModel>? Saved { get; set; }
    }

    /// <summary>A drag of the selected annotation.</summary>
    /// <param name="Start">Where the button was pressed.</param>
    /// <param name="Handle">Which handle is dragged; null when the whole annotation is moved.</param>
    /// <param name="Original">The annotation as it was before the drag.</param>
    private sealed record SelectionDrag(Point Start, int? Handle, AnnotationModel Original);
}
