using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Presenters;
using CincoVertice.MyTools.Ui.Snipping.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Snipping;

public class SnipPresenterTests
{
    private readonly FakeSnipView _view = new();
    private readonly MovableTimeProvider _time = new();
    private readonly SnipPresenter _presenter;

    public SnipPresenterTests()
    {
        _presenter = new SnipPresenter(_view, new Size(1000, 500), _time);
    }

    [Fact]
    public void FocusLost_RightAfterOpening_TakesItBack_AndStaysOpen()
    {
        // Act: as when a window opened from a hotkey takes the focus from the application in front
        _view.LoseFocus();

        // Assert
        Assert.Equal(1, _view.TakeFocusCount);
        Assert.Equal(0, _view.CloseCount);
        Assert.False(_presenter.Completion.IsCompleted);
    }

    [Fact]
    public void FocusLost_SeveralTimesRightAfterOpening_KeepsTakingItBack()
    {
        // Act
        _view.LoseFocus();
        _time.Advance(TimeSpan.FromMilliseconds(100));
        _view.LoseFocus();

        // Assert
        Assert.Equal(2, _view.TakeFocusCount);
        Assert.Equal(0, _view.CloseCount);
    }

    [Fact]
    public async Task FocusLost_Later_IsTheUserSwitchingWindows_AndCancels()
    {
        // Arrange
        _time.Advance(SnipPresenter.FocusGracePeriod);

        // Act
        _view.LoseFocus();

        // Assert
        Assert.Null(await _presenter.Completion);
        Assert.Equal(1, _view.CloseCount);
        Assert.Equal(0, _view.TakeFocusCount);
    }

    [Fact]
    public void FocusLost_RightAfterOpening_KeepsADragGoing()
    {
        // Arrange
        _view.Start(10, 10);

        // Act
        _view.LoseFocus();
        _view.Move(100, 100);

        // Assert
        Assert.Equal(new Rectangle(10, 10, 90, 90), _view.Selection);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5000)]
    public async Task FocusLost_WhileClosing_KeepsTheSnip_AndDoesntTakeItBack(int milliseconds)
    {
        // Arrange: a closing form loses the focus
        _time.Advance(TimeSpan.FromMilliseconds(milliseconds));
        _view.LoseFocusOnClose = true;
        _view.Start(10, 10);

        // Act
        _view.End(100, 100);

        // Assert
        Assert.Equal(new Rectangle(10, 10, 90, 90), await _presenter.Completion);
        Assert.Equal(0, _view.TakeFocusCount);
        Assert.Equal(1, _view.CloseCount);
    }

    [Fact]
    public async Task Cancel_RightAfterOpening_StillCancels()
    {
        // Act: Esc or a right click means it, however soon
        _view.Cancel();

        // Assert
        Assert.Null(await _presenter.Completion);
        Assert.Equal(1, _view.CloseCount);
    }

    [Fact]
    public void Drag_ShowsTheSelectionWithItsSize()
    {
        // Act
        _view.Start(10, 20);
        _view.Move(110, 70);

        // Assert
        Assert.Equal((new Rectangle(10, 20, 100, 50), "100 × 50"), (_view.Selection, _view.SizeText));
        Assert.False(_presenter.Completion.IsCompleted);
    }

    [Fact]
    public async Task Drag_UpAndLeft_GivesTheSameRectangle()
    {
        // Act
        _view.Start(110, 70);
        _view.Move(50, 50);
        _view.End(10, 20);

        // Assert
        Assert.Equal(new Rectangle(10, 20, 100, 50), await _presenter.Completion);
        Assert.Equal(1, _view.CloseCount);
    }

    [Fact]
    public async Task Drag_PastTheScreenshot_StopsAtItsEdges()
    {
        // Act
        _view.Start(-5, 450);
        _view.End(2000, 900);

        // Assert
        Assert.Equal(Rectangle.FromLTRB(0, 450, 1000, 500), await _presenter.Completion);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(11, 300)]
    [InlineData(300, 11)]
    public void Click_SelectsNothingAndKeepsTheViewOpen(int endX, int endY)
    {
        // Arrange
        _view.Start(10, 10);
        _view.Move(endX, endY);

        // Act
        _view.End(endX, endY);

        // Assert
        Assert.Null(_view.Selection);
        Assert.Equal(0, _view.CloseCount);
        Assert.False(_presenter.Completion.IsCompleted);
    }

    [Fact]
    public async Task Click_ThenDrag_Snips()
    {
        // Arrange
        _view.Start(10, 10);
        _view.End(10, 10);

        // Act
        _view.Start(20, 20);
        _view.End(30, 40);

        // Assert
        Assert.Equal(new Rectangle(20, 20, 10, 20), await _presenter.Completion);
    }

    [Fact]
    public void Move_WithoutStart_ShowsNothing()
    {
        // Act
        _view.Move(50, 50);
        _view.End(60, 60);

        // Assert
        Assert.Null(_view.Selection);
        Assert.False(_presenter.Completion.IsCompleted);
    }

    [Fact]
    public async Task Cancel_WhileDragging_ClosesWithoutASnip()
    {
        // Arrange
        _view.Start(10, 10);
        _view.Move(100, 100);

        // Act
        _view.Cancel();

        // Assert
        Assert.Null(await _presenter.Completion);
        Assert.Equal(1, _view.CloseCount);
    }

    [Fact]
    public async Task CancelWhileClosing_KeepsTheSnip()
    {
        // Arrange: losing the focus while closing asks to cancel
        _view.CancelOnClose = true;
        _view.Start(10, 10);

        // Act
        _view.End(100, 100);

        // Assert
        Assert.Equal(new Rectangle(10, 10, 90, 90), await _presenter.Completion);
        Assert.Equal(1, _view.CloseCount);
    }

    [Fact]
    public async Task ClosedByTheUser_GivesNoSnip()
    {
        // Arrange
        _view.Start(10, 10);
        _view.Move(100, 100);

        // Act: e.g. Alt+F4
        _view.RaiseClosed();

        // Assert
        Assert.Null(await _presenter.Completion);
        Assert.Equal(0, _view.CloseCount);
    }

    private (FakeSnipView View, SnipPresenter Presenter) Adjustable(Rectangle? lastArea = null)
    {
        var view = new FakeSnipView();

        return (view, new SnipPresenter(view, new Size(1000, 500), _time, isAdjustable: true, lastArea));
    }

    [Fact]
    public async Task Adjustable_StartsAtTheLastArea_WithHandles_AndEnterTakesIt()
    {
        // Arrange
        (FakeSnipView view, SnipPresenter presenter) = Adjustable(new Rectangle(100, 100, 200, 100));

        // Act
        Rectangle? shown = view.Selection;
        view.PressEnter();

        // Assert
        Assert.Equal(new Rectangle(100, 100, 200, 100), shown);
        Assert.Equal($"200 × 100   {SnipPresenter.ConfirmHint}", view.SizeText);
        Assert.Equal(
            [new(100, 100), new(200, 100), new(300, 100), new(300, 150), new(300, 200), new(200, 200), new(100, 200),
                new(100, 150)],
            view.Handles);
        Assert.Equal(new Rectangle(100, 100, 200, 100), await presenter.Completion);
        Assert.Equal(1, view.CloseCount);
    }

    [Theory]
    [InlineData(900, 400, 300, 300, 900, 400, 100, 100)]
    [InlineData(-50, -50, 100, 100, 0, 0, 50, 50)]
    public void Adjustable_LastAreaPartlyOffTheScreen_IsCutToIt(
        int x, int y, int width, int height, int shownX, int shownY, int shownWidth, int shownHeight)
    {
        // Act: e.g. a monitor unplugged since
        (FakeSnipView view, _) = Adjustable(new Rectangle(x, y, width, height));

        // Assert
        Assert.Equal(new Rectangle(shownX, shownY, shownWidth, shownHeight), view.Selection);
    }

    [Fact]
    public void Adjustable_LastAreaOffTheScreen_OrNotAdjustable_StartsWithNothing()
    {
        // Act
        (FakeSnipView offScreen, _) = Adjustable(new Rectangle(2000, 0, 50, 50));
        var view = new FakeSnipView();
        _ = new SnipPresenter(view, new Size(1000, 500), _time, isAdjustable: false, new Rectangle(10, 10, 50, 50));

        // Assert
        Assert.Null(offScreen.Selection);
        Assert.Null(view.Selection);
    }

    [Fact]
    public async Task Adjustable_ADrag_IsKeptWithHandles_UntilEnter()
    {
        // Arrange
        (FakeSnipView view, SnipPresenter presenter) = Adjustable();

        // Act
        view.Start(10, 10);
        view.Move(110, 60);
        view.End(110, 60);
        bool wasDone = presenter.Completion.IsCompleted;
        view.PressEnter();

        // Assert
        Assert.False(wasDone);
        Assert.Equal(8, view.Handles.Count);
        Assert.Equal(new Rectangle(10, 10, 100, 50), await presenter.Completion);
    }

    [Fact]
    public void Adjustable_DraggingInside_MovesIt_AndStopsAtTheEdges()
    {
        // Arrange
        (FakeSnipView view, _) = Adjustable(new Rectangle(100, 100, 200, 100));

        // Act
        view.Start(150, 150);
        view.Move(170, 160);
        view.End(170, 160);
        Rectangle? moved = view.Selection;
        view.Start(150, 150);
        view.End(2000, 2000);

        // Assert
        Assert.Equal(new Rectangle(120, 110, 200, 100), moved);
        Assert.Equal(new Rectangle(800, 400, 200, 100), view.Selection);
    }

    [Theory]
    [InlineData(300, 200, 350, 260, 100, 100, 250, 160)]
    [InlineData(308, 205, 350, 260, 100, 100, 250, 160)]
    [InlineData(100, 150, 50, 0, 50, 100, 250, 100)]
    [InlineData(200, 100, 999, 0, 100, 0, 200, 200)]
    public void Adjustable_DraggingAHandle_ResizesIt(
        int fromX, int fromY, int toX, int toY, int x, int y, int width, int height)
    {
        // Arrange
        (FakeSnipView view, _) = Adjustable(new Rectangle(100, 100, 200, 100));

        // Act
        view.Start(fromX, fromY);
        view.End(toX, toY);

        // Assert
        Assert.Equal(new Rectangle(x, y, width, height), view.Selection);
    }

    [Fact]
    public void Adjustable_AClickOutside_KeepsTheSelection_AndADragOutside_DrawsAnother()
    {
        // Arrange
        (FakeSnipView view, SnipPresenter presenter) = Adjustable(new Rectangle(100, 100, 200, 100));

        // Act
        view.Start(600, 400);
        view.End(600, 400);
        Rectangle? afterClick = view.Selection;
        view.Start(500, 300);
        view.End(700, 450);

        // Assert
        Assert.Equal(new Rectangle(100, 100, 200, 100), afterClick);
        Assert.Equal(new Rectangle(500, 300, 200, 150), view.Selection);
        Assert.False(presenter.Completion.IsCompleted);
    }

    [Theory]
    [InlineData(300, 200, SnipCursor.SizeTopLeftBottomRight)]
    [InlineData(300, 100, SnipCursor.SizeTopRightBottomLeft)]
    [InlineData(200, 104, SnipCursor.SizeVertical)]
    [InlineData(96, 150, SnipCursor.SizeHorizontal)]
    [InlineData(200, 150, SnipCursor.Move)]
    [InlineData(600, 400, SnipCursor.Cross)]
    public void Adjustable_ThePointer_SaysWhatADragThereDoes(int x, int y, SnipCursor cursor)
    {
        // Arrange
        (FakeSnipView view, _) = Adjustable(new Rectangle(100, 100, 200, 100));

        // Act
        view.Hover(600, 400);
        view.Hover(x, y);

        // Assert
        Assert.Equal(cursor, view.Cursor);
    }

    [Fact]
    public void Enter_WithNothingSelected_OrNotAdjustable_DoesNothing()
    {
        // Arrange
        (FakeSnipView view, SnipPresenter presenter) = Adjustable();
        _view.Start(10, 10);
        _view.Move(100, 100);

        // Act
        view.PressEnter();
        _view.PressEnter();

        // Assert
        Assert.False(presenter.Completion.IsCompleted);
        Assert.False(_presenter.Completion.IsCompleted);
        Assert.Empty(_view.Handles);
    }

    private sealed class MovableTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override DateTimeOffset GetUtcNow() => _now;

        public override long GetTimestamp() => _now.Ticks;

        public void Advance(TimeSpan time) => _now += time;
    }

    private sealed class FakeSnipView : ISnipView
    {
        public event EventHandler? FocusLost;

        public int TakeFocusCount { get; private set; }

        /// <summary>Raises FocusLost while closing, as a form does.</summary>
        public bool LoseFocusOnClose { get; set; }

        public void LoseFocus() => FocusLost?.Invoke(this, EventArgs.Empty);

        public void TakeFocus() => TakeFocusCount++;

        public event EventHandler<Point>? SelectionStarted;

        public event EventHandler<Point>? SelectionMoved;

        public event EventHandler<Point>? SelectionEnded;

        public event EventHandler<Point>? PointerMoved;

        public event EventHandler? ConfirmRequested;

        public event EventHandler? CancelRequested;

        public event EventHandler? ViewClosed;

        public Rectangle? Selection { get; private set; }

        public string? SizeText { get; private set; }

        public IReadOnlyList<Point> Handles { get; private set; } = [];

        public SnipCursor Cursor { get; private set; } = SnipCursor.Cross;

        public void Hover(int x, int y) => PointerMoved?.Invoke(this, new Point(x, y));

        public void PressEnter() => ConfirmRequested?.Invoke(this, EventArgs.Empty);

        public void ShowCursor(SnipCursor cursor) => Cursor = cursor;

        public int CloseCount { get; private set; }

        /// <summary>Raises CancelRequested while closing, as a form losing the focus does.</summary>
        public bool CancelOnClose { get; set; }

        public void Start(int x, int y) => SelectionStarted?.Invoke(this, new Point(x, y));

        public void Move(int x, int y) => SelectionMoved?.Invoke(this, new Point(x, y));

        public void End(int x, int y) => SelectionEnded?.Invoke(this, new Point(x, y));

        public void Cancel() => CancelRequested?.Invoke(this, EventArgs.Empty);

        public void RaiseClosed() => ViewClosed?.Invoke(this, EventArgs.Empty);

        public void ShowSelection(Rectangle selection, string sizeText, IReadOnlyList<Point> handles)
        {
            Selection = selection;
            SizeText = sizeText;
            Handles = handles;
        }

        public void ClearSelection()
        {
            Selection = null;
            SizeText = null;
            Handles = [];
        }

        public void Close()
        {
            CloseCount++;

            if (CancelOnClose)
            {
                Cancel();
            }

            if (LoseFocusOnClose)
            {
                LoseFocus();
            }

            RaiseClosed();
        }
    }
}
