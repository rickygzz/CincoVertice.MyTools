using System.Drawing;
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

        public event EventHandler? CancelRequested;

        public event EventHandler? ViewClosed;

        public Rectangle? Selection { get; private set; }

        public string? SizeText { get; private set; }

        public int CloseCount { get; private set; }

        /// <summary>Raises CancelRequested while closing, as a form losing the focus does.</summary>
        public bool CancelOnClose { get; set; }

        public void Start(int x, int y) => SelectionStarted?.Invoke(this, new Point(x, y));

        public void Move(int x, int y) => SelectionMoved?.Invoke(this, new Point(x, y));

        public void End(int x, int y) => SelectionEnded?.Invoke(this, new Point(x, y));

        public void Cancel() => CancelRequested?.Invoke(this, EventArgs.Empty);

        public void RaiseClosed() => ViewClosed?.Invoke(this, EventArgs.Empty);

        public void ShowSelection(Rectangle selection, string sizeText)
        {
            Selection = selection;
            SizeText = sizeText;
        }

        public void ClearSelection()
        {
            Selection = null;
            SizeText = null;
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
