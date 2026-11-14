using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Snipping;

/// <summary>With the real Direct2D and the emoji font of Windows.</summary>
public class ColorEmojiRendererTests
{
    // Whether a pixel is a color, not black, white or gray: what GDI+ would draw an emoji in
    private static bool IsColored(Color pixel) =>
        pixel.A > 0
        && Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) - Math.Min(pixel.R, Math.Min(pixel.G, pixel.B)) > 30;

    private static int ColoredPixels(Bitmap image)
    {
        int count = 0;

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                if (IsColored(image.GetPixel(x, y)))
                {
                    count++;
                }
            }
        }

        return count;
    }

    [Theory]
    [InlineData("😀")]
    [InlineData("👍")]
    [InlineData("❤️")]
    [InlineData("👍🏽")]
    public void Render_DrawsTheEmojiInColor_OnATransparentSquare(string emoji)
    {
        // Act
        Bitmap image = ColorEmojiRenderer.Render(emoji, 48);

        // Assert: mostly color, and the corners see-through
        Assert.Equal(new Size(48, 48), image.Size);
        Assert.True(ColoredPixels(image) > 48 * 48 / 4, $"{emoji} isn't in color");
        Assert.Equal(0, image.GetPixel(0, 0).A);
    }

    [Fact]
    public void Render_TheSameEmojiAndSize_IsDrawnOnce()
    {
        // Act
        Bitmap first = ColorEmojiRenderer.Render("🔥", 32);
        Bitmap second = ColorEmojiRenderer.Render("🔥", 32);

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void AnEmojiAnnotation_IsDrawnInColor_WhereItIs_AsSeeThroughAsItIs()
    {
        // Arrange
        using var snip = new Bitmap(200, 100);
        using (var graphics = Graphics.FromImage(snip))
        {
            graphics.Clear(Color.White);
        }

        using var solid = new Bitmap(snip);
        using var faint = new Bitmap(snip);

        // Act
        using (var graphics = Graphics.FromImage(solid))
        {
            AnnotationRenderer.Draw(
                graphics,
                [new EmojiAnnotationModel(new Point(50, 50), "🔥", Color.Black, 64)],
                snip);
        }

        using (var graphics = Graphics.FromImage(faint))
        {
            AnnotationRenderer.Draw(
                graphics,
                [new EmojiAnnotationModel(new Point(50, 50), "🔥", Color.FromArgb(64, 0, 0, 0), 64)],
                snip);
        }

        // Assert: in its square, not right of it, and fainter when see-through
        Assert.True(IsColored(solid.GetPixel(50, 55)));
        Assert.Equal(Color.White.ToArgb(), solid.GetPixel(150, 50).ToArgb());

        Color strong = solid.GetPixel(50, 55);
        Color weak = faint.GetPixel(50, 55);
        Assert.True(weak.G > strong.G, "a see-through emoji should show more of the white under it");
    }
}
