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
    ///     The index of the zone the point is in; null when it's in none. When it's in several, the one whose center is
    ///     nearest, so each of zones that overlap is the one chosen around its own middle; of those as near, the
    ///     smallest, so a zone inside another with the same center can still be chosen.
    /// </returns>
    public static int? ZoneAt(IReadOnlyList<Rectangle> zones, Point point, int gap)
    {
        int reach = (gap / 2) + 1;
        List<(double Left, double Top, double Width, double Height)> found = [];
        List<int> indexes = [];

        for (int i = 0; i < zones.Count; i++)
        {
            if (Rectangle.Inflate(zones[i], reach, reach).Contains(point))
            {
                Rectangle zone = zones[i];
                found.Add((zone.Left, zone.Top, zone.Width, zone.Height));
                indexes.Add(i);
            }
        }

        return Nearest(found, point.X, point.Y) is int index ? indexes[index] : null;
    }

    /// <summary>
    ///     Of zones that a point is in, the one whose center is nearest the point; of those as near, the smallest.
    /// </summary>
    /// <param name="zones">Left, top, width and height of each, in any unit, the point's too.</param>
    /// <returns>Its index in the list; null for an empty list.</returns>
    public static int? Nearest(
        IReadOnlyList<(double Left, double Top, double Width, double Height)> zones,
        double x,
        double y)
    {
        int? nearest = null;
        double bestDistance = double.MaxValue;
        double bestArea = double.MaxValue;

        for (int i = 0; i < zones.Count; i++)
        {
            (double left, double top, double width, double height) = zones[i];
            double dx = left + (width / 2) - x;
            double dy = top + (height / 2) - y;
            double distance = (dx * dx) + (dy * dy);
            double area = width * height;

            if (distance < bestDistance - 1e-9 || (Math.Abs(distance - bestDistance) <= 1e-9 && area < bestArea))
            {
                nearest = i;
                bestDistance = distance;
                bestArea = area;
            }
        }

        return nearest;
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

    /// <returns>
    ///     The order to draw zones in: as numbered, but the one on top last, so where zones overlap all of it shows.
    /// </returns>
    /// <param name="count">How many zones there are.</param>
    /// <param name="onTop">
    ///     The index of the one to draw over the others, e.g. the one highlighted; null for none.
    /// </param>
    public static IEnumerable<int> DrawingOrder(int count, int? onTop) =>
        onTop is int top && top >= 0 && top < count
            ? Enumerable.Range(0, count).Where(i => i != top).Append(top)
            : Enumerable.Range(0, count);

    private static int Percent(int length, double percent) => (int)Math.Round(length * percent / 100);
}
