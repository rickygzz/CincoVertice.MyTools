using System.Drawing;
using System.Drawing.Drawing2D;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using CincoVertice.MyTools.Ui.Win.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Diagramming;

public class GdiTextOutlinerTests
{
    private static readonly DiagramStyleModel _style = new(Color.White, Color.Black, 2, false, Color.Black, 20);

    private readonly GdiTextOutliner _outliner = new();

    private static RectangleF BoundsOf(DiagramFigureModel figure)
    {
        PointF[] points = [.. figure.Segments.SelectMany(segment => segment.Points)];
        float left = points.Min(point => point.X);
        float top = points.Min(point => point.Y);

        return RectangleF.FromLTRB(left, top, points.Max(point => point.X), points.Max(point => point.Y));
    }

    [Fact]
    public void Outline_GivesTheLetters_InOneFigure_WhereTheTextIs()
    {
        // Arrange
        DiagramTextBlockModel block = DiagramTextLayout.LayoutAt("Hello", new PointF(200, 100), _style.FontSize);

        // Act
        DiagramFigureModel letters = Assert.Single(_outliner.Outline(block, _style));

        // Assert: "o" has a hole, so there are more outlines than letters
        Assert.Equal(DiagramPathOperation.Move, letters.Segments[0].Operation);
        Assert.True(letters.Segments.Count(segment => segment.Operation == DiagramPathOperation.Move) > 5);
        Assert.Contains(letters.Segments, segment => segment.Operation == DiagramPathOperation.Close);

        RectangleF bounds = BoundsOf(letters);
        Assert.InRange(bounds.X + bounds.Width / 2, 195, 205);
        Assert.InRange(bounds.Top, block.Top, block.Top + block.LineHeight);
        Assert.InRange(bounds.Bottom, block.Top, block.Top + block.LineHeight);
    }

    [Fact]
    public void Outline_OnlySpaces_GivesNothing()
    {
        // Arrange
        DiagramTextBlockModel block = DiagramTextLayout.LayoutAt("   ", new PointF(0, 0), _style.FontSize);

        // Act / Assert
        Assert.Empty(_outliner.Outline(block, _style));
    }

    [Fact]
    public void SegmentsOf_ReadsGdiPlusPointTypes()
    {
        // Arrange: a triangle, then a curve that ends its figure
        using var path = new GraphicsPath();
        path.AddLines([new PointF(0, 0), new PointF(10, 0), new PointF(10, 10)]);
        path.CloseFigure();
        path.StartFigure();
        path.AddBezier(new PointF(20, 0), new PointF(25, 5), new PointF(30, 5), new PointF(35, 0));
        path.CloseFigure();

        // Act
        IReadOnlyList<DiagramPathSegmentModel> segments = GdiTextOutliner.SegmentsOf(path.PathPoints, path.PathTypes);

        // Assert
        Assert.Equal(
            [
                DiagramPathOperation.Move,
                DiagramPathOperation.Line,
                DiagramPathOperation.Line,
                DiagramPathOperation.Close,
                DiagramPathOperation.Move,
                DiagramPathOperation.Curve,
                DiagramPathOperation.Close,
            ],
            segments.Select(segment => segment.Operation));
        Assert.Equal([new PointF(25, 5), new PointF(30, 5), new PointF(35, 0)], segments[5].Points);
    }
}
