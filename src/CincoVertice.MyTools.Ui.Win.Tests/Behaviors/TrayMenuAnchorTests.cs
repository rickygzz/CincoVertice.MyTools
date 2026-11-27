using System.Drawing;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Behaviors;

public class TrayMenuAnchorTests
{
    // A screen right of the main one, as a second monitor is
    private static readonly Rectangle _screen = new(1920, 0, 1920, 1080);
    private static readonly Size _menu = new(250, 580);

    [Theory]
    [InlineData(3810, 1075, 3560, 495)] // the taskbar's corner: above and to the left
    [InlineData(2000, 1075, 2000, 495)] // near the left edge: to the right
    [InlineData(3810, 100, 3560, 100)] // near the top: below
    [InlineData(2000, 100, 2000, 100)] // the top left corner: below and to the right
    [InlineData(1925, 550, 1925, 500)] // room neither above nor below: as low as it fits
    public void PlacedAt_PutsTheCornerAtTheClick_OnTheSideTheScreenHasRoomOn(int x, int y, int left, int top)
    {
        Assert.Equal(new Point(left, top), TrayMenuAnchor.PlacedAt(new Point(x, y), _menu, _screen));
    }

    [Fact]
    public void PlacedAt_AMenuTallerThanTheScreen_StartsAtItsTop()
    {
        Assert.Equal(
            new Point(3560, 0),
            TrayMenuAnchor.PlacedAt(new Point(3810, 1075), new Size(250, 1200), _screen));
    }
}
