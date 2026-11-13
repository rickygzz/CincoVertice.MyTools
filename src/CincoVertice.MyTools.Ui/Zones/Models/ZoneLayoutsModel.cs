using System.Drawing;

namespace CincoVertice.MyTools.Ui.Zones.Models;

/// <summary>
///     Every layout the user set: the default one, and the own layouts of some monitors, which may not all be
///     connected.
/// </summary>
/// <param name="DefaultLayout">For every monitor without its own.</param>
/// <param name="Monitors">The monitors with their own layout, by their bounds.</param>
public sealed record ZoneLayoutsModel(ZoneLayoutModel DefaultLayout, IReadOnlyList<MonitorZoneLayoutModel> Monitors)
{
    /// <summary>Three columns on every monitor.</summary>
    public static ZoneLayoutsModel Default { get; } = new(ZoneLayoutModel.Default, []);

    /// <returns>The monitor's own layout; the default one when it has none.</returns>
    public ZoneLayoutModel For(Rectangle monitorBounds) =>
        Monitors.FirstOrDefault(monitor => monitor.Bounds == monitorBounds)?.Layout ?? DefaultLayout;

    /// <returns>Whether both have the same zones and gaps, for the same monitors.</returns>
    public bool SameAs(ZoneLayoutsModel other) =>
        SameAs(DefaultLayout, other.DefaultLayout)
        && Monitors.Count == other.Monitors.Count
        && Monitors.All(monitor =>
            other.Monitors.Any(o => o.Bounds == monitor.Bounds && SameAs(o.Layout, monitor.Layout)));

    private static bool SameAs(ZoneLayoutModel one, ZoneLayoutModel other) =>
        one.Gap == other.Gap && one.Zones.SequenceEqual(other.Zones);
}
