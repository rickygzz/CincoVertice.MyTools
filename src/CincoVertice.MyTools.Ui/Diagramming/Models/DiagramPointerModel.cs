using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     What the mouse did on the canvas.
/// </summary>
/// <param name="Location">In canvas pixels.</param>
/// <param name="Shift">Adds to the selection, or keeps a shape's proportions while scaling it.</param>
/// <param name="Control">Adds to the selection.</param>
/// <param name="Alt">Turns snapping off.</param>
/// <param name="Pan">True to move the view instead, e.g. the middle button or Space is held.</param>
public sealed record DiagramPointerModel(
    Point Location,
    bool Shift = false,
    bool Control = false,
    bool Alt = false,
    bool Pan = false);
