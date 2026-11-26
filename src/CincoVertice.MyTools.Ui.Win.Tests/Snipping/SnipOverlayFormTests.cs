using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
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

    [Fact]
    public async Task Adjustable_AHandleShowsItsPointer_DraggingItResizes_AndEnterSnips()
    {
        // Arrange: the area snipped last, from (10, 10) to (60, 40)
        using var screenshot = new Bitmap(100, 100);
        using SnipOverlayForm overlay = ShowOverlay(screenshot);
        var presenter = new SnipPresenter(
            overlay,
            screenshot.Size,
            TimeProvider.System,
            isAdjustable: true,
            new Rectangle(10, 10, 50, 30));

        // Act: the bottom right handle, as the mouse and keyboard tell the form
        Send(overlay, WM.WM_MOUSEMOVE, 0, 60, 40);
        Cursor overHandle = overlay.Cursor;
        Send(overlay, WM.WM_LBUTTONDOWN, 1, 60, 40);
        Send(overlay, WM.WM_MOUSEMOVE, 1, 80, 70);
        Send(overlay, WM.WM_LBUTTONUP, 0, 80, 70);
        bool snippedWhenLetGo = presenter.Completion.IsCompleted;
        Send(overlay, WM.WM_KEYDOWN, (int)Keys.Enter, 0, 0);
        Pump();

        // Assert
        Assert.Equal(Cursors.SizeNWSE, overHandle);
        Assert.False(snippedWhenLetGo);
        Assert.Equal(new Rectangle(10, 10, 70, 60), await presenter.Completion);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Adjustable_TheGreenButtonSnips_AndTheCrossCancels(bool isConfirm)
    {
        // Arrange
        using var screenshot = new Bitmap(300, 300);
        using SnipOverlayForm overlay = ShowOverlay(screenshot);
        var presenter = new SnipPresenter(
            overlay,
            screenshot.Size,
            TimeProvider.System,
            isAdjustable: true,
            new Rectangle(20, 20, 200, 100));
        Rectangle button = isConfirm ? overlay.ConfirmButtonBounds : overlay.CancelButtonBounds;
        Point center = new(button.X + button.Width / 2, button.Y + button.Height / 2);

        // Act
        Send(overlay, WM.WM_MOUSEMOVE, 0, center.X, center.Y);
        Cursor overButton = overlay.Cursor;
        // The press is enough: the overlay closes at once
        Send(overlay, WM.WM_LBUTTONDOWN, 1, center.X, center.Y);
        Pump();

        // Assert: below the selection, both on the screenshot, apart
        Assert.True(button.Top > 120 && button.Bottom <= 300, $"{button}");
        Assert.False(overlay.ConfirmButtonBounds.IntersectsWith(overlay.CancelButtonBounds));
        Assert.Equal(Cursors.Hand, overButton);
        Assert.Equal(isConfirm ? new Rectangle(20, 20, 200, 100) : null, await presenter.Completion);
    }

    [Fact]
    public void Adjustable_NoRoomBelow_TheButtonsGoInside_AndNotAdjustable_ThereAreNone()
    {
        // Arrange
        using var screenshot = new Bitmap(300, 300);
        using SnipOverlayForm adjustable = ShowOverlay(screenshot);
        _ = new SnipPresenter(
            adjustable,
            screenshot.Size,
            TimeProvider.System,
            isAdjustable: true,
            new Rectangle(20, 100, 200, 200));
        using SnipOverlayForm plain = ShowOverlay(screenshot);
        var presenter = new SnipPresenter(plain, screenshot.Size, TimeProvider.System);

        // Act
        Send(plain, WM.WM_LBUTTONDOWN, 1, 10, 10);
        Send(plain, WM.WM_MOUSEMOVE, 1, 100, 100);

        // Assert
        Assert.True(new Rectangle(20, 100, 200, 200).Contains(adjustable.ConfirmButtonBounds));
        Assert.True(plain.ConfirmButtonBounds.IsEmpty && plain.CancelButtonBounds.IsEmpty);
        Assert.False(presenter.Completion.IsCompleted);
    }

    private static void Send(SnipOverlayForm overlay, WM message, int wParam, int x, int y) =>
        User32.SendMessage(overlay.Handle, (uint)message, wParam, (y << 16) | x);

    /// <summary>Every reading is much later than the last, so the grace period is over at once.</summary>
    private sealed class LaterTimeProvider : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _timestamp += TimeSpan.TicksPerHour;
    }
}
