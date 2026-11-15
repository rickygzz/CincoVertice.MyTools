namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>A line of one side, as its file has it.</summary>
/// <param name="Number">Its number in the file, from 1.</param>
/// <param name="Text">The line, without its line break.</param>
/// <param name="Changes">The parts that differ from the other side's line, in a changed row; empty in others.</param>
public sealed record DiffLineModel(int Number, string Text, IReadOnlyList<DiffRange> Changes);
