using System.ComponentModel;
using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Presenters;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using CincoVertice.MyTools.Ui.Diagramming.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diagramming;

public class DiagramEditorPresenterTests
{
    private readonly FakeView _view = new();
    private readonly FakeStore _store = new();
    private readonly FakeSettingsStore _settings = new();
    private readonly DiagramEditorPresenter _presenter;

    public DiagramEditorPresenterTests()
    {
        _presenter = new DiagramEditorPresenter(_view, _store, _settings);
        _presenter.Start();
    }

    private DiagramModel Diagram => _presenter.Diagram;

    private static Rectangle Box(int x, int y, int width, int height) => new(x, y, width, height);

    /// <summary>Drags a new rectangle out, which leaves the select tool on and the rectangle selected.</summary>
    private DiagramShapeModel AddShape(int x, int y, int width = 100, int height = 50)
    {
        _view.Tool(DiagramTool.Rectangle);
        _view.Drag(x, y, x + width, y + height);

        return Diagram.Shapes[^1];
    }

    private void Connect(DiagramShapeModel from, DiagramShapeModel to)
    {
        _view.Tool(DiagramTool.Connector);
        _view.Drag(from.Bounds.X + 5, from.Bounds.Y + 5, to.Bounds.X + 5, to.Bounds.Y + 5);
        _view.Tool(DiagramTool.Select);
    }

    private void ClickEmpty() => _view.Click(5000, 5000);

    [Fact]
    public void Start_ShowsTheChoicesAndAnEmptyDiagram()
    {
        Assert.NotEmpty(_view.Palette);
        Assert.Equal(DiagramTool.Select, _view.ShownTool);
        Assert.Empty(_view.Scene!.Diagram.Shapes);
        Assert.Equal(DiagramEditorPresenter.UntitledName, _view.DocumentName);
        Assert.False(_view.HasChanges);
    }

    [Theory]
    [InlineData(DiagramTool.Rectangle, DiagramShapeKind.Rectangle)]
    [InlineData(DiagramTool.RoundedRectangle, DiagramShapeKind.RoundedRectangle)]
    [InlineData(DiagramTool.Ellipse, DiagramShapeKind.Ellipse)]
    [InlineData(DiagramTool.Diamond, DiagramShapeKind.Diamond)]
    [InlineData(DiagramTool.Parallelogram, DiagramShapeKind.Parallelogram)]
    [InlineData(DiagramTool.Hexagon, DiagramShapeKind.Hexagon)]
    [InlineData(DiagramTool.Cylinder, DiagramShapeKind.Cylinder)]
    [InlineData(DiagramTool.Document, DiagramShapeKind.Document)]
    public void ShapeTool_DragMakesThatShape_SelectsIt_AndGoesBackToSelect(DiagramTool tool, DiagramShapeKind kind)
    {
        // Act
        _view.Tool(tool);
        _view.Drag(100, 100, 300, 200);

        // Assert
        DiagramShapeModel shape = Assert.Single(Diagram.Shapes);
        Assert.Equal(kind, shape.Kind);
        Assert.Equal(Box(100, 100, 200, 100), shape.Bounds);
        Assert.Equal([shape.Id], _presenter.SelectedShapeIds);
        Assert.Equal(DiagramTool.Select, _view.ShownTool);
    }

    [Fact]
    public void ShapeTool_DraggedUpAndLeft_IsTheSameShape()
    {
        // Act
        _view.Tool(DiagramTool.Rectangle);
        _view.Drag(300, 200, 100, 100);

        // Assert
        Assert.Equal(Box(100, 100, 200, 100), Diagram.Shapes[0].Bounds);
    }

    [Fact]
    public void ShapeTool_AClick_MakesTheUsualSizeAroundThePoint()
    {
        // Act
        _view.Tool(DiagramTool.Rectangle);
        _view.Click(200, 200);

        // Assert
        Assert.Equal(Box(140, 170, 120, 60), Diagram.Shapes[0].Bounds);
    }

    [Fact]
    public void Drag_ShowsAPreview_ButChangesTheDiagramOnlyAtTheEnd()
    {
        // Act
        _view.Tool(DiagramTool.Rectangle);
        _view.Press(100, 100);
        _view.Move(300, 200);

        // Assert
        Assert.Empty(Diagram.Shapes);
        Assert.Single(_view.Scene!.Diagram.Shapes);

        // Act
        _view.Release(300, 200);

        // Assert
        Assert.Single(Diagram.Shapes);
    }

    [Fact]
    public void Select_ClickOnAShape_SelectsIt_AndOnNothing_ClearsIt()
    {
        // Arrange
        DiagramShapeModel shape = AddShape(100, 100);
        ClickEmpty();

        // Act & Assert
        _view.Click(120, 120);
        Assert.Equal([shape.Id], _presenter.SelectedShapeIds);
        Assert.Equal([shape.Id], _view.Scene!.SelectedShapeIds);
        Assert.Equal(shape.Bounds, _view.Scene.HandleBounds);

        ClickEmpty();
        Assert.Empty(_presenter.SelectedShapeIds);
        Assert.Null(_view.Scene.HandleBounds);
    }

    [Fact]
    public void Select_ShiftClick_AddsAndRemoves()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);
        ClickEmpty();

        // Act & Assert
        _view.Click(120, 120, shift: true);
        _view.Click(420, 120, shift: true);
        Assert.Equal(2, _presenter.SelectedShapeIds.Count);

        _view.Click(120, 120, shift: true);
        Assert.Equal([b.Id], _presenter.SelectedShapeIds);
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Select_DraggingOnNothing_SelectsWhatsFullyInside()
    {
        // Arrange
        DiagramShapeModel inside = AddShape(100, 100);
        AddShape(400, 100);
        ClickEmpty();

        // Act
        _view.Press(50, 50);
        _view.Move(350, 300);
        Assert.Equal(Box(50, 50, 300, 250), _view.Scene!.SelectionBand);
        _view.Release(350, 300);

        // Assert
        Assert.Equal([inside.Id], _presenter.SelectedShapeIds);
        Assert.Null(_view.Scene!.SelectionBand);
    }

    [Fact]
    public void Move_DraggingAShape_MovesIt_AndSnapsToTheGrid()
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.Drag(120, 120, 223, 237);

        // Assert
        Assert.Equal(Box(200, 220, 100, 50), Diagram.Shapes[0].Bounds);
    }

    [Fact]
    public void Move_WithAlt_DoesntSnap()
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.Drag(120, 120, 223, 237, alt: true);

        // Assert
        Assert.Equal(Box(203, 217, 100, 50), Diagram.Shapes[0].Bounds);
    }

    [Fact]
    public void Move_NearAnotherShape_LinesUpAndShowsAGuide()
    {
        // Arrange: a shape off the grid, so only lining up explains where the other one lands
        DiagramShapeModel other = AddShape(100, 100);
        _view.Drag(120, 120, 123, 123, alt: true);
        DiagramShapeModel moving = AddShape(400, 300);

        // Act: its left goes 2 away from the other's left
        _view.Press(420, 320);
        _view.Move(125, 320);

        // Assert
        Assert.NotEmpty(_view.Scene!.Guides);
        Assert.Equal(
            Diagram.Shapes.First(s => s.Id == other.Id).Bounds.Left,
            _view.Scene.Diagram.Shapes.First(s => s.Id == moving.Id).Bounds.Left);

        // Act
        _view.Release(125, 320);

        // Assert
        Assert.Empty(_view.Scene!.Guides);
    }

    [Fact]
    public void Move_ASmallMovement_IsStillAClick()
    {
        // Arrange
        AddShape(103, 103);
        DiagramModel before = Diagram;

        // Act
        _view.Drag(120, 120, 121, 120);

        // Assert
        Assert.Same(before, Diagram);
    }

    [Fact]
    public void Move_BackToWhereItWas_HasNothingToUndo()
    {
        // Arrange
        AddShape(100, 100);
        DiagramModel before = Diagram;

        // Act
        _view.Press(120, 120);
        _view.Move(200, 200);
        _view.Release(120, 120);

        // Assert
        Assert.Same(before, Diagram);
    }

    [Fact]
    public void Move_DraggingOneOfSeveralSelected_MovesThemAll()
    {
        // Arrange
        AddShape(100, 100);
        AddShape(400, 100);
        _view.Command(DiagramCommand.SelectAll);

        // Act
        _view.Drag(120, 120, 120, 220);

        // Assert
        Assert.Equal([200, 200], Diagram.Shapes.Select(shape => shape.Bounds.Top));
    }

    [Theory]
    [InlineData(300, 150)]
    [InlineData(150, 170)]
    public void Scale_TheBottomRightHandle_ChangesTheSize(int toX, int toY)
    {
        // Arrange: from 100, 100 to 200, 150
        DiagramShapeModel shape = AddShape(100, 100);

        // Act
        _view.Drag(shape.Bounds.Right, shape.Bounds.Bottom, toX, toY);

        // Assert
        Assert.Equal(Box(100, 100, toX - 100, toY - 100), Diagram.Shapes[0].Bounds);
    }

    [Fact]
    public void Scale_ATopHandle_OnlyChangesTheHeight()
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.Drag(150, 100, 999, 50);

        // Assert
        Assert.Equal(Box(100, 50, 100, 100), Diagram.Shapes[0].Bounds);
    }

    [Fact]
    public void Scale_PastTheOppositeSide_StopsAtTheSmallestSize()
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.Drag(200, 150, 0, 0);

        // Assert
        Assert.Equal(Box(100, 100, 10, 10), Diagram.Shapes[0].Bounds);
    }

    [Fact]
    public void Scale_WithShift_KeepsTheProportions()
    {
        // Arrange
        AddShape(100, 100, 100, 50);

        // Act
        _view.Drag(200, 150, 300, 170, shift: true);

        // Assert
        Assert.Equal(Box(100, 100, 200, 100), Diagram.Shapes[0].Bounds);
    }

    [Fact]
    public void Scale_SeveralShapes_ScalesThemTogether()
    {
        // Arrange
        AddShape(100, 100, 100, 100);
        AddShape(300, 100, 100, 100);
        _view.Command(DiagramCommand.SelectAll);

        // Act: the box from 100..400 becomes 100..700
        _view.Drag(400, 200, 700, 200);

        // Assert
        Assert.Equal(Box(100, 100, 200, 100), Diagram.Shapes[0].Bounds);
        Assert.Equal(Box(500, 100, 200, 100), Diagram.Shapes[1].Bounds);
    }

    [Fact]
    public void Connector_DragFromOneShapeToAnother_AttachesToBoth_AndStaysOnTheTool()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);

        // Act
        _view.Tool(DiagramTool.Connector);
        _view.Press(110, 110);
        _view.Move(420, 120);
        Assert.Equal(b.Id, _view.Scene!.ConnectTargetId);
        Assert.NotNull(_view.Scene.DraftConnector);
        _view.Release(420, 120);

        // Assert
        DiagramConnectorModel connector = Assert.Single(Diagram.Connectors);
        Assert.Equal((a.Id, b.Id), (connector.FromShapeId, connector.ToShapeId));
        Assert.Equal(DiagramTool.Connector, _view.ShownTool);
        Assert.Null(_view.Scene!.DraftConnector);
    }

    [Fact]
    public void Hover_OnAShape_ShowsItsFourCircles_AndOffItHidesThem()
    {
        // Arrange
        AddShape(100, 100);
        ClickEmpty();

        // Act & Assert
        _view.Hover(150, 125);
        Assert.Equal(
            [new PointF(150, 86), new PointF(214, 125), new PointF(150, 164), new PointF(86, 125)],
            _view.Scene!.Anchors!.Points);
        Assert.Null(_view.Scene.Anchors.Highlighted);

        _view.Hover(900, 900);
        Assert.Null(_view.Scene!.Anchors);
    }

    [Fact]
    public void Hover_JustOutsideTheShape_KeepsTheCircles_SoTheMouseCanReachThem()
    {
        // Arrange
        AddShape(100, 100);
        ClickEmpty();

        // Act: from the shape to its right circle, across the gap
        _view.Hover(150, 125);
        _view.Hover(205, 125);

        // Assert
        Assert.NotNull(_view.Scene!.Anchors);
    }

    [Fact]
    public void Hover_OnACircle_HighlightsIt_AndShowsTheCrossCursor()
    {
        // Arrange
        AddShape(100, 100);
        ClickEmpty();

        // Act
        _view.Hover(214, 125);

        // Assert
        Assert.Equal(new PointF(214, 125), _view.Scene!.Anchors!.Highlighted);
        Assert.Equal(DiagramCursor.Cross, _view.Cursor);
    }

    [Fact]
    public void Hover_LeavingTheCanvas_HidesTheCircles()
    {
        // Arrange
        AddShape(100, 100);
        _view.Hover(150, 125);

        // Act
        _view.LeaveCanvas();

        // Assert
        Assert.Null(_view.Scene!.Anchors);
    }

    [Fact]
    public void Hover_WithAShapeTool_ShowsNoCircles()
    {
        // Arrange
        AddShape(100, 100);
        _view.Tool(DiagramTool.Ellipse);

        // Act
        _view.Hover(150, 125);

        // Assert
        Assert.Null(_view.Scene!.Anchors);
    }

    [Fact]
    public void Hover_ScalesTheCirclesDistanceWithTheZoom()
    {
        // Arrange: at 200%, the right circle stays 14 canvas pixels from the edge
        AddShape(100, 100);
        ClickEmpty();
        _view.Wheel(0, 0, MathF.Log(2) / MathF.Log(1.1f), control: true);

        // Act
        _view.Hover(300, 250);

        // Assert
        Assert.Equal(207, _view.Scene!.Anchors!.Points[1].X, 0.5f);
    }

    [Fact]
    public void DragFromACircle_ToAShape_MakesAConnectorLeavingThatSide_AndStaysOnSelect()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 300);
        ClickEmpty();

        // Act: from the top circle of the first
        _view.Hover(150, 125);
        _view.Drag(150, 86, 450, 325);

        // Assert
        DiagramConnectorModel connector = Assert.Single(Diagram.Connectors);
        Assert.Equal((a.Id, b.Id), (connector.FromShapeId, connector.ToShapeId));
        Assert.Equal(DiagramSide.Top, connector.FromSide);
        Assert.Null(connector.ToSide);
        Assert.Equal(DiagramTool.Select, _view.ShownTool);
        Assert.Equal(new Point(100, 100), Diagram.Shapes[0].Bounds.Location);
    }

    [Fact]
    public void DragFromACircle_ShowsTheDraftFromThatSide_AndTheTargetsCircles()
    {
        // Arrange
        AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 300);
        ClickEmpty();

        // Act
        _view.Press(214, 125);
        _view.Move(450, 325);

        // Assert
        Assert.Equal((new Point(200, 125), new Point(450, 325)), _view.Scene!.DraftConnector);
        Assert.Equal(b.Id, _view.Scene.ConnectTargetId);
        Assert.Equal(4, _view.Scene.Anchors!.Points.Count);
    }

    [Theory]
    [InlineData(386, 325, DiagramSide.Left)]
    [InlineData(400, 325, DiagramSide.Left)]
    [InlineData(450, 286, DiagramSide.Top)]
    [InlineData(514, 325, DiagramSide.Right)]
    public void DragFromACircle_DroppedOnACircleOrEdgeMiddle_ArrivesAtThatSide(int x, int y, DiagramSide expected)
    {
        // Arrange: the second shape's left middle is at 400, 325
        AddShape(100, 100);
        AddShape(400, 300);
        ClickEmpty();

        // Act
        _view.Drag(214, 125, x, y);

        // Assert
        Assert.Equal(expected, Assert.Single(Diagram.Connectors).ToSide);
    }

    [Fact]
    public void DragFromACircle_DroppedOnASide_SnapsTheDraftAndHighlightsIt()
    {
        // Arrange
        AddShape(100, 100);
        AddShape(400, 300);
        ClickEmpty();

        // Act
        _view.Press(214, 125);
        _view.Move(386, 325);

        // Assert
        Assert.Equal(new Point(400, 325), _view.Scene!.DraftConnector!.Value.To);
        Assert.Equal(new PointF(386, 325), _view.Scene.Anchors!.Highlighted);
    }

    [Theory]
    [InlineData(150, 125)]
    [InlineData(900, 900)]
    public void DragFromACircle_NotEndingOnAnotherShape_AddsNothing(int x, int y)
    {
        // Arrange
        AddShape(100, 100);
        ClickEmpty();

        // Act: back onto its own shape, or onto nothing
        _view.Drag(214, 125, x, y);

        // Assert
        Assert.Empty(Diagram.Connectors);
        Assert.False(_view.CanRedo);
    }

    [Fact]
    public void DragFromACircle_OnASelectedShape_ConnectsInsteadOfScaling()
    {
        // Arrange: the shape stays selected, with its handles
        AddShape(100, 100);
        DiagramShapeModel a = Diagram.Shapes[0];
        AddShape(400, 300);
        ClickEmpty();
        _view.Click(150, 125);

        // Act
        _view.Drag(214, 125, 450, 325);

        // Assert
        Assert.Equal(a.Bounds, Diagram.Shapes[0].Bounds);
        Assert.Single(Diagram.Connectors);
    }

    [Fact]
    public void Scale_OnASelectedShape_StillWorksOnTheEdgeMiddle()
    {
        // Arrange: the handle at the right middle is at 200, 125, and the circle at 214, 125
        AddShape(100, 100);

        // Act
        _view.Drag(200, 125, 300, 125);

        // Assert
        Assert.Equal(Box(100, 100, 200, 50), Diagram.Shapes[0].Bounds);
        Assert.Empty(Diagram.Connectors);
    }

    /// <summary>
    ///     Shapes at 100, 100 and 400, 100, and a third at 400, 300, each 100 by 50, and a selected connector from the
    ///     first to the second: its start at 200, 125 and the tip of its head at 400, 125.
    /// </summary>
    private (DiagramShapeModel A, DiagramShapeModel B, DiagramShapeModel C) AddSelectedConnector()
    {
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);
        DiagramShapeModel c = AddShape(400, 300);

        Connect(a, b);
        ClickEmpty();
        _view.Click(300, 125);

        return (a, b, c);
    }

    [Fact]
    public void ConnectorEnds_AreShownOnlyWhileItsSelected()
    {
        // Act
        AddSelectedConnector();

        // Assert
        Assert.Equal([new PointF(200, 125), new PointF(400, 125)], _view.Scene!.ConnectorEnds!);

        // Act
        ClickEmpty();

        // Assert
        Assert.Empty(_view.Scene!.ConnectorEnds!);
    }

    [Fact]
    public void ConnectorEnd_ShowsTheCrossCursor()
    {
        // Arrange
        AddSelectedConnector();

        // Act
        _view.Hover(400, 125);

        // Assert
        Assert.Equal(DiagramCursor.Cross, _view.Cursor);
    }

    [Fact]
    public void ConnectorEnd_DraggedToAnotherShape_AttachesThere()
    {
        // Arrange
        (DiagramShapeModel a, _, DiagramShapeModel c) = AddSelectedConnector();

        // Act
        _view.Drag(400, 125, 450, 325);

        // Assert
        DiagramConnectorModel connector = Assert.Single(Diagram.Connectors);
        Assert.Equal((a.Id, c.Id), (connector.FromShapeId, connector.ToShapeId));
        Assert.Null(connector.ToSide);
        Assert.Equal(DiagramTool.Select, _view.ShownTool);
    }

    [Fact]
    public void ConnectorStart_DraggedToAnotherShape_AttachesThere()
    {
        // Arrange
        (_, DiagramShapeModel b, DiagramShapeModel c) = AddSelectedConnector();

        // Act
        _view.Drag(200, 125, 450, 325);

        // Assert
        DiagramConnectorModel connector = Assert.Single(Diagram.Connectors);
        Assert.Equal((c.Id, b.Id), (connector.FromShapeId, connector.ToShapeId));
    }

    [Theory]
    [InlineData(450, 286, DiagramSide.Top)]
    [InlineData(514, 325, DiagramSide.Right)]
    [InlineData(450, 350, DiagramSide.Bottom)]
    public void ConnectorEnd_DroppedOnACircleOrEdgeMiddle_ArrivesAtThatSide(int x, int y, DiagramSide expected)
    {
        // Arrange
        AddSelectedConnector();

        // Act
        _view.Drag(400, 125, x, y);

        // Assert
        Assert.Equal(expected, Assert.Single(Diagram.Connectors).ToSide);
    }

    [Fact]
    public void ConnectorEnd_DroppedOnASideOfItsOwnShape_ChangesTheSide_AndOnItsMiddle_PicksItAgain()
    {
        // Arrange
        AddSelectedConnector();

        // Act: from the left side to the bottom one
        _view.Drag(400, 125, 450, 164);

        // Assert
        Assert.Equal(DiagramSide.Bottom, Diagram.Connectors[0].ToSide);

        // Act: the tip is at the bottom middle now
        _view.Drag(450, 150, 450, 125);

        // Assert
        Assert.Null(Diagram.Connectors[0].ToSide);
    }

    [Fact]
    public void ConnectorEnd_WhileDragged_ShowsTheConnectorAttached_OrALineToTheMouse()
    {
        // Arrange
        (_, _, DiagramShapeModel c) = AddSelectedConnector();

        // Act: over nothing
        _view.Press(400, 125);
        _view.Move(700, 500);

        // Assert
        Assert.Empty(_view.Scene!.Diagram.Connectors);
        Assert.Equal((new Point(200, 125), new Point(700, 500)), _view.Scene.DraftConnector);

        // Act: over the third shape
        _view.Move(450, 325);

        // Assert
        Assert.Equal(c.Id, Assert.Single(_view.Scene!.Diagram.Connectors).ToShapeId);
        Assert.Null(_view.Scene.DraftConnector);
        Assert.Equal(c.Id, _view.Scene.ConnectTargetId);
        Assert.NotNull(_view.Scene.Anchors);

        // And the diagram itself only changes at the end
        Assert.NotEqual(c.Id, Assert.Single(Diagram.Connectors).ToShapeId);
    }

    [Theory]
    [InlineData(900, 900)]
    [InlineData(150, 125)]
    public void ConnectorEnd_DroppedOnNothing_OrOnItsOtherEndsShape_StaysAsItWas(int x, int y)
    {
        // Arrange
        AddSelectedConnector();
        DiagramModel before = Diagram;

        // Act
        _view.Drag(400, 125, x, y);

        // Assert
        Assert.Same(before, Diagram);
        Assert.Single(_view.Scene!.Diagram.Connectors);
    }

    [Fact]
    public void ConnectorEnd_DroppedOnAnotherPartOfItsShape_HasNothingToUndo()
    {
        // Arrange
        AddSelectedConnector();
        DiagramModel before = Diagram;

        // Act: anywhere inside the shape it's already on
        _view.Drag(400, 125, 450, 125);

        // Assert
        Assert.Same(before, Diagram);
    }

    [Fact]
    public void ConnectorEnd_AClick_ChangesNothing()
    {
        // Arrange
        AddSelectedConnector();
        DiagramModel before = Diagram;

        // Act
        _view.Click(400, 125);

        // Assert
        Assert.Same(before, Diagram);
        Assert.NotEmpty(_presenter.SelectedConnectorIds);
    }

    [Fact]
    public void ConnectorEnd_Cancel_LeavesItAsItWas()
    {
        // Arrange
        AddSelectedConnector();
        DiagramModel before = Diagram;

        // Act
        _view.Press(400, 125);
        _view.Move(450, 325);
        _view.Command(DiagramCommand.Cancel);

        // Assert
        Assert.Same(before, Diagram);
        Assert.Same(before, _view.Scene!.Diagram);
    }

    [Fact]
    public void ConnectorEnd_Undo_PutsItBack()
    {
        // Arrange
        (_, DiagramShapeModel b, _) = AddSelectedConnector();
        _view.Drag(400, 125, 450, 325);

        // Act
        _view.Command(DiagramCommand.Undo);

        // Assert
        Assert.Equal(b.Id, Diagram.Connectors[0].ToShapeId);
    }

    [Fact]
    public void ConnectorEnd_KeepsTheLabelAndStyle()
    {
        // Arrange
        AddSelectedConnector();
        _view.SelectRouting(DiagramRouting.Straight);
        _view.SelectLineWidth(5);
        DiagramConnectorModel before = Diagram.Connectors[0];

        // Act
        _view.Drag(400, 125, 450, 325);

        // Assert
        Assert.Equal((before.Routing, before.Style), (Diagram.Connectors[0].Routing, Diagram.Connectors[0].Style));
    }

    [Fact]
    public void ConnectorEnd_OfAnUnselectedConnector_CantBeDragged()
    {
        // Arrange
        (DiagramShapeModel a, _, _) = AddSelectedConnector();
        ClickEmpty();

        // Act: this presses the second shape's body instead, and moves it
        _view.Drag(405, 125, 405, 325);

        // Assert
        Assert.Equal(a.Id, Diagram.Connectors[0].FromShapeId);
        Assert.NotEqual(100, Diagram.Shapes[1].Bounds.Y);
    }

    private void AssertLineIsLevelAndUpright(DiagramModel diagram)
    {
        foreach (DiagramConnectorModel connector in diagram.Connectors)
        {
            IReadOnlyList<PointF> route = DiagramRouter.RouteFor(connector, diagram)!;

            for (int i = 1; i < route.Count; i++)
            {
                Assert.True(DiagramRouter.IsLevelOrUpright(route[i - 1], route[i]), $"{route[i - 1]} to {route[i]}");
            }
        }
    }

    /// <summary>
    ///     An elbow from the first shape at 100, 100 to the one at 400, 300, selected: across from 200, 125 to 300, down
    ///     to 325, and across to 400.
    /// </summary>
    private void AddSelectedElbowWithCorners()
    {
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel c = AddShape(400, 300);

        Connect(a, c);
        ClickEmpty();
        _view.Click(300, 200);
    }

    [Fact]
    public void Segment_ThePieceOfAStraightLookingLine_StepsAside_WithStubsAtBothShapes_ShownAndSnapped()
    {
        // Arrange: from 200, 125 to 400, 125
        AddSelectedConnector();

        // Act
        _view.Press(300, 125);
        _view.Move(303, 202);

        // Assert: a preview, on the grid, and only sideways
        Assert.Equal(
            [new Point(220, 125), new Point(220, 200), new Point(380, 200), new Point(380, 125)],
            _view.Scene!.Diagram.Connectors[0].Bends!);
        Assert.Null(Diagram.Connectors[0].Bends);

        // Act
        _view.Release(303, 202);

        // Assert
        Assert.Equal(4, Diagram.Connectors[0].Bends!.Count);
        AssertLineIsLevelAndUpright(Diagram);
        Assert.NotEmpty(_presenter.SelectedConnectorIds);
    }

    [Fact]
    public void Segment_NeverMakesASlantedLine_WhereverItsDragged()
    {
        // Arrange
        AddSelectedElbowWithCorners();

        // Act: far off in both directions, which is what made a slanted line with free bends
        _view.Drag(300, 200, 150, 480);

        // Assert
        AssertLineIsLevelAndUpright(Diagram);
        Assert.Equal([new Point(150, 125), new Point(150, 325)], Diagram.Connectors[0].Bends!);
    }

    [Fact]
    public void Segment_WithAlt_DoesntSnap()
    {
        // Arrange
        AddSelectedElbowWithCorners();

        // Act
        _view.Drag(300, 200, 253, 200, alt: true);

        // Assert
        Assert.Equal([new Point(253, 125), new Point(253, 325)], Diagram.Connectors[0].Bends!);
    }

    [Fact]
    public void Segment_NearAShape_LinesUpWithIt_AndShowsAGuide()
    {
        // Arrange
        AddSelectedElbowWithCorners();

        // Act: 3 away from the first shape's left edge
        _view.Press(300, 200);
        _view.Move(103, 400);

        // Assert
        Assert.Equal(100, _view.Scene!.Diagram.Connectors[0].Bends![0].X);
        Assert.All(_view.Scene.Guides, guide => Assert.Equal(guide.From.X, guide.To.X));
        Assert.NotEmpty(_view.Scene.Guides);
    }

    [Fact]
    public void Segment_ThePieceLeavingAShape_KeepsAStubThere()
    {
        // Arrange
        AddSelectedElbowWithCorners();

        // Act
        _view.Drag(250, 125, 250, 160);

        // Assert
        Assert.Equal(
            [new Point(220, 125), new Point(220, 160), new Point(300, 160), new Point(300, 325)],
            Diagram.Connectors[0].Bends!);
        AssertLineIsLevelAndUpright(Diagram);
    }

    [Fact]
    public void Segment_ThePieceArrivingAtAShape_KeepsAStubThere()
    {
        // Arrange
        AddSelectedElbowWithCorners();

        // Act
        _view.Drag(350, 325, 350, 300);

        // Assert
        Assert.Equal(
            [new Point(300, 125), new Point(300, 300), new Point(380, 300), new Point(380, 325)],
            Diagram.Connectors[0].Bends!);
        AssertLineIsLevelAndUpright(Diagram);
    }

    [Fact]
    public void Segment_CanBeDraggedAgain()
    {
        // Arrange
        AddSelectedElbowWithCorners();
        _view.Drag(300, 200, 250, 200);

        // Act
        _view.Drag(250, 200, 350, 200);

        // Assert
        Assert.Equal([new Point(350, 125), new Point(350, 325)], Diagram.Connectors[0].Bends!);
    }

    [Fact]
    public void Segment_AClick_ChangesNothing()
    {
        // Arrange
        AddSelectedConnector();
        DiagramModel before = Diagram;

        // Act
        _view.Click(300, 125);

        // Assert
        Assert.Same(before, Diagram);
        Assert.NotEmpty(_presenter.SelectedConnectorIds);
    }

    [Fact]
    public void Segment_BackToWhereItWas_HasNothingToUndo()
    {
        // Arrange
        AddSelectedElbowWithCorners();
        DiagramModel before = Diagram;

        // Act
        _view.Press(300, 200);
        _view.Move(400, 200);
        _view.Release(300, 200);

        // Assert
        Assert.Same(before, Diagram);
    }

    [Fact]
    public void Segment_OnAnUnselectedConnector_OnlySelectsIt()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);
        Connect(a, b);
        ClickEmpty();

        // Act
        _view.Drag(300, 125, 300, 200);

        // Assert
        Assert.Null(Diagram.Connectors[0].Bends);
        Assert.NotEmpty(_presenter.SelectedConnectorIds);
    }

    [Fact]
    public void Segment_OfAStraightConnector_CantBeMoved()
    {
        // Arrange
        AddSelectedConnector();
        _view.SelectRouting(DiagramRouting.Straight);

        // Act
        _view.Drag(300, 125, 300, 200);

        // Assert
        Assert.Null(Diagram.Connectors[0].Bends);
    }

    [Fact]
    public void Segment_DoubleClickOnTheLine_StillEditsTheLabel()
    {
        // Arrange
        AddSelectedElbowWithCorners();

        // Act
        _view.DoubleClick(300, 200);
        _view.CommitText("yes");

        // Assert
        Assert.Equal("yes", Diagram.Connectors[0].Label);
        Assert.Null(Diagram.Connectors[0].Bends);
    }

    [Fact]
    public void Segment_UndoAndCancel_PutItBack()
    {
        // Arrange
        AddSelectedConnector();
        DiagramModel before = Diagram;

        // Act & Assert
        _view.Press(300, 125);
        _view.Move(300, 200);
        _view.Command(DiagramCommand.Cancel);
        Assert.Same(before, Diagram);
        Assert.Null(_view.Scene!.Diagram.Connectors[0].Bends);

        _view.Drag(300, 125, 300, 200);
        _view.Command(DiagramCommand.Undo);
        Assert.Null(Diagram.Connectors[0].Bends);
    }

    [Fact]
    public void Segment_ShiftOnTheLine_TakesItOffTheSelectionInstead()
    {
        // Arrange
        AddSelectedConnector();

        // Act
        _view.Drag(300, 125, 300, 200, shift: true);

        // Assert
        Assert.Null(Diagram.Connectors[0].Bends);
        Assert.Empty(_presenter.SelectedConnectorIds);
    }

    [Fact]
    public void Segment_ShowsTheCursorForTheWayItMoves()
    {
        // Arrange
        AddSelectedElbowWithCorners();

        // Act & Assert: the upright piece moves sideways, the level one up and down
        _view.Hover(300, 200);
        Assert.Equal(DiagramCursor.SizeHorizontal, _view.Cursor);

        _view.Hover(250, 125);
        Assert.Equal(DiagramCursor.SizeVertical, _view.Cursor);
    }

    [Fact]
    public void Segment_MovedThenAShapeMoves_StaysLevelAndUpright()
    {
        // Arrange
        AddSelectedElbowWithCorners();
        _view.Drag(300, 200, 250, 200);
        DiagramShapeModel a = Diagram.Shapes[0];
        ClickEmpty();

        // Act: only the first shape, which the saved corners aren't lined up with any more
        _view.Drag(a.Bounds.X + 10, a.Bounds.Y + 10, a.Bounds.X + 10, a.Bounds.Y - 40);

        // Assert
        Assert.Equal([new Point(250, 125), new Point(250, 325)], Diagram.Connectors[0].Bends!);
        AssertLineIsLevelAndUpright(Diagram);
    }

    [Fact]
    public void Segment_MovedThenBothShapesMove_GoesAlong()
    {
        // Arrange
        AddSelectedElbowWithCorners();
        _view.Drag(300, 200, 250, 200);
        _view.Command(DiagramCommand.SelectAll);

        // Act
        _view.Nudge(10, 20);

        // Assert
        Assert.Equal([new Point(260, 145), new Point(260, 345)], Diagram.Connectors[0].Bends!);
    }

    [Fact]
    public void Segment_MovedThenBothShapesAreScaled_GoesAlong()
    {
        // Arrange: the box from 100, 100 to 500, 350
        AddSelectedElbowWithCorners();
        _view.Drag(300, 200, 250, 200);
        ClickEmpty();
        _view.Command(DiagramCommand.SelectAll);

        // Act: twice as wide
        _view.Drag(500, 225, 900, 225, alt: true);

        // Assert
        Assert.Equal(400, Diagram.Connectors[0].Bends![0].X);
        AssertLineIsLevelAndUpright(Diagram);
    }

    [Fact]
    public void Segment_MovedGoesAlongWhenCopied()
    {
        // Arrange
        AddSelectedElbowWithCorners();
        _view.Drag(300, 200, 250, 200);
        _view.Command(DiagramCommand.SelectAll);

        // Act
        _view.Command(DiagramCommand.Copy);
        _view.Command(DiagramCommand.Paste);

        // Assert
        Assert.Equal([new Point(270, 145), new Point(270, 345)], Diagram.Connectors[1].Bends!);
    }

    [Fact]
    public void Segment_MovedStaysWhenAnEndIsAttachedElsewhere()
    {
        // Arrange
        (_, _, DiagramShapeModel c) = AddSelectedConnector();
        _view.Drag(300, 125, 300, 200);
        int corners = Diagram.Connectors[0].Bends!.Count;

        // Act
        _view.Drag(400, 125, c.Bounds.X + 50, c.Bounds.Y + 25);

        // Assert
        Assert.Equal(corners, Diagram.Connectors[0].Bends!.Count);
        Assert.Equal(c.Id, Diagram.Connectors[0].ToShapeId);
        AssertLineIsLevelAndUpright(Diagram);
    }

    [Fact]
    public void ResetRoute_LetsItWorkOutItsPathAgain_AndWithoutChangesHasNothingToUndo()
    {
        // Arrange
        AddSelectedConnector();
        DiagramModel before = Diagram;

        // Act & Assert
        _view.Command(DiagramCommand.ResetRoute);
        Assert.Same(before, Diagram);

        _view.Drag(300, 125, 300, 200);
        _view.Command(DiagramCommand.ResetRoute);
        Assert.Null(Diagram.Connectors[0].Bends);
    }

    [Fact]
    public void Routing_ChosenForASelectedConnector_LetsItWorkOutItsPathAgain()
    {
        // Arrange
        AddSelectedConnector();
        _view.Drag(300, 125, 300, 200);

        // Act
        _view.SelectRouting(DiagramRouting.Straight);

        // Assert
        Assert.Null(Diagram.Connectors[0].Bends);
    }

    [Fact]
    public void Properties_WithNothingSelected_ShowOnlyThat()
    {
        // Act
        ClickEmpty();

        // Assert
        Assert.Equal(new DiagramPropertiesModel("Nothing selected"), _view.Properties);
    }

    [Fact]
    public void Properties_OfARectangle_ShowEverythingItHas()
    {
        // Act
        AddShape(100, 100);
        _view.SelectColor(DiagramColorTarget.Fill, Color.Red);

        // Assert
        DiagramPropertiesModel properties = _view.Properties!;
        Assert.Equal("Rectangle", properties.Title);
        Assert.Equal(Box(100, 100, 100, 50), properties.Bounds);
        Assert.Equal(0, properties.CornerRadius);
        Assert.Equal(Color.Red.ToArgb(), properties.Fill!.Value.ToArgb());
        Assert.NotNull(properties.Line);
        Assert.Equal(2, properties.LineWidth);
        Assert.False(properties.Dashed);
        Assert.NotNull(properties.TextColor);
        Assert.Equal(14, properties.FontSize);
        Assert.Equal(string.Empty, properties.Text);
        Assert.Null(properties.Routing);
        Assert.Null(properties.Heads);
    }

    [Theory]
    [InlineData(DiagramTool.RoundedRectangle, "Rounded rectangle", 12)]
    [InlineData(DiagramTool.Rectangle, "Rectangle", 0)]
    public void Properties_OfARectangleKind_HaveACornerRadius(DiagramTool tool, string title, int expected)
    {
        // Act
        _view.Tool(tool);
        _view.Drag(100, 100, 300, 200);

        // Assert
        Assert.Equal((title, expected), (_view.Properties!.Title, _view.Properties.CornerRadius));
    }

    [Theory]
    [InlineData(DiagramTool.Ellipse)]
    [InlineData(DiagramTool.Diamond)]
    [InlineData(DiagramTool.Cylinder)]
    public void Properties_OfOtherShapes_HaveNoCornerRadius(DiagramTool tool)
    {
        // Act
        _view.Tool(tool);
        _view.Drag(100, 100, 300, 200);

        // Assert
        Assert.Null(_view.Properties!.CornerRadius);
    }

    [Fact]
    public void Properties_OfAText_HaveNoFillOrBorder()
    {
        // Act
        _view.DoubleClick(300, 300);
        _view.CommitText("Note");

        // Assert
        DiagramPropertiesModel properties = _view.Properties!;
        Assert.Equal(("Text", "Note"), (properties.Title, properties.Text));
        Assert.NotNull(properties.Bounds);
        Assert.Null(properties.Fill);
        Assert.Null(properties.Line);
        Assert.Null(properties.LineWidth);
        Assert.Null(properties.CornerRadius);
    }

    [Fact]
    public void Properties_OfAConnector_HaveItsOptions_AndNoSizeOrFill()
    {
        // Act
        AddSelectedConnector();

        // Assert
        DiagramPropertiesModel properties = _view.Properties!;
        Assert.Equal("Connector", properties.Title);
        Assert.Null(properties.Bounds);
        Assert.Null(properties.Fill);
        Assert.Null(properties.CornerRadius);
        Assert.NotNull(properties.Line);
        Assert.Equal(DiagramRouting.Elbow, properties.Routing);
        Assert.Equal(DiagramArrowHeads.End, properties.Heads);
        Assert.Equal(string.Empty, properties.Text);
    }

    [Fact]
    public void Properties_OfSeveral_ShowTheBoxAroundThem_AndNoText()
    {
        // Arrange
        AddShape(100, 100);
        AddShape(400, 200);

        // Act
        _view.Command(DiagramCommand.SelectAll);

        // Assert
        Assert.Equal("2 shapes", _view.Properties!.Title);
        Assert.Equal(Box(100, 100, 400, 150), _view.Properties.Bounds);
        Assert.Null(_view.Properties.Text);
    }

    [Fact]
    public void Properties_OfShapesAndConnectors_SayObjects()
    {
        // Arrange
        AddSelectedConnector();

        // Act
        _view.Command(DiagramCommand.SelectAll);

        // Assert
        Assert.Equal("4 objects", _view.Properties!.Title);
        Assert.Null(_view.Properties.CornerRadius);
    }

    [Fact]
    public void Properties_FollowADrag_AndUndo()
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.Press(120, 120);
        _view.Move(220, 220);

        // Assert: while it's still being dragged
        Assert.Equal(new Point(200, 200), _view.Properties!.Bounds!.Value.Location);

        // Act
        _view.Release(220, 220);
        _view.Command(DiagramCommand.Undo);

        // Assert
        Assert.Equal(new Point(100, 100), _view.Properties!.Bounds!.Value.Location);
    }

    [Fact]
    public void Properties_AreOnlySentWhenTheyChange()
    {
        // Arrange
        AddShape(100, 100);
        int count = _view.PropertiesShowCount;

        // Act: only the hover changes
        _view.Hover(150, 125);
        _view.Hover(214, 125);
        _view.Hover(900, 900);

        // Assert
        Assert.Equal(count, _view.PropertiesShowCount);
    }

    [Theory]
    [InlineData(300, 100, 100, 50)]
    [InlineData(100, 400, 100, 50)]
    [InlineData(100, 100, 250, 50)]
    [InlineData(100, 100, 100, 170)]
    public void BoundsEdited_MovesOrSizesTheShape(int x, int y, int width, int height)
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.EditBounds(x, y, width, height);

        // Assert
        Assert.Equal(Box(x, y, width, height), Diagram.Shapes[0].Bounds);
        Assert.Equal(Box(x, y, width, height), _view.Properties!.Bounds);
    }

    [Fact]
    public void BoundsEdited_TooSmall_StopsAtTheSmallestSize()
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.EditBounds(100, 100, 1, 0);

        // Assert
        Assert.Equal(Box(100, 100, 10, 10), Diagram.Shapes[0].Bounds);
    }

    [Fact]
    public void BoundsEdited_WithoutAChange_HasNothingToUndo()
    {
        // Arrange
        AddShape(100, 100);
        DiagramModel before = Diagram;

        // Act
        _view.EditBounds(100, 100, 100, 50);

        // Assert
        Assert.Same(before, Diagram);
    }

    [Fact]
    public void BoundsEdited_OfSeveral_ScalesThemTogether()
    {
        // Arrange
        AddShape(100, 100, 100, 100);
        AddShape(300, 100, 100, 100);
        _view.Command(DiagramCommand.SelectAll);

        // Act: the box from 100..400 becomes 100..700
        _view.EditBounds(100, 100, 600, 100);

        // Assert
        Assert.Equal(Box(100, 100, 200, 100), Diagram.Shapes[0].Bounds);
        Assert.Equal(Box(500, 100, 200, 100), Diagram.Shapes[1].Bounds);
    }

    [Fact]
    public void BoundsEdited_WithNothingSelected_DoesNothing()
    {
        // Act
        _view.EditBounds(0, 0, 100, 100);

        // Assert
        Assert.False(_view.CanUndo);
    }

    [Fact]
    public void BoundsEdited_OfBothShapes_TakesTheConnectorsCornersAlong()
    {
        // Arrange
        AddSelectedElbowWithCorners();
        _view.Drag(300, 200, 250, 200);
        _view.Command(DiagramCommand.SelectAll);

        // Act: 50 further right
        _view.EditBounds(150, 100, 400, 250);

        // Assert
        Assert.Equal(300, Diagram.Connectors[0].Bends![0].X);
    }

    [Fact]
    public void CornerRadiusEdited_ChangesIt_AndCanBeUndone()
    {
        // Arrange
        AddShape(100, 100);

        // Act & Assert
        _view.EditCornerRadius(20);
        Assert.Equal(20, Diagram.Shapes[0].CornerRadius);
        Assert.Equal(20, _view.Properties!.CornerRadius);

        _view.Command(DiagramCommand.Undo);
        Assert.Null(Diagram.Shapes[0].CornerRadius);
    }

    [Fact]
    public void CornerRadiusEdited_ToTheSameValue_HasNothingToUndo()
    {
        // Arrange: a rectangle's usual radius is 0
        AddShape(100, 100);
        DiagramModel before = Diagram;

        // Act
        _view.EditCornerRadius(0);

        // Assert
        Assert.Same(before, Diagram);
    }

    [Fact]
    public void CornerRadiusEdited_OnlyChangesShapesThatHaveCorners()
    {
        // Arrange
        AddShape(100, 100);
        _view.Tool(DiagramTool.Ellipse);
        _view.Drag(400, 100, 500, 200);
        _view.Command(DiagramCommand.SelectAll);

        // Act
        _view.EditCornerRadius(20);

        // Assert
        Assert.Equal(20, Diagram.Shapes[0].CornerRadius);
        Assert.Null(Diagram.Shapes[1].CornerRadius);
    }

    [Fact]
    public void TextEdited_ChangesTheShapesText_OrTheConnectorsLabel()
    {
        // Arrange
        (_, _, _) = AddSelectedConnector();

        // Act & Assert
        _view.EditText("yes\r\n");
        Assert.Equal("yes", Diagram.Connectors[0].Label);

        ClickEmpty();
        _view.Click(120, 120);
        _view.EditText("Hello\r\nthere");
        Assert.Equal("Hello\nthere", Diagram.Shapes[0].Text);
    }

    [Fact]
    public void TextEdited_ToTheSameText_HasNothingToUndo()
    {
        // Arrange: a field that loses the focus sends its text, changed or not
        AddShape(100, 100);
        DiagramModel before = Diagram;

        // Act
        _view.EditText(string.Empty);

        // Assert
        Assert.Same(before, Diagram);
    }

    [Fact]
    public void TextEdited_WithSeveralSelected_DoesNothing()
    {
        // Arrange
        AddShape(100, 100);
        AddShape(400, 100);
        _view.Command(DiagramCommand.SelectAll);

        // Act
        _view.EditText("x");

        // Assert
        Assert.All(Diagram.Shapes, shape => Assert.Equal(string.Empty, shape.Text));
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(37, 37)]
    [InlineData(5000, 100)]
    public void LineWidth_TypedInThePanel_CanBeAnyValueInRange(int width, int expected)
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.SelectLineWidth(width);

        // Assert
        Assert.Equal(expected, Diagram.Shapes[0].Style.LineWidth);
    }

    [Theory]
    [InlineData(1, 6)]
    [InlineData(37, 37)]
    [InlineData(5000, 400)]
    public void FontSize_TypedInThePanel_CanBeAnyValueInRange(int size, int expected)
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.SelectFontSize(size);

        // Assert
        Assert.Equal(expected, Diagram.Shapes[0].Style.FontSize);
    }

    [Fact]
    public void Font_IsRobotoForNewShapes()
    {
        Assert.Equal("Roboto", AddShape(100, 100).Style.FontFamily);
    }

    [Fact]
    public void Font_AppliesToWhatsSelected_AndToWhatComesNext()
    {
        // Arrange
        AddShape(100, 100);
        AddShape(400, 100);

        // Act: only the second is selected
        _view.SelectFontFamily("Consolas");

        // Assert
        Assert.Equal("Roboto", Diagram.Shapes[0].Style.FontFamily);
        Assert.Equal("Consolas", Diagram.Shapes[1].Style.FontFamily);
        Assert.Equal("Consolas", _view.Properties!.FontFamily);
        Assert.Equal("Consolas", AddShape(700, 100).Style.FontFamily);
    }

    [Fact]
    public void Font_AppliesToAConnectorsLabel_AndCanBeUndone()
    {
        // Arrange
        AddSelectedConnector();

        // Act & Assert
        _view.SelectFontFamily("Consolas");
        Assert.Equal("Consolas", Diagram.Connectors[0].Style.FontFamily);

        _view.Command(DiagramCommand.Undo);
        Assert.Equal("Roboto", Diagram.Connectors[0].Style.FontFamily);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Font_WithoutAName_IsIgnored(string name)
    {
        // Arrange
        AddShape(100, 100);
        DiagramModel before = Diagram;

        // Act
        _view.SelectFontFamily(name);

        // Assert
        Assert.Same(before, Diagram);
    }

    [Fact]
    public void Font_IsTheOneTheTextBoxUsesToo()
    {
        // Arrange
        AddShape(100, 100);
        _view.SelectFontFamily("Consolas");

        // Act
        _view.DoubleClick(120, 120);

        // Assert
        Assert.Equal("Consolas", _view.TextFontFamily);
    }

    private (FakeView View, DiagramEditorPresenter Presenter) NewEditor(DiagramEditorSettingsModel? saved)
    {
        var view = new FakeView();
        var presenter = new DiagramEditorPresenter(view, _store, new FakeSettingsStore(saved));

        presenter.Start();

        return (view, presenter);
    }

    private DiagramEditorPresenter NewEditorWithTheSavedSettings(out FakeView view)
    {
        (view, DiagramEditorPresenter presenter) = NewEditor(_settings.Saved[^1]);

        return presenter;
    }

    [Fact]
    public void Settings_WithoutAnySaved_StartWithTheDefaults()
    {
        // Assert
        Assert.Equal("Roboto", _view.ShownStyle!.FontFamily);
        Assert.Equal(DiagramRouting.Elbow, _view.ShownRouting);
        Assert.Equal(DiagramArrowHeads.End, _view.ShownHeads);
        Assert.True(_view.PanelShown);
        Assert.Null(_view.WindowShown);
        Assert.Empty(_settings.Saved);
    }

    [Fact]
    public void Settings_ANewEditor_StartsWithTheLastStyleUsed()
    {
        // Arrange
        _view.SelectColor(DiagramColorTarget.Fill, Color.Red);
        _view.SelectColor(DiagramColorTarget.Line, Color.Blue);
        _view.SelectColor(DiagramColorTarget.Text, Color.Green);
        _view.SelectLineWidth(5);
        _view.SelectDashed(true);
        _view.SelectFontSize(24);
        _view.SelectFontFamily("Consolas");
        _view.SelectRouting(DiagramRouting.Straight);
        _view.SelectHeads(DiagramArrowHeads.Both);

        // Act
        DiagramEditorPresenter presenter = NewEditorWithTheSavedSettings(out FakeView view);

        // Assert: shown, and used by what's drawn next
        DiagramStyleModel expected = new(
            Color.FromArgb(255, Color.Red),
            Color.FromArgb(255, Color.Blue),
            5,
            true,
            Color.FromArgb(255, Color.Green),
            24,
            "Consolas");
        Assert.Equal(expected, view.ShownStyle);
        Assert.Equal(DiagramRouting.Straight, view.ShownRouting);
        Assert.Equal(DiagramArrowHeads.Both, view.ShownHeads);

        view.Tool(DiagramTool.Rectangle);
        view.Drag(100, 100, 200, 200);
        Assert.Equal(expected, presenter.Diagram.Shapes[0].Style);
    }

    [Fact]
    public void Settings_ChangingWhatsSelected_IsWhatsUsedLast()
    {
        // Arrange
        AddShape(100, 100);

        // Act: the shape is selected, and this is what carries over to the next editor
        _view.SelectColor(DiagramColorTarget.Fill, Color.Red);

        // Assert
        Assert.Equal(Color.Red.ToArgb(), _settings.Saved[^1].Style.Fill.ToArgb());
    }

    [Fact]
    public void Settings_NoFill_IsKept()
    {
        // Act
        _view.SelectColor(DiagramColorTarget.Fill, Color.Empty);

        // Assert
        Assert.True(_settings.Saved[^1].Style.Fill.IsEmpty);
    }

    [Fact]
    public void Settings_DrawingAndSelecting_SaveNothing()
    {
        // Act
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);
        Connect(a, b);
        _view.Click(120, 120);
        _view.Drag(120, 120, 220, 220);
        _view.Command(DiagramCommand.Undo);
        _view.Wheel(0, 0, steps: 2, control: true);
        _view.DoubleClick(300, 300);
        _view.CommitText("Note");

        // Assert
        Assert.Empty(_settings.Saved);
    }

    [Fact]
    public void Settings_DontIncludeTheTool()
    {
        // Arrange
        _view.Tool(DiagramTool.Ellipse);
        _view.SelectLineWidth(5);

        // Act
        NewEditorWithTheSavedSettings(out FakeView view);

        // Assert
        Assert.Equal(DiagramTool.Select, view.ShownTool);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Settings_ThePropertiesPanel_IsKept(bool isVisible)
    {
        // Act
        _view.TogglePanel(isVisible);
        NewEditorWithTheSavedSettings(out FakeView view);

        // Assert
        Assert.Equal(isVisible, view.PanelShown);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Settings_TheWindowsSize_IsKeptWhenItCloses(bool isMaximized)
    {
        // Act
        _view.CloseWithSize(900, 600, isMaximized);
        NewEditorWithTheSavedSettings(out FakeView view);

        // Assert
        Assert.Equal(new DiagramWindowSizeModel(new Size(900, 600), isMaximized), view.WindowShown);
    }

    [Fact]
    public void Settings_ChangingSomethingElse_KeepsTheWindowsSizeThatWasSaved()
    {
        // Arrange
        var window = new DiagramWindowSizeModel(new Size(900, 600), false);
        var store = new FakeSettingsStore(new DiagramEditorSettingsModel(
            _view.ShownStyle!,
            DiagramRouting.Elbow,
            DiagramArrowHeads.End,
            Window: window));
        var view = new FakeView();

        new DiagramEditorPresenter(view, _store, store).Start();

        // Act
        view.SelectLineWidth(5);

        // Assert
        Assert.Equal(window, view.WindowShown);
        Assert.Equal(window, store.Saved[^1].Window);
    }

    [Theory]
    [InlineData(0, 600)]
    [InlineData(900, 0)]
    [InlineData(-1, -1)]
    public void Settings_AWindowSizeThatCantBeUsed_IsIgnored(int width, int height)
    {
        // Act
        (FakeView view, _) = NewEditor(new DiagramEditorSettingsModel(
            _view.ShownStyle!,
            DiagramRouting.Elbow,
            DiagramArrowHeads.End,
            Window: new DiagramWindowSizeModel(new Size(width, height), false)));

        // Assert
        Assert.Null(view.WindowShown);
    }

    [Fact]
    public void Settings_ARoutingOrHeadThatDoesntExist_KeepsTheDefault()
    {
        // Act: e.g. a file edited by hand
        (FakeView view, _) = NewEditor(new DiagramEditorSettingsModel(
            _view.ShownStyle!,
            (DiagramRouting)99,
            (DiagramArrowHeads)99));

        // Assert
        Assert.Equal((DiagramRouting.Elbow, DiagramArrowHeads.End), (view.ShownRouting, view.ShownHeads));
    }

    [Fact]
    public void Connector_WithTheTool_FromAShapesBody_PicksTheSides()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);

        // Act
        Connect(a, b);

        // Assert
        DiagramConnectorModel connector = Assert.Single(Diagram.Connectors);
        Assert.Null(connector.FromSide);
        Assert.Null(connector.ToSide);
    }

    [Fact]
    public void Connector_WithSides_KeepsThemWhenCopied()
    {
        // Arrange
        AddShape(100, 100);
        AddShape(400, 300);
        ClickEmpty();
        _view.Drag(214, 125, 386, 325);
        _view.Command(DiagramCommand.SelectAll);

        // Act
        _view.Command(DiagramCommand.Copy);
        _view.Command(DiagramCommand.Paste);

        // Assert
        Assert.Equal(DiagramSide.Right, Diagram.Connectors[1].FromSide);
        Assert.Equal(DiagramSide.Left, Diagram.Connectors[1].ToSide);
    }

    [Theory]
    [InlineData(110, 110)]
    [InlineData(1000, 1000)]
    public void Connector_NotEndingOnAnotherShape_AddsNothing(int toX, int toY)
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.Tool(DiagramTool.Connector);
        _view.Drag(110, 110, toX, toY);

        // Assert
        Assert.Empty(Diagram.Connectors);
    }

    [Fact]
    public void Connector_FromNothing_AddsNothing()
    {
        // Act
        _view.Tool(DiagramTool.Connector);
        _view.Drag(10, 10, 300, 300);

        // Assert
        Assert.Empty(Diagram.Connectors);
    }

    [Fact]
    public void Connector_ClickSelectsIt_AndDeleteRemovesOnlyIt()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);
        Connect(a, b);
        ClickEmpty();

        // Act
        _view.Click(300, 125);

        // Assert
        Assert.Equal([Diagram.Connectors[0].Id], _presenter.SelectedConnectorIds);

        // Act
        _view.Command(DiagramCommand.Delete);

        // Assert
        Assert.Empty(Diagram.Connectors);
        Assert.Equal(2, Diagram.Shapes.Count);
    }

    [Fact]
    public void Delete_AShape_RemovesItsConnectors()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);
        DiagramShapeModel c = AddShape(100, 400);
        Connect(a, b);
        Connect(b, c);
        Connect(a, c);
        ClickEmpty();
        _view.Click(420, 120);

        // Act
        _view.Command(DiagramCommand.Delete);

        // Assert
        Assert.Equal(2, Diagram.Shapes.Count);
        Assert.Equal((a.Id, c.Id), (Diagram.Connectors[0].FromShapeId, Diagram.Connectors[0].ToShapeId));
        Assert.Single(Diagram.Connectors);
        Assert.Empty(_presenter.SelectedShapeIds);
    }

    [Fact]
    public void UndoRedo_GoBackAndForward()
    {
        // Arrange
        AddShape(100, 100);
        Assert.True(_view.CanUndo);
        Assert.False(_view.CanRedo);

        // Act & Assert
        _view.Command(DiagramCommand.Undo);
        Assert.Empty(Diagram.Shapes);
        Assert.Empty(_presenter.SelectedShapeIds);
        Assert.True(_view.CanRedo);
        Assert.False(_view.CanUndo);

        _view.Command(DiagramCommand.Redo);
        Assert.Single(Diagram.Shapes);
    }

    [Fact]
    public void Undo_RestoresAMoveAndAConnector()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);
        Connect(a, b);
        _view.Click(420, 120);
        _view.Drag(420, 120, 420, 320);

        // Act & Assert
        _view.Command(DiagramCommand.Undo);
        Assert.Equal(b.Bounds, Diagram.Shapes[1].Bounds);
        Assert.Single(Diagram.Connectors);

        _view.Command(DiagramCommand.Undo);
        Assert.Empty(Diagram.Connectors);
    }

    [Fact]
    public void NewChange_AfterUndo_LosesTheRedo()
    {
        // Arrange
        AddShape(100, 100);
        _view.Command(DiagramCommand.Undo);

        // Act
        AddShape(200, 200);

        // Assert
        Assert.False(_view.CanRedo);
    }

    [Fact]
    public void Text_DoubleClickOnAShape_EditsItsText()
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.DoubleClick(120, 120);

        // Assert
        Assert.Equal(Box(100, 100, 100, 50), _view.TextBounds);

        // Act
        _view.CommitText("Hello\r\nworld  ");

        // Assert
        Assert.Equal("Hello\nworld", Diagram.Shapes[0].Text);
    }

    [Fact]
    public void Text_DoubleClickOnAConnector_EditsItsLabel()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);
        Connect(a, b);

        // Act
        _view.DoubleClick(300, 125);
        _view.CommitText("yes");

        // Assert
        Assert.Equal("yes", Diagram.Connectors[0].Label);
    }

    [Fact]
    public void Text_DoubleClickOnNothing_AddsATextOnlyOnceItHasSomethingInIt()
    {
        // Act
        _view.DoubleClick(300, 300);
        _view.CommitText("   ");

        // Assert
        Assert.Empty(Diagram.Shapes);

        // Act
        _view.DoubleClick(300, 300);
        _view.CommitText("Note");

        // Assert
        DiagramShapeModel text = Assert.Single(Diagram.Shapes);
        Assert.Equal((DiagramShapeKind.Text, "Note"), (text.Kind, text.Text));
        Assert.Equal(new Point(300, 300), text.Bounds.Location);
        Assert.Equal([text.Id], _presenter.SelectedShapeIds);
    }

    [Fact]
    public void Text_TheTextTool_ClickStartsATextAndGoesBackToSelect()
    {
        // Act
        _view.Tool(DiagramTool.Text);
        _view.Click(300, 300);
        _view.CommitText("Note");

        // Assert
        Assert.Equal(DiagramShapeKind.Text, Assert.Single(Diagram.Shapes).Kind);
        Assert.Equal(DiagramTool.Select, _view.ShownTool);
    }

    [Fact]
    public void Text_EditBox_FollowsTheZoom()
    {
        // Arrange
        AddShape(100, 100);
        _view.Wheel(0, 0, steps: 10, control: true);
        float zoom = _view.Scene!.Viewport.Zoom;

        // Act: where the shape's middle is on the canvas now
        _view.DoubleClick((int)(150 * zoom), (int)(125 * zoom));

        // Assert
        Assert.InRange(_view.TextBounds!.Value.Width, 100 * zoom - 1, 100 * zoom + 1);
        Assert.Equal((int)Math.Round(14 * zoom), _view.TextFontSize);
    }

    [Fact]
    public void Style_AppliesToWhatsSelected_AndToWhatComesNext()
    {
        // Arrange
        AddShape(100, 100);
        DiagramShapeModel other = AddShape(400, 100);
        ClickEmpty();
        _view.Click(120, 120);

        // Act
        _view.SelectColor(DiagramColorTarget.Fill, Color.Red);
        _view.SelectColor(DiagramColorTarget.Line, Color.Blue);
        _view.SelectColor(DiagramColorTarget.Text, Color.Green);
        _view.SelectLineWidth(5);
        _view.SelectDashed(true);
        _view.SelectFontSize(24);

        // Assert
        DiagramStyleModel expected = new(
            Color.FromArgb(255, Color.Red),
            Color.FromArgb(255, Color.Blue),
            5,
            true,
            Color.FromArgb(255, Color.Green),
            24);
        Assert.Equal(expected, Diagram.Shapes[0].Style);
        Assert.Equal(expected, _view.ShownStyle);
        Assert.Equal(other.Style, Diagram.Shapes[1].Style);
        Assert.Equal(expected, AddShape(700, 100).Style);
    }

    [Fact]
    public void Style_NoFill_IsEmpty()
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.SelectColor(DiagramColorTarget.Fill, Color.Empty);

        // Assert
        Assert.True(Diagram.Shapes[0].Style.Fill.IsEmpty);
    }

    [Fact]
    public void Style_WithNothingSelected_ChangesNothingToUndo()
    {
        // Act
        _view.SelectLineWidth(5);

        // Assert
        Assert.False(_view.CanUndo);
        Assert.Equal(5, AddShape(100, 100).Style.LineWidth);
    }

    [Fact]
    public void Style_SelectingAShape_ShowsItsStyle()
    {
        // Arrange
        AddShape(100, 100);
        _view.SelectLineWidth(5);
        ClickEmpty();
        _view.SelectLineWidth(8);

        // Act
        _view.Click(120, 120);

        // Assert
        Assert.Equal(5, _view.ShownStyle!.LineWidth);
    }

    [Fact]
    public void MoreColors_UsesWhatThePickerGives_AndCancelKeepsIt()
    {
        // Arrange
        AddShape(100, 100);
        DiagramStyleModel before = Diagram.Shapes[0].Style;

        // Act & Assert
        _view.RequestMoreColors(DiagramColorTarget.Line);
        Assert.Equal(before, Diagram.Shapes[0].Style);

        _view.ColorToPick = Color.Teal;
        _view.RequestMoreColors(DiagramColorTarget.Line);
        Assert.Equal(Color.Teal.ToArgb(), Diagram.Shapes[0].Style.Line.ToArgb());
    }

    [Fact]
    public void Routing_And_Heads_ApplyToTheSelectedConnector_AndToNewOnes()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);
        Connect(a, b);
        ClickEmpty();
        _view.Click(300, 125);

        // Act
        _view.SelectRouting(DiagramRouting.Straight);
        _view.SelectHeads(DiagramArrowHeads.Both);

        // Assert
        Assert.Equal(
            (DiagramRouting.Straight, DiagramArrowHeads.Both),
            (Diagram.Connectors[0].Routing, Diagram.Connectors[0].Heads));
        Assert.Equal((DiagramRouting.Straight, DiagramArrowHeads.Both), (_view.ShownRouting, _view.ShownHeads));

        // Act
        Connect(b, a);

        // Assert
        Assert.Equal(DiagramArrowHeads.Both, Diagram.Connectors[1].Heads);
    }

    [Fact]
    public void CopyPaste_MakesCopiesWithNewIds_AndTheConnectorsBetweenThem()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);
        Connect(a, b);
        _view.Command(DiagramCommand.SelectAll);

        // Act
        _view.Command(DiagramCommand.Copy);
        _view.Command(DiagramCommand.Paste);

        // Assert
        Assert.Equal(4, Diagram.Shapes.Count);
        Assert.Equal(2, Diagram.Connectors.Count);
        Assert.Equal(Diagram.Shapes.Count, Diagram.Shapes.Select(shape => shape.Id).Distinct().Count());
        Assert.Equal(Moved(a.Bounds, 20), Diagram.Shapes[2].Bounds);
        Assert.Equal(
            (Diagram.Shapes[2].Id, Diagram.Shapes[3].Id),
            (Diagram.Connectors[1].FromShapeId, Diagram.Connectors[1].ToShapeId));
        Assert.Equal([Diagram.Shapes[2].Id, Diagram.Shapes[3].Id], _presenter.SelectedShapeIds);
    }

    [Fact]
    public void CopyPaste_AConnectorWithOnlyOneOfItsShapes_IsLeftOut()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);
        Connect(a, b);
        ClickEmpty();
        _view.Click(120, 120);

        // Act
        _view.Command(DiagramCommand.Copy);
        _view.Command(DiagramCommand.Paste);

        // Assert
        Assert.Equal(3, Diagram.Shapes.Count);
        Assert.Single(Diagram.Connectors);
    }

    [Fact]
    public void Paste_Again_LandsFurtherAway()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);

        // Act
        _view.Command(DiagramCommand.Copy);
        _view.Command(DiagramCommand.Paste);
        _view.Command(DiagramCommand.Paste);

        // Assert
        Assert.Equal(Moved(a.Bounds, 40), Diagram.Shapes[2].Bounds);
    }

    [Fact]
    public void Paste_SomethingElseOnTheClipboard_DoesNothing()
    {
        // Arrange
        _view.Clipboard = "just some text";

        // Act
        _view.Command(DiagramCommand.Paste);

        // Assert
        Assert.Empty(Diagram.Shapes);
        Assert.False(_view.CanUndo);
    }

    [Fact]
    public void Cut_CopiesAndRemoves()
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.Command(DiagramCommand.Cut);

        // Assert
        Assert.Empty(Diagram.Shapes);

        // Act
        _view.Command(DiagramCommand.Paste);

        // Assert
        Assert.Single(Diagram.Shapes);
    }

    [Fact]
    public void Duplicate_LeavesTheClipboardAlone()
    {
        // Arrange
        AddShape(100, 100);
        _view.Clipboard = "kept";

        // Act
        _view.Command(DiagramCommand.Duplicate);

        // Assert
        Assert.Equal(2, Diagram.Shapes.Count);
        Assert.Equal("kept", _view.Clipboard);
    }

    [Fact]
    public void Group_SelectsAndMovesTogether_UntilUngrouped()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(400, 100);
        _view.Command(DiagramCommand.SelectAll);

        // Act
        _view.Command(DiagramCommand.Group);
        ClickEmpty();
        _view.Click(120, 120);

        // Assert
        Assert.Equal([a.Id, b.Id], _presenter.SelectedShapeIds.Order());

        // Act
        _view.Command(DiagramCommand.Ungroup);
        ClickEmpty();
        _view.Click(120, 120);

        // Assert
        Assert.Equal([a.Id], _presenter.SelectedShapeIds);
    }

    [Fact]
    public void Group_OneShape_DoesNothing()
    {
        // Arrange
        AddShape(100, 100);
        DiagramModel before = Diagram;

        // Act
        _view.Command(DiagramCommand.Group);

        // Assert
        Assert.Same(before, Diagram);
    }

    [Fact]
    public void Paste_OfAGroup_IsANewGroup()
    {
        // Arrange
        AddShape(100, 100);
        AddShape(400, 100);
        _view.Command(DiagramCommand.SelectAll);
        _view.Command(DiagramCommand.Group);

        // Act
        _view.Command(DiagramCommand.Copy);
        _view.Command(DiagramCommand.Paste);

        // Assert
        Assert.Equal(2, Diagram.Shapes.Select(shape => shape.GroupId).Distinct().Count());
        Assert.Equal(Diagram.Shapes[2].GroupId, Diagram.Shapes[3].GroupId);
    }

    [Fact]
    public void Reorder_BringsToFrontAndSendsToBack()
    {
        // Arrange
        DiagramShapeModel a = AddShape(100, 100);
        DiagramShapeModel b = AddShape(100, 100);
        ClickEmpty();

        // The one on top
        _view.Click(120, 120);
        Assert.Equal([b.Id], _presenter.SelectedShapeIds);

        // Act & Assert
        _view.Command(DiagramCommand.SendToBack);
        Assert.Equal([b.Id, a.Id], Diagram.Shapes.Select(shape => shape.Id));

        _view.Command(DiagramCommand.BringToFront);
        Assert.Equal([a.Id, b.Id], Diagram.Shapes.Select(shape => shape.Id));
    }

    [Fact]
    public void Nudge_MovesWhatsSelected()
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.Nudge(10, -1);

        // Assert
        Assert.Equal(new Point(110, 99), Diagram.Shapes[0].Bounds.Location);
    }

    [Fact]
    public void Zoom_ControlWheel_KeepsThePointUnderTheMouse()
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.Wheel(150, 125, steps: 3, control: true);
        DiagramViewportModel viewport = _view.Scene!.Viewport;

        // Assert
        Assert.True(viewport.Zoom > 1);
        Assert.Equal(new Point(150, 125), viewport.ToDiagram(new Point(150, 125)));
        Assert.Equal(133, _view.ZoomPercent);
    }

    [Theory]
    [InlineData(100, 400)]
    [InlineData(-100, 25)]
    public void Zoom_StaysInItsRange(float steps, int expectedPercent)
    {
        // Act
        _view.Wheel(0, 0, steps, control: true);

        // Assert
        Assert.Equal(expectedPercent, _view.ZoomPercent);
    }

    [Fact]
    public void Zoom_PointsAreConvertedToTheDiagram()
    {
        // Arrange
        _view.Wheel(0, 0, steps: 100, control: true);

        // Act: at 400%
        _view.Tool(DiagramTool.Rectangle);
        _view.Drag(400, 400, 1200, 800);

        // Assert
        Assert.Equal(Box(100, 100, 200, 100), Diagram.Shapes[0].Bounds);
    }

    [Fact]
    public void ZoomReset_GoesBackTo100()
    {
        // Arrange
        _view.Wheel(0, 0, steps: 5, control: true);

        // Act
        _view.Command(DiagramCommand.ZoomReset);

        // Assert
        Assert.Equal(100, _view.ZoomPercent);
    }

    [Fact]
    public void Pan_DraggingMovesTheView_NotTheShapes()
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.Drag(120, 120, 220, 170, pan: true);

        // Assert
        Assert.Equal(new Point(100, 50), _view.Scene!.Viewport.Pan);
        Assert.Equal(new Point(100, 100), Diagram.Shapes[0].Bounds.Location);
    }

    [Fact]
    public void Wheel_ScrollsTheView()
    {
        // Act
        _view.Wheel(0, 0, steps: 1);
        Assert.Equal(new Point(0, 40), _view.Scene!.Viewport.Pan);

        _view.Wheel(0, 0, steps: -1, shift: true);

        // Assert
        Assert.Equal(new Point(-40, 40), _view.Scene.Viewport.Pan);
    }

    [Fact]
    public void Cancel_EndsADrag_ThenTheTool_ThenTheSelection()
    {
        // Arrange
        AddShape(100, 100);
        _view.Tool(DiagramTool.Ellipse);
        _view.Press(300, 300);
        _view.Move(400, 400);

        // Act & Assert: the drag
        _view.Command(DiagramCommand.Cancel);
        Assert.Single(_view.Scene!.Diagram.Shapes);
        Assert.Equal(DiagramTool.Ellipse, _view.ShownTool);

        // The tool
        _view.Command(DiagramCommand.Cancel);
        Assert.Equal(DiagramTool.Select, _view.ShownTool);
        Assert.NotEmpty(_presenter.SelectedShapeIds);

        // The selection
        _view.Command(DiagramCommand.Cancel);
        Assert.Empty(_presenter.SelectedShapeIds);
    }

    [Fact]
    public void Cursor_ShowsWhatADragWouldDo()
    {
        // Arrange
        AddShape(100, 100);

        // Act & Assert
        _view.Hover(100, 100);
        Assert.Equal(DiagramCursor.SizeTopLeftBottomRight, _view.Cursor);

        _view.Hover(150, 150);
        Assert.Equal(DiagramCursor.SizeVertical, _view.Cursor);

        _view.Hover(150, 125);
        Assert.Equal(DiagramCursor.Move, _view.Cursor);

        _view.Hover(900, 900);
        Assert.Equal(DiagramCursor.Default, _view.Cursor);

        _view.Tool(DiagramTool.Connector);
        _view.Hover(150, 125);
        Assert.Equal(DiagramCursor.Cross, _view.Cursor);
    }

    [Fact]
    public void Save_FirstTime_AsksWhere_ThenSavesThere()
    {
        // Arrange
        AddShape(100, 100);
        Assert.True(_view.HasChanges);
        _view.PathToSave = @"C:\Diagrams\flow.svg";

        // Act
        _view.Command(DiagramCommand.Save);

        // Assert
        Assert.Equal(_view.PathToSave, _store.SavedPath);
        Assert.Same(Diagram, _store.Saved);
        Assert.Equal("flow.svg", _view.DocumentName);
        Assert.False(_view.HasChanges);
        Assert.Equal("Diagram.svg", _view.SuggestedName);
    }

    [Fact]
    public void Save_Again_DoesntAsk()
    {
        // Arrange
        _view.PathToSave = @"C:\Diagrams\flow.svg";
        _view.Command(DiagramCommand.Save);
        _view.PathToSave = null;
        AddShape(100, 100);

        // Act
        _view.Command(DiagramCommand.Save);

        // Assert
        Assert.Equal(@"C:\Diagrams\flow.svg", _store.SavedPath);
        Assert.Same(Diagram, _store.Saved);
    }

    [Fact]
    public void Save_WhenTheUserCancels_KeepsTheChanges()
    {
        // Arrange
        AddShape(100, 100);

        // Act
        _view.Command(DiagramCommand.Save);

        // Assert
        Assert.Null(_store.SavedPath);
        Assert.True(_view.HasChanges);
    }

    [Fact]
    public void Save_WhenItFails_ShowsWhy_AndKeepsTheChanges()
    {
        // Arrange
        AddShape(100, 100);
        _view.PathToSave = @"C:\Diagrams\flow.svg";
        _store.SaveError = new IOException("Disk full");

        // Act
        _view.Command(DiagramCommand.Save);

        // Assert
        Assert.Contains("Disk full", _view.Error);
        Assert.True(_view.HasChanges);
    }

    [Fact]
    public void Open_ReplacesTheDiagram_ClearsUndo_AndShowsItsTopLeft()
    {
        // Arrange
        AddShape(100, 100);
        _view.Command(DiagramCommand.Undo);
        _view.Command(DiagramCommand.Redo);
        _view.SaveChoice = DiagramSaveChoice.Discard;
        _view.PathToOpen = @"C:\Diagrams\other.svg";
        _store.ToLoad = new DiagramModel(
            [new DiagramShapeModel(1, DiagramShapeKind.Ellipse, Box(500, 600, 10, 10), "x", _view.ShownStyle!)],
            []);

        // Act
        _view.Command(DiagramCommand.Open);

        // Assert
        Assert.Same(_store.ToLoad, Diagram);
        Assert.Equal("other.svg", _view.DocumentName);
        Assert.False(_view.HasChanges);
        Assert.False(_view.CanUndo);
        Assert.Empty(_presenter.SelectedShapeIds);
        Assert.Equal(new Point(40, 40), _view.Scene!.Viewport.ToCanvas(new Point(500, 600)));
    }

    [Fact]
    public void Open_AFileThatIsntADiagram_ShowsWhy_AndKeepsTheOne()
    {
        // Arrange
        DiagramShapeModel shape = AddShape(100, 100);
        _view.SaveChoice = DiagramSaveChoice.Discard;
        _view.PathToOpen = @"C:\other.svg";
        _store.LoadError = new InvalidDataException("not a diagram");

        // Act
        _view.Command(DiagramCommand.Open);

        // Assert
        Assert.Contains("not a diagram", _view.Error);
        Assert.Equal(shape.Id, Diagram.Shapes[0].Id);
    }

    [Theory]
    [InlineData(DiagramSaveChoice.Cancel, true)]
    [InlineData(DiagramSaveChoice.Discard, false)]
    public void New_WithChanges_Asks(DiagramSaveChoice choice, bool keepsIt)
    {
        // Arrange
        AddShape(100, 100);
        _view.SaveChoice = choice;

        // Act
        _view.Command(DiagramCommand.New);

        // Assert
        Assert.Equal(keepsIt, Diagram.Shapes.Count == 1);
    }

    [Theory]
    [InlineData(DiagramSaveChoice.Cancel, true)]
    [InlineData(DiagramSaveChoice.Discard, false)]
    public void Closing_WithChanges_Asks(DiagramSaveChoice choice, bool expectedCancel)
    {
        // Arrange
        AddShape(100, 100);
        _view.SaveChoice = choice;

        // Act & Assert
        Assert.Equal(expectedCancel, _view.Close());
    }

    [Fact]
    public void Closing_WithChanges_CanSaveFirst_AndStaysOpenIfThatIsCancelled()
    {
        // Arrange
        AddShape(100, 100);
        _view.SaveChoice = DiagramSaveChoice.Save;

        // Act & Assert: no path given, so the save dialog is cancelled
        Assert.True(_view.Close());

        _view.PathToSave = @"C:\Diagrams\flow.svg";
        Assert.False(_view.Close());
        Assert.Equal(_view.PathToSave, _store.SavedPath);
    }

    [Fact]
    public void Closing_WithoutChanges_DoesntAsk()
    {
        // Arrange
        _view.SaveChoice = DiagramSaveChoice.Cancel;

        // Act & Assert
        Assert.False(_view.Close());
    }

    [Fact]
    public void Commands_DuringADrag_AreIgnored()
    {
        // Arrange
        _view.Tool(DiagramTool.Rectangle);
        _view.Press(100, 100);
        _view.Move(200, 200);

        // Act
        _view.Command(DiagramCommand.Undo);
        _view.Command(DiagramCommand.Paste);
        _view.Release(200, 200);

        // Assert
        Assert.Single(Diagram.Shapes);
    }

    private static Rectangle Moved(Rectangle box, int offset) =>
        new(box.X + offset, box.Y + offset, box.Width, box.Height);

    private sealed class FakeSettingsStore(DiagramEditorSettingsModel? loaded = null) : IDiagramEditorSettingsStore
    {
        public List<DiagramEditorSettingsModel> Saved { get; } = [];

        public DiagramEditorSettingsModel? Load() => loaded;

        public void Save(DiagramEditorSettingsModel settings) => Saved.Add(settings);
    }

    private sealed class FakeStore : IDiagramFileStore
    {
        public string? SavedPath { get; private set; }

        public DiagramModel? Saved { get; private set; }

        public DiagramModel ToLoad { get; set; } = DiagramModel.Empty;

        public Exception? SaveError { get; set; }

        public Exception? LoadError { get; set; }

        public void Save(string path, DiagramModel diagram)
        {
            if (SaveError is not null)
            {
                throw SaveError;
            }

            (SavedPath, Saved) = (path, diagram);
        }

        public DiagramModel Load(string path) => LoadError is null ? ToLoad : throw LoadError;
    }

    private sealed class FakeView : IDiagramEditorView
    {
        public event EventHandler<DiagramPointerModel>? PointerPressed;

        public event EventHandler<DiagramPointerModel>? PointerMoved;

        public event EventHandler<DiagramPointerModel>? PointerReleased;

        public event EventHandler<DiagramPointerModel>? PointerDoubleClicked;

        public event EventHandler? PointerLeft;

        public event EventHandler<DiagramWheelModel>? WheelTurned;

        public event EventHandler<DiagramTool>? ToolSelected;

        public event EventHandler<DiagramCommand>? CommandRequested;

        public event EventHandler<Size>? NudgeRequested;

        public event EventHandler<DiagramColorChoiceModel>? ColorSelected;

        public event EventHandler<DiagramColorTarget>? MoreColorsRequested;

        public event EventHandler<int>? LineWidthSelected;

        public event EventHandler<bool>? DashedSelected;

        public event EventHandler<int>? FontSizeSelected;

        public event EventHandler<string>? FontFamilySelected;

        public event EventHandler<DiagramRouting>? RoutingSelected;

        public event EventHandler<DiagramArrowHeads>? ArrowHeadsSelected;

        public event EventHandler<Rectangle>? BoundsEdited;

        public event EventHandler<int>? CornerRadiusEdited;

        public event EventHandler<string>? TextEdited;

        public event EventHandler<string>? TextCommitted;

        public event EventHandler<CancelEventArgs>? Closing;

        public event EventHandler<bool>? PropertiesPanelToggled;

        public event EventHandler<DiagramWindowSizeModel>? WindowClosed;

        public bool? PanelShown { get; private set; }

        public DiagramWindowSizeModel? WindowShown { get; private set; }

        public void TogglePanel(bool isVisible) => PropertiesPanelToggled?.Invoke(this, isVisible);

        public void CloseWithSize(int width, int height, bool isMaximized = false) =>
            WindowClosed?.Invoke(this, new DiagramWindowSizeModel(new Size(width, height), isMaximized));

        public void ShowPropertiesPanel(bool isVisible) => PanelShown = isVisible;

        public void ShowWindowSize(DiagramWindowSizeModel size) => WindowShown = size;

        public DiagramSceneModel? Scene { get; private set; }

        public DiagramTool ShownTool { get; private set; }

        public IReadOnlyList<Color> Palette { get; private set; } = [];

        public DiagramStyleModel? ShownStyle { get; private set; }

        public DiagramPropertiesModel? Properties { get; private set; }

        public int PropertiesShowCount { get; private set; }

        public DiagramRouting ShownRouting { get; private set; }

        public DiagramArrowHeads ShownHeads { get; private set; }

        public bool CanUndo { get; private set; }

        public bool CanRedo { get; private set; }

        public string? DocumentName { get; private set; }

        public bool HasChanges { get; private set; }

        public int ZoomPercent { get; private set; }

        public DiagramCursor Cursor { get; private set; }

        public Rectangle? TextBounds { get; private set; }

        public int TextFontSize { get; private set; }

        public string? TextFontFamily { get; private set; }

        public Color? ColorToPick { get; set; }

        public string? PathToSave { get; set; }

        public string? SuggestedName { get; private set; }

        public string? PathToOpen { get; set; }

        public DiagramSaveChoice SaveChoice { get; set; } = DiagramSaveChoice.Cancel;

        public string? Error { get; private set; }

        public string? Clipboard { get; set; }

        public void Press(int x, int y, bool shift = false, bool alt = false, bool pan = false) =>
            PointerPressed?.Invoke(this, new DiagramPointerModel(new Point(x, y), shift, Alt: alt, Pan: pan));

        public void Move(int x, int y, bool shift = false, bool alt = false) =>
            PointerMoved?.Invoke(this, new DiagramPointerModel(new Point(x, y), shift, Alt: alt));

        public void Hover(int x, int y) => Move(x, y);

        public void LeaveCanvas() => PointerLeft?.Invoke(this, EventArgs.Empty);

        public void Release(int x, int y, bool shift = false, bool alt = false) =>
            PointerReleased?.Invoke(this, new DiagramPointerModel(new Point(x, y), shift, Alt: alt));

        public void Drag(int fromX, int fromY, int toX, int toY, bool shift = false, bool alt = false, bool pan = false)
        {
            Press(fromX, fromY, shift, alt, pan);
            Move(toX, toY, shift, alt);
            Release(toX, toY, shift, alt);
        }

        public void Click(int x, int y, bool shift = false)
        {
            Press(x, y, shift);
            Release(x, y);
        }

        public void DoubleClick(int x, int y)
        {
            Click(x, y);
            Press(x, y);
            PointerDoubleClicked?.Invoke(this, new DiagramPointerModel(new Point(x, y)));
            Release(x, y);
        }

        public void Wheel(int x, int y, float steps, bool control = false, bool shift = false) =>
            WheelTurned?.Invoke(this, new DiagramWheelModel(new Point(x, y), steps, control, shift));

        public void Tool(DiagramTool tool) => ToolSelected?.Invoke(this, tool);

        public void Command(DiagramCommand command) => CommandRequested?.Invoke(this, command);

        public void Nudge(int x, int y) => NudgeRequested?.Invoke(this, new Size(x, y));

        public void SelectColor(DiagramColorTarget target, Color color) =>
            ColorSelected?.Invoke(this, new DiagramColorChoiceModel(target, color));

        public void RequestMoreColors(DiagramColorTarget target) => MoreColorsRequested?.Invoke(this, target);

        public void SelectLineWidth(int width) => LineWidthSelected?.Invoke(this, width);

        public void SelectDashed(bool isDashed) => DashedSelected?.Invoke(this, isDashed);

        public void SelectFontSize(int size) => FontSizeSelected?.Invoke(this, size);

        public void SelectFontFamily(string fontFamily) => FontFamilySelected?.Invoke(this, fontFamily);

        public void SelectRouting(DiagramRouting routing) => RoutingSelected?.Invoke(this, routing);

        public void SelectHeads(DiagramArrowHeads heads) => ArrowHeadsSelected?.Invoke(this, heads);

        public void CommitText(string text) => TextCommitted?.Invoke(this, text);

        public void EditBounds(int x, int y, int width, int height) =>
            BoundsEdited?.Invoke(this, new Rectangle(x, y, width, height));

        public void EditCornerRadius(int radius) => CornerRadiusEdited?.Invoke(this, radius);

        public void EditText(string text) => TextEdited?.Invoke(this, text);

        /// <returns>True when the window stays open.</returns>
        public bool Close()
        {
            var closing = new CancelEventArgs();

            Closing?.Invoke(this, closing);

            return closing.Cancel;
        }

        public void ShowScene(DiagramSceneModel scene) => Scene = scene;

        public void ShowTool(DiagramTool tool) => ShownTool = tool;

        public void ShowPalette(IReadOnlyList<Color> colors) => Palette = colors;

        public void ShowSizes(IReadOnlyList<int> lineWidths, IReadOnlyList<int> fontSizes)
        {
        }

        public void ShowStyle(DiagramStyleModel style, DiagramRouting routing, DiagramArrowHeads heads) =>
            (ShownStyle, ShownRouting, ShownHeads) = (style, routing, heads);

        public void ShowProperties(DiagramPropertiesModel properties)
        {
            Properties = properties;
            PropertiesShowCount++;
        }

        public void ShowUndoRedo(bool canUndo, bool canRedo) => (CanUndo, CanRedo) = (canUndo, canRedo);

        public void ShowDocument(string name, bool hasChanges) => (DocumentName, HasChanges) = (name, hasChanges);

        public void ShowZoom(int percent) => ZoomPercent = percent;

        public void ShowCursor(DiagramCursor cursor) => Cursor = cursor;

        public void BeginTextEdit(Rectangle bounds, string text, int fontSize, string fontFamily, Color color) =>
            (TextBounds, TextFontSize, TextFontFamily) = (bounds, fontSize, fontFamily);

        public Color? AskColor(Color current) => ColorToPick;

        public string? AskSavePath(string suggestedFileName)
        {
            SuggestedName = suggestedFileName;

            return PathToSave;
        }

        public string? AskOpenPath() => PathToOpen;

        public DiagramSaveChoice AskSaveChanges(string name) => SaveChoice;

        public void ShowError(string message) => Error = message;

        public void SetClipboardText(string text) => Clipboard = text;

        public string? GetClipboardText() => Clipboard;
    }
}
