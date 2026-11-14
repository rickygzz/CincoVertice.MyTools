using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     An image of the catalog dragged from the images panel onto the canvas.
/// </summary>
/// <param name="Path">Which one, as <see cref="DiagramCatalogImageModel.Path"/>.</param>
/// <param name="Location">Where it was dropped, in canvas pixels.</param>
public sealed record DiagramCatalogDropModel(string Path, Point Location);
