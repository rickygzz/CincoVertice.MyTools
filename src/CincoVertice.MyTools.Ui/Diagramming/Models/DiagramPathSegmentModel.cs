using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A part of a figure.
/// </summary>
public sealed record DiagramPathSegmentModel(DiagramPathOperation Operation, IReadOnlyList<PointF> Points);
