namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>What a row of a comparison shows on each side.</summary>
public enum DiffRowKind
{
    /// <summary>The same line on both sides.</summary>
    Same,

    /// <summary>A line on each side, different: the left one was changed into the right one.</summary>
    Changed,

    /// <summary>A line only on the left; the right side is blank.</summary>
    Removed,

    /// <summary>A line only on the right; the left side is blank.</summary>
    Added,
}
