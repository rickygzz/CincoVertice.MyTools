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

    [Theory]
    [InlineData(DiagramTextAlignment.Left)]
    [InlineData(DiagramTextAlignment.Center)]
    [InlineData(DiagramTextAlignment.Right)]
    public void Outline_ABoldPart_IsWider_AndTheLineIsStillWhereItsAlignmentSays(DiagramTextAlignment alignment)
    {
        // Arrange: the same text, then with "Hello" bold
        DiagramTextBlockModel plain =
            DiagramTextLayout.LayoutAt("Hello world", new PointF(200, 100), _style.FontSize, alignment);
        DiagramTextBlockModel bold = DiagramTextLayout.LayoutAt(
            "Hello world",
            new PointF(200, 100),
            _style.FontSize,
            alignment,
            [new DiagramTextSpanModel(0, 5, DiagramTextFormat.Bold)]);

        // Act
        RectangleF plainBounds = BoundsOf(Assert.Single(_outliner.Outline(plain, _style)));
        RectangleF boldBounds = BoundsOf(Assert.Single(_outliner.Outline(bold, _style)));

        // Assert: within a letter's edge, both start, end or have their middle at the same place
        Assert.True(boldBounds.Width > plainBounds.Width + 1);
        float Edge(RectangleF bounds) => alignment switch
        {
            DiagramTextAlignment.Left => bounds.Left,
            DiagramTextAlignment.Right => bounds.Right,
            _ => bounds.X + bounds.Width / 2,
        };
        Assert.InRange(Edge(boldBounds), Edge(plainBounds) - 3, Edge(plainBounds) + 3);
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
