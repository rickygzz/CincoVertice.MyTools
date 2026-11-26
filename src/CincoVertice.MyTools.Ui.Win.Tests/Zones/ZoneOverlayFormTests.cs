using System.Drawing;
using System.Windows.Forms;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Zones;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Zones;

/// <summary>
///     Shows the overlay on the screen for a moment. Alone: another test showing a form would change the window in
///     front while this one looks at it.
/// </summary>
[Collection(KeyboardFocusCollection.Name)]
public class ZoneOverlayFormTests
{
    [Fact]
    public void ShowZones_CoversEveryMonitor_WithoutTakingTheFocus_AndHideZonesHidesIt()
    {
        // Arrange: whichever window is in front, which may not be one of the tests'; Windows lets a program put its own
        // window in front only at times, so the test doesn't try
        nint inFront = WinUser.GetForegroundWindow();
        using var overlay = new ZoneOverlayForm(new TrayNotificationService());

        // Act
        overlay.ShowZones([new Rectangle(0, 0, 200, 100), new Rectangle(200, 0, 200, 100)], 1);
        Application.DoEvents();

        // Assert: it never becomes the active window, and the one in front stays so, as the dragged window must
        long extendedStyle = User32.GetWindowLongPtr(overlay.Handle, User32.GWL_EXSTYLE);
        Assert.True(overlay.Visible);
        Assert.NotEqual(0, extendedStyle & (long)WS.WS_EX_TOPMOST);
        Assert.NotEqual(0, extendedStyle & (long)WS.WS_EX_NOACTIVATE);
        Assert.False(overlay.ContainsFocus);
        Assert.NotEqual(overlay.Handle, WinUser.GetForegroundWindow());
        Assert.Equal(inFront, WinUser.GetForegroundWindow());

        // And
        overlay.HideZones();
        Assert.False(overlay.Visible);
    }

    [Fact]
    public void ShowProblem_IsAWarningNotification()
    {
        // Arrange
        var notifications = new TrayNotificationService();
        var shown = new List<string>();
        notifications.Requested += (_, notification) => shown.Add(notification.Text);
        using var overlay = new ZoneOverlayForm(notifications);

        // Act
        overlay.ShowProblem("It runs as administrator.");

        // Assert
        Assert.Equal(["It runs as administrator."], shown);
    }
}
