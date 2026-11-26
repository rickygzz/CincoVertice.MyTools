using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Models;

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

    /// <summary>Raised when the pointer moves with no button down, for the pointer to show.</summary>
    event EventHandler<Point>? PointerMoved;

    /// <summary>Raised when the user takes the selection as it is (Enter).</summary>
    event EventHandler? ConfirmRequested;

    /// <summary>Raised when the user gives up, e.g. Esc or a right click.</summary>
    event EventHandler? CancelRequested;

    /// <summary>Raised when the view loses the focus, because the user switched windows or for no reason of theirs.</summary>
    event EventHandler? FocusLost;

    /// <summary>Raised once the view is closed, however it was closed.</summary>
    event EventHandler? ViewClosed;

    /// <summary>Shows the selected area undimmed, with its size next to it.</summary>
    /// <param name="handles">The points to drag to change its size, drawn as small squares; none while only dragged.</param>
    void ShowSelection(Rectangle selection, string sizeText, IReadOnlyList<Point> handles);

    /// <summary>Removes the selection, so the whole screenshot is dimmed again.</summary>
    void ClearSelection();

    /// <summary>The pointer over the screenshot.</summary>
    void ShowCursor(SnipCursor cursor);

    /// <summary>Takes the focus back.</summary>
    void TakeFocus();

    void Close();
}
