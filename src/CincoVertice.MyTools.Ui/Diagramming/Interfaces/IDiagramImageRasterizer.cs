using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Interfaces;

/// <summary>
///     Draws an SVG image as pixels, for a file that can't hold it as SVG, such as a PDF.
/// </summary>
public interface IDiagramImageRasterizer
{
    /// <param name="markup">The image, as <see cref="Services.DiagramSvgImage.Prepare"/> gives it.</param>
    /// <param name="width">In pixels, at least 1.</param>
    /// <param name="height">In pixels, at least 1.</param>
    /// <returns>The image scaled to fit, keeping its proportions; null when it can't be drawn.</returns>
    DiagramRasterModel? Rasterize(string markup, int width, int height);
}
