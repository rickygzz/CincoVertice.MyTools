namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>A side's lines after the user changed them.</summary>
public sealed record DiffEditModel(DiffSide Side, IReadOnlyList<string> Lines);
