namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     Rows dragged in the layers panel, to put them in front of or behind the others.
/// </summary>
/// <param name="Ids">The shapes and connectors dragged.</param>
/// <param name="BeforeId">The row they were dropped above, so they go in front of it; null for the bottom.</param>
public sealed record DiagramLayerMoveModel(IReadOnlyList<int> Ids, int? BeforeId);
