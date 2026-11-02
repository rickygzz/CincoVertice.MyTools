using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     The outline of a rectangle with rounded corners.
/// </summary>
/// <param name="Width">The outline width, in pixels.</param>
public sealed record RectangleAnnotationModel(Rectangle Bounds, Color Color, int Width) : AnnotationModel(Color);
