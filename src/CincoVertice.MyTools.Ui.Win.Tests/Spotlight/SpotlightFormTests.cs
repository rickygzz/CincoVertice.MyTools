using System.Drawing;
using System.Windows.Forms;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Win.Spotlight;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Spotlight;

/// <summary>
///     Shows the spotlight on the screen for a moment. Alone: another test showing a form would take the focus.
/// </summary>
[Collection(KeyboardFocusCollection.Name)]
public class SpotlightFormTests
{
    [Fact]
    public void ShowSpotlight_CoversEveryMonitor_WithoutTakingTheFocus_AndHideSpotlightHidesIt()
    {
        // Arrange
        using var other = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual };
        other.Show();
        other.Activate();
        Application.DoEvents();
        using var spotlight = new SpotlightForm();

        // Act
        spotlight.ShowSpotlight(new Point(100, 100), 50);
        spotlight.ShowSpotlight(new Point(120, 100), 50);
        Application.DoEvents();

        // Assert
        long extendedStyle = User32.GetWindowLongPtr(spotlight.Handle, User32.GWL_EXSTYLE);
        Assert.True(spotlight.Visible);
        Assert.NotEqual(0, extendedStyle & (long)WS.WS_EX_TRANSPARENT);
        Assert.NotEqual(0, extendedStyle & (long)WS.WS_EX_TOPMOST);
        Assert.False(spotlight.ContainsFocus);
        Assert.Equal(other, Form.ActiveForm);

        // And
        spotlight.HideSpotlight();
        Assert.False(spotlight.Visible);
    }
}
