using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diagramming;

public class DiagramServicesTests
{
    private static readonly DiagramStyleModel _style = new(Color.Empty, Color.Black, 2, false, Color.Black, 14);

    private static DiagramShapeModel Shape(int id, int x, int y, DiagramShapeKind kind = DiagramShapeKind.Rectangle) =>
        new(id, kind, new Rectangle(x, y, 100, 50), string.Empty, _style);

    private static DiagramConnectorModel Connector(
        DiagramRouting routing = DiagramRouting.Straight,
        DiagramArrowHeads heads = DiagramArrowHeads.None) =>
        new(10, 1, 2, routing, heads, string.Empty, _style);

    [Theory]
    [InlineData(DiagramShapeKind.Rectangle, 1)]
    [InlineData(DiagramShapeKind.RoundedRectangle, 1)]
    [InlineData(DiagramShapeKind.Ellipse, 1)]
    [InlineData(DiagramShapeKind.Diamond, 1)]
    [InlineData(DiagramShapeKind.Parallelogram, 1)]
    [InlineData(DiagramShapeKind.Hexagon, 1)]
    [InlineData(DiagramShapeKind.Cylinder, 2)]
    [InlineData(DiagramShapeKind.Document, 1)]
    [InlineData(DiagramShapeKind.Text, 0)]
    public void Figures_EveryKindHasItsPaths_StartingWithAMove(DiagramShapeKind kind, int expectedFigures)
    {
        // Act
        IReadOnlyList<DiagramFigureModel> figures = DiagramFigures.Of(kind, new Rectangle(0, 0, 100, 50));

        // Assert
        Assert.Equal(expectedFigures, figures.Count);
        Assert.All(figures, figure => Assert.Equal(DiagramPathOperation.Move, figure.Segments[0].Operation));
    }

    [Fact]
    public void Figures_StayInsideTheShape()
    {
        var bounds = new Rectangle(10, 20, 100, 50);

        foreach (DiagramShapeKind kind in Enum.GetValues<DiagramShapeKind>())
        {
            foreach (DiagramFigureModel figure in DiagramFigures.Of(kind, bounds))
            {
                foreach (PointF point in figure.Segments.SelectMany(segment => segment.Points))
                {
                    // A Bezier curve's control points can be a little outside, the curve itself isn't
                    Assert.InRange(point.X, 10 - 1, 110 + 1);
                    Assert.InRange(point.Y, 20 - 20, 70 + 20);
                }
            }
        }
    }

    [Theory]
    [InlineData(DiagramShapeKind.Rectangle, 300, 25, 100, 25)]
    [InlineData(DiagramShapeKind.RoundedRectangle, 50, 300, 50, 50)]
    [InlineData(DiagramShapeKind.Ellipse, 300, 25, 100, 25)]
    [InlineData(DiagramShapeKind.Diamond, 50, 300, 50, 50)]
    [InlineData(DiagramShapeKind.Hexagon, 300, 25, 100, 25)]
    public void BorderPoint_IsWhereTheLineLeavesTheShape(
        DiagramShapeKind kind,
        float towardX,
        float towardY,
        float expectedX,
        float expectedY)
    {
        // Act
        PointF point = DiagramRouter.BorderPoint(Shape(1, 0, 0, kind), new PointF(towardX, towardY));

        // Assert
        Assert.Equal(expectedX, point.X, 1);
        Assert.Equal(expectedY, point.Y, 1);
    }

    [Fact]
    public void BorderPoint_OfAPointInside_IsThatPoint()
    {
        Assert.Equal(new PointF(60, 30), DiagramRouter.BorderPoint(Shape(1, 0, 0), new PointF(60, 30)));
    }

    [Fact]
    public void Connector_FollowsItsShapes()
    {
        // Arrange
        DiagramModel before = new([Shape(1, 0, 0), Shape(2, 300, 0)], [Connector()]);
        DiagramModel after = before with { Shapes = [Shape(1, 0, 0), Shape(2, 300, 200)] };

        // Act
        DiagramConnectorPathModel first = DiagramRouter.PathOf(before.Connectors[0], before)!;
        DiagramConnectorPathModel second = DiagramRouter.PathOf(after.Connectors[0], after)!;

        // Assert
        Assert.Equal(new PointF(300, 25), first.Points[^1]);
        Assert.NotEqual(first.Points[^1], second.Points[^1]);
    }

    [Fact]
    public void Connector_WithoutOneOfItsShapes_HasNoPath()
    {
        DiagramModel diagram = new([Shape(1, 0, 0)], [Connector()]);

        Assert.Null(DiagramRouter.PathOf(diagram.Connectors[0], diagram));
    }

    [Fact]
    public void Elbow_BetweenShapesSideBySide_GoesThroughTheFacingSidesAndTurnsHalfway()
    {
        // Arrange
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 200)], [Connector(DiagramRouting.Elbow)]);

        // Act
        DiagramConnectorPathModel path = DiagramRouter.PathOf(diagram.Connectors[0], diagram)!;

        // Assert
        Assert.Equal(
            [new PointF(100, 25), new PointF(200, 25), new PointF(200, 225), new PointF(300, 225)],
            path.Points);
    }

    [Fact]
    public void Elbow_BetweenShapesAboveEachOther_GoesThroughTopAndBottom()
    {
        // Arrange
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 20, 200)], [Connector(DiagramRouting.Elbow)]);

        // Act
        DiagramConnectorPathModel path = DiagramRouter.PathOf(diagram.Connectors[0], diagram)!;

        // Assert
        Assert.Equal(
            [new PointF(50, 50), new PointF(50, 125), new PointF(70, 125), new PointF(70, 200)],
            path.Points);
    }

    [Fact]
    public void Elbow_BetweenShapesInLine_IsOneLine()
    {
        // Arrange
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 0)], [Connector(DiagramRouting.Elbow)]);

        // Act
        DiagramConnectorPathModel path = DiagramRouter.PathOf(diagram.Connectors[0], diagram)!;

        // Assert
        Assert.Equal([new PointF(100, 25), new PointF(300, 25)], path.Points);
    }

    [Fact]
    public void Elbow_BetweenOverlappingShapes_IsStraight()
    {
        // Arrange
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 50, 25)], [Connector(DiagramRouting.Elbow)]);

        // Assert
        Assert.Equal(2, DiagramRouter.PathOf(diagram.Connectors[0], diagram)!.Points.Count);
    }

    [Theory]
    [InlineData(DiagramSide.Top, 50, 0)]
    [InlineData(DiagramSide.Right, 100, 25)]
    [InlineData(DiagramSide.Bottom, 50, 50)]
    [InlineData(DiagramSide.Left, 0, 25)]
    public void AnchorPoint_IsTheMiddleOfTheSide(DiagramSide side, float expectedX, float expectedY)
    {
        Assert.Equal(new PointF(expectedX, expectedY), DiagramRouter.AnchorPoint(Shape(1, 0, 0), side));
    }

    [Fact]
    public void AnchorPoint_OfAParallelogram_IsOnItsSlantedSide()
    {
        // Act: the shape's left edge is slanted, so its middle is inside the box
        PointF point = DiagramRouter.AnchorPoint(Shape(1, 0, 0, DiagramShapeKind.Parallelogram), DiagramSide.Left);

        // Assert
        Assert.Equal(12.5f, point.X, 1);
    }

    [Fact]
    public void AnchorAt_FindsTheCircleOutsideTheEdge()
    {
        // Assert: the right side's middle is at 100, 25, and its circle 14 further
        Assert.Equal(DiagramSide.Right, DiagramHitTester.AnchorAt(Shape(1, 0, 0), new Point(115, 26), 14, 8));
        Assert.Null(DiagramHitTester.AnchorAt(Shape(1, 0, 0), new Point(50, 25), 14, 8));
    }

    [Fact]
    public void Connector_WithSides_LeavesAndArrivesThere_EvenIfOthersWouldBeCloser()
    {
        // Arrange: the second shape is to the right, but the connector leaves from the top and arrives at the bottom
        DiagramConnectorModel connector = Connector(DiagramRouting.Elbow) with
        {
            FromSide = DiagramSide.Top,
            ToSide = DiagramSide.Bottom,
        };
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 0)], [connector]);

        // Act
        IReadOnlyList<PointF> points = DiagramRouter.PathOf(connector, diagram)!.Points;

        // Assert: straight out of each side first, and around the outside
        Assert.Equal(new PointF(50, 0), points[0]);
        Assert.Equal(50, points[1].X);
        Assert.True(points[1].Y < 0);
        Assert.Equal(new PointF(350, 50), points[^1]);
        Assert.Equal(350, points[^2].X);
        Assert.True(points[^2].Y > 50);
        AssertOnlyLevelOrUpright(points);
    }

    [Theory]
    [InlineData(DiagramSide.Right, DiagramSide.Left)]
    [InlineData(DiagramSide.Right, DiagramSide.Right)]
    [InlineData(DiagramSide.Left, DiagramSide.Right)]
    [InlineData(DiagramSide.Left, DiagramSide.Left)]
    [InlineData(DiagramSide.Top, DiagramSide.Bottom)]
    [InlineData(DiagramSide.Top, DiagramSide.Top)]
    [InlineData(DiagramSide.Bottom, DiagramSide.Top)]
    [InlineData(DiagramSide.Bottom, DiagramSide.Bottom)]
    [InlineData(DiagramSide.Right, DiagramSide.Top)]
    [InlineData(DiagramSide.Right, DiagramSide.Bottom)]
    [InlineData(DiagramSide.Left, DiagramSide.Top)]
    [InlineData(DiagramSide.Left, DiagramSide.Bottom)]
    [InlineData(DiagramSide.Top, DiagramSide.Left)]
    [InlineData(DiagramSide.Top, DiagramSide.Right)]
    [InlineData(DiagramSide.Bottom, DiagramSide.Left)]
    [InlineData(DiagramSide.Bottom, DiagramSide.Right)]
    public void Elbow_BetweenAnyTwoSides_IsLevelAndUpright_AndLeavesStraightOut(DiagramSide from, DiagramSide to)
    {
        // Four places, so the other shape is on every side of the first in turn
        foreach ((int x, int y) in new[] { (300, 200), (-300, 200), (300, -200), (-300, -200), (20, 10) })
        {
            // Arrange
            DiagramConnectorModel connector = Connector(DiagramRouting.Elbow) with { FromSide = from, ToSide = to };
            DiagramShapeModel first = Shape(1, 0, 0);
            DiagramShapeModel second = Shape(2, x, y);
            DiagramModel diagram = new([first, second], [connector]);

            // Act
            IReadOnlyList<PointF> points = DiagramRouter.PathOf(connector, diagram)!.Points;

            // Assert
            Assert.Equal(DiagramRouter.AnchorPoint(first, from), points[0]);
            Assert.Equal(DiagramRouter.AnchorPoint(second, to), points[^1]);
            AssertOnlyLevelOrUpright(points);
            AssertHeadsOut(points[0], points[1], from);
            AssertHeadsOut(points[^1], points[^2], to);
        }
    }

    [Fact]
    public void Connector_WithOnlyTheStartSide_PicksTheOtherSide()
    {
        // Arrange
        DiagramConnectorModel connector = Connector(DiagramRouting.Elbow) with { FromSide = DiagramSide.Bottom };
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 200)], [connector]);

        // Act
        IReadOnlyList<PointF> points = DiagramRouter.PathOf(connector, diagram)!.Points;

        // Assert
        Assert.Equal(new PointF(50, 50), points[0]);
        Assert.Equal(new PointF(300, 225), points[^1]);
    }

    [Theory]
    [InlineData(DiagramSide.Top, DiagramSide.Top, 50, 0, 350, 0)]
    [InlineData(null, DiagramSide.Left, 100, 25, 300, 25)]
    [InlineData(DiagramSide.Bottom, DiagramSide.Bottom, 50, 50, 350, 50)]
    public void Straight_WithSides_GoesThroughThem(
        DiagramSide? from,
        DiagramSide? to,
        float startX,
        float startY,
        float endX,
        float endY)
    {
        // Arrange
        DiagramConnectorModel connector = Connector() with { FromSide = from, ToSide = to };
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 0)], [connector]);

        // Act
        IReadOnlyList<PointF> points = DiagramRouter.PathOf(connector, diagram)!.Points;

        // Assert
        Assert.Equal(new PointF(startX, startY), points[0]);
        Assert.Equal(new PointF(endX, endY), points[^1]);
    }

    private static void AssertOnlyLevelOrUpright(IReadOnlyList<PointF> points)
    {
        for (int i = 1; i < points.Count; i++)
        {
            Assert.True(points[i].X == points[i - 1].X || points[i].Y == points[i - 1].Y);
        }
    }

    private static void AssertHeadsOut(PointF anchor, PointF next, DiagramSide side)
    {
        (int x, int y) = DiagramRouter.DirectionOf(side);

        Assert.True((next.X - anchor.X) * x + (next.Y - anchor.Y) * y > 0, $"{anchor} to {next} isn't out of {side}");
    }

    [Fact]
    public void Bends_TheLineGoesThroughThem_LeavingTowardTheNearest()
    {
        // Arrange
        DiagramConnectorModel connector = Connector(DiagramRouting.Elbow) with
        {
            Bends = [new Point(200, 25), new Point(200, 300)],
        };
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 275)], [connector]);

        // Act
        IReadOnlyList<PointF> points = DiagramRouter.PathOf(connector, diagram)!.Points;

        // Assert
        Assert.Equal(
            [new PointF(100, 25), new PointF(200, 25), new PointF(200, 300), new PointF(300, 300)],
            points);
    }

    [Theory]
    [InlineData(DiagramSide.Top, DiagramSide.Bottom)]
    [InlineData(DiagramSide.Right, DiagramSide.Left)]
    [InlineData(DiagramSide.Left, DiagramSide.Top)]
    [InlineData(null, null)]
    public void Bends_ThatDontLineUp_GetATurn_SoTheLineIsNeverSlanted(DiagramSide? from, DiagramSide? to)
    {
        // Arrange: slanted between them, as free bends in an older file could be
        DiagramConnectorModel connector = Connector(DiagramRouting.Elbow) with
        {
            FromSide = from,
            ToSide = to,
            Bends = [new Point(150, 170), new Point(230, 90), new Point(260, 310)],
        };
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 275)], [connector]);

        // Act
        IReadOnlyList<PointF> points = DiagramRouter.PathOf(connector, diagram)!.Points;

        // Assert
        AssertOnlyLevelOrUpright(points);
    }

    [Fact]
    public void Bends_ThatWouldGoPastACornerAndBack_LeaveNoSpike()
    {
        // Arrange: the turn between the first two is at 360, 105, which the line would reach only after 390, 105
        DiagramConnectorModel connector = Connector(DiagramRouting.Elbow) with
        {
            Bends = [new Point(390, 105), new Point(360, 260), new Point(390, 415)],
        };
        DiagramShapeModel first = new(1, DiagramShapeKind.Rectangle, new Rectangle(30, 60, 160, 90), string.Empty, _style);
        DiagramShapeModel second = new(2, DiagramShapeKind.Rectangle, new Rectangle(600, 370, 200, 95), string.Empty, _style);
        DiagramModel diagram = new([first, second], [connector]);

        // Act
        IReadOnlyList<PointF> points = DiagramRouter.PathOf(connector, diagram)!.Points;

        // Assert
        AssertOnlyLevelOrUpright(points);
        Assert.Equal(new PointF(360, 105), points[1]);
        Assert.DoesNotContain(new PointF(390, 105), points);
    }

    [Fact]
    public void Bends_AHalfPixelOffACorner_NeedNoExtraTurn()
    {
        // Arrange: a shape 51 high has its middle at 25.5
        DiagramShapeModel odd = new(1, DiagramShapeKind.Rectangle, new Rectangle(0, 0, 100, 51), string.Empty, _style);
        DiagramConnectorModel connector = Connector(DiagramRouting.Elbow) with { Bends = [new Point(200, 25), new Point(200, 300)] };
        DiagramModel diagram = new([odd, Shape(2, 300, 275)], [connector]);

        // Act
        IReadOnlyList<PointF> points = DiagramRouter.PathOf(connector, diagram)!.Points;

        // Assert
        Assert.Equal(4, points.Count);
        AssertOnlyLevelOrUpright(points);
    }

    [Fact]
    public void Bends_AnEmptyList_WorksOutItsOwnCorners()
    {
        // Arrange
        DiagramConnectorModel connector = Connector(DiagramRouting.Elbow) with { Bends = [] };
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 0)], [connector]);

        // Assert
        Assert.Equal(2, DiagramRouter.PathOf(connector, diagram)!.Points.Count);
    }

    [Fact]
    public void Bends_OnAStraightConnector_AreIgnored()
    {
        // Arrange
        DiagramConnectorModel connector = Connector() with { Bends = [new Point(200, 300)] };
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 0)], [connector]);

        // Assert
        Assert.Equal(2, DiagramRouter.PathOf(connector, diagram)!.Points.Count);
    }

    [Fact]
    public void SegmentAt_FindsThePieceNear()
    {
        PointF[] line = [new(0, 0), new(100, 0), new(100, 100)];

        Assert.Equal(0, DiagramHitTester.SegmentAt(line, new Point(50, 3), 6));
        Assert.Equal(1, DiagramHitTester.SegmentAt(line, new Point(97, 50), 6));
        Assert.Null(DiagramHitTester.SegmentAt(line, new Point(50, 50), 6));
    }

    [Fact]
    public void WithSegmentMoved_AMiddlePiece_MovesItsBothEnds_OnlySideways()
    {
        // Arrange
        PointF[] route = [new(0, 0), new(100, 0), new(100, 200), new(300, 200)];

        // Act
        List<PointF> moved = DiagramRouter.WithSegmentMoved(route, 1, 150);

        // Assert
        Assert.Equal([new PointF(0, 0), new PointF(150, 0), new PointF(150, 200), new PointF(300, 200)], moved);
    }

    [Fact]
    public void WithSegmentMoved_TheFirstPiece_KeepsAStub()
    {
        // Arrange
        PointF[] route = [new(0, 0), new(100, 0), new(100, 200)];

        // Act
        List<PointF> moved = DiagramRouter.WithSegmentMoved(route, 0, 50);

        // Assert
        Assert.Equal(
            [new PointF(0, 0), new PointF(20, 0), new PointF(20, 50), new PointF(100, 50), new PointF(100, 200)],
            moved);
    }

    [Fact]
    public void WithSegmentMoved_TheOnlyPiece_KeepsAStubAtBothEnds()
    {
        // Arrange: upright this time
        PointF[] route = [new(0, 0), new(0, 300)];

        // Act
        List<PointF> moved = DiagramRouter.WithSegmentMoved(route, 0, 80);

        // Assert
        Assert.Equal(
            [new PointF(0, 0), new PointF(0, 20), new PointF(80, 20), new PointF(80, 280), new PointF(0, 280), new PointF(0, 300)],
            moved);
    }

    [Fact]
    public void WithSegmentMoved_OntoTheNextPiece_LeavesNoExtraCorners()
    {
        // Arrange
        PointF[] route = [new(0, 0), new(100, 0), new(100, 200), new(300, 200), new(300, 400)];

        // Act: the piece at x = 100 slid to x = 300, in line with the last piece
        List<PointF> moved = DiagramRouter.WithSegmentMoved(route, 1, 300);

        // Assert
        Assert.Equal([new PointF(0, 0), new PointF(300, 0), new PointF(300, 400)], moved);
    }

    [Fact]
    public void WithSegmentMoved_OnAShortPiece_UsesAShorterStub()
    {
        // Arrange
        PointF[] route = [new(0, 0), new(30, 0)];

        // Act
        List<PointF> moved = DiagramRouter.WithSegmentMoved(route, 0, 50);

        // Assert
        Assert.Equal(new PointF(10, 0), moved[1]);
        Assert.Equal(new PointF(20, 0), moved[^2]);
    }

    [Theory]
    [InlineData(DiagramShapeKind.Rectangle, null, false)]
    [InlineData(DiagramShapeKind.Rectangle, 0, false)]
    [InlineData(DiagramShapeKind.Rectangle, 10, true)]
    [InlineData(DiagramShapeKind.RoundedRectangle, null, true)]
    [InlineData(DiagramShapeKind.RoundedRectangle, 10, true)]
    [InlineData(DiagramShapeKind.RoundedRectangle, 0, false)]
    public void Figures_ARectanglesCornersAreRoundOnlyWithARadius(DiagramShapeKind kind, int? radius, bool hasCurves)
    {
        // Act
        DiagramFigureModel figure = DiagramFigures.Of(kind, new Rectangle(0, 0, 100, 50), radius)[0];

        // Assert
        Assert.Equal(hasCurves, figure.Segments.Any(segment => segment.Operation == DiagramPathOperation.Curve));
    }

    [Fact]
    public void Figures_ARadiusTooLarge_MakesAPill_StayingInsideTheShape()
    {
        // Act
        DiagramFigureModel figure = DiagramFigures.Of(DiagramShapeKind.Rectangle, new Rectangle(0, 0, 100, 50), 500)[0];

        // Assert: half the height
        Assert.Equal(new PointF(25, 0), figure.Segments[0].Points[0]);
        Assert.All(
            figure.Segments.SelectMany(segment => segment.Points),
            point =>
            {
                Assert.InRange(point.X, 0, 100);
                Assert.InRange(point.Y, 0, 50);
            });
    }

    [Fact]
    public void RoundedLine_HasACurveAtEveryCorner_AndKeepsItsEnds()
    {
        // Act
        DiagramFigureModel line = DiagramFigures.RoundedLine(
            [new PointF(0, 0), new PointF(100, 0), new PointF(100, 100), new PointF(200, 100)],
            10);

        // Assert
        Assert.Equal(
            [
                DiagramPathOperation.Move,
                DiagramPathOperation.Line,
                DiagramPathOperation.Curve,
                DiagramPathOperation.Line,
                DiagramPathOperation.Curve,
                DiagramPathOperation.Line,
            ],
            line.Segments.Select(segment => segment.Operation));
        Assert.False(line.Filled);
        Assert.Equal(new PointF(0, 0), line.Segments[0].Points[0]);
        Assert.Equal(new PointF(90, 0), line.Segments[1].Points[0]);
        Assert.Equal(new PointF(100, 10), line.Segments[2].Points[2]);
        Assert.Equal(new PointF(200, 100), line.Segments[^1].Points[0]);
    }

    [Fact]
    public void RoundedLine_OnAShortPiece_UsesAtMostHalfOfIt()
    {
        // Act: the piece down is only 8 long
        DiagramFigureModel line = DiagramFigures.RoundedLine(
            [new PointF(0, 0), new PointF(100, 0), new PointF(100, 8), new PointF(200, 8)],
            10);

        // Assert
        Assert.Equal(new PointF(96, 0), line.Segments[1].Points[0]);
        Assert.Equal(new PointF(100, 4), line.Segments[2].Points[2]);
        Assert.Equal(new PointF(100, 4), line.Segments[3].Points[0]);
    }

    [Fact]
    public void RoundedLine_WithoutCorners_IsOneLine()
    {
        // Act
        DiagramFigureModel line = DiagramFigures.RoundedLine([new PointF(0, 0), new PointF(100, 0)], 10);

        // Assert
        Assert.Equal(
            [DiagramPathOperation.Move, DiagramPathOperation.Line],
            line.Segments.Select(segment => segment.Operation));
    }

    [Fact]
    public void Connector_LineIsRoundedAtItsTurns_AndHitTestingStillWorks()
    {
        // Arrange
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 200)], [Connector(DiagramRouting.Elbow)]);

        // Act
        DiagramConnectorPathModel path = DiagramRouter.PathOf(diagram.Connectors[0], diagram)!;

        // Assert
        Assert.Equal(2, path.Line.Segments.Count(segment => segment.Operation == DiagramPathOperation.Curve));
        Assert.NotNull(DiagramHitTester.ConnectorAt(diagram, new Point(200, 125), 6));
    }

    [Theory]
    [InlineData(DiagramArrowHeads.None, false, false)]
    [InlineData(DiagramArrowHeads.End, false, true)]
    [InlineData(DiagramArrowHeads.Start, true, false)]
    [InlineData(DiagramArrowHeads.Both, true, true)]
    public void Heads_AreAtTheEndsAskedFor(DiagramArrowHeads heads, bool hasStart, bool hasEnd)
    {
        // Arrange
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 0)], [Connector(heads: heads)]);

        // Act
        DiagramConnectorPathModel path = DiagramRouter.PathOf(diagram.Connectors[0], diagram)!;

        // Assert
        Assert.Equal(hasStart, path.StartHead is not null);
        Assert.Equal(hasEnd, path.EndHead is not null);
    }

    [Fact]
    public void Head_HasItsTipOnTheShape_AndTheLineStopsAtItsBase()
    {
        // Arrange
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 0)], [Connector(heads: DiagramArrowHeads.End)]);

        // Act
        DiagramConnectorPathModel path = DiagramRouter.PathOf(diagram.Connectors[0], diagram)!;

        // Assert
        Assert.Equal(new PointF(300, 25), path.EndHead![0]);
        Assert.True(path.Points[^1].X < 300);
    }

    [Fact]
    public void Label_IsHalfwayAlongTheLine()
    {
        // Arrange
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 0)], [Connector()]);

        // Assert
        Assert.Equal(new PointF(200, 25), DiagramRouter.PathOf(diagram.Connectors[0], diagram)!.LabelCenter);
    }

    [Fact]
    public void HitTest_FindsTheShapeOnTop()
    {
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 50, 0)], []);

        Assert.Equal(2, DiagramHitTester.ShapeAt(diagram, new Point(60, 10))!.Id);
        Assert.Equal(1, DiagramHitTester.ShapeAt(diagram, new Point(10, 10))!.Id);
        Assert.Null(DiagramHitTester.ShapeAt(diagram, new Point(500, 500)));
    }

    [Fact]
    public void HitTest_FindsAConnectorNearItsLine()
    {
        DiagramModel diagram = new([Shape(1, 0, 0), Shape(2, 300, 0)], [Connector()]);

        Assert.NotNull(DiagramHitTester.ConnectorAt(diagram, new Point(200, 28), 6));
        Assert.Null(DiagramHitTester.ConnectorAt(diagram, new Point(200, 60), 6));
    }

    [Theory]
    [InlineData(0, 0, DiagramHandle.TopLeft)]
    [InlineData(50, 0, DiagramHandle.Top)]
    [InlineData(100, 50, DiagramHandle.BottomRight)]
    [InlineData(103, 22, DiagramHandle.Right)]
    public void HitTest_FindsTheHandle(int x, int y, DiagramHandle expected)
    {
        Assert.Equal(expected, DiagramHitTester.HandleAt(new Rectangle(0, 0, 100, 50), new Point(x, y), 6));
    }

    [Fact]
    public void HitTest_InTheMiddleOfTheBox_IsNoHandle()
    {
        Assert.Null(DiagramHitTester.HandleAt(new Rectangle(0, 0, 100, 50), new Point(30, 20), 6));
    }

    [Fact]
    public void SnapBox_LinesUpWithTheEdgeOfAnotherShape_AndShowsAGuide()
    {
        // Act: its left is 3 away from the other's left
        DiagramSnapModel snap = DiagramSnapper.SnapBox(
            new Rectangle(103, 200, 50, 50),
            [new Rectangle(100, 0, 80, 40)],
            tolerance: 6);

        // Assert
        Assert.Equal(-3, snap.DeltaX);
        Assert.Equal(new DiagramGuideModel(new Point(100, 0), new Point(100, 250)), Assert.Single(snap.Guides));
    }

    [Fact]
    public void SnapBox_LinesUpMiddles()
    {
        // Act: its middle is 2 away from the other's
        DiagramSnapModel snap = DiagramSnapper.SnapBox(
            new Rectangle(303, 22, 50, 50),
            [new Rectangle(0, 0, 100, 100)],
            tolerance: 6);

        // Assert
        Assert.Equal(3, snap.DeltaY);
    }

    [Fact]
    public void SnapBox_WithNothingNear_GoesToTheGrid()
    {
        // Act
        DiagramSnapModel snap = DiagramSnapper.SnapBox(new Rectangle(13, 27, 50, 50), [], tolerance: 6);

        // Assert
        Assert.Equal((-3, 3), (snap.DeltaX, snap.DeltaY));
        Assert.Empty(snap.Guides);
    }

    [Fact]
    public void SnapPoint_LinesUpWithAnotherShape()
    {
        // Act
        DiagramSnapModel snap = DiagramSnapper.SnapPoint(new Point(102, 500), [new Rectangle(100, 0, 80, 40)], 6);

        // Assert
        Assert.Equal(-2, snap.DeltaX);
        Assert.Contains(snap.Guides, guide => guide.From.X == 100 && guide.To.X == 100);
    }

    [Theory]
    [InlineData("short", 500, 1)]
    [InlineData("one two three four", 40, 4)]
    [InlineData("line one\nline two", 500, 2)]
    [InlineData("averyveryverylongwordwithoutspaces", 40, 7)]
    [InlineData("", 100, 1)]
    public void Wrap_BreaksTheLinesThatDontFit(string text, float width, int expectedLines)
    {
        Assert.Equal(expectedLines, DiagramTextLayout.Wrap(text, width, 14).Count);
    }

    private static List<PointF> RouteBetween(DiagramConnectorModel connector, Rectangle from, Rectangle to)
    {
        DiagramModel diagram = new(
            [
                new DiagramShapeModel(1, DiagramShapeKind.Rectangle, from, string.Empty, _style),
                new DiagramShapeModel(2, DiagramShapeKind.Rectangle, to, string.Empty, _style),
            ],
            [connector]);

        return DiagramRouter.RouteFor(connector, diagram)!;
    }

    private static bool CrossesInside(IReadOnlyList<PointF> route, Rectangle box)
    {
        for (int i = 1; i < route.Count; i++)
        {
            float left = Math.Min(route[i - 1].X, route[i].X);
            float right = Math.Max(route[i - 1].X, route[i].X);
            float top = Math.Min(route[i - 1].Y, route[i].Y);
            float bottom = Math.Max(route[i - 1].Y, route[i].Y);

            if (left < box.Right && right > box.Left && top < box.Bottom && bottom > box.Top)
            {
                return true;
            }
        }

        return false;
    }

    [Theory]
    [InlineData(150, 25)] // Straight behind it, as in a shape moved past its bend
    [InlineData(150, 40)] // Behind it, not lined up
    [InlineData(150, -50)] // Behind it and above
    [InlineData(300, -50)] // Right above where it leaves
    public void Bends_BehindTheSideItLeaves_GoOutFirst_AndAroundTheShape(int x, int y)
    {
        // Arrange
        var from = new Rectangle(200, 0, 100, 50);
        var to = new Rectangle(600, 300, 100, 50);
        DiagramConnectorModel connector = Connector(DiagramRouting.Elbow) with
        {
            FromSide = DiagramSide.Right,
            Bends = [new Point(x, y), new Point(x, 500)],
        };

        // Act
        List<PointF> route = RouteBetween(connector, from, to);

        // Assert
        Assert.Equal(new PointF(300, 25), route[0]);
        Assert.Equal(25, route[1].Y);
        Assert.True(route[1].X > 300);
        Assert.False(CrossesInside(route, from));
        Assert.All(route.Skip(1), (point, i) => Assert.True(DiagramRouter.IsLevelOrUpright(route[i], point)));
    }

    [Theory]
    [InlineData(750, 325)]
    [InlineData(750, 310)]
    [InlineData(750, 500)]
    [InlineData(600, 500)]
    public void Bends_BehindTheSideItArrivesAt_ComeInFromOutside_AndAroundTheShape(int x, int y)
    {
        // Arrange
        var from = new Rectangle(0, 0, 100, 50);
        var to = new Rectangle(600, 300, 100, 50);
        DiagramConnectorModel connector = Connector(DiagramRouting.Elbow) with
        {
            ToSide = DiagramSide.Left,
            Bends = [new Point(x, -100), new Point(x, y)],
        };

        // Act
        List<PointF> route = RouteBetween(connector, from, to);

        // Assert
        Assert.Equal(new PointF(600, 325), route[^1]);
        Assert.Equal(325, route[^2].Y);
        Assert.True(route[^2].X < 600);
        Assert.False(CrossesInside(route, to));
        Assert.All(route.Skip(1), (point, i) => Assert.True(DiagramRouter.IsLevelOrUpright(route[i], point)));
    }

    [Theory]
    [InlineData(DiagramSide.Top, 225, 90)]
    [InlineData(DiagramSide.Bottom, 240, -40)]
    [InlineData(DiagramSide.Left, 350, 25)]
    public void Bends_BehindAnySide_LeaveThroughThatSide(DiagramSide side, int x, int y)
    {
        // Arrange
        var from = new Rectangle(200, 0, 100, 50);
        var to = new Rectangle(600, 300, 100, 50);
        DiagramConnectorModel connector = Connector(DiagramRouting.Elbow) with
        {
            FromSide = side,
            Bends = [new Point(x, y), new Point(600, y)],
        };

        // Act
        List<PointF> route = RouteBetween(connector, from, to);

        // Assert
        (int outX, int outY) = DiagramRouter.DirectionOf(side);

        Assert.True((route[1].X - route[0].X) * outX + (route[1].Y - route[0].Y) * outY > 0);
        Assert.False(CrossesInside(route, from));
    }

    [Fact]
    public void Bends_AGoingAround_SavedAsCorners_GivesTheSameLine()
    {
        // Arrange: sliding a piece keeps the corners of the line as it is
        var from = new Rectangle(200, 0, 100, 50);
        var to = new Rectangle(600, 300, 100, 50);
        DiagramConnectorModel connector = Connector(DiagramRouting.Elbow) with
        {
            FromSide = DiagramSide.Right,
            Bends = [new Point(150, 25), new Point(150, 500)],
        };
        List<PointF> route = RouteBetween(connector, from, to);

        // Act
        List<PointF> again = RouteBetween(
            connector with { Bends = [.. route.Skip(1).SkipLast(1).Select(Point.Round)] },
            from,
            to);

        // Assert
        Assert.Equal(route, again);
    }

    [Fact]
    public void Bends_InsideTheShape_DontGoAround()
    {
        // Arrange: nothing to go around to: the line ends up inside anyway
        var from = new Rectangle(200, 0, 100, 50);
        var to = new Rectangle(600, 300, 100, 50);
        DiagramConnectorModel connector = Connector(DiagramRouting.Elbow) with
        {
            FromSide = DiagramSide.Right,
            Bends = [new Point(250, 25), new Point(250, 500)],
        };

        // Act
        List<PointF> route = RouteBetween(connector, from, to);

        // Assert
        Assert.Equal(new PointF(250, 25), route[1]);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(60)]
    public void Heads_BothOnAShortLine_BothHaveADirection(int gap)
    {
        // Arrange
        DiagramModel diagram = new(
            [
                new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(0, 0, 100, 50), string.Empty, _style),
                new DiagramShapeModel(2, DiagramShapeKind.Rectangle, new Rectangle(100 + gap, 0, 100, 50), string.Empty, _style),
            ],
            []);

        // Act
        DiagramConnectorPathModel path = DiagramRouter.PathOf(Connector(heads: DiagramArrowHeads.Both), diagram)!;

        // Assert: each points at its shape, and they don't cross
        Assert.Equal(new PointF(100, 25), path.StartHead![0]);
        Assert.Equal(new PointF(100 + gap, 25), path.EndHead![0]);
        Assert.True(path.StartHead[1].X <= path.EndHead[1].X);
    }

    [Theory]
    [InlineData("""{"format":"mytools-diagram","shapes":[null]}""")]
    [InlineData("""{"format":"mytools-diagram","connectors":[null]}""")]
    [InlineData("""{"format":"mytools-diagram","connectors":[{"id":1,"bends":[null]}]}""")]
    [InlineData("""{"format":"mytools-diagram","shapes":[{"id":1,"kind":99}]}""")]
    [InlineData("""{"format":"mytools-diagram","connectors":[{"id":1,"heads":99}]}""")]
    [InlineData("""{"format":"mytools-diagram","connectors":[{"id":1,"routing":99}]}""")]
    [InlineData("""{"format":"mytools-diagram","connectors":[{"id":1,"fromSide":99}]}""")]
    [InlineData("""{"format":"mytools-diagram","connectors":[{"id":1,"toSide":99}]}""")]
    public void Json_AFileWithBadEntries_IsntADiagram(string json)
    {
        Assert.Throws<InvalidDataException>(() => DiagramJson.Read(json));
    }

    [Fact]
    public void Json_AllTheValuesThatAreAllowed_AreRead()
    {
        foreach (DiagramArrowHeads heads in Enum.GetValues<DiagramArrowHeads>())
        {
            DiagramModel diagram = new(
                [Shape(1, 0, 0), Shape(2, 300, 0)],
                [Connector(DiagramRouting.Elbow, heads) with { FromSide = DiagramSide.Top, Bends = [new Point(5, 5)] }]);

            Assert.Equal(heads, DiagramJson.Read(DiagramJson.Write(diagram)).Connectors[0].Heads);
        }
    }

    [Fact]
    public void Json_ALoneSurrogate_IsEitherReadOrRefused()
    {
        // Half an emoji in a file edited by hand: Open only handles InvalidDataException
        const string json = """{"format":"mytools-diagram","shapes":[{"id":1,"kind":"Rectangle","text":"a\uD83D"}]}""";

        try
        {
            DiagramJson.Read(json);
        }
        catch (InvalidDataException)
        {
            // Refused is fine too
        }
    }

    [Theory]
    [InlineData("ab\U0001F600cd", 16.5f, 10)]
    [InlineData("\U0001F600\U0001F600\U0001F600", 16.5f, 10)]
    [InlineData("a\U0001F600b", 5.5f, 10)]
    [InlineData("\U0001F600", 5.5f, 10)]
    public void Wrap_KeepsSurrogatePairsTogether(string text, float width, int fontSize)
    {
        // Act
        IReadOnlyList<string> lines = DiagramTextLayout.Wrap(text, width, fontSize);

        // Assert: half an emoji can't be written to an SVG file. And no blank line after the last one
        foreach (string line in lines)
        {
            Assert.DoesNotContain(line.EnumerateRunes(), rune => rune == System.Text.Rune.ReplacementChar);
            Assert.NotEmpty(line);
        }

        Assert.Equal(text, string.Concat(lines));
    }

    [Theory]
    [InlineData("A  B", 1000f, new[] { "A  B" })]
    [InlineData("  A", 1000f, new[] { "  A" })]
    [InlineData("A\n   B", 1000f, new[] { "A", "   B" })]
    [InlineData("A  ", 1000f, new[] { "A" })] // Spaces at the end only move a centered line
    [InlineData("   ", 1000f, new[] { "" })]
    [InlineData("aaa  bbb", 16.5f, new[] { "aaa", "bbb" })] // None at a break
    [InlineData("a  b  c", 27.5f, new[] { "a  b", "c" })]
    public void Wrap_KeepsTheSpacesTyped(string text, float width, string[] expected)
    {
        Assert.Equal(expected, DiagramTextLayout.Wrap(text, width, 10));
    }

    [Fact]
    public void Wrap_NeverLosesWords()
    {
        // Act
        IReadOnlyList<string> lines = DiagramTextLayout.Wrap("one two three four five six", 50, 14);

        // Assert
        Assert.Equal("one two three four five six", string.Join(' ', lines));
    }

    [Fact]
    public void Layout_CentersTheTextInTheArea()
    {
        // Act
        DiagramTextBlockModel block = DiagramTextLayout.Layout("a", new RectangleF(0, 0, 100, 100), 20);

        // Assert
        Assert.Equal(50, block.X);
        Assert.Equal(50 - block.LineHeight / 2, block.Top);
    }

    [Theory]
    [InlineData(DiagramTextAlignment.Left, 10, 10)]
    [InlineData(DiagramTextAlignment.Center, 60, 49)]
    [InlineData(DiagramTextAlignment.Right, 110, 88)]
    public void Layout_PutsTheLinesWhereTheAlignmentSays(DiagramTextAlignment alignment, float x, float left)
    {
        // Act: 2 letters of 20 pixels are about 22 wide
        DiagramTextBlockModel block = DiagramTextLayout.Layout("ab", new RectangleF(10, 0, 100, 40), 20, alignment);

        // Assert
        Assert.Equal((x, alignment), (block.X, block.Alignment));
        Assert.Equal(left, block.Bounds.Left);
    }

    [Theory]
    [InlineData(DiagramTextAlignment.Left, 89)]
    [InlineData(DiagramTextAlignment.Center, 100)]
    [InlineData(DiagramTextAlignment.Right, 111)]
    public void LayoutAt_TheLinesLineUpAtTheSideChosen_OfABlockCenteredOnThePoint(
        DiagramTextAlignment alignment,
        float x)
    {
        // Act: the longer line, "ab", is about 22 wide
        DiagramTextBlockModel block = DiagramTextLayout.LayoutAt("ab\na", new PointF(100, 50), 20, alignment);

        // Assert
        Assert.Equal(x, block.X);
        Assert.Equal(89, block.Bounds.Left);
    }

    [Theory]
    [InlineData("", 14, 32)]
    [InlineData("Resource group", 14, 32)]
    [InlineData("Resource group", 32, 52)]
    [InlineData("A title long enough to take two lines in a narrow box", 14, 47)]
    public void ContainerTitle_IsAsHighAsItsLines_AndNeverLowerThan32(string title, int fontSize, float height)
    {
        // Arrange
        DiagramShapeModel container = new(
            1,
            DiagramShapeKind.Container,
            new Rectangle(0, 0, 300, 200),
            title,
            _style with { FontSize = fontSize });

        // Act
        RectangleF band = DiagramFigures.ContainerTitle(container);
        DiagramFigureModel line = DiagramFigures.Of(container)[^1];

        // Assert: the line under the band is at its bottom, and the text in the middle of it
        Assert.Equal(height, band.Height);
        Assert.Equal(height, line.Segments[0].Points[0].Y);
        Assert.Equal(height / 2, DiagramTextLayout.OfShape(container).Bounds.Top
            + DiagramTextLayout.OfShape(container).Bounds.Height / 2, 3);
    }

    private static DiagramShapeModel Container(Color titleFill, int? top = null, int? bottom = null) => new(
        1,
        DiagramShapeKind.Container,
        new Rectangle(0, 0, 300, 200),
        string.Empty,
        _style with { Fill = Color.Blue, TitleFill = titleFill },
        CornerRadius: top,
        BottomCornerRadius: bottom);

    [Fact]
    public void Container_ATitleFillOfItsOwnOrNone_LeavesTheInsideFillBelowTheBand()
    {
        // Act
        IReadOnlyList<DiagramFigureModel> same = DiagramFigures.Of(Container(Color.Empty));
        IReadOnlyList<DiagramFigureModel> none = DiagramFigures.Of(Container(DiagramStyleModel.NoTitleFill));
        IReadOnlyList<DiagramFigureModel> own = DiagramFigures.Of(Container(Color.Red));

        // Assert: the inside starts at the top, or under the 32 of the band
        Assert.Equal(0, same[0].Segments[0].Points[0].Y);
        Assert.Equal(32, none[0].Segments[0].Points[0].Y);
        Assert.Equal(32, own[0].Segments[0].Points[0].Y);
        Assert.Equal(Color.Blue, DiagramFigures.FillOf(same[1], Container(Color.Empty).Style));
        Assert.Equal(Color.Empty, DiagramFigures.FillOf(none[1], Container(DiagramStyleModel.NoTitleFill).Style));
        Assert.Equal(Color.Red, DiagramFigures.FillOf(own[1], Container(Color.Red).Style));
    }

    [Theory]
    [InlineData(10, null, 4)]
    [InlineData(10, 0, 2)]
    [InlineData(0, 10, 2)]
    [InlineData(0, null, 0)]
    public void Corners_TopAndBottom_AreRoundedEachAsTheirRadiusSays(int top, int? bottom, int curves)
    {
        // Arrange
        DiagramShapeModel rectangle = new(
            1,
            DiagramShapeKind.Rectangle,
            new Rectangle(0, 0, 100, 50),
            string.Empty,
            _style,
            CornerRadius: top,
            BottomCornerRadius: bottom);

        // Act
        DiagramFigureModel outline = Assert.Single(DiagramFigures.Of(rectangle));

        // Assert
        Assert.Equal(curves, outline.Segments.Count(segment => segment.Operation == DiagramPathOperation.Curve));
    }

    [Fact]
    public void Container_ItsTopCorners_AreNoRounderThanItsBandIsHigh()
    {
        // Act: a radius of 80, on a band of 32
        DiagramFigureModel border = DiagramFigures.Of(Container(Color.Red, top: 80, bottom: 0))[2];

        // Assert: the border starts after the top left corner, 32 from the left
        Assert.Equal(new PointF(32, 0), border.Segments[0].Points[0]);
    }

    [Fact]
    public void ContainerTitle_IsNeverHigherThanTheContainer()
    {
        // Arrange
        DiagramShapeModel container = new(
            1,
            DiagramShapeKind.Container,
            new Rectangle(0, 0, 300, 40),
            "Big",
            _style with { FontSize = 60 });

        // Act / Assert
        Assert.Equal(40, DiagramFigures.ContainerTitle(container).Height);
    }

    [Fact]
    public void HitTest_AContainersTallerTitle_PicksIt()
    {
        // Arrange
        DiagramShapeModel container = new(
            1,
            DiagramShapeKind.Container,
            new Rectangle(0, 0, 300, 200),
            "Resource group",
            _style with { FontSize = 32 });
        var diagram = new DiagramModel([container], []);

        // Act / Assert: 45 is below a title of 14 pixels, but in this one
        Assert.Same(container, DiagramHitTester.ShapeAt(diagram, new Point(150, 45)));
        Assert.Null(DiagramHitTester.ShapeAt(diagram, new Point(150, 100)));
    }
}
