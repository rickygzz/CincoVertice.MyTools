using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Presenters;
using CincoVertice.MyTools.Ui.Snipping.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Snipping;

public class SnipEditorPresenterTests
{
    private static readonly TimeProvider _time =
        new FixedTimeProvider(new DateTimeOffset(2026, 11, 1, 9, 5, 7, TimeSpan.Zero));

    private readonly FakeSnipEditorView _view = new();
    private readonly FakeSettingsStore _store = new();
    private readonly SnipEditorPresenter _presenter;

    public SnipEditorPresenterTests()
    {
        _presenter = new SnipEditorPresenter(_view, _time, _store);
        _presenter.Start();
    }

    [Fact]
    public void Start_SavesNothing()
    {
        Assert.Empty(_store.Saved);
    }

    [Fact]
    public void EveryChangeOfSettings_IsSaved()
    {
        // Act
        _view.SelectTool(SnipTool.Arrow);
        _view.SelectColor(Color.Blue);
        _view.SelectSize(10);
        _view.SelectTransparency(30);

        // Assert
        Assert.Equal(4, _store.Saved.Count);
        SnipEditorSettingsModel last = _store.Saved[^1];
        Assert.Equal(SnipTool.Arrow, last.Tool);
        Assert.Equal(new SnipToolSettingsModel(10, 30, Solid(Color.Blue)), last.Tools[SnipTool.Arrow]);
        Assert.Equal(
            new SnipToolSettingsModel(16, 50, SnipEditorPresenter.Palette[2]),
            last.Tools[SnipTool.Highlighter]);
    }

    [Fact]
    public void EachTool_KeepsItsOwnColor()
    {
        // Arrange
        _view.SelectColor(Color.Blue);
        _view.SelectTool(SnipTool.Highlighter);

        // Assert: the highlighter starts yellow
        Assert.Equal(SnipEditorPresenter.Palette[2], _view.Color);

        // Act
        _view.SelectTool(SnipTool.Pen);
        _view.Press(0, 0);
        _view.Release(5, 5);

        // Assert
        Assert.Equal(Solid(Color.Blue), _view.Color);
        Assert.Equal(Solid(Color.Blue), Assert.Single(_presenter.Annotations).Color);
    }

    [Fact]
    public void Start_ShowsThePalette()
    {
        Assert.Equal(SnipEditorPresenter.Palette, _view.Palette);
        Assert.Equal(24, SnipEditorPresenter.Palette.Count);
    }

    [Fact]
    public void MoreColors_AsksFromTheCurrentColorAndUsesThePickedOne()
    {
        // Arrange
        _view.ColorToPick = Color.FromArgb(255, 18, 52, 86);

        // Act
        _view.RequestMoreColors();

        // Assert
        Assert.Equal(Solid(Color.Red), _view.AskedFrom);
        Assert.Equal(Color.FromArgb(255, 18, 52, 86), _view.Color);
        Assert.Equal(Color.FromArgb(255, 18, 52, 86), Assert.Single(_store.Saved).Tools[SnipTool.Pen].Color);
    }

    [Fact]
    public void MoreColors_KeepsThePickedColorInThePaletteAndSavesIt()
    {
        // Arrange
        Color picked = Color.FromArgb(255, 18, 52, 86);
        _view.ColorToPick = picked;

        // Act
        _view.RequestMoreColors();

        // Assert
        Assert.Equal([picked], _view.CustomColors);
        Assert.Equal(2, _view.PaletteShowCount);
        Assert.Equal([picked], Assert.Single(_store.Saved).CustomColors);
    }

    [Fact]
    public void MoreColors_APaletteColor_IsNotAddedAgain()
    {
        // Arrange
        _view.ColorToPick = Color.FromArgb(255, 0, 99, 177);

        // Act
        _view.RequestMoreColors();

        // Assert
        Assert.Empty(_view.CustomColors);
        Assert.Equal(1, _view.PaletteShowCount);
        Assert.Equal(Color.FromArgb(255, 0, 99, 177), _view.Color);
    }

    [Fact]
    public void CustomColors_KeepTheNewestFirstAndAtMost8()
    {
        // Act: 10 colors, then the third again
        for (int i = 1; i <= 10; i++)
        {
            _view.ColorToPick = Color.FromArgb(255, i, i, i);
            _view.RequestMoreColors();
        }

        _view.ColorToPick = Color.FromArgb(255, 8, 8, 8);
        _view.RequestMoreColors();

        // Assert
        Assert.Equal(SnipEditorPresenter.MaxCustomColors, _view.CustomColors.Count);
        Assert.Equal(
            [8, 10, 9, 7, 6, 5, 4, 3],
            _view.CustomColors.Select(color => (int)color.R));
    }

    [Fact]
    public void MoreColors_OffersTheCustomColors()
    {
        // Arrange
        _view.ColorToPick = Color.FromArgb(255, 18, 52, 86);
        _view.RequestMoreColors();

        // Act
        _view.RequestMoreColors();

        // Assert
        Assert.Equal([Color.FromArgb(255, 18, 52, 86)], _view.AskedWith);
    }

    [Fact]
    public void NewEditor_ShowsTheSavedCustomColorsInTheirOrder()
    {
        // Arrange: blue is in the palette already, so it's left out
        var store = new FakeSettingsStore(new SnipEditorSettingsModel(
            SnipTool.Pen,
            new Dictionary<SnipTool, SnipToolSettingsModel>(),
            [Color.FromArgb(255, 1, 2, 3), Color.FromArgb(255, 0, 99, 177), Color.FromArgb(70, 4, 5, 6)]));
        var view = new FakeSnipEditorView();

        // Act
        new SnipEditorPresenter(view, _time, store).Start();

        // Assert
        Assert.Equal([Color.FromArgb(255, 1, 2, 3), Color.FromArgb(255, 4, 5, 6)], view.CustomColors);
    }

    [Fact]
    public void MoreColors_WhenTheUserCancels_KeepsTheColor()
    {
        // Act
        _view.RequestMoreColors();

        // Assert
        Assert.Equal(Solid(Color.Red), _view.Color);
        Assert.Empty(_store.Saved);
    }

    [Fact]
    public void Drawing_SavesNoSettings()
    {
        // Act
        _view.Press(0, 0);
        _view.Release(10, 10);
        _view.Undo();

        // Assert
        Assert.Empty(_store.Saved);
    }

    [Fact]
    public void NewEditor_StartsWithTheSavedSettings()
    {
        // Arrange
        _view.SelectTool(SnipTool.Highlighter);
        _view.SelectSize(24);
        _view.SelectColor(Color.Yellow);
        var store = new FakeSettingsStore(_store.Saved[^1]);
        var view = new FakeSnipEditorView();

        // Act
        new SnipEditorPresenter(view, _time, store).Start();

        // Assert
        Assert.Equal((SnipTool.Highlighter, 24, 50), (view.Tool, view.Size, view.Transparency));
        Assert.Equal(Color.Yellow.ToArgb(), view.Color.ToArgb());
    }

    [Fact]
    public void SavedSettingsTheEditorDoesntOffer_KeepTheDefaults()
    {
        // Arrange: e.g. a file edited by hand
        var store = new FakeSettingsStore(new SnipEditorSettingsModel(
            (SnipTool)99,
            new Dictionary<SnipTool, SnipToolSettingsModel>
            {
                [SnipTool.Pen] = new(7, 0, Color.Empty),
                [SnipTool.Arrow] = new(8, 55, Color.FromArgb(10, 0, 0, 255)),
                [SnipTool.Text] = new(36, 20, Color.Green),
            },
            []));
        var view = new FakeSnipEditorView();
        var presenter = new SnipEditorPresenter(view, _time, store);
        presenter.Start();

        // Assert: the pen at its default size and color
        Assert.Equal((SnipTool.Pen, 4, Solid(Color.Red)), (view.Tool, view.Size, view.Color));

        // Act
        view.SelectTool(SnipTool.Arrow);

        // Assert: the saved color made solid
        Assert.Equal((8, 0, Color.FromArgb(255, 0, 0, 255)), (view.Size, view.Transparency, view.Color));

        // Act
        view.SelectTool(SnipTool.Text);

        // Assert
        Assert.Equal((36, 20, Solid(Color.Green)), (view.Size, view.Transparency, view.Color));
    }

    [Fact]
    public void Start_ShowsThePenInRedSolidAt4PxAndCopiesTheSnip()
    {
        Assert.Equal(
            (SnipTool.Pen, Solid(Color.Red), 4, 0),
            (_view.Tool, _view.Color, _view.Size, _view.Transparency));
        Assert.Equal(SnipEditorPresenter.LineSizes, _view.Sizes);
        Assert.Equal(SnipEditorPresenter.Transparencies, _view.TransparencyChoices);
        Assert.Empty(_view.Annotations);
        Assert.Equal((false, false), (_view.CanUndo, _view.CanRedo));
        Assert.Equal(1, _view.CopyCount);
    }

    [Fact]
    public void PenDrag_ShowsTheLineWhileDrawingAndKeepsItOnRelease()
    {
        // Act
        _view.Press(1, 1);
        _view.Move(5, 5);

        // Assert: shown, not kept or copied yet
        var shown = Assert.IsType<StrokeAnnotationModel>(Assert.Single(_view.Annotations));
        Assert.Equal([new Point(1, 1), new Point(5, 5)], shown.Points);
        Assert.Empty(_presenter.Annotations);
        Assert.Equal(1, _view.CopyCount);

        // Act
        _view.Move(5, 5);
        _view.Release(9, 3);

        // Assert
        var kept = Assert.IsType<StrokeAnnotationModel>(Assert.Single(_presenter.Annotations));
        Assert.Equal([new Point(1, 1), new Point(5, 5), new Point(9, 3)], kept.Points);
        Assert.Equal((Solid(Color.Red), 4), (kept.Color, kept.Width));
        Assert.Equal(2, _view.CopyCount);
        Assert.True(_view.CanUndo);
    }

    [Fact]
    public void PenClick_LeavesADot()
    {
        // Act
        _view.Press(3, 4);
        _view.Release(3, 4);

        // Assert
        var dot = Assert.IsType<StrokeAnnotationModel>(Assert.Single(_presenter.Annotations));
        Assert.Equal([new Point(3, 4)], dot.Points);
    }

    [Fact]
    public void Highlighter_StartsHalfSeeThroughAt16Px()
    {
        // Arrange
        _view.SelectTool(SnipTool.Highlighter);
        _view.SelectColor(Color.Yellow);

        // Act
        _view.Press(0, 0);
        _view.Release(50, 0);

        // Assert
        var stroke = Assert.IsType<StrokeAnnotationModel>(Assert.Single(_presenter.Annotations));
        Assert.Equal(
            (128, Color.Yellow.ToArgb() & 0xFFFFFF, 16),
            (stroke.Color.A, stroke.Color.ToArgb() & 0xFFFFFF, stroke.Width));
        Assert.Equal((SnipTool.Highlighter, 16, 50), (_view.Tool, _view.Size, _view.Transparency));
    }

    [Fact]
    public void Arrow_GoesFromWhereTheDragStartedToWhereItEnded()
    {
        // Arrange
        _view.SelectTool(SnipTool.Arrow);
        _view.SelectSize(2);

        // Act
        _view.Press(100, 100);
        _view.Move(50, 70);
        _view.Release(10, 20);

        // Assert
        Assert.Equal(
            new ArrowAnnotationModel(new Point(100, 100), new Point(10, 20), Solid(Color.Red), 2),
            Assert.Single(_presenter.Annotations));
    }

    [Theory]
    [InlineData(SnipArrowHeads.End)]
    [InlineData(SnipArrowHeads.Start)]
    [InlineData(SnipArrowHeads.Both)]
    public void Arrow_HasTheSelectedHeads(SnipArrowHeads heads)
    {
        // Arrange
        _view.SelectTool(SnipTool.Arrow);
        _view.SelectArrowHeads(heads);

        // Act
        _view.Press(0, 0);
        _view.Release(50, 50);

        // Assert
        Assert.Equal(heads, Assert.IsType<ArrowAnnotationModel>(Assert.Single(_presenter.Annotations)).Heads);
        Assert.Equal(heads, _view.ArrowHeads);
    }

    [Fact]
    public void Arrow_HeadsSelectedLater_DoNotChangeTheArrowsDrawn()
    {
        // Arrange
        _view.SelectTool(SnipTool.Arrow);
        _view.Press(0, 0);
        _view.Release(50, 50);

        // Act
        _view.SelectArrowHeads(SnipArrowHeads.Both);

        // Assert
        Assert.Equal(
            SnipArrowHeads.End,
            Assert.IsType<ArrowAnnotationModel>(Assert.Single(_presenter.Annotations)).Heads);
    }

    [Theory]
    [InlineData(SnipTool.Pen)]
    [InlineData(SnipTool.Highlighter)]
    [InlineData(SnipTool.Rectangle)]
    [InlineData(SnipTool.Text)]
    public void ArrowHeads_AreOnlyOfferedForTheArrow(SnipTool tool)
    {
        // Arrange
        _view.SelectTool(SnipTool.Arrow);
        _view.SelectArrowHeads(SnipArrowHeads.Start);

        // Act
        _view.SelectTool(tool);

        // Assert
        Assert.Null(_view.ArrowHeads);

        // Act
        _view.SelectTool(SnipTool.Arrow);

        // Assert: still the ones selected
        Assert.Equal(SnipArrowHeads.Start, _view.ArrowHeads);
    }

    [Fact]
    public void ArrowHeads_AreSaved_AndANewEditorStartsWithThem()
    {
        // Arrange
        _view.SelectTool(SnipTool.Arrow);

        // Act
        _view.SelectArrowHeads(SnipArrowHeads.Both);
        var view = new FakeSnipEditorView();
        new SnipEditorPresenter(view, _time, new FakeSettingsStore(_store.Saved[^1])).Start();

        // Assert
        Assert.Equal(SnipArrowHeads.Both, view.ArrowHeads);
    }

    [Fact]
    public void SavedArrowHeadsTheEditorDoesntOffer_KeepTheDefault()
    {
        // Arrange
        var store = new FakeSettingsStore(new SnipEditorSettingsModel(
            SnipTool.Arrow,
            new Dictionary<SnipTool, SnipToolSettingsModel>(),
            [],
            (SnipArrowHeads)99));
        var view = new FakeSnipEditorView();

        // Act
        new SnipEditorPresenter(view, _time, store).Start();

        // Assert
        Assert.Equal(SnipArrowHeads.End, view.ArrowHeads);
    }

    [Fact]
    public void Rectangle_DraggedUpAndLeft_IsTheSameRectangle()
    {
        // Arrange
        _view.SelectTool(SnipTool.Rectangle);

        // Act
        _view.Press(110, 70);
        _view.Release(10, 20);

        // Assert
        Assert.Equal(
            new RectangleAnnotationModel(new Rectangle(10, 20, 100, 50), Solid(Color.Red), 4),
            Assert.Single(_presenter.Annotations));
    }

    [Theory]
    [InlineData(SnipTool.Arrow)]
    [InlineData(SnipTool.Rectangle)]
    public void ArrowOrRectangleClick_AddsNothing(SnipTool tool)
    {
        // Arrange
        _view.SelectTool(tool);

        // Act
        _view.Press(10, 10);
        _view.Release(11, 11);

        // Assert
        Assert.Empty(_presenter.Annotations);
        Assert.Empty(_view.Annotations);
        Assert.Equal(1, _view.CopyCount);
    }

    [Fact]
    public void TextClick_StartsATextAtThePointWithTheColorAndSize()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.SelectColor(Color.Blue);
        _view.SelectSize(36);

        // Act
        _view.Press(30, 40);
        _view.Release(30, 40);

        // Assert
        Assert.Equal((new Point(30, 40), Solid(Color.Blue), 36), _view.TextInput);
        Assert.Empty(_presenter.Annotations);
    }

    [Fact]
    public void TextCommitted_AddsTheTextWithoutTrailingSpaces()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);

        // Act
        _view.CommitText(30, 40, "Look\r\nhere  \r\n");

        // Assert
        Assert.Equal(
            new TextAnnotationModel(new Point(30, 40), "Look\nhere", Solid(Color.Red), 24),
            Assert.Single(_presenter.Annotations));
        Assert.Equal(2, _view.CopyCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \r\n")]
    public void EmptyTextCommitted_AddsNothing(string text)
    {
        // Act
        _view.CommitText(30, 40, text);

        // Assert
        Assert.Empty(_presenter.Annotations);
        Assert.Equal(1, _view.CopyCount);
    }

    [Fact]
    public void UndoAndRedo_RemoveAndRestoreTheLastAnnotation()
    {
        // Arrange
        Draw(1);
        Draw(2);

        // Act
        _view.Undo();

        // Assert
        Assert.Equal([new Point(1, 1)], Points(Assert.Single(_presenter.Annotations)));
        Assert.Equal((true, true), (_view.CanUndo, _view.CanRedo));

        // Act
        _view.Redo();

        // Assert
        Assert.Equal(2, _presenter.Annotations.Count);
        Assert.Equal((true, false), (_view.CanUndo, _view.CanRedo));
        Assert.Equal(5, _view.CopyCount);
    }

    [Fact]
    public void NewAnnotationAfterUndo_CantRedoAnymore()
    {
        // Arrange
        Draw(1);
        _view.Undo();

        // Act
        Draw(2);

        // Assert
        _view.Redo();
        Assert.Equal([new Point(2, 2)], Points(Assert.Single(_presenter.Annotations)));
        Assert.False(_view.CanRedo);
    }

    [Fact]
    public void UndoAndRedo_WithNothingToDo_DoNothing()
    {
        // Act
        _view.Undo();
        _view.Redo();

        // Assert
        Assert.Equal(1, _view.CopyCount);
    }

    [Fact]
    public void Save_SuggestsADatedNameAndSavesWhereTheUserChose()
    {
        // Arrange
        _view.SavePath = @"C:\snips\a.png";

        // Act
        _view.Save();

        // Assert
        Assert.Equal("Snip 2026-11-01 090507.png", _view.SuggestedFileName);
        Assert.Equal(@"C:\snips\a.png", _view.SavedPath);
    }

    [Fact]
    public void Save_WhenTheUserCancels_SavesNothing()
    {
        // Act
        _view.Save();

        // Assert
        Assert.NotNull(_view.SuggestedFileName);
        Assert.Null(_view.SavedPath);
    }

    [Fact]
    public void Copy_CopiesAgain()
    {
        // Act
        _view.Copy();

        // Assert
        Assert.Equal(2, _view.CopyCount);
    }

    [Theory]
    [InlineData(0, 255)]
    [InlineData(30, 179)]
    [InlineData(90, 26)]
    public void Transparency_MakesTheColorSeeThrough(int transparency, int alpha)
    {
        // Arrange
        _view.SelectTransparency(transparency);

        // Act
        _view.Press(0, 0);
        _view.Release(10, 10);

        // Assert
        Assert.Equal(Color.FromArgb(alpha, Color.Red), Assert.Single(_presenter.Annotations).Color);
        Assert.Equal(transparency, _view.Transparency);
    }

    [Fact]
    public void Transparency_AlsoAppliesToText()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.SelectTransparency(50);

        // Act
        _view.CommitText(0, 0, "a");

        // Assert
        Assert.Equal(128, Assert.Single(_presenter.Annotations).Color.A);
    }

    [Theory]
    [InlineData(100, 90)]
    [InlineData(-10, 0)]
    public void Transparency_OutOfRange_IsClamped(int transparency, int expected)
    {
        // Act
        _view.SelectTransparency(transparency);

        // Assert
        Assert.Equal(expected, _view.Transparency);
    }

    [Fact]
    public void EachTool_KeepsItsOwnSizeAndTransparency()
    {
        // Arrange
        _view.SelectSize(10);
        _view.SelectTransparency(20);
        _view.SelectTool(SnipTool.Arrow);
        _view.SelectSize(2);

        // Act
        _view.SelectTool(SnipTool.Pen);

        // Assert
        Assert.Equal((10, 20), (_view.Size, _view.Transparency));

        // Act
        _view.SelectTool(SnipTool.Arrow);

        // Assert
        Assert.Equal((2, 0), (_view.Size, _view.Transparency));
    }

    [Fact]
    public void TextTool_OffersFontSizes()
    {
        // Act
        _view.SelectTool(SnipTool.Text);

        // Assert
        Assert.Equal((SnipEditorPresenter.FontSizes, 24), (_view.Sizes, _view.Size));
    }

    [Fact]
    public void SizeOfZero_IsIgnored()
    {
        // Act
        _view.SelectSize(0);

        // Assert
        Assert.Equal(4, _view.Size);
    }

    /// <summary>The color as the presenter makes it, with its alpha: a named color is not equal to it.</summary>
    private static Color Solid(Color color) => Color.FromArgb(255, color);

    private static IReadOnlyList<Point> Points(AnnotationModel annotation) =>
        Assert.IsType<StrokeAnnotationModel>(annotation).Points;

    private void Draw(int at)
    {
        _view.Press(at, at);
        _view.Release(at, at);
    }

    private sealed class FakeSettingsStore(SnipEditorSettingsModel? loaded = null) : ISnipEditorSettingsStore
    {
        public List<SnipEditorSettingsModel> Saved { get; } = [];

        public SnipEditorSettingsModel? Load() => loaded;

        public void Save(SnipEditorSettingsModel settings) => Saved.Add(settings);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class FakeSnipEditorView : ISnipEditorView
    {
        public event EventHandler<Point>? PointerPressed;

        public event EventHandler<Point>? PointerMoved;

        public event EventHandler<Point>? PointerReleased;

        public event EventHandler<SnipTool>? ToolSelected;

        public event EventHandler<Color>? ColorSelected;

        public event EventHandler? MoreColorsRequested;

        public event EventHandler<int>? SizeSelected;

        public event EventHandler<int>? TransparencySelected;

        public event EventHandler<SnipArrowHeads>? ArrowHeadsSelected;

        public event EventHandler<TextInputModel>? TextCommitted;

        public event EventHandler? UndoRequested;

        public event EventHandler? RedoRequested;

        public event EventHandler? CopyRequested;

        public event EventHandler? SaveRequested;

        public SnipTool Tool { get; private set; }

        public Color Color { get; private set; }

        public IReadOnlyList<Color> Palette { get; private set; } = [];

        public IReadOnlyList<Color> CustomColors { get; private set; } = [];

        public int PaletteShowCount { get; private set; }

        /// <summary>What AskColor returns; null as if the user cancelled.</summary>
        public Color? ColorToPick { get; set; }

        /// <summary>The color AskColor was given to start from.</summary>
        public Color? AskedFrom { get; private set; }

        /// <summary>The custom colors AskColor was given.</summary>
        public IReadOnlyList<Color>? AskedWith { get; private set; }

        public IReadOnlyList<int> Sizes { get; private set; } = [];

        public int Size { get; private set; }

        public IReadOnlyList<int> TransparencyChoices { get; private set; } = [];

        public int Transparency { get; private set; }

        public SnipArrowHeads? ArrowHeads { get; private set; }

        public IReadOnlyList<AnnotationModel> Annotations { get; private set; } = [];

        public bool CanUndo { get; private set; }

        public bool CanRedo { get; private set; }

        public (Point Location, Color Color, int FontSize)? TextInput { get; private set; }

        public int CopyCount { get; private set; }

        /// <summary>What AskSavePath returns; null as if the user cancelled.</summary>
        public string? SavePath { get; set; }

        public string? SuggestedFileName { get; private set; }

        public string? SavedPath { get; private set; }

        public void Press(int x, int y) => PointerPressed?.Invoke(this, new Point(x, y));

        public void Move(int x, int y) => PointerMoved?.Invoke(this, new Point(x, y));

        public void Release(int x, int y) => PointerReleased?.Invoke(this, new Point(x, y));

        public void SelectTool(SnipTool tool) => ToolSelected?.Invoke(this, tool);

        public void SelectColor(Color color) => ColorSelected?.Invoke(this, color);

        public void RequestMoreColors() => MoreColorsRequested?.Invoke(this, EventArgs.Empty);

        public void SelectSize(int size) => SizeSelected?.Invoke(this, size);

        public void SelectTransparency(int transparency) => TransparencySelected?.Invoke(this, transparency);

        public void SelectArrowHeads(SnipArrowHeads heads) => ArrowHeadsSelected?.Invoke(this, heads);

        public void CommitText(int x, int y, string text) =>
            TextCommitted?.Invoke(this, new TextInputModel(new Point(x, y), text));

        public void Undo() => UndoRequested?.Invoke(this, EventArgs.Empty);

        public void Redo() => RedoRequested?.Invoke(this, EventArgs.Empty);

        public void Copy() => CopyRequested?.Invoke(this, EventArgs.Empty);

        public void Save() => SaveRequested?.Invoke(this, EventArgs.Empty);

        public void ShowTool(SnipTool tool) => Tool = tool;

        public void ShowColor(Color color) => Color = color;

        public void ShowPalette(IReadOnlyList<Color> colors, IReadOnlyList<Color> customColors)
        {
            (Palette, CustomColors) = (colors, customColors);
            PaletteShowCount++;
        }

        public Color? AskColor(Color current, IReadOnlyList<Color> customColors)
        {
            (AskedFrom, AskedWith) = (current, customColors);

            return ColorToPick;
        }

        public void ShowSizes(IReadOnlyList<int> sizes, int selected) => (Sizes, Size) = (sizes, selected);

        public void ShowTransparencies(IReadOnlyList<int> transparencies, int selected) =>
            (TransparencyChoices, Transparency) = (transparencies, selected);

        public void ShowArrowHeads(SnipArrowHeads? selected) => ArrowHeads = selected;

        public void ShowAnnotations(IReadOnlyList<AnnotationModel> annotations) => Annotations = annotations;

        public void ShowUndoRedo(bool canUndo, bool canRedo) => (CanUndo, CanRedo) = (canUndo, canRedo);

        public void BeginTextInput(Point location, Color color, int fontSize) =>
            TextInput = (location, color, fontSize);

        public void CopyImageToClipboard() => CopyCount++;

        public string? AskSavePath(string suggestedFileName)
        {
            SuggestedFileName = suggestedFileName;

            return SavePath;
        }

        public void SaveImage(string path) => SavedPath = path;
    }
}
