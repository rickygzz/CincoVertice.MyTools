using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     The circles around a shape that a connector can start from or end on, in diagram pixels.
/// </summary>
/// <param name="Points">The middle of each circle.</param>
/// <param name="Highlighted">The middle of the one under the mouse; null for none.</param>
public sealed record DiagramAnchorsModel(IReadOnlyList<PointF> Points, PointF? Highlighted);
