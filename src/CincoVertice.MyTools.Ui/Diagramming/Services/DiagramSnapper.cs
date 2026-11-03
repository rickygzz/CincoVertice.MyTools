using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     Lines things up while they're dragged: with the edges and middles of the other shapes, showing a guide, or
///     else with the grid.
/// </summary>
public static class DiagramSnapper
{
    /// <summary>The distance between the lines of the grid, in diagram pixels.</summary>
    public const int GridSize = 10;

    /// <summary>Snaps a whole box: whichever of its edges or middle is closest to something in each direction.</summary>
    /// <param name="box">The box being dragged.</param>
    /// <param name="others">The shapes to line up with.</param>
    /// <param name="tolerance">How close it has to be, in diagram pixels.</param>
    public static DiagramSnapModel SnapBox(Rectangle box, IReadOnlyList<Rectangle> others, int tolerance)
    {
        int centerX = box.Left + box.Width / 2;
        int centerY = box.Top + box.Height / 2;
        int deltaX = Closest([box.Left, centerX, box.Right], others, vertical: true, tolerance)
            ?? Grid(box.Left) - box.Left;
        int deltaY = Closest([box.Top, centerY, box.Bottom], others, vertical: false, tolerance)
            ?? Grid(box.Top) - box.Top;

        var snapped = new Rectangle(box.X + deltaX, box.Y + deltaY, box.Width, box.Height);
        List<DiagramGuideModel> guides = [];

        AddGuides(guides, snapped, [snapped.Left, snapped.Left + snapped.Width / 2, snapped.Right], others, vertical: true);
        AddGuides(guides, snapped, [snapped.Top, snapped.Top + snapped.Height / 2, snapped.Bottom], others, vertical: false);

        return new DiagramSnapModel(deltaX, deltaY, guides);
    }

    /// <summary>Snaps a single position, like a point or the one edge of a box being scaled.</summary>
    public static DiagramSnapModel SnapPoint(Point point, IReadOnlyList<Rectangle> others, int tolerance)
    {
        int deltaX = Closest([point.X], others, vertical: true, tolerance) ?? Grid(point.X) - point.X;
        int deltaY = Closest([point.Y], others, vertical: false, tolerance) ?? Grid(point.Y) - point.Y;
        var snapped = new Rectangle(point.X + deltaX, point.Y + deltaY, 0, 0);
        List<DiagramGuideModel> guides = [];

        AddGuides(guides, snapped, [snapped.X], others, vertical: true);
        AddGuides(guides, snapped, [snapped.Y], others, vertical: false);

        return new DiagramSnapModel(deltaX, deltaY, guides);
    }

    private static int Grid(int value) => (int)Math.Round(value / (double)GridSize) * GridSize;

    private static int[] LinesOf(Rectangle other, bool vertical) => vertical
        ? [other.Left, other.Left + other.Width / 2, other.Right]
        : [other.Top, other.Top + other.Height / 2, other.Bottom];

    /// <returns>The shortest shift that lines up one of the values with a shape; null when none is close enough.</returns>
    private static int? Closest(int[] values, IReadOnlyList<Rectangle> others, bool vertical, int tolerance)
    {
        int? closest = null;

        foreach (Rectangle other in others)
        {
            foreach (int line in LinesOf(other, vertical))
            {
                foreach (int value in values)
                {
                    int delta = line - value;

                    if (Math.Abs(delta) <= tolerance && (closest is null || Math.Abs(delta) < Math.Abs(closest.Value)))
                    {
                        closest = delta;
                    }
                }
            }
        }

        return closest;
    }

    /// <summary>One guide per line that's lined up, long enough to reach all the shapes on it.</summary>
    private static void AddGuides(
        List<DiagramGuideModel> guides,
        Rectangle box,
        int[] values,
        IReadOnlyList<Rectangle> others,
        bool vertical)
    {
        foreach (int value in values.Distinct())
        {
            Rectangle[] lined = [.. others.Where(other => LinesOf(other, vertical).Contains(value))];

            if (lined.Length == 0)
            {
                continue;
            }

            int start = lined.Min(other => vertical ? other.Top : other.Left);
            int end = lined.Max(other => vertical ? other.Bottom : other.Right);

            start = Math.Min(start, vertical ? box.Top : box.Left);
            end = Math.Max(end, vertical ? box.Bottom : box.Right);

            guides.Add(vertical
                ? new DiagramGuideModel(new Point(value, start), new Point(value, end))
                : new DiagramGuideModel(new Point(start, value), new Point(end, value)));
        }
    }
}
