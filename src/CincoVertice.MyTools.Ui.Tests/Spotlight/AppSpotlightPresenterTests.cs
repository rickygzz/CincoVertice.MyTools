using System.Drawing;
using CincoVertice.MyTools.Ui.Spotlight.Presenters;
using CincoVertice.MyTools.Ui.Spotlight.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Spotlight;

public class AppSpotlightPresenterTests
{
    private static readonly Rectangle _window = new(100, 100, 800, 600);

    private readonly MovableTimeProvider _time = new();
    private readonly FakeAppSpotlightView _view = new();
    private readonly AppSpotlightPresenter _presenter;

    public AppSpotlightPresenterTests()
    {
        _presenter = new AppSpotlightPresenter(_view, _time);
    }

    [Fact]
    public void Show_StartsFarAroundTheActiveWindow()
    {
        // Act
        _presenter.Show();

        // Assert
        Assert.True(_presenter.IsShown);
        Assert.Equal((_window, AppSpotlightPresenter.StartMargin), _view.Shown);
    }

    [Fact]
    public void Show_WithNoActiveWindow_ShowsNothing()
    {
        // Arrange
        _view.ActiveWindowBounds = null;

        // Act
        _presenter.Show();

        // Assert
        Assert.False(_presenter.IsShown);
        Assert.Null(_view.Shown);
    }

    [Fact]
    public void Ticks_CloseTheFrameOntoTheWindow_AndFollowIt()
    {
        // Arrange
        _presenter.Show();

        // Act
        _time.Advance(AppSpotlightPresenter.ShrinkTime / 2);
        _view.RaiseTick();
        int halfway = _view.Shown!.Value.Margin;
        _time.Advance(AppSpotlightPresenter.ShrinkTime);
        _view.ActiveWindowBounds = new Rectangle(300, 200, 800, 600);
        _view.RaiseTick();

        // Assert
        Assert.InRange(halfway, AppSpotlightPresenter.Margin + 1, AppSpotlightPresenter.StartMargin - 1);
        Assert.Equal((new Rectangle(300, 200, 800, 600), AppSpotlightPresenter.Margin), _view.Shown);
    }

    [Fact]
    public void AWindowThatGoes_KeepsTheFrameWhereItWas()
    {
        // Arrange
        _presenter.Show();
        _view.ActiveWindowBounds = null;

        // Act
        _time.Advance(AppSpotlightPresenter.ShrinkTime);
        _view.RaiseTick();

        // Assert
        Assert.Equal((_window, AppSpotlightPresenter.Margin), _view.Shown);
    }

    [Fact]
    public void ItHides_AfterItsTime()
    {
        // Arrange
        _presenter.Show();

        // Act
        _time.Advance(AppSpotlightPresenter.ShowTime - TimeSpan.FromMilliseconds(1));
        _view.RaiseTick();
        bool before = _presenter.IsShown;
        _time.Advance(TimeSpan.FromMilliseconds(1));
        _view.RaiseTick();

        // Assert
        Assert.True(before);
        Assert.False(_presenter.IsShown);
        Assert.Equal(1, _view.HideCount);
    }

    [Fact]
    public void Hide_HidesIt_Once()
    {
        // Arrange
        _presenter.Show();

        // Act
        _presenter.Hide();
        _presenter.Hide();

        // Assert
        Assert.False(_presenter.IsShown);
        Assert.Equal(1, _view.HideCount);
    }

    [Fact]
    public void MarginAt_SlowsDown_OntoTheMargin()
    {
        // Act
        int quarter = AppSpotlightPresenter.MarginAt(AppSpotlightPresenter.ShrinkTime / 4);
        int half = AppSpotlightPresenter.MarginAt(AppSpotlightPresenter.ShrinkTime / 2);

        // Assert
        Assert.Equal(AppSpotlightPresenter.StartMargin, AppSpotlightPresenter.MarginAt(TimeSpan.Zero));
        Assert.True(AppSpotlightPresenter.StartMargin - quarter > quarter - half);
        Assert.Equal(AppSpotlightPresenter.Margin, AppSpotlightPresenter.MarginAt(AppSpotlightPresenter.ShrinkTime));
    }

    private sealed class MovableTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 19, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan time) => _now += time;
    }

    private sealed class FakeAppSpotlightView : IAppSpotlightView
    {
        public event EventHandler? Tick;

        public Rectangle? ActiveWindowBounds { get; set; } = _window;

        public (Rectangle Window, int Margin)? Shown { get; private set; }

        public int HideCount { get; private set; }

        public void ShowSpotlight(Rectangle window, int margin) => Shown = (window, margin);

        public void HideSpotlight()
        {
            Shown = null;
            HideCount++;
        }

        public void RaiseTick() => Tick?.Invoke(this, EventArgs.Empty);
    }
}
