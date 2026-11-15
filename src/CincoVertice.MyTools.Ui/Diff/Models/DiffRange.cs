namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>Characters of a line, from <see cref="Start"/>, as positions in the line's text.</summary>
public readonly record struct DiffRange(int Start, int Length)
{
    public int End => Start + Length;
}
