using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     Which part of the diagram the canvas shows.
/// </summary>
/// <param name="Zoom">Canvas pixels per diagram pixel.</param>
/// <param name="Pan">Where the diagram's (0, 0) is on the canvas.</param>
public sealed record DiagramViewportModel(float Zoom, Point Pan)
{
    public Point ToDiagram(Point canvas) => new(
        (int)Math.Round((canvas.X - Pan.X) / Zoom),
        (int)Math.Round((canvas.Y - Pan.Y) / Zoom));

    public Point ToCanvas(Point diagram) => new(
        (int)Math.Round(diagram.X * Zoom) + Pan.X,
        (int)Math.Round(diagram.Y * Zoom) + Pan.Y);
}
