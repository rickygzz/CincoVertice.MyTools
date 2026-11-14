using System.Drawing;

namespace CincoVertice.MyTools.Ui.Zones.Models;

/// <summary>A monitor's own layout, instead of the default one.</summary>
/// <param name="Bounds">The monitor it's for, as <see cref="ZoneMonitorModel.Bounds"/>.</param>
public sealed record MonitorZoneLayoutModel(Rectangle Bounds, ZoneLayoutModel Layout);
