using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     The mouse wheel turned on the canvas.
/// </summary>
/// <param name="Location">In canvas pixels.</param>
/// <param name="Steps">How many notches; positive is away from the user.</param>
/// <param name="Control">Zooms around the location.</param>
/// <param name="Shift">Scrolls sideways.</param>
public sealed record DiagramWheelModel(Point Location, float Steps, bool Control = false, bool Shift = false);
