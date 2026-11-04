using System.Drawing;
using System.Drawing.Imaging;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Win.Snipping;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Snipping;

public sealed class BlurRendererTests : IDisposable
{
    private static readonly Rectangle _secretArea = new(40, 30, 80, 40);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "BlurRendererTests_" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    /// <summary>A white snip with something in the area, drawn by the caller.</summary>
    private static Bitmap Snip(Action<Graphics> drawSecret)
    {
        var snip = new Bitmap(200, 100, PixelFormat.Format24bppRgb);

        using var graphics = Graphics.FromImage(snip);

        graphics.Clear(Color.White);
        graphics.SetClip(_secretArea);
        drawSecret(graphics);

        return snip;
    }

    private static Bitmap SecretText(string text) => Snip(graphics =>
    {
        using var font = new Font("Segoe UI", 20, FontStyle.Bold, GraphicsUnit.Pixel);

        graphics.DrawString(text, font, Brushes.Black, _secretArea.Location);
    });

    private static Bitmap Render(Bitmap snip, params AnnotationModel[] annotations)
    {
        var image = new Bitmap(snip.Width, snip.Height, PixelFormat.Format24bppRgb);

        using var graphics = Graphics.FromImage(image);

        graphics.DrawImage(snip, new Rectangle(Point.Empty, snip.Size));
        AnnotationRenderer.Draw(graphics, annotations, snip);

        return image;
    }

    private static List<int> PixelsIn(Bitmap image, Rectangle area)
    {
        List<int> pixels = [];

        for (int y = area.Top; y < area.Bottom; y++)
        {
            for (int x = area.Left; x < area.Right; x++)
            {
                pixels.Add(image.GetPixel(x, y).ToArgb());
            }
        }

        return pixels;
    }

    [Fact]
    public void Blur_IsTheSameWhateverIsUnderIt()
    {
        // Arrange
        using Bitmap first = SecretText("1234");
        using Bitmap second = SecretText("9876");
        using Bitmap solid = Snip(graphics => graphics.Clear(Color.Magenta));
        var blur = new BlurAnnotationModel(_secretArea);

        // Act
        using Bitmap firstImage = Render(first, blur);
        using Bitmap secondImage = Render(second, blur);
        using Bitmap solidImage = Render(solid, blur);

        // Assert: not a single pixel depends on them, so nothing can be worked back from the image
        Assert.Equal(PixelsIn(firstImage, _secretArea), PixelsIn(secondImage, _secretArea));
        Assert.Equal(PixelsIn(firstImage, _secretArea), PixelsIn(solidImage, _secretArea));
    }

    [Fact]
    public void Blur_StaysExactlyInsideItsArea()
    {
        // Arrange
        using Bitmap snip = Snip(graphics => graphics.Clear(Color.Magenta));

        // Act
        using Bitmap image = Render(snip, new BlurAnnotationModel(_secretArea));

        // Assert: none of what it hides shows at its edges, even mixed in, and nothing else changed
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Color pixel = image.GetPixel(x, y);

                if (_secretArea.Contains(x, y))
                {
                    Assert.True(pixel.R - pixel.G < 5, $"Magenta shows at {x}, {y}: {pixel}");
                }
                else
                {
                    Assert.Equal(snip.GetPixel(x, y).ToArgb(), pixel.ToArgb());
                }
            }
        }
    }

    [Fact]
    public void Blur_TakesItsColorFromAround()
    {
        // Arrange: a dark snip with something bright in the area
        using Bitmap snip = new(200, 100, PixelFormat.Format24bppRgb);

        using (var graphics = Graphics.FromImage(snip))
        {
            graphics.Clear(Color.FromArgb(40, 40, 90));
            graphics.FillRectangle(Brushes.White, _secretArea);
        }

        // Act
        using Bitmap image = Render(snip, new BlurAnnotationModel(_secretArea));

        // Assert: on average, as dark and blue as around it, however bright what's under it
        List<int> pixels = PixelsIn(image, _secretArea);
        double blue = pixels.Average(pixel => pixel & 0xFF);
        double red = pixels.Average(pixel => (pixel >> 16) & 0xFF);

        Assert.InRange(red, 25, 55);
        Assert.InRange(blue, 75, 105);
    }

    [Fact]
    public void Blur_NextToAnother_GivesNothingAwayOfWhatThatOneHides()
    {
        // Arrange: the first blur's secret touches the second blur's surroundings
        var other = new Rectangle(_secretArea.Right, _secretArea.Top, 40, _secretArea.Height);
        using Bitmap white = Snip(_ => { });
        using Bitmap withSecret = new(white);

        using (var graphics = Graphics.FromImage(withSecret))
        {
            graphics.FillRectangle(Brushes.Black, other);
        }

        var hiding = new BlurAnnotationModel(other);
        var blur = new BlurAnnotationModel(_secretArea);

        // Act
        using Bitmap first = Render(white, blur, hiding);
        using Bitmap second = Render(withSecret, blur, hiding);

        // Assert
        Assert.Equal(PixelsIn(first, _secretArea), PixelsIn(second, _secretArea));
    }

    [Fact]
    public void Blur_CoversWhatWasDrawnBeforeIt_AndNotWhatComesAfter()
    {
        // Arrange
        using Bitmap snip = Snip(_ => { });
        Color red = Color.FromArgb(255, 255, 0, 0);
        var line = new StrokeAnnotationModel([new Point(50, 50), new Point(100, 50)], red, 8);
        var blur = new BlurAnnotationModel(_secretArea);

        // Act
        using Bitmap covered = Render(snip, line, blur);
        using Bitmap onTop = Render(snip, blur, line);

        // Assert
        Assert.Equal(red.ToArgb(), onTop.GetPixel(75, 50).ToArgb());
        Assert.DoesNotContain(red.ToArgb(), PixelsIn(covered, _secretArea));
    }

    [Theory]
    [InlineData(-50, -50, 400, 300)]
    [InlineData(150, 60, 500, 500)]
    [InlineData(300, 300, 50, 50)]
    public void Blur_PastTheSnip_DrawsWhatsOnIt(int x, int y, int width, int height)
    {
        // Arrange
        using Bitmap snip = Snip(_ => { });

        // Act: reaching the end without an exception is the point
        using Bitmap image = Render(snip, new BlurAnnotationModel(new Rectangle(x, y, width, height)));

        // Assert
        Assert.Equal(snip.Size, image.Size);
    }

    [Theory]
    [InlineData(50, 50, 0, 0)]
    [InlineData(50, 50, 30, 0)]
    [InlineData(50, 50, 0, 30)]
    public void Blur_WithNoSize_DrawsNothing(int x, int y, int width, int height)
    {
        // Arrange: as when the button has just been pressed, or a drag goes in a straight line
        using Bitmap snip = Snip(_ => { });

        // Act
        using Bitmap image = Render(snip, new BlurAnnotationModel(new Rectangle(x, y, width, height)));

        // Assert
        var whole = new Rectangle(Point.Empty, snip.Size);
        Assert.Equal(PixelsIn(snip, whole), PixelsIn(image, whole));
    }

    [Fact]
    public void SavedImage_HasNothingOfWhatsUnderTheBlur()
    {
        // Arrange: the form owns the snip it's given
        Directory.CreateDirectory(_folder);
        string firstPath = Path.Combine(_folder, "first.png");
        string secondPath = Path.Combine(_folder, "second.png");

        using var first = new SnipEditorForm(SecretText("1234"));
        using var second = new SnipEditorForm(SecretText("9876"));

        first.ShowAnnotations([new BlurAnnotationModel(_secretArea)]);
        second.ShowAnnotations([new BlurAnnotationModel(_secretArea)]);

        // Act
        first.SaveImage(firstPath);
        second.SaveImage(secondPath);

        // Assert: two different secrets give the very same file
        Assert.Equal(File.ReadAllBytes(firstPath), File.ReadAllBytes(secondPath));
    }
}
