using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Models;

namespace CincoVertice.MyTools.Ui.ColorPicker.Views;

/// <summary>
///     Shows a frozen screenshot over every monitor, with a magnifier by the pointer. Points are in screenshot
///     pixels: (0, 0) is the screenshot's top left corner.
/// </summary>
public interface IColorPickerView
{
    /// <summary>Raised when the pointer moves, and once when the view is shown, where the pointer is.</summary>
    event EventHandler<Point>? PointerMoved;

    /// <summary>Raised to see more (positive) or fewer (negative) pixels larger: the mouse wheel, + and -.</summary>
    event EventHandler<int>? ZoomRequested;

    /// <summary>Raised to move the pointer by a few pixels: the arrow keys.</summary>
    event EventHandler<Size>? NudgeRequested;

    /// <summary>Raised to pick the color under the pointer: a click, or Enter.</summary>
    event EventHandler<ColorTextFormat>? PickRequested;

    /// <summary>Raised when the user gives up, e.g. Esc or a right click.</summary>
    event EventHandler? CancelRequested;

    /// <summary>Raised when the view loses the focus: the user switched windows, or for no reason of theirs.</summary>
    event EventHandler? FocusLost;

    /// <summary>Raised once the view is closed, however it was closed.</summary>
    event EventHandler? ViewClosed;

    /// <summary>Shows the magnifier by the pixel, with its color.</summary>
    void ShowLens(ColorLensModel lens);

    /// <summary>Moves the mouse pointer onto the pixel.</summary>
    void MovePointer(Point pixel);

    /// <summary>Takes the focus back.</summary>
    void TakeFocus();

    void Close();
}
