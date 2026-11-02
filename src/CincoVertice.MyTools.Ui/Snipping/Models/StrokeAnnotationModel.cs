using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     A freehand line, from the pen or the highlighter. A single point is a dot.
/// </summary>
/// <param name="Points">The points in the order they were drawn; at least one.</param>
/// <param name="Width">The line width, in pixels.</param>
public sealed record StrokeAnnotationModel(IReadOnlyList<Point> Points, Color Color, int Width)
    : AnnotationModel(Color);
