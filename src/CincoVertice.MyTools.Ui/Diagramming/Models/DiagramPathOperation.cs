namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     What a part of a figure does.
/// </summary>
public enum DiagramPathOperation
{
    /// <summary>Starts at its one point.</summary>
    Move,

    /// <summary>A straight line to its one point.</summary>
    Line,

    /// <summary>A Bezier curve through its three points: two control points and the end.</summary>
    Curve,

    /// <summary>A straight line back to the start.</summary>
    Close,
}
