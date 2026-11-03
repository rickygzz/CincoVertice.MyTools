using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     Where a connector goes, ready to draw.
/// </summary>
/// <param name="Points">The line. It stops at the base of a head, so a thick line doesn't stick out of it.</param>
/// <param name="Line">The same line to draw, with its corners rounded.</param>
/// <param name="StartHead">The triangle at the start; null for none.</param>
/// <param name="EndHead">The triangle at the end; null for none.</param>
/// <param name="LabelCenter">Halfway along the line.</param>
public sealed record DiagramConnectorPathModel(
    IReadOnlyList<PointF> Points,
    DiagramFigureModel Line,
    IReadOnlyList<PointF>? StartHead,
    IReadOnlyList<PointF>? EndHead,
    PointF LabelCenter);
