using System.ComponentModel;
using System.Drawing;
using CincoVertice.MyTools.Ui.Zones.Interfaces;
using CincoVertice.MyTools.Ui.Zones.Models;
using CincoVertice.MyTools.Ui.Zones.Presenters;
using CincoVertice.MyTools.Ui.Zones.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Zones;

public class ZoneEditorPresenterTests
{
    private readonly FakeView _view = new();
    private readonly FakeStore _store = new();
    private readonly FakeZoneDesktop _desktop = new();
    private int _textOpened;

    private static ZoneModel Zone(double left, double top, double width, double height) =>
        new(left, top, width, height);

    private void Start(params ZoneModel[] zones)
    {
        _store.Layout = new ZoneLayoutModel(zones, 8);
        new ZoneEditorPresenter(_view, _store, _desktop, () => _textOpened++).Start();
    }

    private void StartWithTwoColumns() => Start(Zone(0, 0, 50, 100), Zone(50, 0, 50, 100));

    [Fact]
    public void Start_ShowsTheSavedZones_TheMonitors_AndTheGap()
    {
        // Act
        StartWithTwoColumns();

        // Assert
        Assert.Equal([Zone(0, 0, 50, 100), Zone(50, 0, 50, 100)], _view.Zones);
        Assert.Equal(["Default layout", "Monitor 1 (1000 x 600)", "Monitor 2 (1000 x 600)"], _view.Targets);
        Assert.Equal(0, _view.Target);
        Assert.Equal(new Size(1000, 600), _view.Shape);
        Assert.Equal((false, false), _view.Own);
        Assert.Equal(8, _view.Gap);
        Assert.Contains(8, _view.GapChoices);
        Assert.False(_view.HasUnsaved);
        Assert.Equal((false, false), _view.UndoRedo);
    }

    [Fact]
    public void Start_WithAGapThatIsntAChoice_AddsIt()
    {
        // Arrange
        _store.Layout = new ZoneLayoutModel([Zone(0, 0, 100, 100)], 7);

        // Act
        new ZoneEditorPresenter(_view, _store, _desktop, () => { }).Start();

        // Assert
        Assert.Contains(7, _view.GapChoices);
        Assert.Equal(7, _view.Gap);
    }

    [Fact]
    public void DraggingTheBorderBetweenColumns_MovesBoth_AndCanBeUndone()
    {
        // Arrange
        StartWithTwoColumns();

        // Act
        _view.Drag((50, 50), (70, 50));

        // Assert
        Assert.Equal([Zone(0, 0, 70, 100), Zone(70, 0, 30, 100)], _view.Zones);
        Assert.True(_view.HasUnsaved);

        // And
        _view.Undo();
        Assert.Equal([Zone(0, 0, 50, 100), Zone(50, 0, 50, 100)], _view.Zones);
        Assert.False(_view.HasUnsaved);
        Assert.Equal((false, true), _view.UndoRedo);
    }

    [Fact]
    public void DraggingABorderOfAGrid_MovesTheWholeLine()
    {
        // Arrange
        Start(Zone(0, 0, 50, 50), Zone(50, 0, 50, 50), Zone(0, 50, 50, 50), Zone(50, 50, 50, 50));

        // Act: the border between the top two
        _view.Drag((50, 20), (40, 20));

        // Assert: the bottom two follow
        Assert.Equal(
            [Zone(0, 0, 40, 50), Zone(40, 0, 60, 50), Zone(0, 50, 40, 50), Zone(40, 50, 60, 50)],
            _view.Zones);
    }

    [Fact]
    public void ABorder_StopsWhereAZoneWouldBeTooSmall()
    {
        // Arrange
        StartWithTwoColumns();

        // Act
        _view.Drag((50, 50), (99, 50));

        // Assert
        Assert.Equal([Zone(0, 0, 95, 100), Zone(95, 0, 5, 100)], _view.Zones);
    }

    [Fact]
    public void ABorder_SnapsToTheEdgeOfAnotherZone_NearIt()
    {
        // Arrange: the bottom zone ends at 60
        Start(Zone(0, 0, 50, 50), Zone(0, 50, 60, 50));

        // Act: the top zone's right border, near 60
        _view.Drag((50, 20), (59.5, 20));

        // Assert
        Assert.Equal(Zone(0, 0, 60, 50), _view.Zones[0]);
    }

    [Fact]
    public void ACornerHandle_ResizesOnlyItsZone_OverTheOneNextToIt_AndCanBeUndone()
    {
        // Arrange
        StartWithTwoColumns();
        _view.Click(20, 50);

        // Act: the bottom right corner
        _view.Drag((50, 100), (70, 80));

        // Assert
        Assert.Equal([Zone(0, 0, 70, 80), Zone(50, 0, 50, 100)], _view.Zones);

        // And
        _view.Undo();
        Assert.Equal([Zone(0, 0, 50, 100), Zone(50, 0, 50, 100)], _view.Zones);
    }

    [Fact]
    public void AHandleInTheMiddleOfASide_MovesOnlyThatSide()
    {
        // Arrange
        StartWithTwoColumns();
        _view.Click(70, 50);

        // Act: the top middle of the right zone
        _view.Drag((75, 0), (90, 30));

        // Assert
        Assert.Equal([Zone(0, 0, 50, 100), Zone(50, 30, 50, 70)], _view.Zones);
    }

    [Fact]
    public void AHandle_StopsWhereTheZoneWouldBeTooSmall()
    {
        // Arrange
        StartWithTwoColumns();
        _view.Click(20, 50);

        // Act: the right middle, nearly to the left edge
        _view.Drag((50, 50), (1, 50));

        // Assert
        Assert.Equal(Zone(0, 0, 5, 100), _view.Zones[0]);
    }

    [Fact]
    public void AHandle_SnapsToTheEdgeOfAnotherZone_NearIt()
    {
        // Arrange
        Start(Zone(0, 0, 40, 100), Zone(60, 0, 40, 100));
        _view.Click(20, 50);

        // Act
        _view.Drag((40, 50), (59.5, 50));

        // Assert
        Assert.Equal(Zone(0, 0, 60, 100), _view.Zones[0]);
    }

    [Fact]
    public void Handles_AreOnlyOnTheSelectedZone()
    {
        // Arrange
        StartWithTwoColumns();

        // Act and assert: a corner of a zone not selected is only its border
        _view.Hover(0.5, 0.5);
        Assert.NotEqual(ZoneEditorCursor.ResizeDiagonalDown, _view.Cursor);

        _view.Click(20, 50);
        _view.Hover(0.5, 0.5);
        Assert.Equal(ZoneEditorCursor.ResizeDiagonalDown, _view.Cursor);

        _view.Hover(49.5, 0.5);
        Assert.Equal(ZoneEditorCursor.ResizeDiagonalUp, _view.Cursor);

        _view.Hover(25, 99.5);
        Assert.Equal(ZoneEditorCursor.ResizeUpDown, _view.Cursor);
    }

    [Theory]
    [InlineData(20, 20, 0)]
    [InlineData(50, 50, 1)]
    public void AClickWhereZonesOverlap_SelectsTheOneWhoseCenterIsNearest(double x, double y, int expected)
    {
        // Arrange: a large zone and a smaller one over its middle
        Start(Zone(0, 0, 100, 100), Zone(30, 30, 40, 40));

        // Act
        _view.Click(x, y);

        // Assert
        Assert.Equal(expected, _view.Selected);
    }

    [Fact]
    public void AMonitorWithoutItsOwnLayout_ShowsTheDefaultOne_UntilItsFirstChange()
    {
        // Arrange
        StartWithTwoColumns();

        // Act
        _view.SelectTarget(2);

        // Assert
        Assert.Equal([Zone(0, 0, 50, 100), Zone(50, 0, 50, 100)], _view.Zones);
        Assert.Equal((true, false), _view.Own);
        Assert.False(_view.HasUnsaved);

        // And its first change gives it its own
        _view.Template(ZoneLayoutTemplate.Rows2);
        Assert.Equal((true, true), _view.Own);
        Assert.Equal("Monitor 2 (1000 x 600), own layout", _view.Targets[2]);
        Assert.True(_view.HasUnsaved);
    }

    [Fact]
    public void AClickOnAMonitorWithoutItsOwnLayout_DoesntGiveItOne()
    {
        // Arrange
        StartWithTwoColumns();
        _view.SelectTarget(1);

        // Act
        _view.Click(20, 50);

        // Assert
        Assert.Equal((true, false), _view.Own);
        Assert.False(_view.HasUnsaved);
    }

    [Fact]
    public void Save_KeepsTheDefaultLayout_AndTheMonitorsOwn()
    {
        // Arrange
        StartWithTwoColumns();
        _view.SelectTarget(2);
        _view.Template(ZoneLayoutTemplate.Rows2);
        _view.SelectGap(16);

        // Act
        _view.Save();

        // Assert
        ZoneLayoutsModel saved = Assert.Single(_store.Saved);
        Assert.Equal([Zone(0, 0, 50, 100), Zone(50, 0, 50, 100)], saved.DefaultLayout.Zones);
        Assert.Equal(8, saved.DefaultLayout.Gap);
        MonitorZoneLayoutModel own = Assert.Single(saved.Monitors);
        Assert.Equal(FakeZoneDesktop.Right, own.Bounds);
        Assert.Equal([Zone(0, 0, 100, 50), Zone(0, 50, 100, 50)], own.Layout.Zones);
        Assert.Equal(16, own.Layout.Gap);
    }

    [Fact]
    public void ChangesToOneLayout_AreKept_WhileAnotherIsEdited()
    {
        // Arrange
        StartWithTwoColumns();
        _view.SelectTarget(1);
        _view.Template(ZoneLayoutTemplate.Rows2);

        // Act
        _view.SelectTarget(0);
        _view.Template(ZoneLayoutTemplate.Columns3);
        _view.SelectTarget(1);

        // Assert
        Assert.Equal([Zone(0, 0, 100, 50), Zone(0, 50, 100, 50)], _view.Zones);

        // And the other monitor uses the default layout as changed
        _view.SelectTarget(2);
        Assert.Equal(3, _view.Zones.Count);
    }

    [Fact]
    public void Undo_GoesBackToTheLayoutTheChangeWasIn()
    {
        // Arrange
        StartWithTwoColumns();
        _view.SelectTarget(2);
        _view.Template(ZoneLayoutTemplate.Rows2);
        _view.SelectTarget(0);

        // Act
        _view.Undo();

        // Assert: on monitor 2 again, as it was before its change
        Assert.Equal(2, _view.Target);
        Assert.Equal((true, false), _view.Own);
        Assert.False(_view.HasUnsaved);
    }

    [Fact]
    public void UseDefault_DropsTheMonitorsOwnLayout()
    {
        // Arrange
        _store.Layouts = new ZoneLayoutsModel(
            new ZoneLayoutModel([Zone(0, 0, 50, 100), Zone(50, 0, 50, 100)], 8),
            [new MonitorZoneLayoutModel(FakeZoneDesktop.Left, new ZoneLayoutModel([Zone(0, 0, 100, 100)], 0))]);
        new ZoneEditorPresenter(_view, _store, _desktop, () => { }).Start();
        _view.SelectTarget(1);
        Assert.Equal((true, true), _view.Own);
        Assert.Equal([Zone(0, 0, 100, 100)], _view.Zones);

        // Act
        _view.UseDefault();
        _view.Save();

        // Assert
        Assert.Equal((true, false), _view.Own);
        Assert.Equal([Zone(0, 0, 50, 100), Zone(50, 0, 50, 100)], _view.Zones);
        Assert.Empty(Assert.Single(_store.Saved).Monitors);
    }

    [Fact]
    public void Save_KeepsTheLayoutOfAMonitorNotConnected()
    {
        // Arrange
        var away = new MonitorZoneLayoutModel(
            new Rectangle(-1920, 0, 1920, 1080),
            new ZoneLayoutModel([Zone(0, 0, 100, 100)], 0));
        _store.Layouts = new ZoneLayoutsModel(ZoneLayoutModel.Default, [away]);
        new ZoneEditorPresenter(_view, _store, _desktop, () => { }).Start();

        // Act
        _view.Template(ZoneLayoutTemplate.Rows2);
        _view.Save();

        // Assert
        Assert.Equal(away, Assert.Single(Assert.Single(_store.Saved).Monitors));
    }

    [Fact]
    public void DraggingInsideAZone_MovesIt_WithinTheMonitor()
    {
        // Arrange
        Start(Zone(10, 10, 30, 30));

        // Act
        _view.Drag((20, 20), (90, 35));

        // Assert: as far right as it goes, and down by 15
        Assert.Equal(Zone(70, 25, 30, 30), _view.Zones[0]);
        Assert.Equal(0, _view.Selected);
    }

    [Fact]
    public void DraggingOnNoZone_DrawsANewOne_Selected()
    {
        // Arrange
        Start(Zone(0, 0, 50, 50));

        // Act
        _view.Drag((60, 60), (80, 90));

        // Assert
        Assert.Equal(Zone(60, 60, 20, 30), _view.Zones[1]);
        Assert.Equal(1, _view.Selected);
        Assert.Equal((true, false), _view.UndoRedo);
    }

    [Fact]
    public void AClickOnNoZone_DrawsNothing_AndSelectsNothing()
    {
        // Arrange
        Start(Zone(0, 0, 50, 50));
        _view.Drag((20, 20), (20, 20));

        // Act
        _view.Drag((70, 70), (71, 71));

        // Assert
        Assert.Single(_view.Zones);
        Assert.Null(_view.Selected);
        Assert.Equal((false, false), _view.UndoRedo);
    }

    [Fact]
    public void SplitVertically_SplitsTheZoneWhereItWasRightClicked()
    {
        // Arrange
        StartWithTwoColumns();
        _view.RightClick(20, 50);

        // Act
        _view.SplitVertically();

        // Assert: the new one comes right after it
        Assert.Equal([Zone(0, 0, 20, 100), Zone(20, 0, 30, 100), Zone(50, 0, 50, 100)], _view.Zones);
        Assert.Equal((20d, 50d), _view.MenuPoint);
    }

    [Fact]
    public void SplitHorizontally_NearAnEdge_SplitsInTheMiddle()
    {
        // Arrange
        StartWithTwoColumns();
        _view.RightClick(70, 2);

        // Act
        _view.SplitHorizontally();

        // Assert
        Assert.Equal([Zone(0, 0, 50, 100), Zone(50, 0, 50, 50), Zone(50, 50, 50, 50)], _view.Zones);
    }

    [Fact]
    public void Split_OfAZoneTooNarrow_SaysSo()
    {
        // Arrange
        Start(Zone(0, 0, 8, 100), Zone(8, 0, 92, 100));
        _view.RightClick(4, 50);

        // Act
        _view.SplitVertically();

        // Assert
        Assert.Equal(2, _view.Zones.Count);
        Assert.Equal(["This zone is too narrow to split into two."], _view.Problems);
    }

    [Fact]
    public void Delete_RemovesTheSelectedZone()
    {
        // Arrange
        StartWithTwoColumns();
        _view.RightClick(70, 50);

        // Act
        _view.Delete();

        // Assert
        Assert.Equal([Zone(0, 0, 50, 100)], _view.Zones);
        Assert.Null(_view.Selected);
    }

    [Fact]
    public void ATemplate_ReplacesTheZones()
    {
        // Arrange
        StartWithTwoColumns();

        // Act
        _view.Template(ZoneLayoutTemplate.Grid2x2);

        // Assert
        Assert.Equal(
            [Zone(0, 0, 50, 50), Zone(50, 0, 50, 50), Zone(0, 50, 50, 50), Zone(50, 50, 50, 50)],
            _view.Zones);
        Assert.True(_view.HasUnsaved);
    }

    [Fact]
    public void AGap_IsShown_AndSaved()
    {
        // Arrange
        StartWithTwoColumns();

        // Act
        _view.SelectGap(16);
        _view.Save();

        // Assert
        Assert.Equal(16, _view.Gap);
        Assert.Equal(16, Assert.Single(_store.Saved).DefaultLayout.Gap);
        Assert.False(_view.HasUnsaved);
    }

    [Fact]
    public void Save_WhenItFails_ShowsWhy_AndStaysUnsaved()
    {
        // Arrange
        StartWithTwoColumns();
        _view.Template(ZoneLayoutTemplate.Rows2);
        _store.Problem = "The file is read-only.";

        // Act
        _view.Save();

        // Assert
        Assert.Equal(["The file is read-only."], _view.Problems);
        Assert.True(_view.HasUnsaved);
    }

    [Theory]
    [InlineData(null, true, 0)]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 1)]
    public void Closing_WithChanges_AsksWhatToDoWithThem(bool? answer, bool isCancelled, int saves)
    {
        // Arrange
        StartWithTwoColumns();
        _view.Template(ZoneLayoutTemplate.Rows2);
        _view.SaveAnswer = answer;

        // Act
        bool cancelled = _view.Close();

        // Assert
        Assert.Equal(isCancelled, cancelled);
        Assert.Equal(saves, _store.Saved.Count);
    }

    [Fact]
    public void Closing_WithoutChanges_AsksNothing()
    {
        // Arrange
        StartWithTwoColumns();

        // Act
        bool cancelled = _view.Close();

        // Assert
        Assert.False(cancelled);
        Assert.Equal(0, _view.Asked);
    }

    [Fact]
    public void EditAsText_OpensTheFile_AndClosesTheEditor()
    {
        // Arrange
        StartWithTwoColumns();

        // Act
        _view.EditAsText();

        // Assert
        Assert.Equal(1, _textOpened);
        Assert.True(_view.IsClosed);
    }

    [Fact]
    public void EditAsText_WithChangesTheUserKeeps_StaysOpen()
    {
        // Arrange
        StartWithTwoColumns();
        _view.Template(ZoneLayoutTemplate.Rows2);
        _view.SaveAnswer = null;

        // Act
        _view.EditAsText();

        // Assert
        Assert.Equal(0, _textOpened);
        Assert.False(_view.IsClosed);
    }

    [Theory]
    [InlineData(50, 50, ZoneEditorCursor.ResizeLeftRight)]
    [InlineData(20, 0.5, ZoneEditorCursor.ResizeUpDown)]
    [InlineData(20, 50, ZoneEditorCursor.Move)]
    public void Hovering_ShowsWhatADragWouldDo(double x, double y, ZoneEditorCursor cursor)
    {
        // Arrange
        StartWithTwoColumns();

        // Act
        _view.Hover(x, y);

        // Assert
        Assert.Equal(cursor, _view.Cursor);
    }

    [Fact]
    public void Hovering_OnNoZone_Draws()
    {
        // Arrange
        Start(Zone(0, 0, 50, 50));

        // Act
        _view.Hover(80, 80);

        // Assert
        Assert.Equal(ZoneEditorCursor.Draw, _view.Cursor);
    }

    private sealed class FakeStore : IZoneLayoutStore
    {
        public ZoneLayoutsModel Layouts { get; set; } = ZoneLayoutsModel.Default;

        public ZoneLayoutModel Layout
        {
            get => Layouts.DefaultLayout;
            set => Layouts = Layouts with { DefaultLayout = value };
        }

        public string? Problem { get; set; }

        public List<ZoneLayoutsModel> Saved { get; } = [];

        public ZoneLayoutsModel Load() => Layouts;

        public string? Save(ZoneLayoutsModel layout)
        {
            if (Problem is not null)
            {
                return Problem;
            }

            Saved.Add(layout);

            return null;
        }
    }

    private sealed class FakeView : IZoneEditorView
    {
        // A border is grabbed within 1 percent
        private const double Tolerance = 1;

        public event EventHandler<ZoneEditorPointerModel>? PointerPressed;

        public event EventHandler<ZoneEditorPointerModel>? PointerMoved;

        public event EventHandler<ZoneEditorPointerModel>? PointerReleased;

        public event EventHandler<ZoneEditorPointerModel>? PointerHovered;

        public event EventHandler<ZoneEditorPointerModel>? PointerRightClicked;

        public event EventHandler<ZoneLayoutTemplate>? TemplateSelected;

        public event EventHandler<int>? GapSelected;

        public event EventHandler<int>? TargetSelected;

        public event EventHandler? UseDefaultRequested;

        public event EventHandler? SplitVerticallyRequested;

        public event EventHandler? SplitHorizontallyRequested;

        public event EventHandler? DeleteRequested;

        public event EventHandler? UndoRequested;

        public event EventHandler? RedoRequested;

        public event EventHandler? SaveRequested;

        public event EventHandler? EditAsTextRequested;

        public event EventHandler<CancelEventArgs>? CloseRequested;

        public IReadOnlyList<ZoneModel> Zones { get; private set; } = [];

        public int? Selected { get; private set; }

        public int Gap { get; private set; }

        public IReadOnlyList<int> GapChoices { get; private set; } = [];

        public IReadOnlyList<string> Targets { get; private set; } = [];

        public int Target { get; private set; }

        public Size Shape { get; private set; }

        public (bool IsMonitor, bool IsOwn) Own { get; private set; }

        public ZoneEditorCursor Cursor { get; private set; }

        public (double X, double Y)? MenuPoint { get; private set; }

        public (bool CanUndo, bool CanRedo) UndoRedo { get; private set; }

        public bool HasUnsaved { get; private set; }

        public List<string> Problems { get; } = [];

        public bool? SaveAnswer { get; set; }

        public int Asked { get; private set; }

        public bool IsClosed { get; private set; }

        public void Drag((double X, double Y) from, (double X, double Y) to)
        {
            PointerPressed?.Invoke(this, Pointer(from.X, from.Y));
            PointerMoved?.Invoke(this, Pointer(to.X, to.Y));
            PointerReleased?.Invoke(this, Pointer(to.X, to.Y));
        }

        public void Hover(double x, double y) => PointerHovered?.Invoke(this, Pointer(x, y));

        /// <summary>A press and release without moving, which selects what's there.</summary>
        public void Click(double x, double y) => Drag((x, y), (x, y));

        public void RightClick(double x, double y) => PointerRightClicked?.Invoke(this, Pointer(x, y));

        public void Template(ZoneLayoutTemplate template) => TemplateSelected?.Invoke(this, template);

        public void SelectGap(int gap) => GapSelected?.Invoke(this, gap);

        public void SelectTarget(int index) => TargetSelected?.Invoke(this, index);

        public void UseDefault() => UseDefaultRequested?.Invoke(this, EventArgs.Empty);

        public void SplitVertically() => SplitVerticallyRequested?.Invoke(this, EventArgs.Empty);

        public void SplitHorizontally() => SplitHorizontallyRequested?.Invoke(this, EventArgs.Empty);

        public void Delete() => DeleteRequested?.Invoke(this, EventArgs.Empty);

        public void Undo() => UndoRequested?.Invoke(this, EventArgs.Empty);

        public void Redo() => RedoRequested?.Invoke(this, EventArgs.Empty);

        public void Save() => SaveRequested?.Invoke(this, EventArgs.Empty);

        public void EditAsText() => EditAsTextRequested?.Invoke(this, EventArgs.Empty);

        /// <returns>True when the close was cancelled.</returns>
        public bool Close()
        {
            var args = new CancelEventArgs();
            CloseRequested?.Invoke(this, args);

            return args.Cancel;
        }

        public void ShowTargets(IReadOnlyList<string> names, int selected, Size shape)
        {
            Targets = names;
            Target = selected;
            Shape = shape;
        }

        public void ShowOwnLayout(bool isMonitor, bool isOwn) => Own = (isMonitor, isOwn);

        public void ShowGaps(IReadOnlyList<int> gaps, int selected)
        {
            GapChoices = gaps;
            Gap = selected;
        }

        public void ShowZones(IReadOnlyList<ZoneModel> zones, int? selected, int gap)
        {
            Zones = zones;
            Selected = selected;
            Gap = gap;
        }

        public void ShowCursor(ZoneEditorCursor cursor) => Cursor = cursor;

        public void ShowContextMenu(double x, double y) => MenuPoint = (x, y);

        public void ShowUndoRedo(bool canUndo, bool canRedo) => UndoRedo = (canUndo, canRedo);

        public void ShowUnsaved(bool hasUnsaved) => HasUnsaved = hasUnsaved;

        public void ShowProblem(string message) => Problems.Add(message);

        public bool? AskSaveChanges()
        {
            Asked++;

            return SaveAnswer;
        }

        public void CloseView() => IsClosed = true;

        private static ZoneEditorPointerModel Pointer(double x, double y) => new(x, y, Tolerance, Tolerance);
    }
}
