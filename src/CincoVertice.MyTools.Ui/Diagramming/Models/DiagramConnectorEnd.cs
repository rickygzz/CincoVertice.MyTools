namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     One of the two ends of a connector.
/// </summary>
public enum DiagramConnectorEnd
{
    /// <summary>The end at <see cref="DiagramConnectorModel.FromShapeId"/>.</summary>
    Start,

    /// <summary>The end at <see cref="DiagramConnectorModel.ToShapeId"/>.</summary>
    End,
}
