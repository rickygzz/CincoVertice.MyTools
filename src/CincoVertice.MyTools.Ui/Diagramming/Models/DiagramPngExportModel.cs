namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     Where to export a PNG picture, and how large.
/// </summary>
/// <param name="Path">The file.</param>
/// <param name="Scale">Pixels in the picture per diagram pixel: 1 or 2.</param>
public sealed record DiagramPngExportModel(string Path, int Scale);
