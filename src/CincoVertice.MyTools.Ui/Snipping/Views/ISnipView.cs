using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Views;

/// <summary>
///     Shows a frozen screenshot over every monitor and lets the user drag a rectangle on it. Points and rectangles
///     are in screenshot pixels: (0, 0) is the screenshot's top left corner.
/// </summary>
public interface ISnipView
{
    /// <summary>Raised when the user starts dragging (left button down).</summary>
    event EventHandler<Point>? SelectionStarted;

    /// <summary>Raised while the user drags.</summary>
    event EventHandler<Point>? SelectionMoved;

    /// <summary>Raised when the user stops dragging (left button up).</summary>
    event EventHandler<Point>? SelectionEnded;

    /// <summary>Raised when the user gives up, e.g. Esc, a right click, or switching to another window.</summary>
    event EventHandler? CancelRequested;

    /// <summary>Raised once the view is closed, however it was closed.</summary>
    event EventHandler? ViewClosed;

    /// <summary>Shows the selected area undimmed, with its size next to it.</summary>
    void ShowSelection(Rectangle selection, string sizeText);

    /// <summary>Removes the selection, so the whole screenshot is dimmed again.</summary>
    void ClearSelection();

    void Close();
}
