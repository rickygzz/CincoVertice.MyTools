using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     A straight line with an arrow head at <paramref name="To"/>.
/// </summary>
/// <param name="Width">The line width, in pixels.</param>
public sealed record ArrowAnnotationModel(Point From, Point To, Color Color, int Width) : AnnotationModel(Color);
