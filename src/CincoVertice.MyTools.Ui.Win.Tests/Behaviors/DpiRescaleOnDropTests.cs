using System.Drawing;
using System.Windows.Forms;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Behaviors;

/// <summary>
///     A drag as Windows tells a form of it. Crossing to a monitor with other scaling needs a per-monitor aware process,
///     which the tests aren't: that's tried with two monitors, not here.
/// </summary>
public class DpiRescaleOnDropTests
{
    private static Form ShowForm()
    {
        var form = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-5000, 100),
            Size = new Size(400, 300),
        };
        form.Controls.Add(new Button { Text = "OK", Location = new Point(10, 10) });
        DpiRescaleOnDrop.Attach(form);
        DpiRescaleOnDrop.Attach(form);
        form.Show();
        Application.DoEvents();

        return form;
    }

    [Fact]
    public void WhileMovedOrResized_TheFormIsPaintedInOneGo_AndAfterAsBefore()
    {
        // Arrange
        using Form form = ShowForm();
        bool before = CompositedPainting.IsOn(form.Handle);

        // Act
        User32.SendMessage(form.Handle, (uint)WM.WM_ENTERSIZEMOVE, 0, 0);
        form.Size = new Size(500, 350);
        Application.DoEvents();
        bool during = CompositedPainting.IsOn(form.Handle);
        User32.SendMessage(form.Handle, (uint)WM.WM_EXITSIZEMOVE, 0, 0);

        // Assert
        Assert.False(before);
        Assert.True(during);
        Assert.False(CompositedPainting.IsOn(form.Handle));
    }

    [Fact]
    public void ADragOnOneMonitor_ChangesNothing()
    {
        // Arrange
        using Form form = ShowForm();
        Control button = form.Controls[0];
        Size size = form.Size;
        Font font = button.Font;

        // Act
        User32.SendMessage(form.Handle, (uint)WM.WM_ENTERSIZEMOVE, 0, 0);
        form.Location = new Point(-4900, 150);
        Application.DoEvents();
        User32.SendMessage(form.Handle, (uint)WM.WM_EXITSIZEMOVE, 0, 0);
        Application.DoEvents();

        // Assert
        Assert.Equal(new Point(-4900, 150), form.Location);
        Assert.Equal(size, form.Size);
        Assert.Same(font, button.Font);
    }

    [Fact]
    public void AfterADrag_TheFormAndItsControlsStillGetTheirMessages()
    {
        // Arrange
        using Form form = ShowForm();
        int clicks = 0;
        var button = (Button)form.Controls[0];
        button.Click += (_, _) => clicks++;

        // Act
        User32.SendMessage(form.Handle, (uint)WM.WM_ENTERSIZEMOVE, 0, 0);
        User32.SendMessage(form.Handle, (uint)WM.WM_EXITSIZEMOVE, 0, 0);
        button.PerformClick();
        form.Text = "Moved";

        // Assert
        Assert.Equal(1, clicks);
        Assert.Equal("Moved", form.Text);
    }
}
