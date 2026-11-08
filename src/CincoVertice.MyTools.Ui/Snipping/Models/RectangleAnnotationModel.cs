using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     The outline of a rectangle with rounded corners.
/// </summary>
/// <param name="Width">The outline width, in pixels.</param>
/// <param name="CornerRadius">How round its corners are, in pixels; 0 for square ones.</param>
public sealed record RectangleAnnotationModel(Rectangle Bounds, Color Color, int Width, int CornerRadius = 12)
    : AnnotationModel(Color);
