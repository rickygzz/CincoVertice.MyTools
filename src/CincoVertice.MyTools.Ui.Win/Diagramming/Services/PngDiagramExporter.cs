using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;

namespace CincoVertice.MyTools.Ui.Win.Diagramming.Services;

/// <summary>
///     Draws the diagram as the canvas does, without the grid or the selection, on white, into a PNG file. The picture
///     is the part of the diagram the SVG and PDF files show. A failed export leaves the old file as it was. Use it on
///     the UI thread only: its images are drawn by Direct2D there.
/// </summary>
public sealed class PngDiagramExporter : IDiagramPngExporter
{
    /// <summary>The longest side a picture can have; a larger diagram gets fewer pixels per diagram pixel.</summary>
    public const int MaxSide = 8000;

    public void Export(string path, DiagramModel diagram, int scale)
    {
        using Bitmap picture = Draw(diagram, scale);

        AtomicFile.Write(path, temporaryPath => picture.Save(temporaryPath, ImageFormat.Png));
    }

    /// <returns>The picture; the caller disposes it.</returns>
    public static Bitmap Draw(DiagramModel diagram, int scale)
    {
        Rectangle area = DiagramArea.Of(diagram);
        float zoom = Math.Min(Math.Max(scale, 1), (float)MaxSide / Math.Max(area.Width, area.Height));
        var picture = new Bitmap(
            Math.Max(1, (int)Math.Ceiling(area.Width * zoom)),
            Math.Max(1, (int)Math.Ceiling(area.Height * zoom)));

        using Graphics graphics = Graphics.FromImage(picture);

        graphics.Clear(Color.White);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.ScaleTransform(zoom, zoom);
        graphics.TranslateTransform(-area.X, -area.Y);
        DiagramRenderer.DrawDiagram(graphics, diagram, new HashSet<int>(), zoom);

        return picture;
    }
}
