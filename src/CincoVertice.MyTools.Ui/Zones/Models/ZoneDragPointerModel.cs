using System.Drawing;

namespace CincoVertice.MyTools.Ui.Zones.Models;

/// <summary>Where the mouse is while a window is dragged, and whether the user asks for the zones (Shift).</summary>
/// <param name="Cursor">In screen pixels.</param>
public sealed record ZoneDragPointerModel(Point Cursor, bool ShowZones);
