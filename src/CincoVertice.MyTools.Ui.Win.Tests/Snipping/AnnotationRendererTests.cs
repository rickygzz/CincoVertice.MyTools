using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Services;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Snipping;

public class AnnotationRendererTests
{
    private static readonly Color _edge = Color.FromArgb(255, 0, 200, 0);
    private static readonly Color _letters = Color.FromArgb(255, 200, 0, 0);

    private static Bitmap Render(TextAnnotationModel text)
    {
        var image = new Bitmap(300, 120);

        using var graphics = Graphics.FromImage(image);

        graphics.Clear(Color.White);
        AnnotationRenderer.Draw(graphics, [text], image);

        return image;
    }

    private static int CountOf(Bitmap image, Func<Color, bool> isCounted)
    {
        int count = 0;

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                if (isCounted(image.GetPixel(x, y)))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static TextAnnotationModel Text(Color color, int width) => new(
        new Point(20, 20),
        "Hello\nworld",
        color,
        32,
        new SnipTextOutlineModel(width, _edge));

    [Fact]
    public void Outline_DrawsItsColorAroundTheLetters()
    {
        // Act
        using Bitmap image = Render(Text(_letters, 3));

        // Assert
        Assert.True(CountOf(image, color => color.ToArgb() == _edge.ToArgb()) > 100);
        Assert.True(CountOf(image, color => color.ToArgb() == _letters.ToArgb()) > 100);
    }

    [Fact]
    public void Outline_Wider_CoversMore()
    {
        // Act
        using Bitmap thin = Render(Text(_letters, 1));
        using Bitmap wide = Render(Text(_letters, 6));

        // Assert
        Assert.True(
            CountOf(wide, color => color.ToArgb() == _edge.ToArgb())
            > CountOf(thin, color => color.ToArgb() == _edge.ToArgb()));
    }

    [Fact]
    public void Outline_OnAVeryLongText_DrawsWhatsOnTheSnip()
    {
        // Arrange: as when a long text is pasted. As a whole, it's far larger than any bitmap can be
        string text = new string('M', 3000) + new string('\n', 3000) + "M";

        // Act
        using Bitmap image = Render(Text(_letters, 3) with { Text = text });

        // Assert
        Assert.True(CountOf(image, color => color.ToArgb() == _letters.ToArgb()) > 100);
    }

    [Theory]
    [InlineData(5000, 5000)]
    [InlineData(-5000, -5000)]
    [InlineData(310, 20)]
    public void Outline_OnATextOutsideTheSnip_DrawsNothing(int x, int y)
    {
        // Act
        using Bitmap image = Render(Text(_letters, 3) with { Location = new Point(x, y) });

        // Assert
        Assert.Equal(0, CountOf(image, color => color.ToArgb() != Color.White.ToArgb()));
    }

    [Fact]
    public void Outline_RepaintingOnlyAPart_DrawsThatPartTheSame()
    {
        // Arrange: a window repaints only what needs it
        var part = new Rectangle(40, 30, 50, 40);
        TextAnnotationModel text = Text(Color.FromArgb(128, _letters), 3);
        using Bitmap whole = Render(text);
        using var clipped = new Bitmap(whole.Width, whole.Height);

        // Act
        using (var graphics = Graphics.FromImage(clipped))
        {
            graphics.Clear(Color.White);
            graphics.SetClip(part);
            AnnotationRenderer.Draw(graphics, [text], clipped);
        }

        // Assert
        for (int y = part.Top; y < part.Bottom; y++)
        {
            for (int x = part.Left; x < part.Right; x++)
            {
                Assert.Equal(whole.GetPixel(x, y).ToArgb(), clipped.GetPixel(x, y).ToArgb());
            }
        }
    }

    [Theory]
    [InlineData(1, SnipArrowHeads.End)]
    [InlineData(2, SnipArrowHeads.Start)]
    [InlineData(4, SnipArrowHeads.Both)]
    [InlineData(16, SnipArrowHeads.End)]
    [InlineData(16, SnipArrowHeads.Both)]
    public void Arrow_EveryPixelDrawn_CanBeClicked(int width, SnipArrowHeads heads)
    {
        // Arrange
        var arrow = new ArrowAnnotationModel(new Point(80, 120), new Point(400, 200), Color.Black, width, heads);
        using var image = new Bitmap(520, 320);
        using (var graphics = Graphics.FromImage(image))
        {
            graphics.Clear(Color.White);
            AnnotationRenderer.Draw(graphics, [arrow], image);
        }

        // Act: as what's drawn matters, the faint pixels at the edges are left out
        List<Point> missed = [];
        int drawn = 0;

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                if (image.GetPixel(x, y).R < 128)
                {
                    drawn++;

                    if (!AnnotationGeometry.Hits(arrow, new Point(x, y), (_, _) => Size.Empty))
                    {
                        missed.Add(new Point(x, y));
                    }
                }
            }
        }

        // Assert
        Assert.True(drawn > 100);
        Assert.Empty(missed);
    }

    private static Bitmap RenderStep(StepAnnotationModel step)
    {
        var image = new Bitmap(120, 120);

        using var graphics = Graphics.FromImage(image);

        graphics.Clear(Color.FromArgb(0, 128, 0));
        AnnotationRenderer.Draw(graphics, [step], image);

        return image;
    }

    [Theory]
    [InlineData(255, 0, 0, true)]
    [InlineData(0, 0, 128, true)]
    [InlineData(255, 255, 0, false)]
    [InlineData(255, 255, 255, false)]
    public void Step_IsARoundDiscWithItsNumber_InWhiteOrBlackForTheColor(int r, int g, int b, bool isWhite)
    {
        // Arrange
        Color fill = Color.FromArgb(r, g, b);

        // Act
        using Bitmap image = RenderStep(new StepAnnotationModel(new Point(60, 60), 7, fill, 80));

        // Assert: the disc's edge is its color, its corner box is the background, and the number is in the middle
        Assert.Equal(fill.ToArgb(), image.GetPixel(60, 24).ToArgb());
        Assert.Equal(Color.FromArgb(0, 128, 0).ToArgb(), image.GetPixel(24, 24).ToArgb());

        Color ink = isWhite ? Color.White : Color.Black;
        int inkPixels = CountOf(image, color => color.ToArgb() == ink.ToArgb());

        Assert.True(inkPixels > 30, $"Only {inkPixels} pixels of the number");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(123)]
    public void Step_NumberStaysInsideItsCircle(int number)
    {
        // Arrange: on a black circle, the number is white, and the background is green
        using Bitmap image = RenderStep(new StepAnnotationModel(new Point(60, 60), number, Color.Black, 40));

        // Act: white pixels outside the circle
        int outside = 0;

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                int dx = x - 60;
                int dy = y - 60;

                if (dx * dx + dy * dy > 20 * 20 && image.GetPixel(x, y).R > 200)
                {
                    outside++;
                }
            }
        }

        // Assert
        Assert.Equal(0, outside);
        Assert.True(CountOf(image, color => color.R > 200) > 10);
    }

    [Fact]
    public void NoOutline_DrawsNoEdge()
    {
        // Act
        using Bitmap image = Render(Text(_letters, 3) with { Outline = null });

        // Assert
        Assert.Equal(0, CountOf(image, color => color.ToArgb() == _edge.ToArgb()));
    }

    [Fact]
    public void SeeThroughText_DoesntShowTheEdgeThroughItsLetters()
    {
        // Arrange: half see-through red, over white
        Color halfRed = Color.FromArgb(128, _letters);

        // Act
        using Bitmap image = Render(Text(halfRed, 3));

        // Assert: the letters are red and white mixed, with no green from the edge under them
        Assert.True(CountOf(image, color => color.R > 200 && color.G is > 90 and < 170 && color.G == color.B) > 100);
    }

    [Fact]
    public void SeeThroughText_HasASeeThroughEdge()
    {
        // Act
        using Bitmap image = Render(Text(Color.FromArgb(128, _letters), 3));

        // Assert: the solid edge's color never shows
        Assert.Equal(0, CountOf(image, color => color.ToArgb() == _edge.ToArgb()));
    }
}
