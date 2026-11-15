using System.Drawing;

namespace CincoVertice.MyTools.Ui.Zones.Models;

/// <summary>Where the mouse is while a window is dragged, and the keys and buttons for the zones.</summary>
/// <param name="Cursor">In screen pixels.</param>
/// <param name="IsShiftDown">True while Shift is held: a press shows the zones, the next hides them.</param>
/// <param name="IsResizing">True when the window is being resized by an edge, not moved: no zones then.</param>
/// <param name="IsRightButtonDown">True while the right mouse button is held: a click works as Shift.</param>
/// <param name="IsEscapeDown">True when Esc is down, which cancels the drag: the window goes back, into no zone.</param>
public sealed record ZoneDragPointerModel(
    Point Cursor,
    bool IsShiftDown,
    bool IsResizing = false,
    bool IsRightButtonDown = false,
    bool IsEscapeDown = false);
