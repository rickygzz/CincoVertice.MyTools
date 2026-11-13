namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A path to draw. The same figures go to the screen and to the SVG file, so they look the same.
/// </summary>
/// <param name="Segments">The path, starting with a move.</param>
/// <param name="Filled">False for a detail drawn over the fill, e.g. the rim of a cylinder.</param>
public sealed record DiagramFigureModel(IReadOnlyList<DiagramPathSegmentModel> Segments, bool Filled = true);
