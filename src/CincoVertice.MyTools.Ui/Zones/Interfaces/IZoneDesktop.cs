using System.Drawing;
using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Zones.Interfaces;

/// <summary>The monitors and the windows of every program, in screen pixels.</summary>
public interface IZoneDesktop
{
    /// <returns>Every monitor connected now.</returns>
    IReadOnlyList<ZoneMonitorModel> GetMonitors();

    /// <returns>The monitor the window is on, or the nearest one.</returns>
    ZoneMonitorModel GetMonitorOf(nint window);

    /// <returns>The window the user works in; 0 when there is none.</returns>
    nint GetActiveWindow();

    /// <returns>The window as the user sees it.</returns>
    Rectangle GetWindowBounds(nint window);

    /// <summary>Moves and sizes the window so that what the user sees of it fills the bounds.</summary>
    /// <returns>Why it couldn't, for the user; null when it moved.</returns>
    string? MoveWindow(nint window, Rectangle bounds);
}
