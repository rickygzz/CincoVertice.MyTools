using System.ComponentModel;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.Common.WinApi.Libs.Structs;
using CincoVertice.MyTools.Ui.Zones.Interfaces;
using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Win.Zones.Services;

/// <summary>
///     The monitors and windows, in physical pixels on every monitor: each call runs per-monitor aware, as the app is,
///     so it stays right should that change.
/// </summary>
public sealed class DesktopZones : IZoneDesktop
{
    private const int ErrorAccessDenied = 5;

    public IReadOnlyList<ZoneMonitorModel> GetMonitors()
    {
        using (DpiAwarenessScope.PerMonitorV2())
        {
            return
            [
                .. MonitorHelper.GetMonitors()
                    .Select(monitor => new ZoneMonitorModel(monitor.Bounds, monitor.WorkArea)),
            ];
        }
    }

    public ZoneMonitorModel GetMonitorOf(nint window)
    {
        using (DpiAwarenessScope.PerMonitorV2())
        {
            (Rectangle bounds, Rectangle workArea) = MonitorHelper.GetMonitor(window);

            return new ZoneMonitorModel(bounds, workArea);
        }
    }

    public nint GetActiveWindow()
    {
        nint window = WinUser.GetForegroundWindow();

        return WindowHelper.IsMovableWindow(window) ? window : 0;
    }

    public Rectangle GetWindowBounds(nint window)
    {
        using (DpiAwarenessScope.PerMonitorV2())
        {
            return WindowHelper.GetVisibleBounds(window);
        }
    }

    public string? MoveWindow(nint window, Rectangle bounds)
    {
        try
        {
            using (DpiAwarenessScope.PerMonitorV2())
            {
                WindowHelper.MoveVisibleBounds(window, bounds);
            }

            return null;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorAccessDenied)
        {
            return "Could not move the window: it runs as administrator, and Windows lets only programs that run as "
                + "administrator too move it.";
        }
        catch (Win32Exception ex)
        {
            return $"Could not move the window: {ex.Message}";
        }
    }

    public Point GetPointerPosition()
    {
        using (DpiAwarenessScope.PerMonitorV2())
        {
            User32.GetCursorPos(out POINT point);

            return new Point(point.X, point.Y);
        }
    }

    public bool IsMaximized(nint window) => User32.IsZoomed(window);

    public void SetMaximized(nint window, bool isMaximized) =>
        WinUser.ShowWindow(window, isMaximized ? SW.SW_SHOWMAXIMIZED : SW.SW_RESTORE);
}
