using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Services;

/// <summary>
///     Evens out the wobble of a hand-drawn line: each point becomes the average of those around it, then the points
///     that add nothing to the shape are left out, for a curve to be drawn through the rest. Its ends stay where they
///     were drawn.
/// </summary>
public static class StrokeSmoother
{
    // How many points on each side are averaged: more is smoother, but rounds off corners meant to be there
    private const int Reach = 6;

    // How far from the line through its neighbours a point must be to be kept, in pixels: lower keeps more of the
    // wobble that's left
    private const double Tolerance = 1.0;

    /// <param name="points">The points as drawn, in order.</param>
    /// <returns>Fewer points along a smoother line, the first and the last as they were.</returns>
    public static IReadOnlyList<Point> Smooth(IReadOnlyList<Point> points)
    {
        if (points.Count < 3)
        {
            return points;
        }

        PointF[] averaged = Averaged(points);
        List<PointF> kept = [averaged[0]];

        KeepFarthest(averaged, 0, averaged.Length - 1, kept);
        kept.Add(averaged[^1]);

        List<Point> smooth = [];

        foreach (PointF point in kept)
        {
            var rounded = Point.Round(point);

            // Rounding can make two neighbours the same pixel
            if (smooth.Count == 0 || smooth[^1] != rounded)
            {
                smooth.Add(rounded);
            }
        }

        return smooth;
    }

    /// <summary>Each point the average of up to <see cref="Reach"/> on each side; fewer near the ends, so they stay.</summary>
    private static PointF[] Averaged(IReadOnlyList<Point> points)
    {
        var averaged = new PointF[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            int reach = Math.Min(Reach, Math.Min(i, points.Count - 1 - i));
            float x = 0;
            float y = 0;

            for (int j = i - reach; j <= i + reach; j++)
            {
                x += points[j].X;
                y += points[j].Y;
            }

            averaged[i] = new PointF(x / (2 * reach + 1), y / (2 * reach + 1));
        }

        return averaged;
    }

    /// <summary>
    ///     Between two points kept, keeps the one farthest from the line joining them if it's far enough, and goes on on
    ///     each side of it (Ramer-Douglas-Peucker).
    /// </summary>
    private static void KeepFarthest(PointF[] points, int first, int last, List<PointF> kept)
    {
        int farthest = -1;
        double farthestDistance = Tolerance;

        for (int i = first + 1; i < last; i++)
        {
            double distance = DistanceToLine(points[i], points[first], points[last]);

            if (distance > farthestDistance)
            {
                farthest = i;
                farthestDistance = distance;
            }
        }

        if (farthest < 0)
        {
            return;
        }

        KeepFarthest(points, first, farthest, kept);
        kept.Add(points[farthest]);
        KeepFarthest(points, farthest, last, kept);
    }

    private static double DistanceToLine(PointF point, PointF from, PointF to)
    {
        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);

        if (length == 0)
        {
            return Math.Sqrt(Math.Pow(point.X - from.X, 2) + Math.Pow(point.Y - from.Y, 2));
        }

        return Math.Abs((point.X - from.X) * dy - (point.Y - from.Y) * dx) / length;
    }
}
