using System.Drawing;
using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Zones.Services;

/// <summary>Where the zones of a layout are on a monitor, and which one a point or a window is in.</summary>
public static class ZoneCalculator
{
    // How far a window can be from a zone, in pixels, and still be in it: programs round their sizes
    private const int SameZoneTolerance = 4;

    /// <param name="layout">The zones, in percent.</param>
    /// <param name="workArea">The monitor's work area, in screen pixels.</param>
    /// <returns>Each zone in screen pixels, in the layout's order, with the gap around it.</returns>
    public static IReadOnlyList<Rectangle> Place(ZoneLayoutModel layout, Rectangle workArea)
    {
        // Half the gap inside the work area, and half around each zone: a whole gap between zones and at the edges
        int half = layout.Gap / 2;
        Rectangle area = Rectangle.Inflate(workArea, -half, -half);

        return
        [
            .. layout.Zones.Select(zone =>
            {
                int left = area.Left + Percent(area.Width, zone.Left);
                int top = area.Top + Percent(area.Height, zone.Top);
                int right = area.Left + Percent(area.Width, zone.Left + zone.Width);
                int bottom = area.Top + Percent(area.Height, zone.Top + zone.Height);
                Rectangle bounds = Rectangle.FromLTRB(left + half, top + half, right - half, bottom - half);

                return new Rectangle(bounds.Location, new Size(Math.Max(1, bounds.Width), Math.Max(1, bounds.Height)));
            }),
        ];
    }

    /// <param name="zones">The zones, in screen pixels.</param>
    /// <param name="point">In screen pixels.</param>
    /// <param name="gap">
    ///     The gap between the zones: a point in it is in the zones on either side, so no zone goes off while the mouse
    ///     crosses it.
    /// </param>
    /// <returns>
    ///     The index of the zone the point is in; of the smallest, when it's in several; null when it's in none.
    /// </returns>
    public static int? ZoneAt(IReadOnlyList<Rectangle> zones, Point point, int gap)
    {
        int? found = null;
        int reach = (gap / 2) + 1;

        for (int i = 0; i < zones.Count; i++)
        {
            bool isIn = Rectangle.Inflate(zones[i], reach, reach).Contains(point);

            if (isIn && (found is null || Area(zones[i]) < Area(zones[found.Value])))
            {
                found = i;
            }
        }

        return found;
    }

    /// <returns>The index of the zone the window fills, as far as a few pixels; null when it fills none.</returns>
    public static int? ZoneOf(IReadOnlyList<Rectangle> zones, Rectangle window)
    {
        for (int i = 0; i < zones.Count; i++)
        {
            Rectangle zone = zones[i];

            if (Math.Abs(zone.Left - window.Left) <= SameZoneTolerance
                && Math.Abs(zone.Top - window.Top) <= SameZoneTolerance
                && Math.Abs(zone.Right - window.Right) <= SameZoneTolerance
                && Math.Abs(zone.Bottom - window.Bottom) <= SameZoneTolerance)
            {
                return i;
            }
        }

        return null;
    }

    private static int Percent(int length, double percent) => (int)Math.Round(length * percent / 100);

    private static long Area(Rectangle rectangle) => (long)rectangle.Width * rectangle.Height;
}
