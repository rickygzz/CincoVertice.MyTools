using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     A straight line with an arrow head at one or both ends.
/// </summary>
/// <param name="From">Where the drag started.</param>
/// <param name="To">Where the drag ended.</param>
/// <param name="Width">The line width, in pixels.</param>
/// <param name="Heads">Which ends have a head.</param>
public sealed record ArrowAnnotationModel(
    Point From,
    Point To,
    Color Color,
    int Width,
    SnipArrowHeads Heads = SnipArrowHeads.End)
    : AnnotationModel(Color);
