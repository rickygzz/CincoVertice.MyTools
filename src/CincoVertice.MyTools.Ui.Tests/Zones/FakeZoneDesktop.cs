using System.Drawing;
using CincoVertice.MyTools.Ui.Zones.Interfaces;
using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Tests.Zones;

/// <summary>
///     Two monitors side by side, 1000 by 600, with one window, and two zones a monitor in halves without a gap.
///     Records where windows are moved.
/// </summary>
internal sealed class FakeZoneDesktop : IZoneDesktop, IZoneLayoutSource
{
    public const nint Window = 42;

    public static Rectangle Left { get; } = new(0, 0, 1000, 600);

    public static Rectangle Right { get; } = new(1000, 0, 1000, 600);

    public nint ActiveWindow { get; set; } = Window;

    /// <summary>The window as the user sees it; on the left monitor at first.</summary>
    public Rectangle WindowBounds { get; set; } = new(100, 100, 300, 200);

    public ZoneLayoutsModel Layouts { get; set; } =
        new(new ZoneLayoutModel(ZoneLayoutModel.Columns(ZoneLayoutModel.Equal(2)), 0), []);

    /// <summary>The default layout, for every monitor without its own.</summary>
    public ZoneLayoutModel Layout
    {
        get => Layouts.DefaultLayout;
        set => Layouts = Layouts with { DefaultLayout = value };
    }

    /// <summary>When set, MoveWindow fails with it.</summary>
    public string? MoveProblem { get; set; }

    public List<(nint Window, Rectangle Bounds)> Moves { get; } = [];

    public int Loads { get; private set; }

    public IReadOnlyList<ZoneMonitorModel> GetMonitors() => [new(Left, Left), new(Right, Right)];

    public ZoneMonitorModel GetMonitorOf(nint window) =>
        WindowBounds.Left >= Right.Left ? new(Right, Right) : new(Left, Left);

    public nint GetActiveWindow() => ActiveWindow;

    public Rectangle GetWindowBounds(nint window) => WindowBounds;

    public string? MoveWindow(nint window, Rectangle bounds)
    {
        if (MoveProblem is not null)
        {
            return MoveProblem;
        }

        Moves.Add((window, bounds));
        WindowBounds = bounds;

        return null;
    }

    public ZoneLayoutsModel Load()
    {
        Loads++;

        return Layouts;
    }
}
