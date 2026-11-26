using System.Drawing;
using System.Windows.Forms;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Win.Spotlight;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Spotlight;

/// <summary>
///     Shows the spotlight on the screen for a moment. Alone: another test showing a form would change the window in
///     front while this one looks at it.
/// </summary>
[Collection(KeyboardFocusCollection.Name)]
public class SpotlightFormTests
{
    [Fact]
    public void ShowSpotlight_CoversEveryMonitor_WithoutTakingTheFocus_AndHideSpotlightHidesIt()
    {
        // Arrange: whichever window is in front, which may not be one of the tests'; Windows lets a program put its own
        // window in front only at times, so the test doesn't try
        nint inFront = WinUser.GetForegroundWindow();
        using var spotlight = new SpotlightForm();

        // Act
        spotlight.ShowSpotlight(new Point(100, 100), 50);
        spotlight.ShowSpotlight(new Point(120, 100), 50);
        Application.DoEvents();

        // Assert: it never becomes the active window, and the one in front stays so
        long extendedStyle = User32.GetWindowLongPtr(spotlight.Handle, User32.GWL_EXSTYLE);
        Assert.True(spotlight.Visible);
        Assert.NotEqual(0, extendedStyle & (long)WS.WS_EX_TRANSPARENT);
        Assert.NotEqual(0, extendedStyle & (long)WS.WS_EX_TOPMOST);
        Assert.NotEqual(0, extendedStyle & (long)WS.WS_EX_NOACTIVATE);
        Assert.False(spotlight.ContainsFocus);
        Assert.NotEqual(spotlight.Handle, WinUser.GetForegroundWindow());
        Assert.Equal(inFront, WinUser.GetForegroundWindow());

        // And
        spotlight.HideSpotlight();
        Assert.False(spotlight.Visible);
    }

    [Fact]
    public void ShowSpotlight_OfAWindow_FramesIt_TheMarginOutsideIt()
    {
        // Arrange
        using var spotlight = new SpotlightForm();

        // Act
        spotlight.ShowSpotlight(new Rectangle(200, 150, 400, 300), 10);
        Application.DoEvents();

        // Assert
        int margin = 10 * spotlight.DeviceDpi / 96;
        var expected = new Rectangle(
            200 - spotlight.Left - margin,
            150 - spotlight.Top - margin,
            400 + margin * 2,
            300 + margin * 2);
        Assert.True(spotlight.Visible);
        Assert.Equal((expected, false), spotlight.Drawn);

        // And: the circle again
        spotlight.ShowSpotlight(new Point(100, 100), 50);
        Assert.True(spotlight.Drawn.IsRound);
        spotlight.HideSpotlight();
    }
}
