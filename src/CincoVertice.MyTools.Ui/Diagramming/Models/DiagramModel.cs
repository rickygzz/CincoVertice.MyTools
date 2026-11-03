namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A whole diagram. Immutable, so undo only has to keep the earlier ones.
/// </summary>
/// <param name="Shapes">The shapes; later ones are drawn over earlier ones.</param>
/// <param name="Connectors">The connectors, drawn over the shapes.</param>
public sealed record DiagramModel(
    IReadOnlyList<DiagramShapeModel> Shapes,
    IReadOnlyList<DiagramConnectorModel> Connectors)
{
    public static DiagramModel Empty { get; } = new([], []);

    /// <summary>An id that no shape or connector has.</summary>
    public int NextId => Shapes.Select(shape => shape.Id)
        .Concat(Connectors.Select(connector => connector.Id))
        .DefaultIfEmpty(0)
        .Max() + 1;

    /// <summary>A group id that no shape has.</summary>
    public int NextGroupId => Shapes.Select(shape => shape.GroupId ?? 0).DefaultIfEmpty(0).Max() + 1;
}
