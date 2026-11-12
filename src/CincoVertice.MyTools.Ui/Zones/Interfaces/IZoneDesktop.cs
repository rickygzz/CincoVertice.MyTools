using System.Drawing;

namespace CincoVertice.MyTools.Ui.Zones.Interfaces;

/// <summary>The monitors and the windows of every program, in screen pixels.</summary>
public interface IZoneDesktop
{
    /// <returns>The work area of every monitor: each monitor without the taskbar.</returns>
    IReadOnlyList<Rectangle> GetWorkAreas();

    /// <returns>The work area of the monitor the window is on, or the nearest one.</returns>
    Rectangle GetWorkAreaOf(nint window);

    /// <returns>The window the user works in; 0 when there is none.</returns>
    nint GetActiveWindow();

    /// <returns>The window as the user sees it.</returns>
    Rectangle GetWindowBounds(nint window);

    /// <summary>Moves and sizes the window so that what the user sees of it fills the bounds.</summary>
    /// <returns>Why it couldn't, for the user; null when it moved.</returns>
    string? MoveWindow(nint window, Rectangle bounds);
}
