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

    /// <summary>How wide a step's circle can be, in pixels.</summary>
    public static IReadOnlyList<int> StepSizes { get; } = [16, 20, 24, 28, 32, 40, 48, 64, 80];

    /// <summary>How large an emoji can be, in pixels.</summary>
    public static IReadOnlyList<int> EmojiSizes { get; } = [16, 20, 24, 32, 40, 48, 64, 80, 96, 128, 160, 200];

    /// <summary>The transparencies to choose from, in percent; 100 would be invisible.</summary>
    public static IReadOnlyList<int> Transparencies { get; } = [0, 10, 20, 30, 40, 50, 60, 70, 80, 90];

    // How far down and to the right a copy goes from what it's a copy of, in pixels
    private const int DuplicateOffset = 10;

    /// <summary>How large the snip can be shown, in percent.</summary>
    public static IReadOnlyList<int> ZoomLevels { get; } = [25, 33, 50, 67, 75, 100, 125, 150, 200, 300, 400, 600, 800];

    /// <summary>How round corners can be, in pixels; 0 is square.</summary>
    public static IReadOnlyList<int> CornerRadii { get; } = [0, 4, 8, 12, 16, 24, 32, 48];

    /// <summary>The widths of the edge around a text's letters to choose from, in pixels; 0 is no edge.</summary>
    public static IReadOnlyList<int> TextOutlineWidths { get; } = [0, 1, 2, 3, 4, 5, 6, 8, 10, 12];

    private readonly ISnipEditorView _view;
    private readonly TimeProvider _timeProvider;
    private readonly ISnipEditorSettingsStore _settingsStore;
    private readonly ITextRecognizer _textRecognizer;

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

        [SnipTool.Step] = new SnipToolSettingsModel(28, 0, Palette[0]),

        // Its color isn't used: an emoji keeps its own colors
        [SnipTool.Emoji] = new SnipToolSettingsModel(48, 0, Palette[16]),
        [SnipTool.SmoothPen] = new SnipToolSettingsModel(4, 0, Palette[0]),

        // Not used: a blur has no color, size or transparency, and the select tool doesn't draw
        [SnipTool.Blur] = new SnipToolSettingsModel(4, 0, Palette[0]),
        [SnipTool.Select] = new SnipToolSettingsModel(4, 0, Palette[0]),
        [SnipTool.Crop] = new SnipToolSettingsModel(4, 0, Palette[0]),
    };

    // Colors picked with More colors that aren't in the palette, newest first, at most MaxCustomColors
    private readonly List<Color> _customColors = [];

    // The emoji the emoji tool drops, and those dropped last, most recent first, at most EmojiCatalog.MaxRecent
    private string _emoji = EmojiCatalog.DefaultEmoji;
    private readonly List<string> _recentEmojis = [];

    private SnipTool _tool = SnipTool.Pen;
    private SnipArrowHeads _arrowHeads = SnipArrowHeads.End;

    // What the rectangle tool's corners get
    private int _cornerRadius = 12;

    // A snip is mostly of something else; the editor in it would be in the way
    private bool _hideWhileSnipping = true;

    // How large the snip is shown, in percent
    private int _zoomPercent = 100;

    // Off until a width is chosen; white shows on most of what a text is written over
    private SnipTextOutlineModel _textOutline = new(0, Rgb(0xFFFFFF));

    // The drag in progress: where it started, the points of a freehand line, and what it would add
    private Point? _start;
    private List<Point> _points = [];
    private AnnotationModel? _drawing;

    // Which annotation of the snip shown is selected, and the drag of it in progress with what it would become
    private IReadOnlyList<int> _selection = [];
    private SelectionDrag? _selectionDrag;

    // What the selected annotations would become, in the selection's order, while they're dragged
    private IReadOnlyList<AnnotationModel>? _previews;

    // The box being dragged to select what's in it: where it started, whether it adds to what's selected, and what it
    // covers so far
    private Point? _bandStart;
    private bool _bandAdds;
    private Rectangle? _band;

    // The selected text while it's being typed over, when it isn't drawn since the text box stands in for it
    private bool _isEditingText;

    // While the text of a snip is being read, asking again does nothing
    private bool _isReadingText;

    private SnipCursor _cursor = SnipCursor.Cross;

    // The crop being adjusted before it's applied, and the drag of it; and the crop the view shows, to tell it only of a
    // change
    private Rectangle? _pendingCrop;
    private CropDrag? _cropDrag;
    private Rectangle? _shownCrop;

    // What to go back to after cropping, which is done once
    private SnipTool _toolBeforeCrop = SnipTool.Select;

    /// <param name="settingsStore">
    ///     Where the tool, each tool's settings and the custom colors are kept: the editor starts with the ones saved
    ///     last, and saves them on every change.
    /// </param>
    /// <param name="textRecognizer">Reads the text of the snip shown, for Copy text.</param>
    public SnipEditorPresenter(
        ISnipEditorView view,
        TimeProvider timeProvider,
        ISnipEditorSettingsStore settingsStore,
        ITextRecognizer textRecognizer)
    {
        _view = view;
        _timeProvider = timeProvider;
        _settingsStore = settingsStore;
        _textRecognizer = textRecognizer;

        Apply(settingsStore.Load());

        _view.PointerPressed += OnPointerPressed;
        _view.PointerMoved += OnPointerMoved;
        _view.PointerReleased += OnPointerReleased;
        _view.PointerDoubleClicked += OnPointerDoubleClicked;
        _view.SelectAllRequested += OnSelectAllRequested;
        _view.PointerHovered += OnPointerHovered;
        _view.PointerRightClicked += OnPointerRightClicked;
        _view.DeleteRequested += OnDeleteRequested;
        _view.ResetRequested += OnResetRequested;
        _view.DuplicateRequested += OnDuplicateRequested;
        _view.BringToFrontRequested += OnBringToFrontRequested;
        _view.SendToBackRequested += OnSendToBackRequested;
        _view.NudgeRequested += OnNudgeRequested;
        _view.DeselectRequested += OnDeselectRequested;
        _view.TextEscaped += OnTextEscaped;
        _view.ToolSelected += OnToolSelected;
        _view.EmojiSelected += OnEmojiSelected;
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
        _view.CopyTextRequested += OnCopyTextRequested;
        _view.SaveRequested += OnSaveRequested;
        _view.CloseRequested += OnCloseRequested;
        _view.HideWhileSnippingSelected += OnHideWhileSnippingSelected;
        _view.ZoomRequested += OnZoomRequested;
        _view.CropApplyRequested += OnCropApplyRequested;
        _view.CropCancelRequested += OnCropCancelRequested;
    }

    /// <summary>The annotations kept on the snip shown, oldest first; without the one being drawn.</summary>
    public IReadOnlyList<AnnotationModel> Annotations => Page.Annotations;

    private SnipPage Page => _pages[_current];

    /// <summary>The one selected, for what only one at a time can have, like handles; null with none or several.</summary>
    private AnnotationModel? Selected => _selection.Count == 1 ? Page.Annotations[_selection[0]] : null;

    private int? SelectedIndex => _selection.Count == 1 ? _selection[0] : null;

    /// <summary>Everything selected, in the order they're drawn.</summary>
    private IReadOnlyList<AnnotationModel> SelectedAll => [.. _selection.Select(index => Page.Annotations[index])];

    private bool HasSelection => _selection.Count > 0;

    /// <summary>What the toolbar shows when several are selected: the first that has a color and a size.</summary>
    private AnnotationModel? Primary => SelectedAll.FirstOrDefault(annotation => annotation is not BlurAnnotationModel);

    private double Zoom => _zoomPercent / 100.0;

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
        _view.ShowHideWhileSnipping(_hideWhileSnipping);
        _view.ShowZoom(_zoomPercent);
        ShowTool();
        ShowPage(0);
    }

    /// <summary>The sizes a tool can use: font sizes for the text tool, circle sizes for steps, line widths for the others.</summary>
    public static IReadOnlyList<int> SizesOf(SnipTool tool) => tool switch
    {
        SnipTool.Text => FontSizes,
        SnipTool.Step => StepSizes,
        SnipTool.Emoji => EmojiSizes,
        _ => LineSizes,
    };

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

        _hideWhileSnipping = saved.HideWhileSnipping;

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

        if (!string.IsNullOrEmpty(saved.Emoji))
        {
            _emoji = saved.Emoji;
        }

        _recentEmojis.AddRange((saved.RecentEmojis ?? [])
            .Where(emoji => !string.IsNullOrEmpty(emoji))
            .Distinct()
            .Take(EmojiCatalog.MaxRecent));

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
            _cornerRadius,
            _hideWhileSnipping,
            _emoji,
            [.. _recentEmojis]));
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

    /// <summary>
    ///     The toolbar's choices: those of what's selected, or else those of the tool. With several selected, each choice
    ///     shows when any of them has it, as the first that has it is; a change goes to all of them that have it.
    /// </summary>
    private void ShowChoices()
    {
        if (!HasSelection)
        {
            _view.ShowDrawingChoices(_tool is not (SnipTool.Blur or SnipTool.Select or SnipTool.Crop));
            _view.ShowColor(Settings.Color);
            _view.ShowSizes(SizesOf(_tool), Settings.Size);
            _view.ShowTransparencies(Transparencies, Settings.Transparency);
            _view.ShowArrowHeads(_tool == SnipTool.Arrow ? _arrowHeads : null);
            _view.ShowCornerRadius(CornerRadii, _tool == SnipTool.Rectangle ? _cornerRadius : null);
            ShowTextOutline();
            _view.ShowEmojis([.. _recentEmojis], _tool == SnipTool.Emoji ? _emoji : null);
            _view.ShowColorChoice(_tool != SnipTool.Emoji);

            return;
        }

        IReadOnlyList<AnnotationModel> selected = SelectedAll;

        _view.ShowDrawingChoices(Primary is not null);

        if (Primary is { } primary)
        {
            SnipToolSettingsModel settings = SettingsOf(primary);

            _view.ShowColor(settings.Color);
            _view.ShowSizes(SizesFor(primary), settings.Size);
            _view.ShowTransparencies(Transparencies, settings.Transparency);
        }

        _view.ShowArrowHeads(selected.OfType<ArrowAnnotationModel>().FirstOrDefault()?.Heads);
        _view.ShowCornerRadius(CornerRadii, selected.Select(CornerRadiusOf).FirstOrDefault(radius => radius is not null));
        ShowTextOutline();

        // An emoji keeps its own colors: the color shows only when something else is selected too
        _view.ShowEmojis([.. _recentEmojis], selected.OfType<EmojiAnnotationModel>().FirstOrDefault()?.Emoji);
        _view.ShowColorChoice(selected.Any(annotation => annotation is not EmojiAnnotationModel));
    }

    private static int? CornerRadiusOf(AnnotationModel annotation) => annotation switch
    {
        RectangleAnnotationModel rectangle => rectangle.CornerRadius,
        HighlightAnnotationModel highlight => highlight.CornerRadius,
        _ => null,
    };

    /// <summary>The sizes an annotation can be given; none for a scaled highlight, whose frame is its size.</summary>
    private static IReadOnlyList<int> SizesFor(AnnotationModel annotation) => annotation switch
    {
        TextAnnotationModel => FontSizes,
        StepAnnotationModel => StepSizes,
        EmojiAnnotationModel => EmojiSizes,
        HighlightAnnotationModel or BlurAnnotationModel => [],
        _ => LineSizes,
    };

    private void ShowTextOutline()
    {
        SnipTextOutlineModel? outline = _tool == SnipTool.Text ? _textOutline : null;

        if (HasSelection)
        {
            outline = SelectedAll.OfType<TextAnnotationModel>().FirstOrDefault() is { } text
                ? text.Outline ?? _textOutline with { Width = 0 }
                : null;
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
            StepAnnotationModel step => step.Diameter,
            EmojiAnnotationModel emoji => emoji.Size,
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
        _selection = [];
        _selectionDrag = null;
        _previews = null;
        _bandStart = null;
        _band = null;
        _isEditingText = false;
    }

    /// <summary>
    ///     Keeps a change to each annotation selected, as one step to undo; what was undone can't be redone after it.
    ///     The change gives back the annotation as it is for one it doesn't apply to.
    /// </summary>
    private void ChangeSelected(Func<AnnotationModel, AnnotationModel> change)
    {
        if (!HasSelection)
        {
            return;
        }

        List<AnnotationModel> annotations = [.. Page.Annotations];
        bool isChanged = false;

        foreach (int index in _selection)
        {
            AnnotationModel current = annotations[index];
            AnnotationModel changed = change(current);

            if (changed != current)
            {
                annotations[index] = changed with { Original = current.Original ?? current };
                isChanged = true;
            }
        }

        if (isChanged)
        {
            Commit(annotations);
        }

        ShowChoices();
    }

    /// <returns>The one on top at the point, which is the last one drawn; null when there's none.</returns>
    private int? TopmostAt(Point point)
    {
        for (int i = Page.Annotations.Count - 1; i >= 0; i--)
        {
            if (AnnotationGeometry.Hits(Page.Annotations[i], point, MeasureText, Zoom))
            {
                return i;
            }
        }

        return null;
    }

    /// <summary>
    ///     The frame around what's selected: one's own frame, or one around all of them. Null for an arrow alone, which
    ///     has only its ends.
    /// </summary>
    private Rectangle? SelectionFrame(IReadOnlyList<AnnotationModel> selected) => selected.Count switch
    {
        0 => null,
        1 => AnnotationGeometry.SelectionOf(selected[0], MeasureText).Box,
        _ => selected.Select(annotation => AnnotationGeometry.BoxOf(annotation, MeasureText)).Aggregate(Rectangle.Union),
    };

    /// <summary>
    ///     What shows what's selected: one's own frame and handles, or a frame with handles around several, which scales
    ///     them all together. Null when nothing is.
    /// </summary>
    private SnipSelectionModel? SelectionShown(IReadOnlyList<AnnotationModel> selected) => selected.Count switch
    {
        0 => null,
        1 => AnnotationGeometry.SelectionOf(selected[0], MeasureText),
        _ => AnnotationGeometry.FrameOf(SelectionFrame(selected)!.Value),
    };

    /// <returns>Which handle of what's selected is at the point; null when none is.</returns>
    private int? SelectionHandleAt(Point point) =>
        SelectionShown(SelectedAll) is { } shown ? AnnotationGeometry.HandleAt(shown, point, Zoom) : null;

    /// <summary>Inside the frame holds what's selected, as a rectangle has only its outline to take hold of.</summary>
    private bool IsInsideSelectionFrame(Point point) => SelectionFrame(SelectedAll)?.Contains(point) == true;

    /// <summary>
    ///     A handle scales what's selected: the one, or all of several together. On something, it's selected, or with
    ///     Shift added or taken away; a drag then moves all that's selected. Elsewhere, a drag draws a box that selects
    ///     what's wholly inside it.
    /// </summary>
    private void PressWithSelectTool(Point point, bool shift)
    {
        if (!shift && SelectionHandleAt(point) is int handle)
        {
            Rectangle? groupFrame = _selection.Count > 1 ? SelectionFrame(SelectedAll) : null;

            _selectionDrag = new SelectionDrag(point, handle, SelectedAll, GroupFrame: groupFrame);

            return;
        }

        int? hit = TopmostAt(point);

        if (shift)
        {
            if (hit is int toggled)
            {
                _selection = _selection.Contains(toggled)
                    ? [.. _selection.Where(index => index != toggled)]
                    : [.. _selection.Append(toggled).Order()];
                ShowChoices();
                Refresh();
            }
            else
            {
                StartBand(point, adds: true);
            }

            return;
        }

        if (hit is int target)
        {
            if (!_selection.Contains(target))
            {
                SelectOnly(target);
            }

            // On one of several, a drag moves them all, and a click with no drag selects that one alone
            _selectionDrag = new SelectionDrag(point, null, SelectedAll, target);

            return;
        }

        if (IsInsideSelectionFrame(point))
        {
            _selectionDrag = new SelectionDrag(point, null, SelectedAll);

            return;
        }

        if (HasSelection)
        {
            Deselect();
            ShowChoices();
            Refresh();
        }

        StartBand(point, adds: false);
    }

    private void StartBand(Point point, bool adds)
    {
        _bandStart = point;
        _bandAdds = adds;
        _band = null;
    }

    /// <summary>What's wholly inside the box is selected, with what was before when it adds.</summary>
    private void EndBand()
    {
        Rectangle? band = _band;

        _bandStart = null;
        _band = null;

        if (band is Rectangle area)
        {
            IEnumerable<int> inside = Enumerable.Range(0, Page.Annotations.Count)
                .Where(index => area.Contains(AnnotationGeometry.BoxOf(Page.Annotations[index], MeasureText)));

            _selection = [.. (_bandAdds ? _selection.Union(inside) : inside).Order()];
            ShowChoices();
        }

        Refresh();
    }

    private void SelectOnly(int index)
    {
        _selection = [index];
        ShowChoices();
        Refresh();
    }

    private void OnSelectAllRequested(object? sender, EventArgs e)
    {
        if (_tool != SnipTool.Select)
        {
            OnToolSelected(sender, SnipTool.Select);
        }

        _selection = [.. Enumerable.Range(0, Page.Annotations.Count)];
        ShowChoices();
        Refresh();
    }

    private void OnPointerPressed(object? sender, SnipPointerModel pointer)
    {
        Point point = pointer.Location;

        if (_tool == SnipTool.Select)
        {
            PressWithSelectTool(point, pointer.Shift);

            return;
        }

        if (_tool == SnipTool.Crop)
        {
            PressWithCropTool(point);

            return;
        }

        if (_tool == SnipTool.Emoji)
        {
            Add(new EmojiAnnotationModel(
                point,
                _emoji,
                WithTransparency(Settings.Color, Settings.Transparency),
                Settings.Size));
            RememberEmoji(_emoji);

            return;
        }

        if (_tool == SnipTool.Step)
        {
            Add(new StepAnnotationModel(
                point,
                NextStepNumber(),
                WithTransparency(Settings.Color, Settings.Transparency),
                Settings.Size));

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
        if (_cropDrag is { } cropDrag)
        {
            _pendingCrop = DraggedCrop(cropDrag, point);
            _view.ShowCropPreview(_pendingCrop);

            return;
        }

        if (_bandStart is Point bandStart)
        {
            _band = FromCorners(bandStart, point);
            Refresh();

            return;
        }

        if (_selectionDrag is { } drag)
        {
            _previews = point == drag.Start
                ? null
                : [.. drag.Originals.Select(annotation => Dragged(drag, annotation, point))];
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
        if (_cropDrag is not null)
        {
            OnPointerMoved(sender, point);
            _cropDrag = null;

            // A click outside, or a drag too small to keep anything, leaves nothing to crop
            if (_pendingCrop is { Width: < 2 } or { Height: < 2 })
            {
                CancelCrop();
            }

            return;
        }

        if (_bandStart is not null)
        {
            OnPointerMoved(sender, point);
            EndBand();

            return;
        }

        if (_selectionDrag is { } drag)
        {
            _selectionDrag = null;
            _previews = null;

            if (point == drag.Start && drag.Clicked is int clicked && _selection.Count > 1)
            {
                SelectOnly(clicked);
            }
            else if (point == drag.Start)
            {
                Refresh();
            }
            else
            {
                ChangeSelected(annotation => Dragged(drag, annotation, point));
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

    /// <summary>The part of the snip shown: its crop, or all of it.</summary>
    private Rectangle ShownArea => Page.Crop ?? new Rectangle(Point.Empty, _view.GetSnipSize(_current));

    /// <summary>
    ///     On the crop being adjusted, a handle drags that side and inside moves it; anywhere else starts another one.
    /// </summary>
    private void PressWithCropTool(Point point)
    {
        if (_pendingCrop is Rectangle pending)
        {
            if (AnnotationGeometry.HandleAt(AnnotationGeometry.FrameOf(pending), point, Zoom) is int handle)
            {
                _cropDrag = new CropDrag(point, handle, pending);

                return;
            }

            if (pending.Contains(point))
            {
                _cropDrag = new CropDrag(point, null, pending);

                return;
            }
        }

        _cropDrag = new CropDrag(point, null, null);
    }

    /// <summary>The crop a drag makes, always inside the part of the snip shown.</summary>
    private Rectangle DraggedCrop(CropDrag drag, Point point)
    {
        Rectangle shown = ShownArea;

        if (drag.Original is not Rectangle original)
        {
            return Rectangle.Intersect(FromCorners(drag.Start, point), shown);
        }

        if (drag.Handle is int handle)
        {
            return Rectangle.Intersect(AnnotationGeometry.ResizedBox(original, handle, point), shown);
        }

        // Moved whole, it stops at the edges instead of getting smaller
        int x = Math.Clamp(original.X + point.X - drag.Start.X, shown.Left, Math.Max(shown.Left, shown.Right - original.Width));
        int y = Math.Clamp(original.Y + point.Y - drag.Start.Y, shown.Top, Math.Max(shown.Top, shown.Bottom - original.Height));

        return new Rectangle(new Point(x, y), original.Size);
    }

    private void CancelCrop()
    {
        _cropDrag = null;
        _pendingCrop = null;
        _view.ShowCropPreview(null);
    }

    /// <summary>
    ///     Keeps only the part being adjusted, as a step to undo; a crop of everything changes nothing. Then it's back to
    ///     the tool from before, as a snip is cropped once.
    /// </summary>
    private void OnCropApplyRequested(object? sender, EventArgs e)
    {
        if (_pendingCrop is not Rectangle area)
        {
            return;
        }

        CancelCrop();

        if (area == ShownArea)
        {
            return;
        }

        Deselect();
        Commit(Page.State with { Crop = area });
        OnToolSelected(this, _toolBeforeCrop);
    }

    private void OnCropCancelRequested(object? sender, EventArgs e)
    {
        CancelCrop();
    }

    /// <summary>
    ///     One more than the highest on the snip shown, so each snip counts from 1, and after deleting or undoing the last
    ///     step its number is used again.
    /// </summary>
    private int NextStepNumber() =>
        Page.Annotations.OfType<StepAnnotationModel>().Select(step => step.Number).DefaultIfEmpty(0).Max() + 1;

    /// <summary>
    ///     What an annotation becomes with the drag: scaled by a handle of the one selected or of the frame around
    ///     several, or moved.
    /// </summary>
    private AnnotationModel Dragged(SelectionDrag drag, AnnotationModel annotation, Point point)
    {
        if (drag.Handle is not int handle)
        {
            return AnnotationGeometry.Moved(annotation, new Size(point.X - drag.Start.X, point.Y - drag.Start.Y));
        }

        if (drag.GroupFrame is Rectangle frame)
        {
            Rectangle scaled = AnnotationGeometry.ResizedBox(frame, handle, point);

            return AnnotationGeometry.ScaledInGroup(annotation, frame, scaled, MeasureText);
        }

        return AnnotationGeometry.Resized(annotation, handle, point, MeasureText);
    }

    /// <summary>On a selected text, lets the user type over it; inside a crop being adjusted, crops.</summary>
    private void OnPointerDoubleClicked(object? sender, Point point)
    {
        if (_tool == SnipTool.Crop && _pendingCrop is Rectangle pending && pending.Contains(point))
        {
            OnCropApplyRequested(sender, EventArgs.Empty);

            return;
        }

        if (_tool != SnipTool.Select || Selected is not TextAnnotationModel text
            || !AnnotationGeometry.Hits(text, point, MeasureText, Zoom))
        {
            return;
        }

        _selectionDrag = null;
        _previews = null;
        _isEditingText = true;
        Refresh();

        // Solid: the text box can't show transparency
        _view.BeginTextInput(text.Location, Solid(text.Color), text.FontSize, text.Text);
    }

    private void OnPointerHovered(object? sender, Point point)
    {
        if (_tool == SnipTool.Crop)
        {
            ShowCursor(CropCursorAt(point));

            return;
        }

        if (_tool != SnipTool.Select)
        {
            return;
        }

        if (SelectionShown(SelectedAll) is { } selection && AnnotationGeometry.HandleAt(selection, point, Zoom) is int handle)
        {
            ShowCursor(AnnotationGeometry.CursorOf(selection, handle));

            return;
        }

        ShowCursor(IsInsideSelectionFrame(point) || TopmostAt(point) is not null ? SnipCursor.Move : SnipCursor.Default);
    }

    private SnipCursor CropCursorAt(Point point)
    {
        if (_pendingCrop is not Rectangle pending)
        {
            return SnipCursor.Cross;
        }

        SnipSelectionModel frame = AnnotationGeometry.FrameOf(pending);

        if (AnnotationGeometry.HandleAt(frame, point, Zoom) is int handle)
        {
            return AnnotationGeometry.CursorOf(frame, handle);
        }

        return pending.Contains(point) ? SnipCursor.Move : SnipCursor.Cross;
    }

    /// <summary>
    ///     Over something with the select tool, offers what can be done to it: to all that's selected when it's one of
    ///     them, or inside their frame; otherwise it's selected alone first.
    /// </summary>
    private void OnPointerRightClicked(object? sender, Point point)
    {
        if (_tool != SnipTool.Select || _selectionDrag is not null || _bandStart is not null)
        {
            return;
        }

        bool isOnHandle = SelectionHandleAt(point) is not null;
        int? hit = TopmostAt(point);
        bool isOnSelection = isOnHandle || (hit is int on ? _selection.Contains(on) : IsInsideSelectionFrame(point));

        if (!isOnSelection)
        {
            if (hit is not int target)
            {
                return;
            }

            SelectOnly(target);
        }

        _view.ShowContextMenu(
            point,
            new SnipContextMenuModel(
                SelectedAll.Any(annotation => annotation.Original is not null),
                !IsOnTop(),
                !IsAtBottom()));
    }

    /// <summary>Whether what's selected is above all the rest already, so bringing it to the front would change nothing.</summary>
    private bool IsOnTop() =>
        _selection.SequenceEqual(Enumerable.Range(Page.Annotations.Count - _selection.Count, _selection.Count));

    private bool IsAtBottom() => _selection.SequenceEqual(Enumerable.Range(0, _selection.Count));

    /// <summary>
    ///     Copies a little down and to the right, so both show, on top of the others and selected, to drag where they go.
    ///     A step's copy has the next number, as a step added by a click does.
    /// </summary>
    private void OnDuplicateRequested(object? sender, EventArgs e)
    {
        if (_selectionDrag is not null || !HasSelection)
        {
            return;
        }

        int nextStep = NextStepNumber();
        List<AnnotationModel> copies = [];

        foreach (AnnotationModel selected in SelectedAll)
        {
            AnnotationModel copy = AnnotationGeometry.Moved(selected, new Size(DuplicateOffset, DuplicateOffset)) with
            {
                Original = null,
            };

            copies.Add(copy is StepAnnotationModel step ? step with { Number = nextStep++ } : copy);
        }

        _selection = [.. Enumerable.Range(Page.Annotations.Count, copies.Count)];
        Commit([.. Page.Annotations, .. copies]);
        ShowChoices();
    }

    /// <summary>Above all the rest, keeping their order among themselves.</summary>
    private void OnBringToFrontRequested(object? sender, EventArgs e)
    {
        if (!HasSelection || IsOnTop() || _selectionDrag is not null)
        {
            return;
        }

        IReadOnlyList<int> moved = _selection;
        List<AnnotationModel> annotations = [.. Page.Annotations.Where((_, i) => !moved.Contains(i)), .. SelectedAll];

        _selection = [.. Enumerable.Range(annotations.Count - moved.Count, moved.Count)];
        Commit(annotations);
    }

    private void OnSendToBackRequested(object? sender, EventArgs e)
    {
        if (!HasSelection || IsAtBottom() || _selectionDrag is not null)
        {
            return;
        }

        IReadOnlyList<int> moved = _selection;
        List<AnnotationModel> annotations = [.. SelectedAll, .. Page.Annotations.Where((_, i) => !moved.Contains(i))];

        _selection = [.. Enumerable.Range(0, moved.Count)];
        Commit(annotations);
    }

    private void OnResetRequested(object? sender, EventArgs e)
    {
        if (!SelectedAll.Any(annotation => annotation.Original is not null))
        {
            return;
        }

        Commit([.. Page.Annotations.Select((annotation, i) =>
            _selection.Contains(i) && annotation.Original is { } original ? original : annotation)]);
        ShowChoices();
    }

    private void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (!HasSelection)
        {
            return;
        }

        IReadOnlyList<int> deleted = _selection;

        Deselect();
        Commit([.. Page.Annotations.Where((_, i) => !deleted.Contains(i))]);
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
        int? editing = _isEditingText ? SelectedIndex : null;

        OnTextCommitted(sender, input);

        if (CleanText(input.Text).Length == 0)
        {
            return;
        }

        if (_tool != SnipTool.Select)
        {
            OnToolSelected(sender, SnipTool.Select);
        }

        SelectOnly(editing ?? Page.Annotations.Count - 1);
    }

    /// <summary>The color without transparency, compared by its value only, as palette colors are.</summary>
    private static Color Solid(Color color) => Color.FromArgb(255, color.R, color.G, color.B);

    private static Color Rgb(int rgb) => Solid(Color.FromArgb(rgb));

    private void OnToolSelected(object? sender, SnipTool tool)
    {
        if (tool == SnipTool.Crop && _tool != SnipTool.Crop)
        {
            _toolBeforeCrop = _tool;
        }

        CancelCrop();
        _tool = tool;
        Deselect();
        ShowTool();
        Refresh();
        SaveSettings();
    }

    // With emoji selected, it changes them; else it's the one the emoji tool drops, which it turns to
    private void OnEmojiSelected(object? sender, string emoji)
    {
        if (string.IsNullOrEmpty(emoji))
        {
            return;
        }

        _emoji = emoji;

        if (HasSelection && SelectedAll.Any(annotation => annotation is EmojiAnnotationModel))
        {
            ChangeSelected(annotation => annotation is EmojiAnnotationModel selected
                ? selected with { Emoji = emoji }
                : annotation);
            RememberEmoji(emoji);

            return;
        }

        if (_tool == SnipTool.Emoji)
        {
            ShowChoices();
            SaveSettings();

            return;
        }

        OnToolSelected(this, SnipTool.Emoji);
    }

    // First among the recent ones, the oldest beyond EmojiCatalog.MaxRecent dropped, and saved
    private void RememberEmoji(string emoji)
    {
        _recentEmojis.Remove(emoji);
        _recentEmojis.Insert(0, emoji);

        if (_recentEmojis.Count > EmojiCatalog.MaxRecent)
        {
            _recentEmojis.RemoveAt(EmojiCatalog.MaxRecent);
        }

        ShowChoices();
        SaveSettings();
    }

    private void OnColorSelected(object? sender, Color color)
    {
        if (HasSelection)
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
        Color current = Primary is { } primary ? Solid(primary.Color) : Settings.Color;

        if (AskColor(current) is Color color)
        {
            OnColorSelected(sender, color);

            // A change to what's selected saves nothing by itself, and the custom color is to be kept
            if (HasSelection)
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

        if (HasSelection)
        {
            // Only to those with the same kind of size as the one shown: a line's width isn't a text's font size
            IReadOnlyList<int> shown = Primary is { } primary ? SizesFor(primary) : [];

            ChangeSelected(annotation => !ReferenceEquals(SizesFor(annotation), shown) ? annotation : annotation switch
            {
                StrokeAnnotationModel stroke => stroke with { Width = size },
                ArrowAnnotationModel arrow => arrow with { Width = size },
                RectangleAnnotationModel rectangle => rectangle with { Width = size },
                TextAnnotationModel text => text with { FontSize = size },
                StepAnnotationModel step => step with { Diameter = size },
                EmojiAnnotationModel emoji => emoji with { Size = size },
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

        if (HasSelection)
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
        if (HasSelection)
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

        if (HasSelection)
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

        if (HasSelection)
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
        if (HasSelection)
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
        Color current = SelectedAll.OfType<TextAnnotationModel>().FirstOrDefault()?.Outline?.Color ?? _textOutline.Color;

        if (AskColor(current) is Color color)
        {
            OnTextOutlineColorSelected(sender, color);

            if (HasSelection)
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

        Page.Redo.Push(Page.State);
        Page.State = Page.Undo.Pop();
        Restored();
    }

    private void OnRedoRequested(object? sender, EventArgs e)
    {
        if (Page.Redo.Count == 0)
        {
            return;
        }

        Page.Undo.Push(Page.State);
        Page.State = Page.Redo.Pop();
        Restored();
    }

    /// <summary>After undo or redo the annotations are other ones, so what was selected may not be there.</summary>
    private void Restored()
    {
        Deselect();
        ShowChoices();
        ShowCrop();
        Changed();
    }

    /// <summary>
    ///     One level larger or smaller, or 0 for 100%. A drag can't go on: what's under the mouse is somewhere else now.
    /// </summary>
    private void OnZoomRequested(object? sender, int step)
    {
        int percent = step switch
        {
            > 0 => ZoomLevels.FirstOrDefault(level => level > _zoomPercent, ZoomLevels[^1]),
            < 0 => ZoomLevels.LastOrDefault(level => level < _zoomPercent, ZoomLevels[0]),
            _ => 100,
        };

        if (percent != _zoomPercent)
        {
            SetZoom(percent);
        }
    }

    private void SetZoom(int percent)
    {
        _zoomPercent = percent;

        // A crop being adjusted stays; only a drag of it stops
        _cropDrag = null;
        _start = null;
        _points = [];
        _drawing = null;
        _selectionDrag = null;
        _previews = null;
        _bandStart = null;
        _band = null;
        _view.ShowZoom(percent);
        Refresh();
    }

    private void OnSnipAdded(object? sender, EventArgs e)
    {
        // At its own size, as a new window shows it
        if (_zoomPercent != 100)
        {
            SetZoom(100);
        }

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

        bool hadSelection = HasSelection;

        Deselect();
        CancelCrop();
        _current = page;

        // Before the snip, which fits the window to the part shown
        _shownCrop = Page.Crop;
        _view.ShowCrop(Page.Crop);
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

    private async void OnCopyTextRequested(object? sender, EventArgs e)
    {
        if (_isReadingText)
        {
            return;
        }

        _isReadingText = true;

        try
        {
            IReadOnlyList<string> lines =
                RecognizedText.Format(await _textRecognizer.ReadLinesAsync(_view.RenderImagePng()));

            if (lines.Count == 0)
            {
                _view.ShowNotice(RecognizedText.NoneFoundMessage);

                return;
            }

            _view.CopyTextToClipboard(RecognizedText.Join(lines));
            _view.ShowNotice(RecognizedText.CopiedMessage(lines.Count));
        }
        catch (TextRecognitionException ex)
        {
            _view.ShowNotice(ex.Message);
        }
        finally
        {
            _isReadingText = false;
        }
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
        Page.Saved = Page.State;
    }

    /// <summary>Saved, as it's what the next snips read, whether this editor is still open then or not.</summary>
    private void OnHideWhileSnippingSelected(object? sender, bool isHidden)
    {
        _hideWhileSnipping = isHidden;
        _view.ShowHideWhileSnipping(isHidden);
        SaveSettings();
    }

    /// <summary>
    ///     With more than one snip that would be lost, asks first; one alone closes at once, as when a snip is taken,
    ///     pasted and done with.
    /// </summary>
    private void OnCloseRequested(object? sender, CancelEventArgs e)
    {
        int unsaved = _pages.Count(page => !ReferenceEquals(page.Saved, page.State));

        if (unsaved > 1 && !_view.AskDiscardSnips(unsaved))
        {
            e.Cancel = true;
        }
    }

    private AnnotationModel CreateAnnotation(Point start, Point end)
    {
        int width = Settings.Size;
        Color color = WithTransparency(Settings.Color, Settings.Transparency);

        // The pen and the highlighter differ only in their settings; the smooth pen evens out the line
        return _tool switch
        {
            SnipTool.Arrow => new ArrowAnnotationModel(start, end, color, width, _arrowHeads),
            SnipTool.Rectangle => new RectangleAnnotationModel(FromCorners(start, end), color, width, _cornerRadius),
            SnipTool.Blur => new BlurAnnotationModel(FromCorners(start, end)),
            // Smoothed again as it grows: the start of the line settles once there are points enough around it
            SnipTool.SmoothPen => new StrokeAnnotationModel(StrokeSmoother.Smooth(_points), color, width, IsSmooth: true),
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
        Commit(Page.State with { Annotations = annotations });
    }

    private void Commit(SnipPageState state)
    {
        Page.Undo.Push(Page.State);
        Page.Redo.Clear();
        Page.State = state;
        ShowCrop();
        Changed();
    }

    /// <summary>Only when it changed: the view fits the window to it.</summary>
    private void ShowCrop()
    {
        if (_shownCrop != Page.Crop)
        {
            _shownCrop = Page.Crop;
            _view.ShowCrop(Page.Crop);
        }
    }

    private void Changed()
    {
        Refresh();
        _view.CopyImageToClipboard();
    }

    private void Refresh()
    {
        List<AnnotationModel> shown = [];

        List<AnnotationModel> selected = [];

        for (int i = 0; i < Page.Annotations.Count; i++)
        {
            int place = IndexInSelection(i);

            if (place < 0)
            {
                shown.Add(Page.Annotations[i]);
            }
            else if (!_isEditingText)
            {
                // While dragged, as they would be
                AnnotationModel annotation = _previews is { } previews ? previews[place] : Page.Annotations[i];

                shown.Add(annotation);
                selected.Add(annotation);
            }
        }

        if (_drawing is not null)
        {
            shown.Add(_drawing);
        }

        _view.ShowAnnotations(shown);

        _view.ShowSelection(SelectionShown(selected));
        _view.ShowSelectionBand(_band);
        _view.ShowUndoRedo(Page.Undo.Count > 0, Page.Redo.Count > 0);
    }

    /// <returns>Where an annotation is in the selection; -1 when it isn't selected.</returns>
    private int IndexInSelection(int index)
    {
        for (int i = 0; i < _selection.Count; i++)
        {
            if (_selection[i] == index)
            {
                return i;
            }
        }

        return -1;
    }

    private static Rectangle FromCorners(Point a, Point b) => Rectangle.FromLTRB(
        Math.Min(a.X, b.X),
        Math.Min(a.Y, b.Y),
        Math.Max(a.X, b.X),
        Math.Max(a.Y, b.Y));

    /// <summary>What a snip of the window has drawn over it, and how it's cropped, with what to undo and redo.</summary>
    private sealed class SnipPage
    {
        public SnipPageState State { get; set; } = new([], null);

        public IReadOnlyList<AnnotationModel> Annotations => State.Annotations;

        public Rectangle? Crop => State.Crop;

        public Stack<SnipPageState> Undo { get; } = new();

        public Stack<SnipPageState> Redo { get; } = new();

        /// <summary>
        ///     What it was last saved as; null when it never was. Undo can bring that back, which makes it saved again:
        ///     the states are kept as they are, so the same state is the same snip.
        /// </summary>
        public SnipPageState? Saved { get; set; }
    }

    /// <summary>A snip as it is at one step of its undo.</summary>
    /// <param name="Annotations">What's drawn over it, in the pixels of the whole snip.</param>
    /// <param name="Crop">The part of the snip that's kept; null for all of it.</param>
    private sealed record SnipPageState(IReadOnlyList<AnnotationModel> Annotations, Rectangle? Crop);

    /// <summary>A drag of what's selected.</summary>
    /// <param name="Start">Where the button was pressed.</param>
    /// <param name="Handle">Which handle of the selection is dragged; null when they're all moved.</param>
    /// <param name="Originals">What's selected as it was before the drag, in the selection's order.</param>
    /// <param name="Clicked">What the button was pressed on; null for the empty space inside the frame.</param>
    /// <param name="GroupFrame">The frame around several selected, before a drag of its handle; null for one.</param>
    private sealed record SelectionDrag(
        Point Start,
        int? Handle,
        IReadOnlyList<AnnotationModel> Originals,
        int? Clicked = null,
        Rectangle? GroupFrame = null);

    /// <summary>A drag with the crop tool.</summary>
    /// <param name="Start">Where the button was pressed.</param>
    /// <param name="Handle">Which handle of the crop is dragged; null when it's moved whole, or a new one is drawn.</param>
    /// <param name="Original">The crop as it was before the drag; null when a new one is drawn.</param>
    private sealed record CropDrag(Point Start, int? Handle, Rectangle? Original);
}
