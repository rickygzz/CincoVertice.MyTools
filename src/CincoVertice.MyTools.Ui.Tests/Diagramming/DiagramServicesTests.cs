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
        Assert.Equal(50, block.CenterX);
        Assert.Equal(50 - block.LineHeight / 2, block.Top);
    }
}
