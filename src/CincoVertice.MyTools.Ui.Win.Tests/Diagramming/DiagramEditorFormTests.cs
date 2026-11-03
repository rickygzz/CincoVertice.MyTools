using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Presenters;
using CincoVertice.MyTools.Ui.Win.Diagramming;
using CincoVertice.MyTools.Ui.Win.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Diagramming;

public class DiagramEditorFormTests
{
    private static readonly DiagramStyleModel _style = new(
        Color.FromArgb(255, 255, 0, 0),
        Color.FromArgb(255, 0, 0, 255),
        4,
        false,
        Color.Black,
        14);

    private static readonly DiagramViewportModel _viewport = new(1, Point.Empty);

    private static DiagramSceneModel Scene(DiagramModel diagram, IReadOnlySet<int>? selected = null) => new(
        diagram,
        selected ?? new HashSet<int>(),
        new HashSet<int>(),
        _viewport,
        null,
        [],
        null,
        null,
        null);

    private static Bitmap Render(DiagramSceneModel scene)
    {
        var image = new Bitmap(300, 200);

        using var graphics = Graphics.FromImage(image);

        DiagramRenderer.Draw(graphics, scene, image.Size);

        return image;
    }

    [Fact]
    public void Form_StartsWithItsPresenter()
    {
        // Every tool button's tag, the palettes and the lists are read here
        using var form = new DiagramEditorForm();
        var presenter = new DiagramEditorPresenter(form, new NoStore());

        presenter.Start();

        Assert.Contains(DiagramEditorPresenter.UntitledName, form.Text);
    }

    [Theory]
    [InlineData(DiagramTool.Select)]
    [InlineData(DiagramTool.Connector)]
    [InlineData(DiagramTool.Rectangle)]
    [InlineData(DiagramTool.RoundedRectangle)]
    [InlineData(DiagramTool.Ellipse)]
    [InlineData(DiagramTool.Diamond)]
    [InlineData(DiagramTool.Parallelogram)]
    [InlineData(DiagramTool.Hexagon)]
    [InlineData(DiagramTool.Cylinder)]
    [InlineData(DiagramTool.Document)]
    [InlineData(DiagramTool.Text)]
    public void ToolIcon_HasSomethingOnIt(DiagramTool tool)
    {
        // Act
        using Bitmap icon = DiagramToolIcons.Create(tool, 32);

        // Assert
        bool hasInk = Enumerable.Range(0, 32)
            .SelectMany(x => Enumerable.Range(0, 32).Select(y => icon.GetPixel(x, y).A))
            .Any(alpha => alpha > 0);

        Assert.True(hasInk);
    }

    [Theory]
    [InlineData(DiagramShapeKind.Rectangle)]
    [InlineData(DiagramShapeKind.RoundedRectangle)]
    [InlineData(DiagramShapeKind.Ellipse)]
    [InlineData(DiagramShapeKind.Diamond)]
    [InlineData(DiagramShapeKind.Parallelogram)]
    [InlineData(DiagramShapeKind.Hexagon)]
    [InlineData(DiagramShapeKind.Cylinder)]
    [InlineData(DiagramShapeKind.Document)]
    public void Renderer_FillsTheShape(DiagramShapeKind kind)
    {
        // Arrange
        var shape = new DiagramShapeModel(1, kind, new Rectangle(50, 50, 200, 100), string.Empty, _style);

        // Act
        using Bitmap image = Render(Scene(new DiagramModel([shape], [])));

        // Assert: its middle has the fill, and outside it nothing
        Assert.Equal(_style.Fill.ToArgb(), image.GetPixel(150, 100).ToArgb());
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(5, 5).ToArgb());
    }

    [Fact]
    public void Renderer_AShapeWithNoFill_ShowsWhatsBehindIt()
    {
        // Arrange
        var shape = new DiagramShapeModel(
            1,
            DiagramShapeKind.Rectangle,
            new Rectangle(50, 50, 200, 100),
            string.Empty,
            _style with { Fill = Color.Empty });

        // Act
        using Bitmap image = Render(Scene(new DiagramModel([shape], [])));

        // Assert
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(155, 105).ToArgb());
        Assert.Equal(_style.Line.ToArgb(), image.GetPixel(50, 100).ToArgb());
    }

    [Theory]
    [InlineData(DiagramRouting.Straight, DiagramArrowHeads.Both, false)]
    [InlineData(DiagramRouting.Elbow, DiagramArrowHeads.End, true)]
    [InlineData(DiagramRouting.Elbow, DiagramArrowHeads.None, false)]
    public void Renderer_DrawsConnectors(DiagramRouting routing, DiagramArrowHeads heads, bool dashed)
    {
        // Arrange
        DiagramStyleModel style = _style with { Dashed = dashed };
        DiagramModel diagram = new(
            [
                new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(10, 10, 60, 40), "a", style),
                new DiagramShapeModel(2, DiagramShapeKind.Ellipse, new Rectangle(200, 120, 60, 40), "b", style),
            ],
            [new DiagramConnectorModel(3, 1, 2, routing, heads, "label", style)]);

        // Act
        using Bitmap image = Render(Scene(diagram));

        // Assert: something other than the shapes and the grid was drawn between them, in the line's color
        bool hasLine = Enumerable.Range(75, 120)
            .SelectMany(x => Enumerable.Range(0, 200).Select(y => image.GetPixel(x, y).ToArgb()))
            .Contains(style.Line.ToArgb());

        Assert.True(hasLine);
    }

    [Fact]
    public void Renderer_AConnectorWithoutItsShape_IsSkipped()
    {
        // Arrange
        DiagramModel diagram = new(
            [],
            [new DiagramConnectorModel(3, 1, 2, DiagramRouting.Elbow, DiagramArrowHeads.End, string.Empty, _style)]);

        // Act
        using Bitmap image = Render(Scene(diagram));

        // Assert
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(5, 5).ToArgb());
    }

    [Fact]
    public void Renderer_ShowsTheSelectionHandles()
    {
        // Arrange
        var shape = new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(50, 50, 200, 100), "x", _style);
        DiagramSceneModel scene = Scene(new DiagramModel([shape], []), new HashSet<int> { 1 }) with
        {
            HandleBounds = shape.Bounds,
        };

        // Act
        using Bitmap image = Render(scene);

        // Assert: white in the middle of the square at the corner, where the outline would otherwise be blue
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(50, 50).ToArgb());
    }

    [Fact]
    public void Renderer_DrawsTheCircles_WithTheHighlightedOneFilled()
    {
        // Arrange
        var shape = new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(50, 50, 200, 100), "x", _style);
        DiagramSceneModel scene = Scene(new DiagramModel([shape], [])) with
        {
            Anchors = new DiagramAnchorsModel([new PointF(150, 36), new PointF(264, 100)], new PointF(264, 100)),
        };

        // Act
        using Bitmap image = Render(scene);

        // Assert: white inside the plain one, and not inside the highlighted one
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(150, 36).ToArgb());
        Assert.NotEqual(Color.White.ToArgb(), image.GetPixel(264, 100).ToArgb());
    }

    [Fact]
    public void Renderer_DrawsTheEndsOfASelectedConnector()
    {
        // Arrange
        DiagramSceneModel scene = Scene(DiagramModel.Empty) with { ConnectorEnds = [new PointF(100, 100)] };

        // Act
        using Bitmap image = Render(scene);

        // Assert
        Assert.Equal(Color.FromArgb(0, 120, 215).ToArgb(), image.GetPixel(100, 100).ToArgb());
    }

    [Fact]
    public void Form_ShowsAnyProperties_EvenOutOfItsFieldsRange()
    {
        // Arrange
        using var form = new DiagramEditorForm();
        new DiagramEditorPresenter(form, new NoStore()).Start();

        // Act: from a file edited by hand, say; a field's value out of its range would throw
        form.ShowProperties(new DiagramPropertiesModel(
            "Rectangle",
            new Rectangle(-999999, 999999, 1, 999999),
            5000,
            Color.Empty,
            Color.Red,
            5000,
            true,
            Color.Blue,
            1,
            "text",
            DiagramRouting.Straight,
            DiagramArrowHeads.None));
        form.ShowProperties(new DiagramPropertiesModel("Nothing selected"));

        // Assert: reaching here is the point
        Assert.NotNull(form);
    }

    private sealed class NoStore : IDiagramFileStore
    {
        public void Save(string path, DiagramModel diagram)
        {
        }

        public DiagramModel Load(string path) => DiagramModel.Empty;
    }
}
