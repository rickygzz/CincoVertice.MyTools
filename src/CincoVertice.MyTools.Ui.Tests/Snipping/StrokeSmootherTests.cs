using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Snipping;

public class StrokeSmootherTests
{
    /// <summary>A level line drawn by a shaky hand: up and down by up to 3 pixels, a point every 2.</summary>
    private static List<Point> ShakyLevelLine()
    {
        var random = new Random(7);
        List<Point> points = [];

        for (int x = 0; x <= 300; x += 2)
        {
            points.Add(new Point(x, 100 + random.Next(-3, 4)));
        }

        return points;
    }

    /// <summary>How far up and down a line goes from one point to the next, added up.</summary>
    private static int Wobble(IReadOnlyList<Point> points)
    {
        int wobble = 0;

        for (int i = 1; i < points.Count; i++)
        {
            wobble += Math.Abs(points[i].Y - points[i - 1].Y);
        }

        return wobble;
    }

    [Fact]
    public void Smooth_EvensOutTheWobble()
    {
        // Arrange
        List<Point> shaky = ShakyLevelLine();

        // Act
        IReadOnlyList<Point> smooth = StrokeSmoother.Smooth(shaky);

        // Assert: much less up and down, and still about where it was drawn
        Assert.True(Wobble(smooth) * 5 < Wobble(shaky), $"{Wobble(smooth)} against {Wobble(shaky)}");
        Assert.All(smooth, point => Assert.InRange(point.Y, 97, 103));
    }

    [Fact]
    public void Smooth_KeepsBothEnds()
    {
        // Arrange
        List<Point> shaky = ShakyLevelLine();

        // Act
        IReadOnlyList<Point> smooth = StrokeSmoother.Smooth(shaky);

        // Assert
        Assert.Equal(shaky[0], smooth[0]);
        Assert.Equal(shaky[^1], smooth[^1]);
    }

    [Fact]
    public void Smooth_LeavesOutThePointsThatAddNothing()
    {
        // Act: a straight line drawn a point at a time
        IReadOnlyList<Point> smooth = StrokeSmoother.Smooth([.. Enumerable.Range(0, 100).Select(i => new Point(i * 3, i))]);

        // Assert
        Assert.Equal([new Point(0, 0), new Point(297, 99)], smooth);
    }

    [Fact]
    public void Smooth_KeepsTheShapeOfACurve()
    {
        // Arrange: a half circle of radius 100, drawn a point every few degrees
        List<Point> arc =
        [
            .. Enumerable.Range(0, 61).Select(i => new Point(
                (int)Math.Round(200 + 100 * Math.Cos(Math.PI * i / 60)),
                (int)Math.Round(200 - 100 * Math.Sin(Math.PI * i / 60)))),
        ];

        // Act
        IReadOnlyList<Point> smooth = StrokeSmoother.Smooth(arc);

        // Assert: every point still on the circle, give or take, with enough of them left to draw it round
        Assert.True(smooth.Count >= 8, $"Only {smooth.Count} points");
        Assert.All(smooth, point =>
            Assert.InRange(Math.Sqrt(Math.Pow(point.X - 200, 2) + Math.Pow(point.Y - 200, 2)), 96, 102));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Smooth_OfADotOrTwoPoints_LeavesThem(int count)
    {
        // Arrange
        List<Point> points = [.. Enumerable.Range(0, count).Select(i => new Point(10 + i * 7, 20))];

        // Act & Assert
        Assert.Equal(points, StrokeSmoother.Smooth(points));
    }

    [Fact]
    public void Smooth_OfAClickThatMovedOnePixel_NeverGivesTwoPointsTheSame()
    {
        // Act
        IReadOnlyList<Point> smooth = StrokeSmoother.Smooth([new Point(5, 5), new Point(5, 6), new Point(5, 5), new Point(6, 5)]);

        // Assert
        for (int i = 1; i < smooth.Count; i++)
        {
            Assert.NotEqual(smooth[i - 1], smooth[i]);
        }
    }
}
