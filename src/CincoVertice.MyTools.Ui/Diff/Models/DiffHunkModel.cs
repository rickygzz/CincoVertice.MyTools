namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>A change: rows next to each other that aren't the same, between rows that are.</summary>
public sealed record DiffHunkModel(int FirstRow, int RowCount)
{
    public int EndRow => FirstRow + RowCount;

    public bool Contains(int row) => row >= FirstRow && row < EndRow;
}
