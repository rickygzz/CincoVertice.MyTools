using System.Drawing;
using CincoVertice.MyTools.Ui.Zones.Models;
using CincoVertice.MyTools.Ui.Zones.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Zones;

public class ZoneCalculatorTests
{
    private static readonly Rectangle _workArea = new(0, 0, 1000, 600);

    [Fact]
    public void Place_WithoutAGap_SharesTheWorkArea()
    {
        // Act
        IReadOnlyList<Rectangle> zones = ZoneCalculator.Place(
            new ZoneLayoutModel(ZoneLayoutModel.Columns([25, 50, 25]), 0),
            _workArea);

        // Assert
        Assert.Equal(
            [new Rectangle(0, 0, 250, 600), new Rectangle(250, 0, 500, 600), new Rectangle(750, 0, 250, 600)],
            zones);
    }

    [Fact]
    public void Place_LeavesTheGapBetweenZones_AndAtTheEdges()
    {
        // Act
        IReadOnlyList<Rectangle> zones = ZoneCalculator.Place(
            new ZoneLayoutModel(ZoneLayoutModel.Columns(ZoneLayoutModel.Equal(2)), 10),
            new Rectangle(100, 50, 1010, 610));

        // Assert: 10 at every edge and between the two
        Assert.Equal([new Rectangle(110, 60, 490, 590), new Rectangle(610, 60, 490, 590)], zones);
    }

    [Fact]
    public void Place_OfAGrid_GoesRowByRow()
    {
        // Act
        IReadOnlyList<Rectangle> zones = ZoneCalculator.Place(
            new ZoneLayoutModel(ZoneLayoutModel.Grid(ZoneLayoutModel.Equal(2), ZoneLayoutModel.Equal(2)), 0),
            _workArea);

        // Assert
        Assert.Equal(
        [
            new Rectangle(0, 0, 500, 300), new Rectangle(500, 0, 500, 300),
            new Rectangle(0, 300, 500, 300), new Rectangle(500, 300, 500, 300),
        ],
            zones);
    }

    [Fact]
    public void Place_ThirdsOfAnOddWidth_LeaveNoPixelOut()
    {
        // Act
        IReadOnlyList<Rectangle> zones = ZoneCalculator.Place(
            new ZoneLayoutModel(ZoneLayoutModel.Columns(ZoneLayoutModel.Equal(3)), 0),
            new Rectangle(0, 0, 1001, 600));

        // Assert: each starts where the one before ends
        Assert.Equal(0, zones[0].Left);
        Assert.Equal(zones[0].Right, zones[1].Left);
        Assert.Equal(zones[1].Right, zones[2].Left);
        Assert.Equal(1001, zones[2].Right);
    }

    [Theory]
    [InlineData(100, 100, 0)]
    [InlineData(700, 500, 1)]
    [InlineData(504, 300, 1)]
    [InlineData(496, 300, 0)]
    [InlineData(1200, 300, null)]
    public void ZoneAt_IsTheZoneUnderThePoint_GapsIncluded(int x, int y, int? expected)
    {
        // Arrange: a gap of 10 from 495 to 505
        IReadOnlyList<Rectangle> zones = [Rectangle.FromLTRB(5, 5, 495, 595), Rectangle.FromLTRB(505, 5, 995, 595)];

        // Act
        int? zone = ZoneCalculator.ZoneAt(zones, new Point(x, y), 10);

        // Assert
        Assert.Equal(expected, zone);
    }

    [Theory]
    [InlineData(800, 200, 1)]
    [InlineData(800, 500, 1)]
    [InlineData(800, 600, 3)]
    [InlineData(300, 200, 0)]
    [InlineData(300, 700, 2)]
    public void ZoneAt_InZonesThatOverlap_IsTheOneWhoseCenterIsNearest(int x, int y, int expected)
    {
        // Arrange: four zones that overlap, as saved by a user, on a monitor of 1000 x 1000
        IReadOnlyList<Rectangle> zones =
        [
            new Rectangle(0, 0, 635, 722),
            new Rectangle(366, 0, 634, 722),
            new Rectangle(0, 500, 635, 500),
            new Rectangle(366, 363, 634, 637),
        ];

        // Act
        int? zone = ZoneCalculator.ZoneAt(zones, new Point(x, y), 0);

        // Assert: before, the smallest of them was chosen, e.g. zone 4 at (800, 500), in the middle of zone 2
        Assert.Equal(expected, zone);
    }

    [Fact]
    public void ZoneAt_InAZoneInsideAnotherWithTheSameCenter_IsTheSmaller()
    {
        // Arrange
        IReadOnlyList<Rectangle> zones = [new Rectangle(0, 0, 1000, 600), new Rectangle(400, 200, 200, 200)];

        // Act
        int? zone = ZoneCalculator.ZoneAt(zones, new Point(500, 300), 0);

        // Assert
        Assert.Equal(1, zone);
    }

    [Theory]
    [InlineData(null, new[] { 0, 1, 2 })]
    [InlineData(0, new[] { 1, 2, 0 })]
    [InlineData(1, new[] { 0, 2, 1 })]
    [InlineData(5, new[] { 0, 1, 2 })]
    public void DrawingOrder_PutsTheOneOnTopLast(int? onTop, int[] expected)
    {
        Assert.Equal(expected, ZoneCalculator.DrawingOrder(3, onTop));
    }

    [Theory]
    [InlineData(0, 0, 500, 600, 0)]
    [InlineData(502, -3, 497, 604, 1)]
    [InlineData(100, 100, 300, 300, null)]
    public void ZoneOf_IsTheZoneTheWindowFills_ToAFewPixels(int x, int y, int width, int height, int? expected)
    {
        // Arrange
        IReadOnlyList<Rectangle> zones = [new Rectangle(0, 0, 500, 600), new Rectangle(500, 0, 500, 600)];

        // Act
        int? zone = ZoneCalculator.ZoneOf(zones, new Rectangle(x, y, width, height));

        // Assert
        Assert.Equal(expected, zone);
    }

    [Theory]
    [InlineData(0, 516, 900, 516, 2)]
    [InlineData(0, 516, 690, 509, 2)]
    [InlineData(0, 0, 940, 745, 0)]
    public void ZoneOf_AWindowNotTheZonesSize_IsInTheZoneWhoseCornerItsAt(
        int x,
        int y,
        int width,
        int height,
        int expected)
    {
        // Arrange: zones that overlap, as a user saved them; the third is too narrow for some programs, which keep
        // their windows wider, and a terminal sizes its window in rows
        IReadOnlyList<Rectangle> zones =
        [
            new Rectangle(0, 0, 938, 745),
            new Rectangle(703, 0, 1217, 745),
            new Rectangle(0, 516, 703, 516),
            new Rectangle(703, 375, 1217, 657),
        ];

        // Act
        int? zone = ZoneCalculator.ZoneOf(zones, new Rectangle(x, y, width, height));

        // Assert
        Assert.Equal(expected, zone);
    }

    [Fact]
    public void ZoneOf_ZonesWithTheSameCorner_IsTheOneNearestTheWindowsSize()
    {
        // Arrange
        IReadOnlyList<Rectangle> zones = [new Rectangle(0, 0, 1000, 600), new Rectangle(0, 0, 500, 300)];

        // Act and assert
        Assert.Equal(1, ZoneCalculator.ZoneOf(zones, new Rectangle(0, 0, 520, 310)));
        Assert.Equal(0, ZoneCalculator.ZoneOf(zones, new Rectangle(0, 0, 990, 600)));
    }
}
