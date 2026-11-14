using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Win.Diagramming.Services;

/// <summary>
///     Draws SVG images as pixels with <see cref="SvgImageRenderer"/>, so a PDF shows them as the canvas does. Use it
///     on the UI thread only.
/// </summary>
public sealed class Direct2DImageRasterizer : IDiagramImageRasterizer
{
    public DiagramRasterModel? Rasterize(string markup, int width, int height)
    {
        width = Math.Max(width, 1);
        height = Math.Max(height, 1);

        if (SvgImageRenderer.RenderPixels(markup, width, height) is not byte[] pixels)
        {
            return null;
        }

        int count = width * height;
        byte[] rgb = new byte[count * 3];
        byte[] alpha = new byte[count];

        for (int i = 0; i < count; i++)
        {
            byte a = pixels[(i * 4) + 3];

            alpha[i] = a;

            if (a == 0)
            {
                continue;
            }

            // Direct2D's colors are multiplied by the alpha, which a PDF's soft mask applies again
            rgb[i * 3] = Unmultiplied(pixels[(i * 4) + 2], a);
            rgb[(i * 3) + 1] = Unmultiplied(pixels[(i * 4) + 1], a);
            rgb[(i * 3) + 2] = Unmultiplied(pixels[i * 4], a);
        }

        return new DiagramRasterModel(width, height, rgb, alpha);
    }

    private static byte Unmultiplied(byte color, byte alpha) =>
        (byte)Math.Min(255, ((color * 255) + (alpha / 2)) / alpha);
}
