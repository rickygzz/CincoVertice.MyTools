using System.Drawing;
using CincoVertice.MyTools.Ui.Spotlight.Presenters;
using CincoVertice.MyTools.Ui.Spotlight.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Spotlight;

public class SpotlightPresenterTests
{
    private readonly MovableTimeProvider _time = new();
    private readonly FakeSpotlightView _view = new();
    private readonly SpotlightPresenter _presenter;

    public SpotlightPresenterTests()
    {
        _presenter = new SpotlightPresenter(_view, _time);
    }

    [Fact]
    public void Show_StartsLarge_AroundThePointer()
    {
        // Arrange
        _view.CursorPosition = new Point(500, 300);

        // Act
        _presenter.Show();

        // Assert
        Assert.True(_presenter.IsShown);
        Assert.Equal((new Point(500, 300), SpotlightPresenter.StartRadius), _view.Shown);
    }

    [Fact]
    public void Ticks_ShrinkIt_OntoItsRadius_AndFollowThePointer()
    {
        // Arrange
        _presenter.Show();

        // Act
        _time.Advance(SpotlightPresenter.ShrinkTime / 2);
        _view.MovePointer(new Point(10, 20));
        int halfway = _view.Shown!.Value.Radius;
        _time.Advance(SpotlightPresenter.ShrinkTime);
        _view.MovePointer(new Point(30, 40));

        // Assert
        Assert.InRange(halfway, SpotlightPresenter.Radius + 1, SpotlightPresenter.StartRadius - 1);
        Assert.Equal((new Point(30, 40), SpotlightPresenter.Radius), _view.Shown);
    }

    [Fact]
    public void RadiusAt_SlowsDown()
    {
        // Act
        int quarter = SpotlightPresenter.RadiusAt(SpotlightPresenter.ShrinkTime / 4);
        int half = SpotlightPresenter.RadiusAt(SpotlightPresenter.ShrinkTime / 2);
        int threeQuarters = SpotlightPresenter.RadiusAt(SpotlightPresenter.ShrinkTime * 3 / 4);

        // Assert: it shrinks less in each quarter than in the one before
        Assert.True(SpotlightPresenter.StartRadius - quarter > quarter - half);
        Assert.True(quarter - half > half - threeQuarters);
    }

    [Fact]
    public void APointerStillLongEnough_HidesIt()
    {
        // Arrange
        _presenter.Show();

        // Act
        _time.Advance(SpotlightPresenter.StillTime - TimeSpan.FromMilliseconds(1));
        _view.MovePointer(_view.CursorPosition);
        bool shownBefore = _presenter.IsShown;
        _time.Advance(TimeSpan.FromMilliseconds(1));
        _view.MovePointer(_view.CursorPosition);

        // Assert
        Assert.True(shownBefore);
        Assert.False(_presenter.IsShown);
        Assert.Null(_view.Shown);
    }

    [Fact]
    public void APointerThatMoves_KeepsIt()
    {
        // Arrange
        _presenter.Show();

        // Act
        _time.Advance(SpotlightPresenter.StillTime - TimeSpan.FromMilliseconds(1));
        _view.MovePointer(new Point(1, 1));
        _time.Advance(SpotlightPresenter.StillTime - TimeSpan.FromMilliseconds(1));
        _view.MovePointer(new Point(1, 1));

        // Assert
        Assert.True(_presenter.IsShown);
    }

    [Fact]
    public void Hide_WhenNotShown_DoesNothing()
    {
        // Act
        _presenter.Hide();

        // Assert
        Assert.Equal(0, _view.HideCount);
    }

    private sealed class MovableTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan time) => _now += time;
    }

    private sealed class FakeSpotlightView : ISpotlightView
    {
        public event EventHandler? Tick;

        public Point CursorPosition { get; set; }

        public (Point Center, int Radius)? Shown { get; private set; }

        public int HideCount { get; private set; }

        public void ShowSpotlight(Point center, int radius) => Shown = (center, radius);

        public void HideSpotlight()
        {
            Shown = null;
            HideCount++;
        }

        public void MovePointer(Point cursor)
        {
            CursorPosition = cursor;
            Tick?.Invoke(this, EventArgs.Empty);
        }
    }
}
