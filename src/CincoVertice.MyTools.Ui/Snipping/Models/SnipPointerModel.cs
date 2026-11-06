using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     Where the user pressed the mouse button on the snip, and with which key held.
/// </summary>
/// <param name="Location">In snip pixels.</param>
/// <param name="Shift">True with Shift held: the select tool then adds to what's selected.</param>
public sealed record SnipPointerModel(Point Location, bool Shift = false);
