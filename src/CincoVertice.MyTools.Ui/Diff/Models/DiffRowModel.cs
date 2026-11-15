namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>A row of the comparison: a line of each side, side by side, or of one side, the other blank.</summary>
public sealed record DiffRowModel(DiffRowKind Kind, DiffLineModel? Left, DiffLineModel? Right)
{
    public DiffLineModel? On(DiffSide side) => side == DiffSide.Left ? Left : Right;
}
