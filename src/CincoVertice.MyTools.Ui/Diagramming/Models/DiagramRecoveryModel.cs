namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A copy of a diagram with changes that weren't saved, kept in case the app or Windows ends before they are.
/// </summary>
/// <param name="Id">Which editor window it's from; each window keeps one copy.</param>
/// <param name="Path">The file the diagram was opened from or saved to; null for one never saved.</param>
/// <param name="Version">
///     The file's version when it was opened or saved, to tell whether it was changed elsewhere since; null for none.
/// </param>
/// <param name="Diagram">The diagram, with its changes.</param>
/// <param name="SavedAt">When the copy was made, in UTC.</param>
public sealed record DiagramRecoveryModel(
    string Id,
    string? Path,
    string? Version,
    DiagramModel Diagram,
    DateTime SavedAt);
