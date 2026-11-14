namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     The shapes a diagram can have. A text and an image have no outline of their own.
/// </summary>
public enum DiagramShapeKind
{
    Rectangle,
    RoundedRectangle,
    Ellipse,
    Diamond,
    Parallelogram,
    Hexagon,
    Cylinder,
    Document,
    Text,

    /// <summary>An SVG image, such as an icon, drawn to fit the shape; its text goes under it.</summary>
    Image,
}
