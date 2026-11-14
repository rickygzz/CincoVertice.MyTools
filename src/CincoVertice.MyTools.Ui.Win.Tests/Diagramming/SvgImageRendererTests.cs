using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using CincoVertice.MyTools.Ui.Win.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Diagramming;

public class SvgImageRendererTests
{
    // Like Azure's icons: a gradient, a shape filled by class from a style sheet, and a size of 18
    private const string Icon = """
        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 18 18">
          <defs>
            <linearGradient id="a" x1="9" y1="18" x2="9" y2="0" gradientUnits="userSpaceOnUse">
              <stop offset="0" stop-color="#0078d4"/><stop offset="1" stop-color="#0078d4"/>
            </linearGradient>
            <style>.cls-1{fill:#e8121d;}</style>
          </defs>
          <rect width="18" height="9" fill="url(#a)"/>
          <rect class="cls-1" y="9" width="18" height="9"/>
        </svg>
        """;

    private static void AssertNear(Color expected, Color actual)
    {
        Assert.InRange(actual.R, expected.R - 3, expected.R + 3);
        Assert.InRange(actual.G, expected.G - 3, expected.G + 3);
        Assert.InRange(actual.B, expected.B - 3, expected.B + 3);
        Assert.InRange(actual.A, expected.A - 3, expected.A + 3);
    }

    [Fact]
    public void Render_AnIcon_DrawsItsGradientsAndItsStyleSheetsColors_AtTheSizeAsked()
    {
        // Act
        Bitmap image = SvgImageRenderer.Render(DiagramSvgImage.Prepare(Icon), 64, 64)!;

        // Assert
        Assert.Equal(new Size(64, 64), image.Size);
        AssertNear(Color.FromArgb(0x00, 0x78, 0xD4), image.GetPixel(32, 10));
        AssertNear(Color.FromArgb(0xE8, 0x12, 0x1D), image.GetPixel(32, 54));
    }

    [Fact]
    public void Render_AWideImage_KeepsItsProportions_InTheMiddleOfTheBox()
    {
        // Arrange
        string markup = DiagramSvgImage.Prepare(
            """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 2 1"><rect width="2" height="1"/></svg>""");

        // Act
        Bitmap image = SvgImageRenderer.Render(markup, 40, 40)!;

        // Assert
        Assert.Equal(0, image.GetPixel(20, 5).A);
        Assert.Equal(255, image.GetPixel(20, 20).A);
        Assert.Equal(0, image.GetPixel(20, 35).A);
    }

    [Fact]
    public void Render_AWideImage_Filling_IsStretchedOverTheWholeBox()
    {
        // Arrange
        string markup = DiagramSvgImage.Filling(DiagramSvgImage.Prepare(
            """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 2 1"><rect width="2" height="1"/></svg>"""));

        // Act
        Bitmap image = SvgImageRenderer.Render(markup, 40, 40)!;

        // Assert
        Assert.Equal(255, image.GetPixel(20, 2).A);
        Assert.Equal(255, image.GetPixel(20, 37).A);
    }

    [Fact]
    public void Render_MarkupWindowsCantRead_IsNothing()
    {
        // Act / Assert
        Assert.Null(SvgImageRenderer.Render("not markup", 10, 10));
    }

    [Fact]
    public void Draw_AnImageThatCantBeDrawn_IsACrossedBox_SoItCanStillBeSeen()
    {
        // Arrange
        using var target = new Bitmap(20, 20);
        using var graphics = Graphics.FromImage(target);

        // Act
        SvgImageRenderer.Draw(graphics, "not markup", new Rectangle(0, 0, 19, 19));

        // Assert
        Assert.NotEqual(0, target.GetPixel(10, 10).A);
    }

    [Fact]
    public void Rasterize_GivesColorsWithoutTheirTransparency_AndTheTransparencyApart()
    {
        // Arrange
        string markup = DiagramSvgImage.Prepare("""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 2 2">
              <rect width="2" height="2" fill="#ff8000" fill-opacity="0.5"/>
            </svg>
            """);

        // Act
        DiagramRasterModel raster = new Direct2DImageRasterizer().Rasterize(markup, 4, 4)!;

        // Assert
        Assert.Equal((4, 4), (raster.Width, raster.Height));
        Assert.Equal(48, raster.Rgb.Length);
        Assert.InRange(raster.Alpha[5], 125, 130);
        Assert.InRange(raster.Rgb[15], 252, 255);
        Assert.InRange(raster.Rgb[16], 125, 131);
        Assert.InRange(raster.Rgb[17], 0, 3);
    }
}
