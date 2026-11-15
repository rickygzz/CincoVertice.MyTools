namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>
///     Files chosen, or dropped on the window, and the side chosen for, or dropped on. Two or more go on both sides,
///     the first on the left.
/// </summary>
public sealed record DiffFilesModel(IReadOnlyList<string> Paths, DiffSide Side);
