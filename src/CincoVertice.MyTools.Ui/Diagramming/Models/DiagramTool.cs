namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     What dragging on the canvas does. Each shape tool has the name of its <see cref="DiagramShapeKind"/>.
/// </summary>
public enum DiagramTool
{
    Select,
    Connector,

    /// <summary>Draws a line on its own, from where the drag starts to where it ends, attached to nothing.</summary>
    Line,
    Rectangle,
    RoundedRectangle,
    Ellipse,
    Diamond,
    Parallelogram,
    Hexagon,
    Cylinder,
    Document,
    Text,
}
