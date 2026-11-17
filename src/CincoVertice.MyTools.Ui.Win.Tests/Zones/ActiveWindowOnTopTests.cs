using System.Windows.Forms;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Win.Zones.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Zones;

/// <summary>
///     With a real window made the active one for a moment. Alone: another test showing a form would take the focus.
/// </summary>
[Collection(KeyboardFocusCollection.Name)]
public class ActiveWindowOnTopTests
{
    [Fact]
    public void Set_SwitchesTheActiveWindow_OnTop_AndBack()
    {
        // Arrange
        using var form = new Form { Text = "On top test", ShowInTaskbar = false };
        form.Show();
        WindowHelper.ForceForeground(form.Handle);
        Application.DoEvents();

        // Act
        (string title, bool on) = ActiveWindowOnTop.Set(null);
        bool isTopMost = WindowHelper.IsTopMost(form.Handle);
        (_, bool off) = ActiveWindowOnTop.Set(null);
        (_, bool setOn) = ActiveWindowOnTop.Set(true);
        (_, bool stillOn) = ActiveWindowOnTop.Set(true);

        // Assert
        Assert.Equal("On top test", title);
        Assert.True(on);
        Assert.True(isTopMost);
        Assert.False(off);
        Assert.True(setOn);
        Assert.True(stillOn);
    }
}
