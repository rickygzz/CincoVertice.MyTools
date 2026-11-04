using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Models;
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
