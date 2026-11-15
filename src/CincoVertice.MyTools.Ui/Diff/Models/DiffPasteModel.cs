namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>Text pasted as one side, in place of its file.</summary>
public sealed record DiffPasteModel(DiffSide Side, string Text);
