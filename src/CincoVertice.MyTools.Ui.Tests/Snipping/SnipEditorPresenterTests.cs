using System.ComponentModel;
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

    private static readonly Color _red = Color.FromArgb(255, 255, 0, 0);

    /// <summary>Draws a rectangle from (100, 100) to (200, 180) and an arrow, then picks the select tool.</summary>
    private void DrawRectangleAndArrow()
    {
        _view.SelectTool(SnipTool.Rectangle);
        _view.Press(100, 100);
        _view.Release(200, 180);
        _view.SelectTool(SnipTool.Arrow);
        _view.Press(300, 300);
        _view.Release(400, 300);
        _view.SelectTool(SnipTool.Select);
    }

    private void Click(int x, int y)
    {
        _view.Press(x, y);
        _view.Release(x, y);
    }

    private void Drag(int fromX, int fromY, int toX, int toY)
    {
        _view.Press(fromX, fromY);
        _view.Move(toX, toY);
        _view.Release(toX, toY);
    }

    private RectangleAnnotationModel TheRectangle => Assert.IsType<RectangleAnnotationModel>(_presenter.Annotations[0]);

    private ArrowAnnotationModel TheArrow => Assert.IsType<ArrowAnnotationModel>(_presenter.Annotations[1]);

    [Fact]
    public void Select_DrawsNothing()
    {
        // Arrange
        _view.SelectTool(SnipTool.Select);

        // Act
        Drag(10, 10, 100, 100);

        // Assert
        Assert.Empty(_presenter.Annotations);
        Assert.Null(_view.Selection);
    }

    [Fact]
    public void Click_OnALine_SelectsIt_ShowingAFrameWithHandles()
    {
        // Arrange
        DrawRectangleAndArrow();

        // Act
        Click(100, 140);

        // Assert
        Assert.Equal(new Rectangle(100, 100, 100, 80), _view.Selection!.Box);
        Assert.Equal(8, _view.Selection.Handles.Count);
    }

    [Fact]
    public void Click_OnAnArrow_ShowsOnlyItsEnds()
    {
        // Arrange
        DrawRectangleAndArrow();

        // Act
        Click(350, 300);

        // Assert
        Assert.Null(_view.Selection!.Box);
        Assert.Equal([new Point(300, 300), new Point(400, 300)], _view.Selection.Handles);
    }

    [Fact]
    public void Click_InTheEmptyPartOfARectangle_SelectsNothing()
    {
        // Arrange
        DrawRectangleAndArrow();

        // Act
        Click(150, 140);

        // Assert
        Assert.Null(_view.Selection);
    }

    [Fact]
    public void Click_WhereTwoOverlap_SelectsTheOneOnTop()
    {
        // Arrange: a second rectangle exactly over the first
        _view.SelectTool(SnipTool.Rectangle);
        _view.Press(100, 100);
        _view.Release(200, 180);
        _view.SelectColor(Color.Blue);
        _view.Press(100, 100);
        _view.Release(200, 180);
        _view.SelectTool(SnipTool.Select);

        // Act
        Click(100, 140);

        // Assert
        Assert.Equal(Solid(Color.Blue), _view.Color);
    }

    [Fact]
    public void Click_Elsewhere_Deselects()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act
        Click(600, 600);

        // Assert
        Assert.Null(_view.Selection);
    }

    [Fact]
    public void Drag_MovesIt_ShowingItOnTheWay_AndKeepingItOnRelease()
    {
        // Arrange
        DrawRectangleAndArrow();
        int copies = _view.CopyCount;

        // Act
        _view.Press(100, 140);
        _view.Move(110, 150);

        // Assert: only a preview so far
        Assert.Equal(new Rectangle(110, 110, 100, 80), Assert.IsType<RectangleAnnotationModel>(_view.Annotations[0]).Bounds);
        Assert.Equal(new Rectangle(100, 100, 100, 80), TheRectangle.Bounds);
        Assert.Equal(copies, _view.CopyCount);

        // Act
        _view.Release(130, 170);

        // Assert
        Assert.Equal(new Rectangle(130, 130, 100, 80), TheRectangle.Bounds);
        Assert.Equal(copies + 1, _view.CopyCount);
    }

    [Fact]
    public void Drag_InsideTheFrameOfWhatsSelected_MovesIt()
    {
        // Arrange: nothing is drawn there, but it's what the frame holds
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act
        Drag(150, 140, 160, 150);

        // Assert
        Assert.Equal(new Rectangle(110, 110, 100, 80), TheRectangle.Bounds);
    }

    [Fact]
    public void Drag_EveryKindOfAnnotation_Moves()
    {
        // Arrange
        _view.SelectTool(SnipTool.Pen);
        _view.Press(10, 10);
        _view.Move(20, 10);
        _view.Release(30, 10);
        _view.SelectTool(SnipTool.Blur);
        _view.Press(100, 100);
        _view.Release(150, 150);
        _view.SelectTool(SnipTool.Text);
        _view.CommitText(300, 300, "hi");
        _view.SelectTool(SnipTool.Select);

        // Act
        Drag(20, 10, 25, 15);
        Drag(120, 120, 125, 125);
        Drag(305, 305, 310, 310);

        // Assert
        Assert.Equal([new Point(15, 15), new Point(25, 15), new Point(35, 15)], Assert.IsType<StrokeAnnotationModel>(_presenter.Annotations[0]).Points);
        Assert.Equal(new Rectangle(105, 105, 50, 50), Assert.IsType<BlurAnnotationModel>(_presenter.Annotations[1]).Bounds);
        Assert.Equal(new Point(305, 305), Assert.IsType<TextAnnotationModel>(_presenter.Annotations[2]).Location);
    }

    [Fact]
    public void Click_ChangesNothing_AndLeavesNothingToUndo()
    {
        // Arrange
        DrawRectangleAndArrow();
        int copies = _view.CopyCount;

        // Act
        Click(100, 140);
        _view.Undo();

        // Assert: the undo took away the arrow, not a change the click made
        Assert.Single(_presenter.Annotations);
        Assert.Equal(copies + 1, _view.CopyCount);
    }

    [Theory]
    [InlineData(200, 180, 240, 200, 100, 100, 140, 100)]
    [InlineData(100, 100, 80, 90, 80, 90, 120, 90)]
    [InlineData(150, 180, 999, 220, 100, 100, 100, 120)]
    [InlineData(200, 140, 260, 999, 100, 100, 160, 80)]
    public void DragAHandle_ScalesFromTheOtherSide(int fromX, int fromY, int toX, int toY, int x, int y, int width, int height)
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act
        Drag(fromX, fromY, toX, toY);

        // Assert
        Assert.Equal(new Rectangle(x, y, width, height), TheRectangle.Bounds);
    }

    [Fact]
    public void DragAHandle_PastTheOtherSide_StopsThere()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act
        Drag(200, 180, 10, 10);

        // Assert
        Assert.Equal(new Rectangle(100, 100, 2, 2), TheRectangle.Bounds);
    }

    /// <summary>Draws with the pen, 16 wide, through these points, then selects it at the first one.</summary>
    private void ThickLine(params Point[] points)
    {
        _view.SelectTool(SnipTool.Pen);
        _view.SelectSize(16);
        _view.Press(points[0].X, points[0].Y);

        foreach (Point point in points.Skip(1))
        {
            _view.Move(point.X, point.Y);
        }

        _view.Release(points[^1].X, points[^1].Y);
        _view.SelectTool(SnipTool.Select);
        Click(points[0].X, points[0].Y);
    }

    private StrokeAnnotationModel TheStroke => Assert.IsType<StrokeAnnotationModel>(_presenter.Annotations[0]);

    [Fact]
    public void StrokesFrame_GoesAroundItsThickness()
    {
        // Act
        ThickLine(new Point(100, 100), new Point(300, 100));

        // Assert: what's drawn, not only the line down its middle
        Assert.Equal(new Rectangle(92, 92, 216, 16), _view.Selection!.Box);
    }

    [Fact]
    public void DragAHandle_ScalesAPenLine_AndItsWidth()
    {
        // Arrange: 4 wide, so the frame is (98, 98) to (202, 202)
        _view.SelectTool(SnipTool.Pen);
        _view.Press(100, 100);
        _view.Move(150, 150);
        _view.Release(200, 200);
        _view.SelectTool(SnipTool.Select);
        Click(150, 150);

        // Act: twice as large
        Drag(202, 202, 306, 306);

        // Assert
        Assert.Equal([new Point(102, 102), new Point(202, 202), new Point(302, 302)], TheStroke.Points);
        Assert.Equal(8, TheStroke.Width);
    }

    [Fact]
    public void DragAcrossALevelLine_MakesItThicker_AndKeepsItLevel()
    {
        // Arrange: its frame is 16 tall
        ThickLine(new Point(100, 100), new Point(300, 100));

        // Act: the bottom middle handle, to twice the height
        Drag(200, 108, 200, 124);

        // Assert
        Assert.Equal(32, TheStroke.Width);
        Assert.Equal([new Point(108, 108), new Point(292, 108)], TheStroke.Points);

        // Its top and its sides stay where they were, as dragging only the bottom should do
        Assert.Equal(new Rectangle(92, 92, 216, 32), _view.Selection!.Box);
    }

    [Fact]
    public void DragAlongALevelLine_MakesItLonger_KeepingItsThickness()
    {
        // Arrange: its frame is 216 wide
        ThickLine(new Point(100, 100), new Point(300, 100));

        // Act: the right middle handle
        Drag(308, 100, 524, 100);

        // Assert
        Assert.Equal(16, TheStroke.Width);
        Assert.Equal(100, TheStroke.Points[0].Y);
        Assert.Equal(100, TheStroke.Points[^1].Y);
        Assert.True(TheStroke.Points[^1].X > 500);
    }

    [Fact]
    public void DragAcrossALine_DoesntStretchTheWobbleOfTheHand()
    {
        // Arrange: nearly level, drawn by hand
        ThickLine(new Point(100, 100), new Point(150, 102), new Point(200, 100), new Point(250, 102), new Point(300, 100));

        // Act: to about 3 times the height
        Drag(200, 110, 200, 140);

        // Assert: the thickness grows, and the line stays nearly level
        Assert.True(TheStroke.Width >= 40, $"Width {TheStroke.Width}");
        Assert.True(TheStroke.Points.Max(point => point.Y) - TheStroke.Points.Min(point => point.Y) <= 6);
    }

    [Fact]
    public void DragDownAVerticalLine_MakesItLonger_KeepingItsThickness()
    {
        // Arrange
        ThickLine(new Point(100, 100), new Point(100, 300));

        // Act: the bottom middle handle
        Drag(100, 308, 100, 524);

        // Assert
        Assert.Equal(16, TheStroke.Width);
    }

    [Fact]
    public void DragAHandleOfADot_ScalesItInPlace()
    {
        // Arrange: a click with a thick pen
        ThickLine(new Point(100, 100));

        // Act: its frame is (92, 92) to (108, 108), and this makes it twice as large
        Drag(108, 108, 124, 124);

        // Assert
        Assert.Equal(32, TheStroke.Width);
        Assert.Equal(new Point(108, 108), Assert.Single(TheStroke.Points));
    }

    /// <summary>Draws with the highlighter, 16 wide, through these points, then selects it at the first one.</summary>
    private void DrawHighlight(params Point[] points)
    {
        _view.SelectTool(SnipTool.Highlighter);
        _view.Press(points[0].X, points[0].Y);

        foreach (Point point in points.Skip(1))
        {
            _view.Move(point.X, point.Y);
        }

        _view.Release(points[^1].X, points[^1].Y);
        _view.SelectTool(SnipTool.Select);
        Click(points[0].X, points[0].Y);
    }

    [Fact]
    public void ScalingAHighlight_HighlightsTheWholeFrame_InItsColor()
    {
        // Arrange: drawn by hand, nearly level, in yellow and half see-through
        DrawHighlight(new Point(100, 100), new Point(150, 103), new Point(200, 100));

        // Assert: its frame is (92, 92) to (208, 111)
        Assert.Equal(new Rectangle(92, 92, 116, 19), _view.Selection!.Box);
        Color color = TheStroke.Color;

        // Act: the bottom middle handle
        Drag(150, 111, 150, 151);

        // Assert
        HighlightAnnotationModel highlight = Assert.IsType<HighlightAnnotationModel>(_presenter.Annotations[0]);
        Assert.Equal(new Rectangle(92, 92, 116, 59), highlight.Bounds);
        Assert.Equal(color, highlight.Color);
        Assert.Equal(highlight.Bounds, _view.Selection!.Box);
    }

    [Fact]
    public void ScalingAHighlight_ShowsTheWholeFrameWhileDragging()
    {
        // Arrange
        DrawHighlight(new Point(100, 100), new Point(200, 100));

        // Act
        _view.Press(150, 108);
        _view.Move(150, 148);

        // Assert
        Assert.IsType<HighlightAnnotationModel>(Assert.Single(_view.Annotations));
        Assert.IsType<StrokeAnnotationModel>(_presenter.Annotations[0]);
    }

    [Fact]
    public void MovingAHighlight_LeavesItAsDrawn()
    {
        // Arrange
        DrawHighlight(new Point(100, 100), new Point(200, 100));

        // Act
        Drag(150, 100, 160, 110);

        // Assert
        Assert.Equal(new Point(110, 110), TheStroke.Points[0]);
    }

    [Fact]
    public void AScaledHighlight_ScalesMovesAndChangesColorAsABox_WithNoSizeToChoose()
    {
        // Arrange
        DrawHighlight(new Point(100, 100), new Point(200, 100));
        Drag(150, 108, 150, 148);

        // Assert
        Assert.Empty(_view.Sizes);
        Assert.Equal(50, _view.Transparency);

        // Act
        Drag(150, 148, 150, 158);
        Drag(150, 120, 160, 130);
        _view.SelectColor(Color.Blue);

        // Assert
        HighlightAnnotationModel highlight = Assert.IsType<HighlightAnnotationModel>(_presenter.Annotations[0]);
        Assert.Equal(new Rectangle(102, 102, 116, 66), highlight.Bounds);
        Assert.Equal(Color.FromArgb(128, Solid(Color.Blue)), highlight.Color);
    }

    [Fact]
    public void AScaledHighlight_IsSelectedByClickingAnywhereInIt()
    {
        // Arrange
        DrawHighlight(new Point(100, 100), new Point(200, 100));
        Drag(150, 108, 150, 148);
        Click(5000, 5000);

        // Act
        Click(150, 130);

        // Assert
        Assert.NotNull(_view.Selection);
    }

    [Fact]
    public void Undo_TakesAScaledHighlightBackToItsLine()
    {
        // Arrange
        DrawHighlight(new Point(100, 100), new Point(200, 100));
        Drag(150, 108, 150, 148);

        // Act
        _view.Undo();

        // Assert
        Assert.IsType<StrokeAnnotationModel>(_presenter.Annotations[0]);
    }

    [Fact]
    public void CornerRadius_IsOnlyShownForTheRectangleTool()
    {
        // Assert: the pen is the first tool
        Assert.Null(_view.CornerRadius);

        // Act
        _view.SelectTool(SnipTool.Rectangle);

        // Assert
        Assert.Equal(12, _view.CornerRadius);
        Assert.Equal(SnipEditorPresenter.CornerRadii, _view.CornerRadii);

        // Act
        _view.SelectTool(SnipTool.Text);

        // Assert
        Assert.Null(_view.CornerRadius);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void CornerRadius_GoesToNewRectangles_AndIsSaved(int radius)
    {
        // Arrange
        _view.SelectTool(SnipTool.Rectangle);

        // Act
        _view.SelectCornerRadius(radius);
        _view.Press(10, 10);
        _view.Release(100, 100);

        // Assert
        Assert.Equal(radius, Assert.IsType<RectangleAnnotationModel>(_presenter.Annotations[0]).CornerRadius);
        Assert.Equal(radius, _view.CornerRadius);
        Assert.Equal(radius, _store.Saved[^1].CornerRadius);
    }

    [Fact]
    public void CornerRadius_ThatIsntOffered_IsIgnored()
    {
        // Arrange
        _view.SelectTool(SnipTool.Rectangle);
        int saves = _store.Saved.Count;

        // Act
        _view.SelectCornerRadius(7);

        // Assert
        Assert.Equal(12, _view.CornerRadius);
        Assert.Equal(saves, _store.Saved.Count);
    }

    [Theory]
    [InlineData(24, 24)]
    [InlineData(5, 12)]
    public void SavedCornerRadius_IsUsed_OrTheDefaultWhenNotOffered(int saved, int expected)
    {
        // Arrange
        var view = new FakeSnipEditorView();
        var settings = new SnipEditorSettingsModel(
            SnipTool.Rectangle,
            new Dictionary<SnipTool, SnipToolSettingsModel>(),
            [],
            CornerRadius: saved);

        // Act
        new SnipEditorPresenter(view, _time, new FakeSettingsStore(settings)).Start();

        // Assert
        Assert.Equal(expected, view.CornerRadius);
    }

    [Fact]
    public void CornerRadius_OfASelectedRectangle_IsShownAndChanged_LeavingTheTools()
    {
        // Arrange
        DrawRectangleAndArrow();

        // Act
        Click(100, 140);

        // Assert
        Assert.Equal(12, _view.CornerRadius);

        // Act
        _view.SelectCornerRadius(48);

        // Assert
        Assert.Equal(48, TheRectangle.CornerRadius);
        _view.SelectTool(SnipTool.Rectangle);
        Assert.Equal(12, _view.CornerRadius);
    }

    [Fact]
    public void CornerRadius_OfASelectedRectangle_CanBeUndoneAndReset()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);
        _view.SelectCornerRadius(0);

        // Act
        _view.Reset();

        // Assert
        Assert.Equal(12, TheRectangle.CornerRadius);

        // Act
        _view.Undo();

        // Assert
        Assert.Equal(0, TheRectangle.CornerRadius);
    }

    [Fact]
    public void CornerRadius_OfAScaledHighlight_StartsAt8_AndCanBeChanged()
    {
        // Arrange
        DrawHighlight(new Point(100, 100), new Point(200, 100));
        Drag(150, 108, 150, 148);

        // Assert
        Assert.Equal(8, _view.CornerRadius);

        // Act
        _view.SelectCornerRadius(0);

        // Assert
        Assert.Equal(0, Assert.IsType<HighlightAnnotationModel>(_presenter.Annotations[0]).CornerRadius);
    }

    [Fact]
    public void CornerRadius_IsHiddenForWhatHasNoCorners()
    {
        // Arrange: an arrow, and a highlight not yet scaled
        DrawRectangleAndArrow();
        _view.SelectTool(SnipTool.Highlighter);
        _view.Press(500, 500);
        _view.Release(600, 500);
        _view.SelectTool(SnipTool.Select);

        // Act & Assert
        Click(350, 300);
        Assert.Null(_view.CornerRadius);

        Click(550, 500);
        Assert.Null(_view.CornerRadius);
    }

    [Fact]
    public void CornerRadius_OfSomethingWithout_ChangesNothing()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(350, 300);
        int copies = _view.CopyCount;

        // Act
        _view.SelectCornerRadius(48);

        // Assert
        Assert.Equal(copies, _view.CopyCount);
        Assert.Null(TheArrow.Original);
    }

    private void ShiftClick(int x, int y)
    {
        _view.ShiftPress(x, y);
        _view.Release(x, y);
    }

    /// <summary>A rectangle from (100, 100) to (200, 180), an arrow from (300, 300) to (400, 300), and a text at (500, 500).</summary>
    private void DrawThree()
    {
        DrawRectangleAndArrow();
        _view.SelectTool(SnipTool.Text);
        _view.CommitText(500, 500, "hi");
        _view.SelectTool(SnipTool.Select);
    }

    private TextAnnotationModel TheText => Assert.IsType<TextAnnotationModel>(_presenter.Annotations[2]);

    [Fact]
    public void ShiftClick_AddsToWhatsSelected_ShowingOneFrameAroundThem()
    {
        // Arrange
        DrawThree();
        Click(100, 140);

        // Act
        ShiftClick(350, 300);

        // Assert: around the rectangle and the arrow, with no handles, since several can't be scaled
        Assert.Equal(Rectangle.FromLTRB(100, 100, 400, 300), _view.Selection!.Box);
        Assert.Empty(_view.Selection.Handles);
    }

    [Fact]
    public void ShiftClick_OnWhatsSelected_TakesItAway()
    {
        // Arrange
        DrawThree();
        Click(100, 140);
        ShiftClick(350, 300);

        // Act
        ShiftClick(100, 140);

        // Assert: the arrow alone, with its ends
        Assert.Null(_view.Selection!.Box);
        Assert.Equal(2, _view.Selection.Handles.Count);
    }

    [Fact]
    public void Click_OnSomethingElse_SelectsOnlyIt()
    {
        // Arrange
        DrawThree();
        _view.SelectAll();

        // Act
        Click(510, 510);

        // Assert
        Assert.Equal(new Rectangle(500, 500, 24, 24), _view.Selection!.Box);
    }

    [Fact]
    public void Click_InTheEmptyPartOfTheFrameOfSeveral_KeepsThemAll()
    {
        // Arrange
        DrawThree();
        Click(100, 140);
        ShiftClick(350, 300);

        // Act: on neither, inside their frame
        Click(250, 250);

        // Assert
        Assert.Equal(Rectangle.FromLTRB(100, 100, 400, 300), _view.Selection!.Box);
    }

    [Fact]
    public void Band_SelectsWhatsWhollyInsideIt()
    {
        // Arrange
        DrawThree();

        // Act: from the empty space up and left of everything, past the arrow but not the text
        _view.Press(50, 50);
        _view.Move(450, 350);

        // Assert: shown while it's dragged
        Assert.Equal(Rectangle.FromLTRB(50, 50, 450, 350), _view.SelectionBand);

        // Act
        _view.Release(450, 350);

        // Assert
        Assert.Null(_view.SelectionBand);
        Assert.Equal(Rectangle.FromLTRB(100, 100, 400, 300), _view.Selection!.Box);
    }

    [Fact]
    public void Band_ThatCutsThroughSomething_LeavesItOut()
    {
        // Arrange
        DrawThree();

        // Act: across half the rectangle
        Drag(50, 50, 150, 250);

        // Assert
        Assert.Null(_view.Selection);
    }

    [Fact]
    public void ShiftBand_AddsToWhatsSelected()
    {
        // Arrange
        DrawThree();
        Click(510, 510);

        // Act
        _view.ShiftPress(50, 50);
        _view.Move(250, 250);
        _view.Release(250, 250);

        // Assert: the text and the rectangle
        Assert.Equal(Rectangle.FromLTRB(100, 100, 524, 524), _view.Selection!.Box);
    }

    [Fact]
    public void SelectAll_SelectsEverything_WithTheSelectTool()
    {
        // Arrange
        DrawThree();
        _view.SelectTool(SnipTool.Pen);

        // Act
        _view.SelectAll();

        // Assert
        Assert.Equal(SnipTool.Select, _view.Tool);
        Assert.Equal(Rectangle.FromLTRB(100, 100, 524, 524), _view.Selection!.Box);
    }

    [Fact]
    public void Drag_MovesAllThatsSelected_AsOneStepToUndo()
    {
        // Arrange
        DrawThree();
        Click(100, 140);
        ShiftClick(350, 300);

        // Act: from the arrow
        Drag(350, 300, 360, 320);

        // Assert
        Assert.Equal(new Rectangle(110, 120, 100, 80), TheRectangle.Bounds);
        Assert.Equal((new Point(310, 320), new Point(410, 320)), (TheArrow.From, TheArrow.To));
        Assert.Equal(new Point(500, 500), TheText.Location);

        // Act
        _view.Undo();

        // Assert
        Assert.Equal(new Rectangle(100, 100, 100, 80), TheRectangle.Bounds);
        Assert.Equal(new Point(300, 300), TheArrow.From);
    }

    [Fact]
    public void Drag_InsideTheFrameOfSeveral_MovesThemAll()
    {
        // Arrange: (250, 250) is on neither, but inside their frame
        DrawThree();
        Click(100, 140);
        ShiftClick(350, 300);

        // Act
        Drag(250, 250, 255, 255);

        // Assert
        Assert.Equal(new Point(105, 105), TheRectangle.Bounds.Location);
        Assert.Equal(new Point(305, 305), TheArrow.From);
    }

    [Fact]
    public void Drag_ShowsThemAllMovingBeforeRelease()
    {
        // Arrange
        DrawThree();
        _view.SelectAll();

        // Act
        _view.Press(350, 300);
        _view.Move(360, 300);

        // Assert
        Assert.Equal(new Rectangle(110, 100, 100, 80), Assert.IsType<RectangleAnnotationModel>(_view.Annotations[0]).Bounds);
        Assert.Equal(Rectangle.FromLTRB(110, 100, 534, 524), _view.Selection!.Box);
        Assert.Equal(new Rectangle(100, 100, 100, 80), TheRectangle.Bounds);
    }

    [Fact]
    public void Nudge_DeleteAndReset_DoThemAll()
    {
        // Arrange
        DrawThree();
        _view.SelectAll();

        // Act & Assert
        _view.Nudge(0, 10);
        Assert.Equal(new Point(100, 110), TheRectangle.Bounds.Location);
        Assert.Equal(new Point(500, 510), TheText.Location);

        _view.Reset();
        Assert.Equal(new Point(100, 100), TheRectangle.Bounds.Location);
        Assert.Equal(new Point(500, 500), TheText.Location);

        _view.Delete();
        Assert.Empty(_presenter.Annotations);
    }

    [Fact]
    public void Toolbar_WithSeveral_ShowsTheFirstsSettings_AndChangesThemAll()
    {
        // Arrange
        DrawThree();
        _view.SelectAll();

        // Assert: the rectangle's, and the arrow's heads, and the text's edge, as each is the first with it
        Assert.Equal(4, _view.Size);
        Assert.Equal(SnipEditorPresenter.LineSizes, _view.Sizes);
        Assert.Equal(SnipArrowHeads.End, _view.ArrowHeads);
        Assert.Equal(12, _view.CornerRadius);
        Assert.NotNull(_view.TextOutline);

        // Act
        _view.SelectColor(Color.Blue);
        _view.SelectTransparency(50);

        // Assert
        Assert.All(_presenter.Annotations, annotation => Assert.Equal(Color.FromArgb(128, Solid(Color.Blue)), annotation.Color));
    }

    [Fact]
    public void Toolbar_SizeWithSeveral_GoesOnlyToThoseWithThatKindOfSize()
    {
        // Arrange
        DrawThree();
        _view.SelectAll();

        // Act: a line width
        _view.SelectSize(10);

        // Assert: the text keeps its font size
        Assert.Equal(10, TheRectangle.Width);
        Assert.Equal(10, TheArrow.Width);
        Assert.Equal(24, TheText.FontSize);
    }

    [Fact]
    public void Toolbar_ChoiceOfOneKind_GoesOnlyToThoseItsFor()
    {
        // Arrange
        DrawThree();
        _view.SelectAll();

        // Act
        _view.SelectArrowHeads(SnipArrowHeads.Both);
        _view.SelectCornerRadius(0);

        // Assert
        Assert.Equal(SnipArrowHeads.Both, TheArrow.Heads);
        Assert.Equal(0, TheRectangle.CornerRadius);
        Assert.Equal("hi", TheText.Text);
    }

    [Fact]
    public void Duplicate_OfSeveral_CopiesThemAll_SelectingTheCopies()
    {
        // Arrange
        DrawThree();
        Click(100, 140);
        ShiftClick(350, 300);

        // Act
        _view.Duplicate();

        // Assert
        Assert.Equal(5, _presenter.Annotations.Count);
        Assert.Equal(new Rectangle(110, 110, 100, 80), Assert.IsType<RectangleAnnotationModel>(_presenter.Annotations[3]).Bounds);
        Assert.Equal(new Point(310, 310), Assert.IsType<ArrowAnnotationModel>(_presenter.Annotations[4]).From);
        Assert.Equal(Rectangle.FromLTRB(110, 110, 410, 310), _view.Selection!.Box);
    }

    [Fact]
    public void Duplicate_OfSeveralSteps_NumbersThemOnFromTheHighest()
    {
        // Arrange
        ClickSteps(100, 200);
        _view.SelectAll();

        // Act
        _view.Duplicate();

        // Assert
        Assert.Equal([1, 2, 3, 4], Steps.Select(step => step.Number));
    }

    [Fact]
    public void BringToFrontAndSendToBack_MoveSeveral_KeepingTheirOrder()
    {
        // Arrange: the rectangle and the text, around the arrow
        DrawThree();
        Click(100, 140);
        ShiftClick(510, 510);

        // Act
        _view.BringToFront();

        // Assert
        Assert.IsType<ArrowAnnotationModel>(_presenter.Annotations[0]);
        Assert.IsType<RectangleAnnotationModel>(_presenter.Annotations[1]);
        Assert.IsType<TextAnnotationModel>(_presenter.Annotations[2]);

        // Act
        _view.SendToBack();

        // Assert
        Assert.IsType<RectangleAnnotationModel>(_presenter.Annotations[0]);
        Assert.IsType<TextAnnotationModel>(_presenter.Annotations[1]);
        Assert.IsType<ArrowAnnotationModel>(_presenter.Annotations[2]);
        Assert.Equal(Rectangle.FromLTRB(100, 100, 524, 524), _view.Selection!.Box);
    }

    [Fact]
    public void RightClick_OnOneOfSeveral_KeepsThemAll()
    {
        // Arrange
        DrawThree();
        Click(100, 140);
        ShiftClick(350, 300);

        // Act
        _view.RightClick(350, 300);

        // Assert: the rectangle and the arrow are the two at the bottom
        Assert.Equal(Rectangle.FromLTRB(100, 100, 400, 300), _view.Selection!.Box);
        Assert.Equal(new SnipContextMenuModel(false, true, false), _view.ContextMenuModel);
    }

    [Fact]
    public void RightClick_OnSomethingNotSelected_SelectsItAlone()
    {
        // Arrange
        DrawThree();
        Click(100, 140);
        ShiftClick(350, 300);

        // Act
        _view.RightClick(510, 510);

        // Assert
        Assert.Equal(new Rectangle(500, 500, 24, 24), _view.Selection!.Box);
    }

    [Fact]
    public void Several_HaveNoHandles_AndNoTextToEdit()
    {
        // Arrange
        DrawThree();
        _view.SelectAll();

        // Act: where the rectangle's bottom right handle would be, and a double-click on the text
        _view.Hover(200, 180);
        SnipCursor cursor = _view.Cursor;
        _view.DoubleClick(510, 510);

        // Assert
        Assert.Equal(SnipCursor.Move, cursor);
        Assert.Null(_view.TextInput);
    }

    [Fact]
    public void Duplicate_AddsACopyOnTop_ALittleAside_Selected()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);
        int copies = _view.CopyCount;

        // Act
        _view.Duplicate();

        // Assert
        RectangleAnnotationModel copy = Assert.IsType<RectangleAnnotationModel>(_presenter.Annotations[^1]);
        Assert.Equal(3, _presenter.Annotations.Count);
        Assert.Equal(new Rectangle(110, 110, 100, 80), copy.Bounds);
        Assert.Equal(TheRectangle with { Bounds = copy.Bounds }, copy);
        Assert.Equal(copy.Bounds, _view.Selection!.Box);
        Assert.Equal(copies + 1, _view.CopyCount);
    }

    [Fact]
    public void Duplicate_OfSomethingChanged_IsNewToReset()
    {
        // Arrange: the original was moved, so it has something to go back to
        DrawRectangleAndArrow();
        Drag(100, 140, 110, 150);

        // Act
        _view.Duplicate();

        // Assert: the copy was never anything else
        Assert.Null(_presenter.Annotations[^1].Original);
        Assert.NotNull(_presenter.Annotations[0].Original);
    }

    [Fact]
    public void Duplicate_OfAStep_HasTheNextNumber()
    {
        // Arrange
        ClickSteps(100, 200);
        _view.SelectTool(SnipTool.Select);
        Click(100, 100);

        // Act
        _view.Duplicate();

        // Assert
        Assert.Equal([1, 2, 3], Steps.Select(step => step.Number));
        Assert.Equal(new Point(110, 110), Steps[2].Center);
    }

    [Fact]
    public void Duplicate_CanBeUndone()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);
        _view.Duplicate();

        // Act
        _view.Undo();

        // Assert
        Assert.Equal(2, _presenter.Annotations.Count);
    }

    [Fact]
    public void Duplicate_WithNothingSelected_DoesNothing()
    {
        // Arrange
        DrawRectangleAndArrow();

        // Act
        _view.Duplicate();

        // Assert
        Assert.Equal(2, _presenter.Annotations.Count);
    }

    [Fact]
    public void BringToFront_PutsItOnTop_StillSelected()
    {
        // Arrange: the rectangle is under the arrow
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act
        _view.BringToFront();

        // Assert
        Assert.IsType<ArrowAnnotationModel>(_presenter.Annotations[0]);
        Assert.IsType<RectangleAnnotationModel>(_presenter.Annotations[1]);
        Assert.Equal(new Rectangle(100, 100, 100, 80), _view.Selection!.Box);

        // Act: it follows the selection, which is what's changed
        _view.SelectColor(Color.Blue);

        // Assert
        Assert.Equal(Solid(Color.Blue), Assert.IsType<RectangleAnnotationModel>(_presenter.Annotations[1]).Color);
    }

    [Fact]
    public void SendToBack_PutsItUnderTheOthers_StillSelected()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(350, 300);

        // Act
        _view.SendToBack();

        // Assert
        Assert.IsType<ArrowAnnotationModel>(_presenter.Annotations[0]);
        Assert.Null(_view.Selection!.Box);
    }

    [Fact]
    public void BringToFrontAndSendToBack_WhenItsThereAlready_LeaveNothingToUndo()
    {
        // Arrange: the arrow is on top
        DrawRectangleAndArrow();
        Click(350, 300);
        int copies = _view.CopyCount;

        // Act
        _view.BringToFront();
        Click(100, 140);
        _view.SendToBack();

        // Assert
        Assert.Equal(copies, _view.CopyCount);
    }

    [Fact]
    public void BringToFront_CanBeUndone()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);
        _view.BringToFront();

        // Act
        _view.Undo();

        // Assert
        Assert.IsType<RectangleAnnotationModel>(_presenter.Annotations[0]);
    }

    [Theory]
    [InlineData(100, 140, true, false)]
    [InlineData(350, 300, false, true)]
    public void ContextMenu_OffersOnlyTheMovesThatChangeSomething(int x, int y, bool front, bool back)
    {
        // Arrange: the rectangle under, the arrow on top
        DrawRectangleAndArrow();

        // Act
        _view.RightClick(x, y);

        // Assert
        Assert.Equal(new SnipContextMenuModel(false, front, back), _view.ContextMenuModel);
    }

    [Fact]
    public void RightClick_OnSomething_SelectsItAndShowsTheMenu()
    {
        // Arrange
        DrawRectangleAndArrow();

        // Act
        _view.RightClick(100, 140);

        // Assert: nothing to reset, as it's as drawn
        Assert.Equal((new Point(100, 140), false), _view.ContextMenu);
        Assert.Equal(new Rectangle(100, 100, 100, 80), _view.Selection!.Box);
    }

    [Fact]
    public void RightClick_OnNothing_ShowsNoMenu_AndKeepsTheSelection()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act
        _view.RightClick(600, 600);

        // Assert
        Assert.Null(_view.ContextMenu);
        Assert.NotNull(_view.Selection);
    }

    [Fact]
    public void RightClick_InsideTheFrameOrOnAHandleOfWhatsSelected_ShowsTheMenu()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act & Assert
        _view.RightClick(150, 140);
        Assert.Equal(new Point(150, 140), _view.ContextMenu!.Value.Location);

        _view.RightClick(200, 180);
        Assert.Equal(new Point(200, 180), _view.ContextMenu!.Value.Location);
    }

    [Fact]
    public void RightClick_WithADrawingTool_DoesNothing()
    {
        // Arrange
        DrawRectangleAndArrow();
        _view.SelectTool(SnipTool.Pen);

        // Act
        _view.RightClick(100, 140);

        // Assert
        Assert.Null(_view.ContextMenu);
    }

    [Fact]
    public void RightClick_AfterAChange_OffersTheReset()
    {
        // Arrange
        DrawRectangleAndArrow();
        Drag(100, 140, 110, 150);

        // Act
        _view.RightClick(110, 150);

        // Assert
        Assert.True(_view.ContextMenu!.Value.CanReset);
    }

    [Fact]
    public void Reset_AfterSeveralChanges_GoesBackToAsDrawn_AndCanBeUndone()
    {
        // Arrange
        DrawRectangleAndArrow();
        Drag(100, 140, 110, 150);
        Drag(200, 190, 300, 290);
        _view.SelectColor(Color.Blue);
        _view.SelectSize(20);
        Rectangle changed = TheRectangle.Bounds;

        // Act
        _view.Reset();

        // Assert
        Assert.Equal(new Rectangle(100, 100, 100, 80), TheRectangle.Bounds);
        Assert.Equal(SnipEditorPresenter.Palette[0], TheRectangle.Color);
        Assert.Equal(4, TheRectangle.Width);
        Assert.Equal(4, _view.Size);
        Assert.Null(TheRectangle.Original);

        // Act
        _view.Undo();

        // Assert
        Assert.Equal(changed, TheRectangle.Bounds);
    }

    [Fact]
    public void Reset_TakesAScaledHighlightBackToItsLine()
    {
        // Arrange
        DrawHighlight(new Point(100, 100), new Point(200, 100));
        Drag(150, 108, 150, 148);

        // Act
        _view.Reset();

        // Assert
        Assert.Equal([new Point(100, 100), new Point(200, 100)], TheStroke.Points);
    }

    [Fact]
    public void Reset_TakesAnEditedTextBack()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.CommitText(100, 100, "hi");
        _view.SelectTool(SnipTool.Select);
        Click(110, 110);
        _view.DoubleClick(110, 110);
        _view.CommitText(100, 100, "changed");

        // Act
        _view.Reset();

        // Assert
        Assert.Equal("hi", Assert.IsType<TextAnnotationModel>(_presenter.Annotations[0]).Text);
    }

    [Fact]
    public void Reset_OfWhatHasntChanged_LeavesNothingToUndo()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);
        int copies = _view.CopyCount;

        // Act
        _view.Reset();

        // Assert
        Assert.Equal(copies, _view.CopyCount);
    }

    [Fact]
    public void DragAnArrowsEnd_MovesOnlyThatEnd()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(350, 300);

        // Act
        Drag(400, 300, 450, 380);

        // Assert
        Assert.Equal((new Point(300, 300), new Point(450, 380)), (TheArrow.From, TheArrow.To));
    }

    [Fact]
    public void DragAHandle_ScalesATextsFont_KeepingTheOtherCornerInPlace()
    {
        // Arrange: 2 characters at 24 is 24 by 24 here
        _view.SelectTool(SnipTool.Text);
        _view.CommitText(100, 100, "hi");
        _view.SelectTool(SnipTool.Select);
        Click(110, 110);

        // Act: from the top left, to twice the height
        Drag(100, 100, 76, 76);

        // Assert
        TextAnnotationModel text = Assert.IsType<TextAnnotationModel>(_presenter.Annotations[0]);
        Assert.Equal(48, text.FontSize);
        Assert.Equal(new Point(76, 76), text.Location);
        Assert.Equal(new Rectangle(76, 76, 48, 48), _view.Selection!.Box);
    }

    [Fact]
    public void Undo_TakesBackAMove_AndRedoDoesItAgain()
    {
        // Arrange
        DrawRectangleAndArrow();
        Drag(100, 140, 110, 150);

        // Act
        _view.Undo();

        // Assert
        Assert.Equal(new Rectangle(100, 100, 100, 80), TheRectangle.Bounds);
        Assert.Null(_view.Selection);

        // Act
        _view.Redo();

        // Assert
        Assert.Equal(new Rectangle(110, 110, 100, 80), TheRectangle.Bounds);
    }

    [Fact]
    public void Delete_RemovesIt_AndUndoBringsItBack()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act
        _view.Delete();

        // Assert
        Assert.IsType<ArrowAnnotationModel>(Assert.Single(_presenter.Annotations));
        Assert.Null(_view.Selection);

        // Act
        _view.Undo();

        // Assert
        Assert.Equal(2, _presenter.Annotations.Count);
    }

    [Fact]
    public void Delete_WithNothingSelected_DoesNothing()
    {
        // Arrange
        DrawRectangleAndArrow();

        // Act
        _view.Delete();

        // Assert
        Assert.Equal(2, _presenter.Annotations.Count);
    }

    [Fact]
    public void Nudge_MovesWhatsSelected()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act
        _view.Nudge(10, -1);

        // Assert
        Assert.Equal(new Rectangle(110, 99, 100, 80), TheRectangle.Bounds);
    }

    [Fact]
    public void Toolbar_ShowsWhatsSelected_AndChangesOnlyIt()
    {
        // Arrange: the arrow is drawn with its own settings
        _view.SelectTool(SnipTool.Arrow);
        _view.SelectColor(Color.Blue);
        _view.SelectSize(10);
        _view.SelectTransparency(30);
        _view.Press(300, 300);
        _view.Release(400, 300);
        _view.SelectColor(Color.Green);
        _view.SelectTool(SnipTool.Select);

        // Act
        Click(350, 300);

        // Assert
        Assert.True(_view.DrawingChoicesVisible);
        Assert.Equal(Solid(Color.Blue), _view.Color);
        Assert.Equal(10, _view.Size);
        Assert.Equal(30, _view.Transparency);

        // Act
        _view.SelectColor(_red);
        _view.SelectSize(16);
        _view.SelectTransparency(50);

        // Assert: its color is see-through as chosen, and the arrow tool keeps what it had
        ArrowAnnotationModel arrow = Assert.IsType<ArrowAnnotationModel>(_presenter.Annotations[0]);
        Assert.Equal(Color.FromArgb(128, _red), arrow.Color);
        Assert.Equal(16, arrow.Width);

        _view.SelectTool(SnipTool.Arrow);
        Assert.Equal(Solid(Color.Green), _view.Color);
        Assert.Equal(10, _view.Size);
        Assert.Equal(30, _view.Transparency);
    }

    [Fact]
    public void MoreColors_OnWhatsSelected_StartsFromItsColor_AndKeepsTheCustomColor()
    {
        // Arrange
        Color picked = Color.FromArgb(255, 18, 52, 86);
        DrawRectangleAndArrow();
        Click(100, 140);
        _view.ColorToPick = picked;

        // Act
        _view.RequestMoreColors();

        // Assert
        Assert.Equal(SnipEditorPresenter.Palette[0], _view.AskedFrom);
        Assert.Equal(picked, TheRectangle.Color);
        Assert.Equal([picked], _store.Saved[^1].CustomColors);
    }

    [Fact]
    public void Toolbar_ChangesASelectedArrowsHeads()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(350, 300);

        // Assert
        Assert.Equal(SnipArrowHeads.End, _view.ArrowHeads);

        // Act
        _view.SelectArrowHeads(SnipArrowHeads.Both);

        // Assert
        Assert.Equal(SnipArrowHeads.Both, TheArrow.Heads);
    }

    [Fact]
    public void Toolbar_ChangesASelectedTextsSizeAndEdge()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.CommitText(100, 100, "hi");
        _view.SelectTool(SnipTool.Select);
        Click(110, 110);

        // Assert
        Assert.Equal(SnipEditorPresenter.FontSizes, _view.Sizes);
        Assert.Equal(0, _view.TextOutline!.Width);

        // Act: the color first, as it should wait for a width
        _view.SelectTextOutlineColor(Color.Blue);
        _view.SelectTextOutlineWidth(3);
        _view.SelectSize(36);

        // Assert
        TextAnnotationModel text = Assert.IsType<TextAnnotationModel>(_presenter.Annotations[0]);
        Assert.Equal(new SnipTextOutlineModel(3, Solid(Color.Blue)), text.Outline);
        Assert.Equal(36, text.FontSize);
    }

    [Fact]
    public void ABlur_HasNoChoices_ButCanBeMovedAndDeleted()
    {
        // Arrange
        _view.SelectTool(SnipTool.Blur);
        _view.Press(100, 100);
        _view.Release(150, 150);
        _view.SelectTool(SnipTool.Select);

        // Assert
        Assert.False(_view.DrawingChoicesVisible);

        // Act
        Click(120, 120);

        // Assert
        Assert.False(_view.DrawingChoicesVisible);
        Assert.NotNull(_view.Selection);

        // Act
        _view.Delete();

        // Assert
        Assert.Empty(_presenter.Annotations);
    }

    [Fact]
    public void DoubleClick_OnAText_LetsYouTypeOverIt()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.SelectTransparency(50);
        _view.CommitText(100, 100, "hi");
        _view.SelectTool(SnipTool.Select);
        Click(110, 110);

        // Act
        _view.DoubleClick(110, 110);

        // Assert: from its text, solid in the box, and hidden meanwhile
        Assert.Equal((new Point(100, 100), SnipEditorPresenter.Palette[0], 24), _view.TextInput);
        Assert.Equal("hi", _view.TextInputText);
        Assert.Empty(_view.Annotations);
        Assert.Null(_view.Selection);

        // Act
        _view.CommitText(100, 100, "hello");

        // Assert: the one text, with its text changed and the rest as it was
        TextAnnotationModel text = Assert.IsType<TextAnnotationModel>(Assert.Single(_presenter.Annotations));
        Assert.Equal("hello", text.Text);
        Assert.Equal(128, text.Color.A);
        Assert.Single(_view.Annotations);
    }

    [Fact]
    public void DoubleClick_ThenEmptyingTheText_DeletesIt()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.CommitText(100, 100, "hi");
        _view.SelectTool(SnipTool.Select);
        Click(110, 110);
        _view.DoubleClick(110, 110);

        // Act
        _view.CommitText(100, 100, "  ");

        // Assert
        Assert.Empty(_presenter.Annotations);
        Assert.Null(_view.Selection);
    }

    [Fact]
    public void Esc_OnANewTextWithSomethingInIt_KeepsIt_SelectedWithTheSelectTool()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.Press(100, 100);

        // Act
        _view.EscapeText(100, 100, "hi");

        // Assert
        Assert.Equal("hi", Assert.IsType<TextAnnotationModel>(Assert.Single(_presenter.Annotations)).Text);
        Assert.Equal(SnipTool.Select, _view.Tool);
        Assert.Equal(SnipTool.Select, _store.Saved[^1].Tool);
        Assert.Equal(SnipCursor.Default, _view.Cursor);
        Assert.Equal(new Rectangle(100, 100, 24, 24), _view.Selection!.Box);

        // The toolbar is the text's, so it's ready to change too
        Assert.Equal(SnipEditorPresenter.FontSizes, _view.Sizes);
    }

    [Fact]
    public void Esc_OnANewText_SelectsThatOne_NotAnEarlierOne()
    {
        // Arrange
        DrawRectangleAndArrow();
        _view.SelectTool(SnipTool.Text);

        // Act
        _view.EscapeText(500, 500, "hi");

        // Assert
        Assert.Equal(new Rectangle(500, 500, 24, 24), _view.Selection!.Box);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n")]
    public void Esc_OnAnEmptyNewText_AddsNothing_AndStaysWithTheTextTool(string text)
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);

        // Act
        _view.EscapeText(100, 100, text);

        // Assert
        Assert.Empty(_presenter.Annotations);
        Assert.Equal(SnipTool.Text, _view.Tool);
        Assert.Null(_view.Selection);
    }

    [Fact]
    public void Esc_OnATextBeingEdited_KeepsWhatWasTyped_AndItStaysSelected()
    {
        // Arrange
        DrawRectangleAndArrow();
        _view.SelectTool(SnipTool.Text);
        _view.CommitText(500, 500, "hi");
        _view.SelectTool(SnipTool.Select);
        Click(510, 510);
        _view.DoubleClick(510, 510);

        // Act
        _view.EscapeText(500, 500, "changed");

        // Assert
        Assert.Equal("changed", Assert.IsType<TextAnnotationModel>(_presenter.Annotations[2]).Text);
        Assert.Equal(3, _view.Annotations.Count);
        Assert.Equal(new Rectangle(500, 500, 84, 24), _view.Selection!.Box);
        Assert.Equal(SnipTool.Select, _view.Tool);
    }

    [Fact]
    public void Esc_OnATextEmptiedWhileEditing_DeletesIt()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.CommitText(100, 100, "hi");
        _view.SelectTool(SnipTool.Select);
        Click(110, 110);
        _view.DoubleClick(110, 110);

        // Act
        _view.EscapeText(100, 100, string.Empty);

        // Assert
        Assert.Empty(_presenter.Annotations);
        Assert.Null(_view.Selection);
    }

    [Fact]
    public void Esc_OnATextBeingEdited_Unchanged_ShowsItAgain_WithNothingToUndo()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.CommitText(100, 100, "hi");
        _view.SelectTool(SnipTool.Select);
        Click(110, 110);
        int copies = _view.CopyCount;
        _view.DoubleClick(110, 110);

        // Act
        _view.EscapeText(100, 100, "hi");

        // Assert
        Assert.Single(_view.Annotations);
        Assert.NotNull(_view.Selection);
        Assert.Equal(copies, _view.CopyCount);
    }

    [Fact]
    public void Esc_AfterAKeptText_CanBeUndone()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.EscapeText(100, 100, "hi");

        // Act
        _view.Undo();

        // Assert
        Assert.Empty(_presenter.Annotations);
        Assert.Null(_view.Selection);
    }

    [Fact]
    public void DoubleClick_ThenTheSameText_LeavesNothingToUndo()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.CommitText(100, 100, "hi");
        _view.SelectTool(SnipTool.Select);
        Click(110, 110);
        int copies = _view.CopyCount;
        _view.DoubleClick(110, 110);

        // Act
        _view.CommitText(100, 100, "hi");

        // Assert
        Assert.Equal(copies, _view.CopyCount);
        Assert.Single(_view.Annotations);
    }

    [Fact]
    public void ChangingTool_Deselects_AndGoesBackToTheToolsChoices()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act
        _view.SelectTool(SnipTool.Text);

        // Assert
        Assert.Null(_view.Selection);
        Assert.Equal(SnipEditorPresenter.FontSizes, _view.Sizes);
    }

    [Fact]
    public void Deselect_ClearsTheSelection()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act
        _view.Deselect();

        // Assert
        Assert.Null(_view.Selection);
    }

    [Fact]
    public void GoingToAnotherSnip_Deselects()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act
        _view.AddSnip();

        // Assert
        Assert.Null(_view.Selection);
    }

    [Fact]
    public void Hover_ShowsWhatADragWouldDo()
    {
        // Arrange
        DrawRectangleAndArrow();

        // Act & Assert
        _view.Hover(600, 600);
        Assert.Equal(SnipCursor.Default, _view.Cursor);

        _view.Hover(100, 140);
        Assert.Equal(SnipCursor.Move, _view.Cursor);

        Click(100, 140);
        _view.Hover(200, 180);
        Assert.Equal(SnipCursor.SizeTopLeftBottomRight, _view.Cursor);

        _view.Hover(150, 180);
        Assert.Equal(SnipCursor.SizeVertical, _view.Cursor);

        _view.Hover(200, 140);
        Assert.Equal(SnipCursor.SizeHorizontal, _view.Cursor);

        _view.Hover(150, 140);
        Assert.Equal(SnipCursor.Move, _view.Cursor);
    }

    [Fact]
    public void DrawingTools_ShowACross()
    {
        // Act
        _view.SelectTool(SnipTool.Select);
        _view.SelectTool(SnipTool.Pen);

        // Assert
        Assert.Equal(SnipCursor.Cross, _view.Cursor);
    }

    /// <summary>Draws a shaky, nearly level line with a tool.</summary>
    private void DrawShakyLine(SnipTool tool)
    {
        int[] wobble = [0, 3, -2, 2, -3, 1, -1, 3, -2, 0, 2, -3, 1];

        _view.SelectTool(tool);
        _view.Press(100, 100);

        for (int i = 1; i < wobble.Length; i++)
        {
            _view.Move(100 + i * 10, 100 + wobble[i]);
        }

        _view.Release(100 + wobble.Length * 10, 100);
    }

    [Fact]
    public void SmoothPen_EvensOutTheLine_IntoFewerPoints()
    {
        // Act
        DrawShakyLine(SnipTool.SmoothPen);

        // Assert
        StrokeAnnotationModel stroke = TheStroke;
        Assert.True(stroke.IsSmooth);
        Assert.False(stroke.IsHighlight);
        Assert.True(stroke.Points.Count < 13, $"{stroke.Points.Count} points");
        Assert.Equal(new Point(100, 100), stroke.Points[0]);
        Assert.Equal(new Point(230, 100), stroke.Points[^1]);
    }

    [Fact]
    public void Pen_StillDrawsEveryPointAsItIs()
    {
        // Act
        DrawShakyLine(SnipTool.Pen);

        // Assert
        Assert.False(TheStroke.IsSmooth);
        Assert.Equal(14, TheStroke.Points.Count);
    }

    [Fact]
    public void SmoothPen_ShowsTheSmoothLineWhileDrawing()
    {
        // Arrange
        _view.SelectTool(SnipTool.SmoothPen);
        _view.Press(100, 100);

        // Act
        for (int i = 1; i <= 20; i++)
        {
            _view.Move(100 + i * 5, 100 + (i % 2 == 0 ? 2 : -2));
        }

        // Assert
        StrokeAnnotationModel shown = Assert.IsType<StrokeAnnotationModel>(Assert.Single(_view.Annotations));
        Assert.True(shown.IsSmooth);
        Assert.True(shown.Points.Count < 21, $"{shown.Points.Count} points");
    }

    [Fact]
    public void SmoothPen_HasItsOwnSettings()
    {
        // Arrange
        _view.SelectTool(SnipTool.Pen);
        _view.SelectColor(Color.Blue);

        // Act
        _view.SelectTool(SnipTool.SmoothPen);

        // Assert: red, as the pen starts
        Assert.Equal(SnipEditorPresenter.Palette[0], _view.Color);
        Assert.Equal(SnipEditorPresenter.LineSizes, _view.Sizes);
    }

    [Fact]
    public void SmoothPen_Click_LeavesADot()
    {
        // Arrange
        _view.SelectTool(SnipTool.SmoothPen);

        // Act
        Click(100, 100);

        // Assert
        Assert.Equal([new Point(100, 100)], TheStroke.Points);
    }

    [Fact]
    public void SmoothLine_StaysSmooth_WhenScaledOrMoved()
    {
        // Arrange
        DrawShakyLine(SnipTool.SmoothPen);
        _view.SelectTool(SnipTool.Select);
        Click(100, 100);

        // Act
        Drag(100, 100, 120, 130);
        _view.Nudge(5, 0);

        // Assert
        Assert.True(TheStroke.IsSmooth);
    }

    /// <summary>Drags a crop and applies it.</summary>
    private void CropBy(int fromX, int fromY, int toX, int toY)
    {
        Drag(fromX, fromY, toX, toY);
        _view.ApplyCrop();
    }

    [Fact]
    public void Crop_DragShowsWhatItWouldKeep_AndItStaysToAdjustUntilApplied()
    {
        // Arrange
        _view.SelectTool(SnipTool.Crop);
        int copies = _view.CopyCount;

        // Act
        _view.Press(300, 200);
        _view.Move(150, 100);
        _view.Release(100, 50);

        // Assert: dragged either way, only shown so far
        Assert.Equal(Rectangle.FromLTRB(100, 50, 300, 200), _view.CropPreview);
        Assert.Null(_view.Crop);
        Assert.Equal(copies, _view.CopyCount);

        // Act
        _view.ApplyCrop();

        // Assert
        Assert.Equal(Rectangle.FromLTRB(100, 50, 300, 200), _view.Crop);
        Assert.Null(_view.CropPreview);
        Assert.Equal(copies + 1, _view.CopyCount);
    }

    [Fact]
    public void Crop_HandlesChangeItsSides_BeforeItsApplied()
    {
        // Arrange: (100, 100) to (300, 200)
        _view.SelectTool(SnipTool.Crop);
        Drag(100, 100, 300, 200);

        // Act: the bottom right corner, then the left side
        Drag(300, 200, 400, 250);
        Drag(100, 175, 50, 175);
        _view.ApplyCrop();

        // Assert
        Assert.Equal(Rectangle.FromLTRB(50, 100, 400, 250), _view.Crop);
    }

    [Fact]
    public void Crop_DraggedFromInside_MovesWhole_StoppingAtTheEdges()
    {
        // Arrange: 200 by 100 of the 800 by 600 snip
        _view.SelectTool(SnipTool.Crop);
        Drag(100, 100, 300, 200);

        // Act & Assert
        Drag(200, 150, 250, 180);
        Assert.Equal(new Rectangle(150, 130, 200, 100), _view.CropPreview);

        Drag(250, 180, 5000, 5000);
        Assert.Equal(new Rectangle(600, 500, 200, 100), _view.CropPreview);
    }

    [Fact]
    public void Crop_HandlesAreAsLargeOnTheScreenAtAnyZoom()
    {
        // Arrange: the bottom right handle is at (300, 200)
        _view.SelectTool(SnipTool.Crop);
        Drag(100, 100, 300, 200);

        // Act & Assert
        _view.Hover(305, 205);
        Assert.Equal(SnipCursor.SizeTopLeftBottomRight, _view.Cursor);

        _view.Hover(200, 150);
        Assert.Equal(SnipCursor.Move, _view.Cursor);

        _view.Hover(500, 500);
        Assert.Equal(SnipCursor.Cross, _view.Cursor);
    }

    [Fact]
    public void Crop_ClickOutside_DropsIt()
    {
        // Arrange
        _view.SelectTool(SnipTool.Crop);
        Drag(100, 100, 300, 200);

        // Act
        Click(500, 500);

        // Assert
        Assert.Null(_view.CropPreview);
    }

    [Fact]
    public void Crop_DragOutside_StartsAnotherOne()
    {
        // Arrange
        _view.SelectTool(SnipTool.Crop);
        Drag(100, 100, 300, 200);

        // Act
        Drag(400, 300, 600, 500);

        // Assert
        Assert.Equal(Rectangle.FromLTRB(400, 300, 600, 500), _view.CropPreview);
    }

    [Fact]
    public void Crop_Cancelled_ChangesNothing()
    {
        // Arrange
        _view.SelectTool(SnipTool.Crop);
        Drag(100, 100, 300, 200);

        // Act
        _view.CancelCrop();
        _view.ApplyCrop();

        // Assert
        Assert.Null(_view.CropPreview);
        Assert.Null(_view.Crop);
        Assert.False(_view.CanUndo);
    }

    [Fact]
    public void Crop_DoubleClickInside_AppliesIt()
    {
        // Arrange
        _view.SelectTool(SnipTool.Crop);
        Drag(100, 100, 300, 200);

        // Act
        _view.DoubleClick(200, 150);

        // Assert
        Assert.Equal(Rectangle.FromLTRB(100, 100, 300, 200), _view.Crop);
    }

    [Fact]
    public void Crop_ChangingTool_DropsIt()
    {
        // Arrange
        _view.SelectTool(SnipTool.Crop);
        Drag(100, 100, 300, 200);

        // Act
        _view.SelectTool(SnipTool.Pen);
        _view.ApplyCrop();

        // Assert
        Assert.Null(_view.CropPreview);
        Assert.Null(_view.Crop);
    }

    [Fact]
    public void Crop_Zoom_KeepsItToAdjust()
    {
        // Arrange
        _view.SelectTool(SnipTool.Crop);
        Drag(100, 100, 300, 200);

        // Act
        _view.Zoom(1);

        // Assert
        Assert.Equal(Rectangle.FromLTRB(100, 100, 300, 200), _view.CropPreview);
    }

    [Fact]
    public void Crop_PastTheSnip_StopsAtItsEdges()
    {
        // Arrange
        _view.SelectTool(SnipTool.Crop);

        // Act
        CropBy(-50, -50, 100, 5000);

        // Assert
        Assert.Equal(Rectangle.FromLTRB(0, 0, 100, 600), _view.Crop);
    }

    [Fact]
    public void Crop_AgainAfterACrop_StaysInsideWhatsLeft()
    {
        // Arrange
        _view.SelectTool(SnipTool.Crop);
        CropBy(100, 100, 400, 400);
        _view.SelectTool(SnipTool.Crop);

        // Act
        CropBy(0, 0, 200, 200);

        // Assert
        Assert.Equal(Rectangle.FromLTRB(100, 100, 200, 200), _view.Crop);
    }

    [Theory]
    [InlineData(50, 50, 50, 50)]
    [InlineData(50, 50, 51, 300)]
    [InlineData(0, 0, 800, 600)]
    [InlineData(-10, -10, 900, 700)]
    public void Crop_OfNothingOrOfEverything_ChangesNothing(int fromX, int fromY, int toX, int toY)
    {
        // Arrange
        _view.SelectTool(SnipTool.Crop);
        int shows = _view.CropShowCount;

        // Act
        CropBy(fromX, fromY, toX, toY);

        // Assert
        Assert.Null(_view.Crop);
        Assert.Equal(shows, _view.CropShowCount);
        Assert.False(_view.CanUndo);
        Assert.Equal(SnipTool.Crop, _view.Tool);
    }

    [Fact]
    public void Crop_CanBeUndoneAndRedone()
    {
        // Arrange
        _view.SelectTool(SnipTool.Crop);
        CropBy(100, 100, 400, 400);

        // Act
        _view.Undo();

        // Assert
        Assert.Null(_view.Crop);

        // Act
        _view.Redo();

        // Assert
        Assert.Equal(Rectangle.FromLTRB(100, 100, 400, 400), _view.Crop);
    }

    [Fact]
    public void Crop_LeavesTheAnnotationsWhereTheyAreOnTheSnip()
    {
        // Arrange
        DrawRectangleAndArrow();
        _view.SelectTool(SnipTool.Crop);

        // Act
        CropBy(50, 50, 500, 500);

        // Assert: in the pixels of the whole snip, so they stay on what they were drawn over
        Assert.Equal(new Rectangle(100, 100, 100, 80), TheRectangle.Bounds);
        Assert.Equal(2, _view.Annotations.Count);
    }

    [Fact]
    public void Crop_GoesBackToTheToolFromBefore()
    {
        // Arrange
        _view.SelectTool(SnipTool.Arrow);
        _view.SelectTool(SnipTool.Crop);

        // Act
        CropBy(100, 100, 400, 400);

        // Assert
        Assert.Equal(SnipTool.Arrow, _view.Tool);
    }

    [Fact]
    public void Crop_HasNoChoices_AndDropsWhatsSelected()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act
        _view.SelectTool(SnipTool.Crop);

        // Assert
        Assert.False(_view.DrawingChoicesVisible);
        Assert.Null(_view.Selection);
    }

    [Fact]
    public void Crop_EachSnipHasItsOwn()
    {
        // Arrange
        _view.SelectTool(SnipTool.Crop);
        CropBy(100, 100, 400, 400);

        // Act
        _view.AddSnip();

        // Assert
        Assert.Null(_view.Crop);

        // Act
        _view.StepSnip(-1);

        // Assert
        Assert.Equal(Rectangle.FromLTRB(100, 100, 400, 400), _view.Crop);
    }

    [Fact]
    public void Crop_CountsAsAChangeToSave()
    {
        // Arrange: saved, then cropped
        SaveThisSnip();
        _view.SelectTool(SnipTool.Crop);
        CropBy(100, 100, 400, 400);
        _view.AddSnip();

        // Act
        _view.Close();

        // Assert
        Assert.Equal(2, _view.AskedToDiscard);
    }

    [Fact]
    public void Crop_ZoomMidDrag_StopsTheDrag_KeepingWhatItHadReached()
    {
        // Arrange
        _view.SelectTool(SnipTool.Crop);
        _view.Press(100, 100);
        _view.Move(200, 200);

        // Act: the mouse is over something else once zoomed
        _view.Zoom(1);
        _view.Move(300, 300);
        _view.Release(300, 300);

        // Assert
        Assert.Equal(Rectangle.FromLTRB(100, 100, 200, 200), _view.CropPreview);
        Assert.Null(_view.Crop);
    }

    private List<StepAnnotationModel> Steps => [.. _presenter.Annotations.OfType<StepAnnotationModel>()];

    private void ClickSteps(params int[] xs)
    {
        _view.SelectTool(SnipTool.Step);

        foreach (int x in xs)
        {
            Click(x, 100);
        }
    }

    [Fact]
    public void Step_EachClickAddsTheNextNumber_WhereItWasClicked()
    {
        // Act
        ClickSteps(100, 200, 300);

        // Assert
        Assert.Equal([1, 2, 3], Steps.Select(step => step.Number));
        Assert.Equal([new Point(100, 100), new Point(200, 100), new Point(300, 100)], Steps.Select(step => step.Center));
        Assert.All(Steps, step => Assert.Equal((28, SnipEditorPresenter.Palette[0]), (step.Diameter, step.Color)));
    }

    [Fact]
    public void Step_UsesTheToolsSizeColorAndTransparency()
    {
        // Arrange
        _view.SelectTool(SnipTool.Step);

        // Assert
        Assert.Equal(SnipEditorPresenter.StepSizes, _view.Sizes);

        // Act
        _view.SelectSize(48);
        _view.SelectColor(Color.Blue);
        _view.SelectTransparency(50);
        Click(100, 100);

        // Assert
        StepAnnotationModel step = Assert.Single(Steps);
        Assert.Equal(48, step.Diameter);
        Assert.Equal(Color.FromArgb(128, Solid(Color.Blue)), step.Color);
    }

    [Fact]
    public void Step_AfterTheLastIsUndone_UsesItsNumberAgain()
    {
        // Arrange
        ClickSteps(100, 200, 300);

        // Act
        _view.Undo();
        Click(400, 100);

        // Assert
        Assert.Equal([1, 2, 3], Steps.Select(step => step.Number));
    }

    [Fact]
    public void Step_AfterOneInTheMiddleIsDeleted_GoesOnFromTheHighest()
    {
        // Arrange
        ClickSteps(100, 200, 300);
        _view.SelectTool(SnipTool.Select);
        Click(200, 100);
        _view.Delete();

        // Act
        ClickSteps(400);

        // Assert
        Assert.Equal([1, 3, 4], Steps.Select(step => step.Number));
    }

    [Fact]
    public void Step_EachSnipCountsFrom1()
    {
        // Arrange
        ClickSteps(100, 200);

        // Act
        _view.AddSnip();
        Click(100, 100);

        // Assert
        Assert.Equal(1, Assert.Single(Steps).Number);

        // Act: back to the first, which goes on where it was
        _view.StepSnip(-1);
        Click(300, 100);

        // Assert
        Assert.Equal([1, 2, 3], Steps.Select(step => step.Number));
    }

    [Fact]
    public void Step_CountsOnlySteps()
    {
        // Arrange: other things drawn first
        DrawRectangleAndArrow();

        // Act
        ClickSteps(500);

        // Assert
        Assert.Equal(1, Assert.Single(Steps).Number);
    }

    [Fact]
    public void SelectedStep_CanBeMovedRecoloredAndResized_KeepingItsNumber()
    {
        // Arrange
        ClickSteps(100, 200);
        _view.SelectTool(SnipTool.Select);
        Click(200, 100);

        // Assert
        Assert.Equal(SnipEditorPresenter.StepSizes, _view.Sizes);
        Assert.Equal(28, _view.Size);
        Assert.Equal(new Rectangle(186, 86, 28, 28), _view.Selection!.Box);

        // Act
        Drag(200, 100, 250, 150);
        _view.SelectColor(Color.Blue);
        _view.SelectSize(64);

        // Assert
        StepAnnotationModel step = Steps[1];
        Assert.Equal((2, new Point(250, 150), 64), (step.Number, step.Center, step.Diameter));
        Assert.Equal(Solid(Color.Blue), step.Color);
    }

    [Fact]
    public void SelectedStep_ScaledByAHandle_StaysRound_FromTheOtherCorner()
    {
        // Arrange: its frame is (86, 86) to (114, 114)
        ClickSteps(100);
        _view.SelectTool(SnipTool.Select);
        Click(100, 100);

        // Act: the bottom right handle, to twice as large
        Drag(114, 114, 142, 142);

        // Assert
        StepAnnotationModel step = Assert.Single(Steps);
        Assert.Equal(56, step.Diameter);
        Assert.Equal(new Rectangle(86, 86, 56, 56), _view.Selection!.Box);
    }

    [Fact]
    public void SelectedStep_CanBeReset()
    {
        // Arrange
        ClickSteps(100);
        _view.SelectTool(SnipTool.Select);
        Click(100, 100);
        Drag(100, 100, 300, 300);

        // Act
        _view.Reset();

        // Assert
        Assert.Equal(new Point(100, 100), Assert.Single(Steps).Center);
    }

    [Fact]
    public void StepTool_HasNoArrowHeadsEdgeOrCorners()
    {
        // Act
        _view.SelectTool(SnipTool.Step);

        // Assert
        Assert.True(_view.DrawingChoicesVisible);
        Assert.Null(_view.ArrowHeads);
        Assert.Null(_view.TextOutline);
        Assert.Null(_view.CornerRadius);
    }

    [Fact]
    public void Zoom_StartsAt100()
    {
        Assert.Equal(100, _view.ZoomPercent);
    }

    [Fact]
    public void Zoom_GoesThroughTheLevels_AndStopsAtTheEnds()
    {
        // Act & Assert
        List<int> larger = [];

        for (int i = 0; i < 10; i++)
        {
            _view.Zoom(1);
            larger.Add(_view.ZoomPercent!.Value);
        }

        Assert.Equal([125, 150, 200, 300, 400, 600, 800, 800, 800, 800], larger);

        _view.Zoom(0);
        Assert.Equal(100, _view.ZoomPercent);

        List<int> smaller = [];

        for (int i = 0; i < 7; i++)
        {
            _view.Zoom(-1);
            smaller.Add(_view.ZoomPercent!.Value);
        }

        Assert.Equal([75, 67, 50, 33, 25, 25, 25], smaller);
    }

    [Fact]
    public void Zoom_DropsADragInProgress()
    {
        // Arrange
        _view.Press(10, 10);
        _view.Move(50, 50);

        // Act
        _view.Zoom(1);
        _view.Release(60, 60);

        // Assert
        Assert.Empty(_presenter.Annotations);
        Assert.Empty(_view.Annotations);
    }

    [Fact]
    public void Zoom_KeepsWhatsSelected()
    {
        // Arrange
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act
        _view.Zoom(1);

        // Assert
        Assert.NotNull(_view.Selection);
    }

    [Fact]
    public void NewSnip_IsShownAtItsOwnSize()
    {
        // Arrange
        _view.Zoom(1);
        _view.Zoom(1);

        // Act
        _view.AddSnip();

        // Assert
        Assert.Equal(100, _view.ZoomPercent);
    }

    [Fact]
    public void GoingToAnotherSnip_KeepsTheZoom()
    {
        // Arrange
        _view.AddSnip();
        _view.Zoom(1);

        // Act
        _view.StepSnip(-1);

        // Assert
        Assert.Equal(125, _view.ZoomPercent);
    }

    [Theory]
    [InlineData(100, 106, true)]
    [InlineData(100, 107, false)]
    [InlineData(400, 103, true)]
    [InlineData(400, 104, false)]
    [InlineData(25, 115, true)]
    public void Clicking_NearALine_IsAsNearOnTheScreenAtAnyZoom(int percent, int y, bool selects)
    {
        // Arrange: a level line 4 wide at y 100, so its edge is 2 from the middle; near enough is 4 screen pixels
        // past it, which is 4 snip pixels at 100% and 1 at 400%
        _view.SelectTool(SnipTool.Pen);
        _view.Press(100, 100);
        _view.Release(300, 100);
        _view.SelectTool(SnipTool.Select);

        while (_view.ZoomPercent != percent)
        {
            _view.Zoom(percent > _view.ZoomPercent ? 1 : -1);
        }

        // Act
        Click(200, y);

        // Assert
        Assert.Equal(selects, _view.Selection is not null);
    }

    [Fact]
    public void Handles_AreAsLargeOnTheScreenAtAnyZoom()
    {
        // Arrange: the rectangle's bottom right handle is at (200, 180)
        DrawRectangleAndArrow();
        Click(100, 140);

        // Act & Assert: 5 snip pixels away is near at 100%, too far at 400%
        _view.Hover(205, 185);
        Assert.Equal(SnipCursor.SizeTopLeftBottomRight, _view.Cursor);

        _view.Zoom(1);
        _view.Zoom(1);
        _view.Zoom(1);
        _view.Zoom(1);
        _view.Zoom(1);
        Assert.Equal(400, _view.ZoomPercent);

        _view.Hover(205, 185);
        Assert.NotEqual(SnipCursor.SizeTopLeftBottomRight, _view.Cursor);
    }

    [Fact]
    public void HideWhileSnipping_IsOnAtFirst()
    {
        Assert.True(_view.HidesWhileSnipping);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HideWhileSnipping_IsShownAndSaved(bool isHidden)
    {
        // Act
        _view.SelectHideWhileSnipping(isHidden);

        // Assert
        Assert.Equal(isHidden, _view.HidesWhileSnipping);
        Assert.Equal(isHidden, _store.Saved[^1].HideWhileSnipping);
    }

    [Fact]
    public void HideWhileSnipping_TurnedOffBefore_StaysOff()
    {
        // Arrange
        var view = new FakeSnipEditorView();
        var settings = new SnipEditorSettingsModel(
            SnipTool.Pen,
            new Dictionary<SnipTool, SnipToolSettingsModel>(),
            [],
            HideWhileSnipping: false);

        // Act
        new SnipEditorPresenter(view, _time, new FakeSettingsStore(settings)).Start();

        // Assert
        Assert.False(view.HidesWhileSnipping);
    }

    [Fact]
    public void HideWhileSnipping_IsKeptWhenOtherSettingsAreSaved()
    {
        // Arrange
        _view.SelectHideWhileSnipping(false);

        // Act
        _view.SelectTool(SnipTool.Arrow);

        // Assert
        Assert.False(_store.Saved[^1].HideWhileSnipping);
    }

    private void SaveThisSnip()
    {
        _view.SavePath = @"C:\snips\x.png";
        _view.Save();
    }

    [Fact]
    public void Close_WithOneSnip_ClosesWithoutAsking()
    {
        // Arrange
        Draw(10);

        // Act & Assert
        Assert.False(_view.Close());
        Assert.Null(_view.AskedToDiscard);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Close_WithSeveralUnsavedSnips_AsksHowMany_AndDoesWhatsAnswered(bool discard, bool staysOpen)
    {
        // Arrange
        _view.AddSnip();
        _view.AddSnip();
        _view.DiscardAnswer = discard;

        // Act & Assert
        Assert.Equal(staysOpen, _view.Close());
        Assert.Equal(3, _view.AskedToDiscard);
    }

    [Fact]
    public void Close_CountsOnlyTheSnipsThatWouldBeLost()
    {
        // Arrange: the first and third are saved
        SaveThisSnip();
        _view.AddSnip();
        _view.AddSnip();
        SaveThisSnip();
        _view.AddSnip();

        // Act
        _view.Close();

        // Assert
        Assert.Equal(2, _view.AskedToDiscard);
    }

    [Fact]
    public void Close_WithAllButOneSaved_DoesntAsk()
    {
        // Arrange
        SaveThisSnip();
        _view.AddSnip();

        // Act & Assert
        Assert.False(_view.Close());
        Assert.Null(_view.AskedToDiscard);
    }

    [Fact]
    public void ChangedAfterBeingSaved_CountsAsUnsavedAgain()
    {
        // Arrange
        SaveThisSnip();
        _view.AddSnip();
        _view.StepSnip(-1);
        Draw(10);

        // Act
        _view.Close();

        // Assert
        Assert.Equal(2, _view.AskedToDiscard);
    }

    [Fact]
    public void UndoneBackToWhatWasSaved_CountsAsSaved()
    {
        // Arrange
        Draw(10);
        SaveThisSnip();
        Draw(20);
        _view.Undo();
        _view.AddSnip();

        // Act & Assert
        Assert.False(_view.Close());
        Assert.Null(_view.AskedToDiscard);
    }

    [Fact]
    public void SaveCancelled_LeavesItUnsaved()
    {
        // Arrange: the dialog is cancelled
        _view.Save();
        _view.AddSnip();

        // Act
        _view.Close();

        // Assert
        Assert.Equal(2, _view.AskedToDiscard);
    }

    [Fact]
    public void Copying_DoesntCountAsSaving()
    {
        // Arrange
        _view.Copy();
        _view.AddSnip();
        _view.Copy();

        // Act
        _view.Close();

        // Assert
        Assert.Equal(2, _view.AskedToDiscard);
    }

    [Fact]
    public void Start_ShowsTheFirstSnip()
    {
        Assert.Equal((0, 1), (_view.SnipIndex, _view.SnipCount));
    }

    [Fact]
    public void NewSnip_IsShownWithNothingOnIt_AndCopied()
    {
        // Arrange
        Draw(10);
        int copies = _view.CopyCount;

        // Act
        _view.AddSnip();

        // Assert
        Assert.Equal((1, 2), (_view.SnipIndex, _view.SnipCount));
        Assert.Empty(_view.Annotations);
        Assert.Empty(_presenter.Annotations);
        Assert.False(_view.CanUndo);
        Assert.Equal(copies + 1, _view.CopyCount);
    }

    [Fact]
    public void GoingBack_ShowsThatSnipsAnnotations_AndCopiesIt()
    {
        // Arrange
        Draw(10);
        AnnotationModel first = _presenter.Annotations[0];
        _view.AddSnip();
        Draw(20);
        AnnotationModel second = _presenter.Annotations[0];
        int copies = _view.CopyCount;

        // Act
        _view.StepSnip(-1);

        // Assert
        Assert.Equal((0, 2), (_view.SnipIndex, _view.SnipCount));
        Assert.Equal([first], _view.Annotations);
        Assert.Equal(copies + 1, _view.CopyCount);

        // Act
        _view.StepSnip(1);

        // Assert
        Assert.Equal([second], _view.Annotations);
    }

    [Fact]
    public void EachSnip_KeepsItsOwnUndoAndRedo()
    {
        // Arrange: the first has something to redo, the second something to undo
        Draw(10);
        _view.Undo();
        _view.AddSnip();
        Draw(20);

        // Assert
        Assert.Equal((true, false), (_view.CanUndo, _view.CanRedo));

        // Act
        _view.StepSnip(-1);

        // Assert
        Assert.Equal((false, true), (_view.CanUndo, _view.CanRedo));

        // Act
        _view.Redo();

        // Assert
        Assert.Single(_presenter.Annotations);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void Step_PastTheEnds_DoesNothing(int step)
    {
        // Arrange
        _view.AddSnip();
        _view.StepSnip(-1);
        int copies = _view.CopyCount;

        // Act
        _view.StepSnip(step);

        // Assert
        Assert.Equal(0, _view.SnipIndex);
        Assert.Equal(copies, _view.CopyCount);
    }

    [Fact]
    public void NewSnip_WhileDragging_DropsTheDrag()
    {
        // Arrange
        _view.Press(10, 10);
        _view.Move(50, 50);

        // Act
        _view.AddSnip();
        _view.Move(60, 60);
        _view.Release(70, 70);

        // Assert
        Assert.Empty(_view.Annotations);
        Assert.Empty(_presenter.Annotations);
    }

    [Fact]
    public void ToolAndSettings_StayWhenASnipArrives()
    {
        // Arrange
        _view.SelectTool(SnipTool.Arrow);
        _view.SelectColor(Color.Blue);

        // Act
        _view.AddSnip();

        // Assert
        Assert.Equal(SnipTool.Arrow, _view.Tool);
        Assert.Equal(Solid(Color.Blue), _view.Color);
    }

    [Fact]
    public void Save_WhenASnipArrivesDuringTheDialog_SavesTheOneItAskedAbout()
    {
        // Arrange
        _view.SavePath = @"C:\x.png";
        _view.DuringAskSavePath = _view.AddSnip;

        // Act
        _view.Save();

        // Assert
        Assert.Equal(0, _view.SnipIndexWhenSaved);
        Assert.Equal(2, _view.SnipCount);
    }

    [Fact]
    public void Blur_DragHidesThatArea_WhicheverWayItsDragged()
    {
        // Arrange
        _view.SelectTool(SnipTool.Blur);

        // Act
        _view.Press(110, 70);
        _view.Move(50, 50);
        _view.Release(10, 20);

        // Assert
        Assert.Equal(new BlurAnnotationModel(new Rectangle(10, 20, 100, 50)), Assert.Single(_presenter.Annotations));
    }

    [Fact]
    public void Blur_ShowsWhileDragging()
    {
        // Arrange
        _view.SelectTool(SnipTool.Blur);

        // Act
        _view.Press(10, 10);
        _view.Move(60, 40);

        // Assert
        Assert.Equal(new BlurAnnotationModel(new Rectangle(10, 10, 50, 30)), Assert.Single(_view.Annotations));
        Assert.Empty(_presenter.Annotations);
    }

    [Fact]
    public void Blur_Click_LeavesNothing()
    {
        // Arrange
        _view.SelectTool(SnipTool.Blur);

        // Act
        _view.Press(10, 10);
        _view.Release(10, 10);

        // Assert
        Assert.Empty(_presenter.Annotations);
    }

    [Fact]
    public void Blur_IsCopiedAtOnce_SoTheClipboardDoesntKeepWhatItHides()
    {
        // Arrange
        int copies = _view.CopyCount;
        _view.SelectTool(SnipTool.Blur);

        // Act
        _view.Press(10, 10);
        _view.Release(60, 40);

        // Assert
        Assert.Equal(copies + 1, _view.CopyCount);
    }

    [Fact]
    public void BlurTool_HidesTheChoicesItDoesntHave_AndOtherToolsShowThemAgain()
    {
        // Act
        _view.SelectTool(SnipTool.Blur);

        // Assert
        Assert.False(_view.DrawingChoicesVisible);

        // Act
        _view.SelectTool(SnipTool.Pen);

        // Assert
        Assert.True(_view.DrawingChoicesVisible);
    }

    [Fact]
    public void BlurTool_IsRememberedLikeTheOthers()
    {
        // Act
        _view.SelectTool(SnipTool.Blur);

        // Assert
        Assert.Equal(SnipTool.Blur, _store.Saved[^1].Tool);
    }

    [Fact]
    public void TextOutline_IsOffAtFirst_AndOnlyShownForTheTextTool()
    {
        // Assert: the pen is the first tool
        Assert.Null(_view.TextOutline);

        // Act
        _view.SelectTool(SnipTool.Text);

        // Assert
        Assert.Equal(new SnipTextOutlineModel(0, Solid(Color.White)), _view.TextOutline);
        Assert.Equal(SnipEditorPresenter.TextOutlineWidths, _view.TextOutlineWidths);

        // Act
        _view.SelectTool(SnipTool.Arrow);

        // Assert
        Assert.Null(_view.TextOutline);
    }

    [Fact]
    public void Text_WithoutAnOutlineWidth_HasNoOutline()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.SelectTextOutlineColor(Color.Blue);

        // Act
        _view.CommitText(1, 2, "hi");

        // Assert
        Assert.Null(Assert.IsType<TextAnnotationModel>(_presenter.Annotations[^1]).Outline);
    }

    [Fact]
    public void Text_GetsTheChosenOutline_AndKeepsItWhenTheChoiceChanges()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.SelectTextOutlineWidth(3);
        _view.SelectTextOutlineColor(Color.Blue);

        // Act
        _view.CommitText(1, 2, "first");
        _view.SelectTextOutlineWidth(0);
        _view.CommitText(1, 2, "second");

        // Assert
        Assert.Equal(new SnipTextOutlineModel(3, Solid(Color.Blue)), Assert.IsType<TextAnnotationModel>(_presenter.Annotations[0]).Outline);
        Assert.Null(Assert.IsType<TextAnnotationModel>(_presenter.Annotations[1]).Outline);
    }

    [Fact]
    public void TextOutlineChoices_AreShownAndSaved()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);

        // Act
        _view.SelectTextOutlineWidth(4);
        _view.SelectTextOutlineColor(Color.Blue);

        // Assert
        var expected = new SnipTextOutlineModel(4, Solid(Color.Blue));
        Assert.Equal(expected, _view.TextOutline);
        Assert.Equal(expected, _store.Saved[^1].TextOutline);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void TextOutlineWidth_ThatIsntOffered_IsIgnored(int width)
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);

        // Act
        _view.SelectTextOutlineWidth(width);

        // Assert
        Assert.Equal(0, _view.TextOutline!.Width);
    }

    [Fact]
    public void MoreTextOutlineColors_StartsFromTheOutlineColor_AndKeepsTheCustomColor()
    {
        // Arrange
        Color picked = Color.FromArgb(255, 18, 52, 86);
        _view.SelectTool(SnipTool.Text);
        _view.SelectTextOutlineColor(Color.Blue);
        _view.ColorToPick = picked;

        // Act
        _view.RequestMoreTextOutlineColors();

        // Assert
        Assert.Equal(Solid(Color.Blue), _view.AskedFrom);
        Assert.Equal(picked, _view.TextOutline!.Color);
        Assert.Equal([picked], _view.CustomColors);
    }

    [Fact]
    public void MoreTextOutlineColors_Cancelled_KeepsTheColor()
    {
        // Arrange
        _view.SelectTool(SnipTool.Text);
        _view.SelectTextOutlineColor(Color.Blue);

        // Act
        _view.RequestMoreTextOutlineColors();

        // Assert
        Assert.Equal(Solid(Color.Blue), _view.TextOutline!.Color);
    }

    [Theory]
    [InlineData(4, 4)]
    [InlineData(7, 0)]
    public void SavedTextOutline_IsUsed_OrItsDefaultWhenNotOffered(int saved, int expected)
    {
        // Arrange
        var view = new FakeSnipEditorView();
        var settings = new SnipEditorSettingsModel(
            SnipTool.Text,
            new Dictionary<SnipTool, SnipToolSettingsModel>(),
            [],
            TextOutline: new SnipTextOutlineModel(saved, Color.Empty));

        // Act
        new SnipEditorPresenter(view, _time, new FakeSettingsStore(settings)).Start();

        // Assert: an unknown color keeps the default too
        Assert.Equal(new SnipTextOutlineModel(expected, Solid(Color.White)), view.TextOutline);
    }

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
        public event EventHandler<SnipPointerModel>? PointerPressed;

        public event EventHandler? SelectAllRequested;

        public Rectangle? SelectionBand { get; private set; }

        public void SelectAll() => SelectAllRequested?.Invoke(this, EventArgs.Empty);

        public void ShowSelectionBand(Rectangle? band) => SelectionBand = band;

        public void ShiftPress(int x, int y) => PointerPressed?.Invoke(this, new SnipPointerModel(new Point(x, y), Shift: true));

        public event EventHandler<Point>? PointerMoved;

        public event EventHandler<Point>? PointerReleased;

        public event EventHandler<SnipTool>? ToolSelected;

        public event EventHandler<Color>? ColorSelected;

        public event EventHandler? MoreColorsRequested;

        public event EventHandler<int>? SizeSelected;

        public event EventHandler<int>? TransparencySelected;

        public event EventHandler<SnipArrowHeads>? ArrowHeadsSelected;

        public event EventHandler<int>? TextOutlineWidthSelected;

        public event EventHandler<Color>? TextOutlineColorSelected;

        public event EventHandler? MoreTextOutlineColorsRequested;

        public event EventHandler<TextInputModel>? TextCommitted;

        public event EventHandler<CancelEventArgs>? CloseRequested;

        public event EventHandler<bool>? HideWhileSnippingSelected;

        public event EventHandler<int>? ZoomRequested;

        /// <summary>How large every snip is, before any crop.</summary>
        public Size SnipSize { get; set; } = new(800, 600);

        public Rectangle? Crop { get; private set; }

        public int CropShowCount { get; private set; }

        public Rectangle? CropPreview { get; private set; }

        public Size GetSnipSize(int index) => SnipSize;

        public void ShowCrop(Rectangle? crop)
        {
            Crop = crop;
            CropShowCount++;
        }

        public void ShowCropPreview(Rectangle? area) => CropPreview = area;

        public event EventHandler? CropApplyRequested;

        public event EventHandler? CropCancelRequested;

        public void ApplyCrop() => CropApplyRequested?.Invoke(this, EventArgs.Empty);

        public void CancelCrop() => CropCancelRequested?.Invoke(this, EventArgs.Empty);

        public int? ZoomPercent { get; private set; }

        public void Zoom(int step) => ZoomRequested?.Invoke(this, step);

        public void ShowZoom(int percent) => ZoomPercent = percent;

        public bool? HidesWhileSnipping { get; private set; }

        public void SelectHideWhileSnipping(bool isHidden) => HideWhileSnippingSelected?.Invoke(this, isHidden);

        public void ShowHideWhileSnipping(bool isHidden) => HidesWhileSnipping = isHidden;

        /// <summary>What AskDiscardSnips answers.</summary>
        public bool DiscardAnswer { get; set; }

        /// <summary>How many snips AskDiscardSnips was told about; null when it wasn't asked.</summary>
        public int? AskedToDiscard { get; private set; }

        public bool AskDiscardSnips(int count)
        {
            AskedToDiscard = count;

            return DiscardAnswer;
        }

        /// <returns>True when the window stays open.</returns>
        public bool Close()
        {
            var closing = new CancelEventArgs();

            CloseRequested?.Invoke(this, closing);

            return closing.Cancel;
        }

        public event EventHandler? SnipAdded;

        public event EventHandler<int>? SnipStepRequested;

        public int SnipIndex { get; private set; }

        public int SnipCount { get; private set; }

        public int? SnipIndexWhenSaved { get; private set; }

        /// <summary>Runs while the save dialog is open, as a global hotkey can.</summary>
        public Action? DuringAskSavePath { get; set; }

        public void AddSnip() => SnipAdded?.Invoke(this, EventArgs.Empty);

        public void StepSnip(int step) => SnipStepRequested?.Invoke(this, step);

        public void ShowSnip(int index, int count) => (SnipIndex, SnipCount) = (index, count);

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

        public IReadOnlyList<int> TextOutlineWidths { get; private set; } = [];

        public SnipTextOutlineModel? TextOutline { get; private set; }

        public IReadOnlyList<AnnotationModel> Annotations { get; private set; } = [];

        public bool CanUndo { get; private set; }

        public bool CanRedo { get; private set; }

        public (Point Location, Color Color, int FontSize)? TextInput { get; private set; }

        public int CopyCount { get; private set; }

        /// <summary>What AskSavePath returns; null as if the user cancelled.</summary>
        public string? SavePath { get; set; }

        public string? SuggestedFileName { get; private set; }

        public string? SavedPath { get; private set; }

        public void Press(int x, int y) => PointerPressed?.Invoke(this, new SnipPointerModel(new Point(x, y)));

        public void Move(int x, int y) => PointerMoved?.Invoke(this, new Point(x, y));

        public void Release(int x, int y) => PointerReleased?.Invoke(this, new Point(x, y));

        public void SelectTool(SnipTool tool) => ToolSelected?.Invoke(this, tool);

        public void SelectColor(Color color) => ColorSelected?.Invoke(this, color);

        public void RequestMoreColors() => MoreColorsRequested?.Invoke(this, EventArgs.Empty);

        public void SelectSize(int size) => SizeSelected?.Invoke(this, size);

        public void SelectTransparency(int transparency) => TransparencySelected?.Invoke(this, transparency);

        public void SelectArrowHeads(SnipArrowHeads heads) => ArrowHeadsSelected?.Invoke(this, heads);

        public void SelectTextOutlineWidth(int width) => TextOutlineWidthSelected?.Invoke(this, width);

        public void SelectTextOutlineColor(Color color) => TextOutlineColorSelected?.Invoke(this, color);

        public void RequestMoreTextOutlineColors() => MoreTextOutlineColorsRequested?.Invoke(this, EventArgs.Empty);

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

        public event EventHandler<int>? CornerRadiusSelected;

        public IReadOnlyList<int> CornerRadii { get; private set; } = [];

        public int? CornerRadius { get; private set; }

        public void SelectCornerRadius(int radius) => CornerRadiusSelected?.Invoke(this, radius);

        public void ShowCornerRadius(IReadOnlyList<int> radii, int? selected) => (CornerRadii, CornerRadius) = (radii, selected);

        public bool DrawingChoicesVisible { get; private set; }

        public void ShowDrawingChoices(bool isVisible) => DrawingChoicesVisible = isVisible;

        public void ShowTextOutline(IReadOnlyList<int> widths, SnipTextOutlineModel? outline) =>
            (TextOutlineWidths, TextOutline) = (widths, outline);

        public void ShowAnnotations(IReadOnlyList<AnnotationModel> annotations) => Annotations = annotations;

        public void ShowUndoRedo(bool canUndo, bool canRedo) => (CanUndo, CanRedo) = (canUndo, canRedo);

        public void BeginTextInput(Point location, Color color, int fontSize, string text)
        {
            TextInput = (location, color, fontSize);
            TextInputText = text;
        }

        public string? TextInputText { get; private set; }

        public event EventHandler<Point>? PointerDoubleClicked;

        public event EventHandler<Point>? PointerHovered;

        public event EventHandler<Point>? PointerRightClicked;

        public event EventHandler? ResetRequested;

        public (Point Location, bool CanReset)? ContextMenu { get; private set; }

        public SnipContextMenuModel? ContextMenuModel { get; private set; }

        public event EventHandler? DuplicateRequested;

        public event EventHandler? BringToFrontRequested;

        public event EventHandler? SendToBackRequested;

        public void RightClick(int x, int y) => PointerRightClicked?.Invoke(this, new Point(x, y));

        public void Reset() => ResetRequested?.Invoke(this, EventArgs.Empty);

        public void Duplicate() => DuplicateRequested?.Invoke(this, EventArgs.Empty);

        public void BringToFront() => BringToFrontRequested?.Invoke(this, EventArgs.Empty);

        public void SendToBack() => SendToBackRequested?.Invoke(this, EventArgs.Empty);

        public void ShowContextMenu(Point location, SnipContextMenuModel menu)
        {
            ContextMenu = (location, menu.CanReset);
            ContextMenuModel = menu;
        }

        public event EventHandler? DeleteRequested;

        public event EventHandler<Size>? NudgeRequested;

        public event EventHandler? DeselectRequested;

        public event EventHandler<TextInputModel>? TextEscaped;

        public SnipSelectionModel? Selection { get; private set; }

        public SnipCursor Cursor { get; private set; } = SnipCursor.Cross;

        public void DoubleClick(int x, int y)
        {
            Press(x, y);
            PointerDoubleClicked?.Invoke(this, new Point(x, y));
            Release(x, y);
        }

        public void Hover(int x, int y) => PointerHovered?.Invoke(this, new Point(x, y));

        public void Delete() => DeleteRequested?.Invoke(this, EventArgs.Empty);

        public void Nudge(int x, int y) => NudgeRequested?.Invoke(this, new Size(x, y));

        public void Deselect() => DeselectRequested?.Invoke(this, EventArgs.Empty);

        public void EscapeText(int x, int y, string text) =>
            TextEscaped?.Invoke(this, new TextInputModel(new Point(x, y), text));

        public void ShowSelection(SnipSelectionModel? selection) => Selection = selection;

        public void ShowCursor(SnipCursor cursor) => Cursor = cursor;

        /// <summary>Every character is half as wide as the font is tall.</summary>
        public Size MeasureText(string text, int fontSize)
        {
            string[] lines = text.Split('\n');

            return new Size(lines.Max(line => line.Length) * fontSize / 2, lines.Length * fontSize);
        }

        public void CopyImageToClipboard() => CopyCount++;

        public string? AskSavePath(string suggestedFileName)
        {
            SuggestedFileName = suggestedFileName;
            DuringAskSavePath?.Invoke();

            return SavePath;
        }

        public void SaveImage(string path)
        {
            SavedPath = path;
            SnipIndexWhenSaved = SnipIndex;
        }
    }
}
