namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A line attached to two shapes, which follows them when they move.
/// </summary>
/// <param name="Id">Unique in its diagram.</param>
/// <param name="FromShapeId">The shape where the drag started.</param>
/// <param name="ToShapeId">The shape where the drag ended.</param>
/// <param name="Routing">How it goes between them.</param>
/// <param name="Heads">Which ends have a head.</param>
/// <param name="Label">The text in its middle; can be empty.</param>
/// <param name="Style">How it looks.</param>
/// <param name="FromSide">The side it leaves from; null to pick the one that fits best as the shapes move.</param>
/// <param name="ToSide">The side it arrives at; null to pick the one that fits best as the shapes move.</param>
/// <param name="Bends">
///     The points it goes through on its way, in order, with straight lines between them; null to follow
///     <paramref name="Routing"/>. They stay where they are when only one of its shapes moves.
/// </param>
public sealed record DiagramConnectorModel(
    int Id,
    int FromShapeId,
    int ToShapeId,
    DiagramRouting Routing,
    DiagramArrowHeads Heads,
    string Label,
    DiagramStyleModel Style,
    DiagramSide? FromSide = null,
    DiagramSide? ToSide = null,
    IReadOnlyList<System.Drawing.Point>? Bends = null);
