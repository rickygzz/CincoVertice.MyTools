using System.Drawing;

namespace CincoVertice.MyTools.Ui.Zones.Models;

/// <summary>A monitor, in screen pixels.</summary>
/// <param name="Bounds">All of it, which tells it apart from the other monitors across restarts.</param>
/// <param name="WorkArea">It without the taskbar, where the zones go.</param>
public sealed record ZoneMonitorModel(Rectangle Bounds, Rectangle WorkArea);
