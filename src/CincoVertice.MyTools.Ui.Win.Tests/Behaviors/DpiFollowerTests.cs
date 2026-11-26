using System.Drawing;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Behaviors;

public class DpiFollowerTests
{
    [Fact]
    public void Attach_RunsTheSetupAtOnce_AndNotAgainWhileTheDpiIsTheSame()
    {
        // Arrange
        using var form = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-5000, 100),
        };
        List<int> runs = [];

        // Act: shown, its window is made on the monitor it's on
        DpiFollower.Attach(form, runs.Add);
        form.Show();
        Application.DoEvents();

        // Assert
        Assert.Equal([form.DeviceDpi], runs);
    }

    [Theory]
    [InlineData(16, 96, 16)]
    [InlineData(16, 120, 20)]
    [InlineData(16, 144, 24)]
    [InlineData(48, 120, 60)]
    public void Scaled_IsThePixelsAt100Percent_AtTheDpi(int pixels, int dpi, int expected)
    {
        // Act / Assert
        Assert.Equal(expected, DpiFollower.Scaled(pixels, dpi));
    }

    [Fact]
    public void FontScale_IsHowMuchTheFontGrewFromItsDesignedSize()
    {
        // Arrange
        using var label = new Label { Font = new Font("Segoe UI", 11.25f) };

        // Act / Assert: 9 points rescaled to 11.25 on a monitor at 125% when the main one is at 100%
        Assert.Equal(1.25f, DpiFollower.FontScale(label, 9f), 3);
    }
}
