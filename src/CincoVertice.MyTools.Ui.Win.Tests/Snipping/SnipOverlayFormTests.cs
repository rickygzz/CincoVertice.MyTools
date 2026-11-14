using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Snipping.Presenters;
using CincoVertice.MyTools.Ui.Win.Snipping;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Snipping;

public class SnipOverlayFormTests
{
    private static SnipOverlayForm ShowOverlay(Bitmap screenshot)
    {
        var overlay = new SnipOverlayForm(screenshot, new Rectangle(-3000, 100, screenshot.Width, screenshot.Height));

        overlay.Show();

        return overlay;
    }

    private static void RaiseDeactivate(SnipOverlayForm overlay)
    {
        typeof(Form)
            .GetMethod("OnDeactivate", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(overlay, [EventArgs.Empty]);
    }

    private static void Pump()
    {
        for (int i = 0; i < 5; i++)
        {
            Application.DoEvents();
        }
    }

    [Fact]
    public void Deactivate_RightAfterOpening_LeavesTheOverlayOpen()
    {
        // Arrange
        using var screenshot = new Bitmap(100, 100);
        using SnipOverlayForm overlay = ShowOverlay(screenshot);
        var presenter = new SnipPresenter(overlay, screenshot.Size, TimeProvider.System);

        // Act: the real form's own event, then what the presenter asked it to do
        RaiseDeactivate(overlay);
        Pump();

        // Assert
        Assert.False(presenter.Completion.IsCompleted);
        Assert.False(overlay.IsDisposed);
        Assert.True(overlay.Visible);
    }

    [Fact]
    public async Task Deactivate_Later_ClosesTheOverlay()
    {
        // Arrange
        using var screenshot = new Bitmap(100, 100);
        using SnipOverlayForm overlay = ShowOverlay(screenshot);
        var presenter = new SnipPresenter(overlay, screenshot.Size, new LaterTimeProvider());

        // Act
        RaiseDeactivate(overlay);
        Pump();

        // Assert
        Assert.Null(await presenter.Completion);
    }

    [Fact]
    public void TakeFocus_WhenItsClosedByThen_DoesNothing()
    {
        // Arrange: the focus was asked for, and the overlay closed before that ran
        using var screenshot = new Bitmap(100, 100);
        SnipOverlayForm overlay = ShowOverlay(screenshot);

        // Act
        overlay.TakeFocus();
        overlay.Close();
        Pump();

        // Assert: reaching here is the point
        Assert.True(overlay.IsDisposed);
    }

    /// <summary>Every reading is much later than the last, so the grace period is over at once.</summary>
    private sealed class LaterTimeProvider : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _timestamp += TimeSpan.TicksPerHour;
    }
}
