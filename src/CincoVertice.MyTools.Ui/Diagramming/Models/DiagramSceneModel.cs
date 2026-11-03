using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     Everything the canvas draws.
/// </summary>
/// <param name="Diagram">The diagram, with the change being dragged, if any.</param>
/// <param name="SelectedShapeIds">The selected shapes.</param>
/// <param name="SelectedConnectorIds">The selected connectors.</param>
/// <param name="Viewport">Which part of the diagram to show.</param>
/// <param name="HandleBounds">The box the scale handles go around; null when there are none.</param>
/// <param name="Guides">The alignment lines to show.</param>
/// <param name="SelectionBand">The rectangle being dragged to select; null for none.</param>
/// <param name="DraftConnector">The line of a connector being dragged, in diagram pixels; null for none.</param>
/// <param name="ConnectTargetId">The shape a connector being dragged would attach to; null for none.</param>
/// <param name="Anchors">The circles to connect from or to, around the shape under the mouse; null for none.</param>
/// <param name="ConnectorEnds">The ends of the selected connectors, which can be dragged to another place.</param>
/// <param name="ConnectorBends">The bends of the selected connectors, which can be dragged or removed.</param>
public sealed record DiagramSceneModel(
    DiagramModel Diagram,
    IReadOnlySet<int> SelectedShapeIds,
    IReadOnlySet<int> SelectedConnectorIds,
    DiagramViewportModel Viewport,
    Rectangle? HandleBounds,
    IReadOnlyList<DiagramGuideModel> Guides,
    Rectangle? SelectionBand,
    (Point From, Point To)? DraftConnector,
    int? ConnectTargetId,
    DiagramAnchorsModel? Anchors = null,
    IReadOnlyList<PointF>? ConnectorEnds = null,
    IReadOnlyList<Point>? ConnectorBends = null);
