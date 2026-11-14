using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Models;
using CincoVertice.MyTools.Ui.ColorPicker.Presenters;
using CincoVertice.MyTools.Ui.ColorPicker.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.ColorPicker;

public class ColorPickerPresenterTests
{
    private readonly FakeColorPickerView _view = new();
    private readonly MovableTimeProvider _time = new();
    private readonly ColorPickerPresenter _presenter;

    public ColorPickerPresenterTests()
    {
        // Each pixel's color tells where it is, with an alpha the screenshot may have
        _presenter = new ColorPickerPresenter(
            _view,
            new Size(200, 100),
            pixel => Color.FromArgb(0, pixel.X, pixel.Y, 7),
            _time);
    }

    [Fact]
    public void PointerMoved_ShowsTheLens_WithThePixelsColor()
    {
        // Act
        _view.MovePointerTo(30, 144);

        // Assert: clamped into the screenshot's last row
        Assert.Equal(
            new ColorLensModel(
                new Point(30, 99),
                ColorPickerPresenter.DefaultZoom,
                ColorPickerPresenter.CellsAt(ColorPickerPresenter.DefaultZoom),
                Color.FromArgb(30, 99, 7),
                "#1E6307",
                "rgb(30, 99, 7)"),
            _view.Lens);
    }

    [Theory]
    [InlineData(ColorTextFormat.Hex, "#0A1407")]
    [InlineData(ColorTextFormat.Rgb, "rgb(10, 20, 7)")]
    public async Task Pick_ClosesTheView_WithTheColorUnderThePointer(ColorTextFormat format, string text)
    {
        // Arrange
        _view.MovePointerTo(10, 20);

        // Act
        _view.Pick(format);

        // Assert
        PickedColorModel? picked = await _presenter.Completion;
        Assert.Equal(1, _view.CloseCount);
        Assert.Equal(Color.FromArgb(255, 10, 20, 7), picked!.Color);
        Assert.Equal(text, picked.Text);
    }

    [Fact]
    public void Pick_BeforeThePointerIsKnown_DoesNothing()
    {
        // Act
        _view.Pick(ColorTextFormat.Hex);

        // Assert
        Assert.Equal(0, _view.CloseCount);
    }

    [Fact]
    public async Task Cancel_ClosesTheView_WithNoColor()
    {
        // Arrange
        _view.MovePointerTo(10, 20);

        // Act
        _view.Cancel();

        // Assert
        Assert.Null(await _presenter.Completion);
    }

    [Fact]
    public void Nudge_MovesThePointer_InsideTheScreenshot()
    {
        // Arrange
        _view.MovePointerTo(198, 50);

        // Act
        _view.Nudge(new Size(ColorPickerPresenter.LargeNudge, -1));

        // Assert
        Assert.Equal(new Point(199, 49), _view.PointerMovedTo);
        Assert.Equal(new Point(199, 49), _view.Lens!.Pixel);
    }

    [Fact]
    public void Zoom_StepsThroughTheZooms_AndStopsAtTheEnds()
    {
        // Arrange
        _view.MovePointerTo(10, 20);

        // Act / Assert
        _view.Zoom(1);
        Assert.Equal(12, _view.Lens!.Zoom);

        _view.Zoom(100);
        Assert.Equal(ColorPickerPresenter.Zooms[^1], _view.Lens.Zoom);

        _view.Zoom(-100);
        Assert.Equal(ColorPickerPresenter.Zooms[0], _view.Lens.Zoom);
    }

    [Fact]
    public void CellsAt_IsOdd_AndAboutTheLensSide()
    {
        foreach (int zoom in ColorPickerPresenter.Zooms)
        {
            int cells = ColorPickerPresenter.CellsAt(zoom);

            Assert.Equal(1, cells % 2);
            Assert.InRange(cells * zoom, ColorPickerPresenter.LensSide - 2 * zoom, ColorPickerPresenter.LensSide);
        }
    }

    [Fact]
    public void FocusLost_RightAfterOpening_TakesItBack()
    {
        // Act
        _view.LoseFocus();

        // Assert
        Assert.Equal(1, _view.TakeFocusCount);
        Assert.Equal(0, _view.CloseCount);
    }

    [Fact]
    public async Task FocusLost_Later_Cancels()
    {
        // Arrange
        _view.MovePointerTo(10, 20);
        _time.Advance(ColorPickerPresenter.FocusGracePeriod);

        // Act
        _view.LoseFocus();

        // Assert
        Assert.Null(await _presenter.Completion);
    }

    [Fact]
    public async Task FocusLost_WhileClosing_KeepsThePick()
    {
        // Arrange: a closing form loses the focus
        _time.Advance(TimeSpan.FromSeconds(5));
        _view.LoseFocusOnClose = true;
        _view.MovePointerTo(10, 20);

        // Act
        _view.Pick(ColorTextFormat.Hex);

        // Assert
        Assert.Equal("#0A1407", (await _presenter.Completion)!.Text);
    }

    private sealed class MovableTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override DateTimeOffset GetUtcNow() => _now;

        public override long GetTimestamp() => _now.Ticks;

        public void Advance(TimeSpan time) => _now += time;
    }

    private sealed class FakeColorPickerView : IColorPickerView
    {
        public event EventHandler<Point>? PointerMoved;

        public event EventHandler<int>? ZoomRequested;

        public event EventHandler<Size>? NudgeRequested;

        public event EventHandler<ColorTextFormat>? PickRequested;

        public event EventHandler? CancelRequested;

        public event EventHandler? FocusLost;

        public event EventHandler? ViewClosed;

        public ColorLensModel? Lens { get; private set; }

        public Point? PointerMovedTo { get; private set; }

        public int TakeFocusCount { get; private set; }

        public int CloseCount { get; private set; }

        /// <summary>Raises FocusLost while closing, as a form does.</summary>
        public bool LoseFocusOnClose { get; set; }

        public void MovePointerTo(int x, int y) => PointerMoved?.Invoke(this, new Point(x, y));

        public void Zoom(int steps) => ZoomRequested?.Invoke(this, steps);

        public void Nudge(Size offset) => NudgeRequested?.Invoke(this, offset);

        public void Pick(ColorTextFormat format) => PickRequested?.Invoke(this, format);

        public void Cancel() => CancelRequested?.Invoke(this, EventArgs.Empty);

        public void LoseFocus() => FocusLost?.Invoke(this, EventArgs.Empty);

        public void ShowLens(ColorLensModel lens) => Lens = lens;

        public void MovePointer(Point pixel) => PointerMovedTo = pixel;

        public void TakeFocus() => TakeFocusCount++;

        public void Close()
        {
            CloseCount++;

            if (LoseFocusOnClose)
            {
                LoseFocus();
            }

            ViewClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
