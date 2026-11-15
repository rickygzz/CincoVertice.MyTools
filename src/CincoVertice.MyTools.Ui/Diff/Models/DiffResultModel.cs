namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>Two texts compared: their rows side by side, and the changes among them, in order.</summary>
/// <param name="Added">Rows with a line only on the right.</param>
/// <param name="Removed">Rows with a line only on the left.</param>
/// <param name="Changed">Rows with a different line on each side.</param>
public sealed record DiffResultModel(
    IReadOnlyList<DiffRowModel> Rows,
    IReadOnlyList<DiffHunkModel> Hunks,
    int Added,
    int Removed,
    int Changed)
{
    public bool HasChanges => Hunks.Count > 0;
}
