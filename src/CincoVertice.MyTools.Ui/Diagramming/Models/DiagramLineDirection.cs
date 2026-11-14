namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     Which way a line on its own may point; its ends move only so it keeps pointing that way.
/// </summary>
public enum DiagramLineDirection
{
    /// <summary>Any way.</summary>
    Free,

    /// <summary>Level: both ends at the same height.</summary>
    Horizontal,

    /// <summary>Upright: both ends at the same distance from the left.</summary>
    Vertical,
}
