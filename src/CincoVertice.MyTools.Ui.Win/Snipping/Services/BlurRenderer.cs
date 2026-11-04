using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     Draws a blur that can't be reversed. A real blur or pixelation is worked out from the pixels it covers, and
///     can be partly undone: there are tools that read pixelated text. So this one never reads them. It's the color
///     of the pixels around the area, which anyone can see anyway, with a fixed cloudy pattern over it, and it
///     replaces what's under it completely.
/// </summary>
public static class BlurRenderer
{
    private const int TileSize = 256;
    private const int TileMask = TileSize - 1;

    // For a blur with nothing around it, e.g. over the whole snip
    private static readonly Color _noSurroundings = Color.FromArgb(128, 128, 128);

    // Lightness to add to each pixel. Made once with a fixed seed, so a blur looks the same however often it's drawn
    private static readonly Lazy<sbyte[]> _pattern = new(CreatePattern);

    /// <param name="graphics">Where to draw it; a pixel of it must be a pixel of the snip.</param>
    /// <param name="blur">What to draw.</param>
    /// <param name="allBlurs">Every blur on the snip, whose pixels can't give it its color.</param>
    /// <param name="snip">The snip without annotations.</param>
    public static void Draw(
        Graphics graphics,
        BlurAnnotationModel blur,
        IReadOnlyList<Rectangle> allBlurs,
        Bitmap snip)
    {
        Rectangle area = Rectangle.Intersect(blur.Bounds, new Rectangle(Point.Empty, snip.Size));

        // Not IsEmpty, which is false for no size away from (0, 0): as a blur is when the button has just been pressed
        if (area.Width == 0 || area.Height == 0)
        {
            return;
        }

        using Bitmap patch = CreatePatch(area, ColorAround(area, allBlurs, snip));
        GraphicsState state = graphics.Save();

        // Pixel for pixel and replacing what's there: scaling or blending would let the pixels under it show at its edges
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.DrawImage(patch, area, new Rectangle(Point.Empty, area.Size), GraphicsUnit.Pixel);
        graphics.Restore(state);
    }

    /// <summary>
    ///     The average of the line of pixels just outside the area. Pixels under another blur don't count, or this one
    ///     would give away something of what that one hides.
    /// </summary>
    private static Color ColorAround(Rectangle area, IReadOnlyList<Rectangle> allBlurs, Bitmap snip)
    {
        long red = 0;
        long green = 0;
        long blue = 0;
        long count = 0;

        void Add(int x, int y)
        {
            if (x < 0 || y < 0 || x >= snip.Width || y >= snip.Height
                || allBlurs.Any(other => other.Contains(x, y)))
            {
                return;
            }

            Color color = snip.GetPixel(x, y);

            red += color.R;
            green += color.G;
            blue += color.B;
            count++;
        }

        for (int x = area.Left - 1; x <= area.Right; x++)
        {
            Add(x, area.Top - 1);
            Add(x, area.Bottom);
        }

        for (int y = area.Top; y < area.Bottom; y++)
        {
            Add(area.Left - 1, y);
            Add(area.Right, y);
        }

        return count == 0
            ? _noSurroundings
            : Color.FromArgb((int)(red / count), (int)(green / count), (int)(blue / count));
    }

    /// <summary>The pattern depends only on where a pixel is, so it stays still while a blur is dragged larger.</summary>
    private static Bitmap CreatePatch(Rectangle area, Color color)
    {
        var patch = new Bitmap(area.Width, area.Height, PixelFormat.Format24bppRgb);
        BitmapData data = patch.LockBits(
            new Rectangle(Point.Empty, area.Size),
            ImageLockMode.WriteOnly,
            PixelFormat.Format24bppRgb);
        sbyte[] pattern = _pattern.Value;
        byte[] row = new byte[area.Width * 3];

        try
        {
            for (int y = 0; y < area.Height; y++)
            {
                int patternRow = ((area.Y + y) & TileMask) * TileSize;

                for (int x = 0; x < area.Width; x++)
                {
                    int lightness = pattern[patternRow + ((area.X + x) & TileMask)];

                    row[3 * x] = (byte)Math.Clamp(color.B + lightness, 0, 255);
                    row[3 * x + 1] = (byte)Math.Clamp(color.G + lightness, 0, 255);
                    row[3 * x + 2] = (byte)Math.Clamp(color.R + lightness, 0, 255);
                }

                Marshal.Copy(row, 0, data.Scan0 + y * data.Stride, row.Length);
            }
        }
        finally
        {
            patch.UnlockBits(data);
        }

        return patch;
    }

    /// <summary>Soft clouds of two sizes, repeating without a seam, with a little grain like frosted glass.</summary>
    private static sbyte[] CreatePattern()
    {
        var random = new Random(20261103);
        float[] large = CreateGrid(random, 8);
        float[] small = CreateGrid(random, 32);
        sbyte[] pattern = new sbyte[TileSize * TileSize];

        for (int y = 0; y < TileSize; y++)
        {
            for (int x = 0; x < TileSize; x++)
            {
                float value = 16 * Smooth(large, 8, x, y) + 8 * Smooth(small, 32, x, y) + 3 * (random.NextSingle() - 0.5f);

                pattern[y * TileSize + x] = (sbyte)Math.Clamp(value, -30, 30);
            }
        }

        return pattern;
    }

    /// <summary>A square of random values from -1 to 1.</summary>
    private static float[] CreateGrid(Random random, int cells)
    {
        float[] grid = new float[cells * cells];

        for (int i = 0; i < grid.Length; i++)
        {
            grid[i] = 2 * random.NextSingle() - 1;
        }

        return grid;
    }

    /// <summary>The grid's value at a pixel, gliding between its corners; past the last cell it starts over.</summary>
    private static float Smooth(float[] grid, int cells, int x, int y)
    {
        float cellSize = TileSize / (float)cells;
        float gridX = x / cellSize;
        float gridY = y / cellSize;
        int left = (int)gridX;
        int top = (int)gridY;
        int right = (left + 1) % cells;
        int bottom = (top + 1) % cells;
        float across = Ease(gridX - left);
        float down = Ease(gridY - top);
        float upper = grid[top * cells + left] + (grid[top * cells + right] - grid[top * cells + left]) * across;
        float lower = grid[bottom * cells + left] + (grid[bottom * cells + right] - grid[bottom * cells + left]) * across;

        return upper + (lower - upper) * down;
    }

    private static float Ease(float value) => value * value * (3 - 2 * value);
}
