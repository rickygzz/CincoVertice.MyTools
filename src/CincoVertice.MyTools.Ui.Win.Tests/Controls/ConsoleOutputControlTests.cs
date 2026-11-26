using System.Drawing;
using System.Windows.Forms;
using CincoVertice.Common.Ui.Win.Controls.Rtf;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Controls;

public class ConsoleOutputControlTests
{
    [Fact]
    public void ContentZoom_IsKept_WhenNewTextIsShown()
    {
        // Arrange: new RTF shows at a zoom of 1 again in a rich text box
        using var host = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-5000, 100),
        };
        var output = new ConsoleOutputControl { Dock = DockStyle.Fill };
        host.Controls.Add(output);
        host.Show();

        // Act
        output.ContentZoom = 1.25f;
        output.SetText("12 + 30 = 42", updateContent: true);
        float afterText = output.ZoomFactor;
        output.ClearContent();
        float afterClear = output.ZoomFactor;

        // Assert
        Assert.Equal(1.25f, afterText, 3);
        Assert.Equal(1.25f, afterClear, 3);
    }

    [Fact]
    public void ContentZoom_OfOne_ShowsTheTextAsWritten()
    {
        // Arrange
        using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual };
        host.Location = new Point(-5000, 100);
        var output = new ConsoleOutputControl { Dock = DockStyle.Fill };
        host.Controls.Add(output);
        host.Show();
        output.ContentZoom = 1.25f;

        // Act
        output.ContentZoom = 1;
        output.SetText("42", updateContent: true);

        // Assert
        Assert.Equal(1f, output.ZoomFactor, 3);
    }
}
