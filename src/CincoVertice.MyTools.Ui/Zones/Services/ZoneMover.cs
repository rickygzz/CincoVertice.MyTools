using System.Drawing;
using CincoVertice.MyTools.Ui.Zones.Interfaces;

namespace CincoVertice.MyTools.Ui.Zones.Services;

/// <summary>Moves the window the user works in to a zone of its monitor, by number or to the next one.</summary>
public sealed class ZoneMover
{
    private readonly IZoneDesktop _desktop;
    private readonly IZoneLayoutSource _layouts;

    public ZoneMover(IZoneDesktop desktop, IZoneLayoutSource layouts)
    {
        _desktop = desktop;
        _layouts = layouts;
    }

    /// <param name="number">The zone, from 1.</param>
    /// <returns>Why it couldn't, for the user; null when it moved.</returns>
    public string? MoveActiveWindowTo(int number)
    {
        if (!TryGetActiveWindowZones(out nint window, out IReadOnlyList<Rectangle> zones, out string? problem))
        {
            return problem;
        }

        if (number < 1 || number > zones.Count)
        {
            return $"There's no zone {number}: there are {zones.Count}, from 1 to {zones.Count}.";
        }

        return _desktop.MoveWindow(window, zones[number - 1]);
    }

    /// <summary>
    ///     To the zone after the one the window is in, or before it; from the last zone to the first and back. A window
    ///     in no zone goes to the first one, or the last.
    /// </summary>
    /// <param name="step">1 for the next zone, -1 for the one before.</param>
    /// <returns>Why it couldn't, for the user; null when it moved.</returns>
    public string? MoveActiveWindowBy(int step)
    {
        if (!TryGetActiveWindowZones(out nint window, out IReadOnlyList<Rectangle> zones, out string? problem))
        {
            return problem;
        }

        int? current = ZoneCalculator.ZoneOf(zones, _desktop.GetWindowBounds(window));
        int next = current is int index
            ? (((index + step) % zones.Count) + zones.Count) % zones.Count
            : step > 0 ? 0 : zones.Count - 1;

        return _desktop.MoveWindow(window, zones[next]);
    }

    private bool TryGetActiveWindowZones(out nint window, out IReadOnlyList<Rectangle> zones, out string? problem)
    {
        window = _desktop.GetActiveWindow();
        zones = [];
        problem = null;

        if (window == 0)
        {
            problem = "There's no window to move.";

            return false;
        }

        zones = ZoneCalculator.Place(_layouts.Load(), _desktop.GetWorkAreaOf(window));

        return true;
    }
}
