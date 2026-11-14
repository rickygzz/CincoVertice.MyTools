using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using CincoVertice.MyTools.Ui.Win.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Diagramming;

public sealed class PngDiagramExporterTests : IDisposable
{
    private static readonly Color _red = Color.FromArgb(255, 0, 0);
    private static readonly DiagramStyleModel _style = new(_red, Color.Black, 2, false, Color.Black, 14);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PngExportTests_" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static DiagramModel OneShape() => new(
        [new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(100, 50, 200, 100), string.Empty, _style)],
        []);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Export_IsThePartTheSvgShows_AtTheScale_OnWhite(int scale)
    {
        // Arrange
        Directory.CreateDirectory(_folder);
        string path = Path.Combine(_folder, "flow.png");
        Rectangle area = DiagramArea.Of(OneShape());

        // Act
        new PngDiagramExporter().Export(path, OneShape(), scale);

        // Assert: the shape's middle is its fill, the margin white
        using var picture = new Bitmap(path);
        Assert.Equal(new Size(area.Width * scale, area.Height * scale), picture.Size);
        Assert.Equal(_red.ToArgb(), picture.GetPixel(picture.Width / 2, picture.Height / 2).ToArgb());
        Assert.Equal(Color.White.ToArgb(), picture.GetPixel(2, 2).ToArgb());
        Assert.Equal([path], Directory.GetFiles(_folder));
    }

    [Fact]
    public void Draw_AContainer_FillsItsTitleBandAndItsInsideEachInItsColor_UnderItsBorder()
    {
        // Arrange: a thick border, to find it
        Color blue = Color.FromArgb(0, 120, 212);
        Color pale = Color.FromArgb(240, 247, 255);
        DiagramModel diagram = new(
            [
                new DiagramShapeModel(
                    1,
                    DiagramShapeKind.Container,
                    new Rectangle(0, 0, 300, 200),
                    string.Empty,
                    _style with { Fill = pale, TitleFill = blue, LineWidth = 6 }),
            ],
            []);
        Rectangle area = DiagramArea.Of(diagram);

        // Act
        using Bitmap picture = PngDiagramExporter.Draw(diagram, 1);

        // Assert: the band is 32 high; the border, black, goes over its fill
        Assert.Equal(blue.ToArgb(), picture.GetPixel(150 - area.X, 16 - area.Y).ToArgb());
        Assert.Equal(pale.ToArgb(), picture.GetPixel(150 - area.X, 120 - area.Y).ToArgb());
        Assert.Equal(Color.Black.ToArgb(), picture.GetPixel(1 - area.X, 16 - area.Y).ToArgb());
    }

    [Fact]
    public void Draw_AContainerWithoutATitleFill_RoundAtTheTopOnly_ShowsWhatsBehindItsBand()
    {
        // Arrange
        Color pale = Color.FromArgb(240, 247, 255);
        DiagramModel diagram = new(
            [
                new DiagramShapeModel(
                    1,
                    DiagramShapeKind.Container,
                    new Rectangle(0, 0, 300, 200),
                    string.Empty,
                    _style with { Fill = pale, TitleFill = DiagramStyleModel.NoTitleFill },
                    CornerRadius: 30,
                    BottomCornerRadius: 0),
            ],
            []);
        Rectangle area = DiagramArea.Of(diagram);

        Color At(Bitmap picture, int x, int y) => picture.GetPixel(x - area.X, y - area.Y);

        // Act
        using Bitmap picture = PngDiagramExporter.Draw(diagram, 1);

        // Assert: the band white, as the page; the inside its fill; the top corner round, the bottom one square
        Assert.Equal(Color.White.ToArgb(), At(picture, 150, 16).ToArgb());
        Assert.Equal(pale.ToArgb(), At(picture, 150, 120).ToArgb());
        Assert.Equal(Color.White.ToArgb(), At(picture, 3, 3).ToArgb());
        Assert.Equal(pale.ToArgb(), At(picture, 4, 196).ToArgb());
    }

    [Fact]
    public void Draw_AHugeDiagram_HasFewerPixelsPerDiagramPixel()
    {
        // Arrange
        DiagramModel huge = new(
            [new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(0, 0, 10000, 100), "", _style)],
            []);

        // Act
        using Bitmap picture = PngDiagramExporter.Draw(huge, 2);

        // Assert
        Assert.Equal(PngDiagramExporter.MaxSide, picture.Width);
    }
}
