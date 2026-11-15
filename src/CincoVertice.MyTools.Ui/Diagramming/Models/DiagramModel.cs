namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A whole diagram. Immutable, so undo only has to keep the earlier ones.
/// </summary>
/// <param name="Shapes">The shapes; later ones are drawn over earlier ones.</param>
/// <param name="Connectors">The connectors, drawn over the shapes.</param>
/// <param name="Theme">The name of its color theme; null for the default one, as in a file from before themes.</param>
public sealed record DiagramModel(
    IReadOnlyList<DiagramShapeModel> Shapes,
    IReadOnlyList<DiagramConnectorModel> Connectors,
    string? Theme = null)
{
    public static DiagramModel Empty { get; } = new([], []);

    /// <summary>An id that no shape or connector has.</summary>
    public int NextId => Shapes.Select(shape => shape.Id)
        .Concat(Connectors.Select(connector => connector.Id))
        .DefaultIfEmpty(0)
        .Max() + 1;

    /// <summary>A group id that no shape has, as its group or as a group inside it.</summary>
    public int NextGroupId => Shapes
        .SelectMany(shape => (shape.InnerGroupIds ?? []).Append(shape.GroupId ?? 0))
        .DefaultIfEmpty(0)
        .Max() + 1;
}
