using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     Works out where a connector goes from where its shapes are now, so it follows them when they move. An elbow
///     connector's lines are always level or upright. It doesn't avoid other shapes on its way.
/// </summary>
public static class DiagramRouter
{
    private const float CornerRadius = 10;

    // How far a connector goes straight out of a side before it may turn back
    private const float ElbowStub = 20;

    /// <returns>Where the connector goes; null if one of its shapes doesn't exist.</returns>
    public static DiagramConnectorPathModel? PathOf(DiagramConnectorModel connector, DiagramModel diagram)
    {
        DiagramShapeModel? from = diagram.Shapes.FirstOrDefault(shape => shape.Id == connector.FromShapeId);
        DiagramShapeModel? to = diagram.Shapes.FirstOrDefault(shape => shape.Id == connector.ToShapeId);

        if (from is null || to is null)
        {
            return null;
        }

        List<PointF> route = RouteOf(connector, from, to);
        PointF labelCenter = PointAlong(route, 0.5f);
        float headLength = 8 + 2 * connector.Style.LineWidth;

        IReadOnlyList<PointF>? startHead = connector.Heads is DiagramArrowHeads.Start or DiagramArrowHeads.Both
            ? AddHead(route, headLength, atEnd: false)
            : null;
        IReadOnlyList<PointF>? endHead = connector.Heads is DiagramArrowHeads.End or DiagramArrowHeads.Both
            ? AddHead(route, headLength, atEnd: true)
            : null;

        return new DiagramConnectorPathModel(
            route,
            DiagramFigures.RoundedLine(route, CornerRadius),
            startHead,
            endHead,
            labelCenter);
    }

    /// <summary>
    ///     Where a line from the middle of a shape to the point leaves the shape. The point itself when it's inside.
    /// </summary>
    public static PointF BorderPoint(DiagramShapeModel shape, PointF toward)
    {
        Rectangle bounds = shape.Bounds;
        PointF center = CenterOf(bounds);
        float dx = toward.X - center.X;
        float dy = toward.Y - center.Y;

        if (dx == 0 && dy == 0)
        {
            return center;
        }

        // How far along the line to the point the border is: 1 is the point itself
        float along;
        IReadOnlyList<PointF>? corners = DiagramFigures.PolygonOf(shape.Kind, bounds);

        if (shape.Kind == DiagramShapeKind.Ellipse)
        {
            float x = dx / (bounds.Width / 2f);
            float y = dy / (bounds.Height / 2f);

            along = 1 / MathF.Sqrt(x * x + y * y);
        }
        else if (corners is not null && shape.Kind != DiagramShapeKind.Rectangle)
        {
            along = AlongToPolygon(center, dx, dy, corners);
        }
        else
        {
            float x = dx == 0 ? float.MaxValue : bounds.Width / 2f / Math.Abs(dx);
            float y = dy == 0 ? float.MaxValue : bounds.Height / 2f / Math.Abs(dy);

            along = Math.Min(x, y);
        }

        return along >= 1 ? toward : new PointF(center.X + dx * along, center.Y + dy * along);
    }

    /// <summary>Where a connector attached to a side of the shape touches it.</summary>
    public static PointF AnchorPoint(DiagramShapeModel shape, DiagramSide side)
    {
        PointF center = CenterOf(shape.Bounds);
        (int x, int y) = DirectionOf(side);

        // Far enough to always be outside the shape
        int away = shape.Bounds.Width + shape.Bounds.Height;

        return BorderPoint(shape, new PointF(center.X + x * away, center.Y + y * away));
    }

    /// <summary>Which way a side faces, away from the shape.</summary>
    public static (int X, int Y) DirectionOf(DiagramSide side) => side switch
    {
        DiagramSide.Top => (0, -1),
        DiagramSide.Right => (1, 0),
        DiagramSide.Bottom => (0, 1),
        _ => (-1, 0),
    };

    private static PointF CenterOf(Rectangle bounds) =>
        new(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);

    /// <summary>
    ///     The whole line of a connector, from one shape to the other; its head isn't taken off the line yet.
    /// </summary>
    /// <returns>The line; null if one of its shapes doesn't exist.</returns>
    public static List<PointF>? RouteFor(DiagramConnectorModel connector, DiagramModel diagram)
    {
        DiagramShapeModel? from = diagram.Shapes.FirstOrDefault(shape => shape.Id == connector.FromShapeId);
        DiagramShapeModel? to = diagram.Shapes.FirstOrDefault(shape => shape.Id == connector.ToShapeId);

        return from is null || to is null ? null : RouteOf(connector, from, to);
    }

    /// <returns>True when the piece of line between the points is level or upright.</returns>
    public static bool IsLevelOrUpright(PointF a, PointF b) => a.X == b.X || a.Y == b.Y;

    /// <summary>
    ///     A line with one of its pieces slid sideways, and the pieces next to it longer or shorter to meet it. A piece
    ///     touching a shape keeps a short stub there, so the line still leaves that shape straight.
    /// </summary>
    /// <param name="route">The whole line, only level and upright.</param>
    /// <param name="segment">The index of the point that starts the piece.</param>
    /// <param name="position">Where the piece goes: its Y if it's level, or else its X.</param>
    public static List<PointF> WithSegmentMoved(IReadOnlyList<PointF> route, int segment, float position)
    {
        PointF a = route[segment];
        PointF b = route[segment + 1];
        bool isLevel = a.Y == b.Y;
        float length = Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y);
        float stub = Math.Min(ElbowStub, length / 3);
        List<PointF> moved = [.. route.Take(segment)];

        PointF Slid(PointF point) => isLevel ? new PointF(point.X, position) : new PointF(position, point.Y);

        PointF Toward(PointF from, PointF to) => new(
            from.X + Math.Sign(to.X - from.X) * stub,
            from.Y + Math.Sign(to.Y - from.Y) * stub);

        if (segment == 0)
        {
            PointF start = Toward(a, b);

            moved.AddRange([a, start, Slid(start)]);
        }
        else
        {
            moved.Add(Slid(a));
        }

        if (segment + 1 == route.Count - 1)
        {
            PointF end = Toward(b, a);

            moved.AddRange([Slid(end), end, b]);
        }
        else
        {
            moved.Add(Slid(b));
        }

        moved.AddRange(route.Skip(segment + 2));

        return WithoutExtraPoints(moved);
    }

    /// <summary>
    ///     Through the corners, leaving and arriving straight at the sides it has, or else those facing the nearest
    ///     corner. A shape that moved, or corners from an older file, may not line up with the next corner, so a turn
    ///     is added between them: the line is always level and upright.
    /// </summary>
    private static List<PointF> RouteThroughBends(
        DiagramConnectorModel connector,
        IReadOnlyList<Point> bends,
        DiagramShapeModel from,
        DiagramShapeModel to)
    {
        PointF first = bends[0];
        PointF last = bends[^1];
        DiagramSide startSide = connector.FromSide ?? FacingSide(from.Bounds, first);
        DiagramSide endSide = connector.ToSide ?? FacingSide(to.Bounds, last);
        PointF start = LinedUpWith(AnchorPoint(from, startSide), first);
        PointF end = LinedUpWith(AnchorPoint(to, endSide), last);
        List<PointF> route = [start];

        if (!IsLevelOrUpright(start, first))
        {
            route.Add(DirectionOf(startSide).X != 0 ? new PointF(first.X, start.Y) : new PointF(start.X, first.Y));
        }

        for (int i = 0; i < bends.Count; i++)
        {
            PointF bend = bends[i];

            route.Add(bend);

            if (i + 1 < bends.Count && !IsLevelOrUpright(bend, bends[i + 1]))
            {
                route.Add(new PointF(bends[i + 1].X, bend.Y));
            }
        }

        if (!IsLevelOrUpright(last, end))
        {
            route.Add(DirectionOf(endSide).X != 0 ? new PointF(last.X, end.Y) : new PointF(end.X, last.Y));
        }

        route.Add(end);

        return WithoutExtraPoints(route);
    }

    /// <summary>
    ///     A shape's middle can be half a pixel off a corner, which is always on a whole pixel, and that would need a
    ///     tiny extra turn. So the line touches the shape that little bit off the middle instead.
    /// </summary>
    private static PointF LinedUpWith(PointF anchor, PointF corner) => new(
        Math.Abs(anchor.X - corner.X) < 1 ? corner.X : anchor.X,
        Math.Abs(anchor.Y - corner.Y) < 1 ? corner.Y : anchor.Y);

    private static List<PointF> RouteOf(DiagramConnectorModel connector, DiagramShapeModel from, DiagramShapeModel to)
    {
        if (connector.Routing == DiagramRouting.Elbow && connector.Bends is { Count: > 0 } bends)
        {
            return RouteThroughBends(connector, bends, from, to);
        }

        if (connector.FromSide is null && connector.ToSide is null)
        {
            return connector.Routing == DiagramRouting.Elbow ? ElbowRoute(from, to) : StraightRoute(from, to);
        }

        if (connector.Routing == DiagramRouting.Straight)
        {
            PointF toward = connector.ToSide is DiagramSide toSide ? AnchorPoint(to, toSide) : CenterOf(to.Bounds);
            PointF back = connector.FromSide is DiagramSide fromSide ? AnchorPoint(from, fromSide) : CenterOf(from.Bounds);

            return
            [
                connector.FromSide is null ? BorderPoint(from, toward) : back,
                connector.ToSide is null ? BorderPoint(to, back) : toward,
            ];
        }

        DiagramSide start = connector.FromSide
            ?? FacingSide(from.Bounds, connector.ToSide is DiagramSide s ? AnchorPoint(to, s) : CenterOf(to.Bounds));
        DiagramSide end = connector.ToSide ?? FacingSide(to.Bounds, AnchorPoint(from, start));

        return WithoutExtraPoints(ElbowBetweenSides(from, start, to, end));
    }

    /// <summary>The side of a box that looks most directly at the point.</summary>
    private static DiagramSide FacingSide(Rectangle box, PointF point)
    {
        PointF center = CenterOf(box);
        float dx = point.X - center.X;
        float dy = point.Y - center.Y;

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            return dx >= 0 ? DiagramSide.Right : DiagramSide.Left;
        }

        return dy >= 0 ? DiagramSide.Bottom : DiagramSide.Top;
    }

    /// <summary>
    ///     Leaves one shape straight out of a side and arrives straight into a side of the other. It may go around
    ///     the two shapes it connects, but not around others on its way.
    /// </summary>
    private static List<PointF> ElbowBetweenSides(
        DiagramShapeModel from,
        DiagramSide fromSide,
        DiagramShapeModel to,
        DiagramSide toSide)
    {
        PointF a = AnchorPoint(from, fromSide);
        PointF b = AnchorPoint(to, toSide);
        (int fromX, int fromY) = DirectionOf(fromSide);
        (int toX, int toY) = DirectionOf(toSide);
        var outA = new PointF(a.X + fromX * ElbowStub, a.Y + fromY * ElbowStub);
        var outB = new PointF(b.X + toX * ElbowStub, b.Y + toY * ElbowStub);

        if (fromX != 0 && toX != 0)
        {
            if (fromX == -toX && (b.X - a.X) * fromX > 0)
            {
                float middle = (a.X + b.X) / 2;

                return [a, new PointF(middle, a.Y), new PointF(middle, b.Y), b];
            }

            if (fromX == toX)
            {
                float x = fromX > 0 ? Math.Max(outA.X, outB.X) : Math.Min(outA.X, outB.X);

                return [a, new PointF(x, a.Y), new PointF(x, b.Y), b];
            }

            float y = BetweenOrPast(from.Bounds.Top, from.Bounds.Bottom, to.Bounds.Top, to.Bounds.Bottom);

            return [a, outA, new PointF(outA.X, y), new PointF(outB.X, y), outB, b];
        }

        if (fromY != 0 && toY != 0)
        {
            if (fromY == -toY && (b.Y - a.Y) * fromY > 0)
            {
                float middle = (a.Y + b.Y) / 2;

                return [a, new PointF(a.X, middle), new PointF(b.X, middle), b];
            }

            if (fromY == toY)
            {
                float y = fromY > 0 ? Math.Max(outA.Y, outB.Y) : Math.Min(outA.Y, outB.Y);

                return [a, new PointF(a.X, y), new PointF(b.X, y), b];
            }

            float x = BetweenOrPast(from.Bounds.Left, from.Bounds.Right, to.Bounds.Left, to.Bounds.Right);

            return [a, outA, new PointF(x, outA.Y), new PointF(x, outB.Y), outB, b];
        }

        // One side is level and the other upright, so a single turn does it when both are heading to that corner
        PointF corner = fromX != 0 ? new PointF(b.X, a.Y) : new PointF(a.X, b.Y);
        bool leavesToCorner = (corner.X - a.X) * fromX + (corner.Y - a.Y) * fromY > 0;
        bool arrivesFromCorner = (corner.X - b.X) * toX + (corner.Y - b.Y) * toY > 0;

        if (leavesToCorner && arrivesFromCorner)
        {
            return [a, corner, b];
        }

        PointF turn = fromX != 0 ? new PointF(outA.X, outB.Y) : new PointF(outB.X, outA.Y);

        return [a, outA, turn, outB, b];
    }

    /// <summary>The middle of the gap between two ranges; if they overlap, a little past both.</summary>
    private static float BetweenOrPast(int startA, int endA, int startB, int endB)
    {
        if (endA <= startB)
        {
            return (endA + startB) / 2f;
        }

        return endB <= startA ? (endB + startA) / 2f : Math.Max(endA, endB) + ElbowStub;
    }

    /// <summary>
    ///     Without points that don't turn, which would make a rounded corner out of a straight line, and without points
    ///     where the line goes past a corner and doubles back on itself, which would show as a spike.
    /// </summary>
    private static List<PointF> WithoutExtraPoints(List<PointF> route)
    {
        List<PointF> kept = [route[0]];

        for (int i = 1; i < route.Count; i++)
        {
            PointF point = route[i];

            if (point == kept[^1])
            {
                continue;
            }

            if (kept.Count >= 2 && IsInLine(kept[^2], kept[^1], point))
            {
                kept.RemoveAt(kept.Count - 1);

                // Doubled back all the way to the point before
                if (point == kept[^1])
                {
                    continue;
                }
            }

            kept.Add(point);
        }

        return kept.Count >= 2 ? kept : [route[0], route[^1]];
    }

    private static bool IsInLine(PointF a, PointF b, PointF c) =>
        Math.Abs((b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X)) < 0.001f;

    private static List<PointF> StraightRoute(DiagramShapeModel from, DiagramShapeModel to)
    {
        PointF fromCenter = CenterOf(from.Bounds);
        PointF toCenter = CenterOf(to.Bounds);

        return [BorderPoint(from, toCenter), BorderPoint(to, fromCenter)];
    }

    /// <summary>
    ///     Leaves one shape and enters the other through the sides that face each other, turning halfway. Shapes
    ///     that overlap in both directions have no such sides, so they get a straight line.
    /// </summary>
    private static List<PointF> ElbowRoute(DiagramShapeModel from, DiagramShapeModel to)
    {
        Rectangle a = from.Bounds;
        Rectangle b = to.Bounds;
        PointF fromCenter = CenterOf(a);
        PointF toCenter = CenterOf(b);

        if (a.Right <= b.Left || b.Right <= a.Left)
        {
            // Far enough to always be outside either shape
            float away = a.Right <= b.Left ? a.Width + b.Width : -a.Width - b.Width;
            PointF start = BorderPoint(from, new PointF(fromCenter.X + away, fromCenter.Y));
            PointF end = BorderPoint(to, new PointF(toCenter.X - away, toCenter.Y));
            float middle = (start.X + end.X) / 2;

            return WithoutRepeats([start, new PointF(middle, start.Y), new PointF(middle, end.Y), end]);
        }

        if (a.Bottom <= b.Top || b.Bottom <= a.Top)
        {
            float away = a.Bottom <= b.Top ? a.Height + b.Height : -a.Height - b.Height;
            PointF start = BorderPoint(from, new PointF(fromCenter.X, fromCenter.Y + away));
            PointF end = BorderPoint(to, new PointF(toCenter.X, toCenter.Y - away));
            float middle = (start.Y + end.Y) / 2;

            return WithoutRepeats([start, new PointF(start.X, middle), new PointF(end.X, middle), end]);
        }

        return StraightRoute(from, to);
    }

    /// <summary>Two shapes in line have no turn to make.</summary>
    private static List<PointF> WithoutRepeats(List<PointF> route) => route[0].X == route[^1].X || route[0].Y == route[^1].Y
        ? [route[0], route[^1]]
        : route;

    private static float AlongToPolygon(PointF center, float dx, float dy, IReadOnlyList<PointF> corners)
    {
        float nearest = float.MaxValue;

        for (int i = 0; i < corners.Count; i++)
        {
            PointF a = corners[i];
            PointF b = corners[(i + 1) % corners.Count];
            float sideX = b.X - a.X;
            float sideY = b.Y - a.Y;
            float cross = dx * sideY - dy * sideX;

            if (cross == 0)
            {
                continue;
            }

            float along = ((a.X - center.X) * sideY - (a.Y - center.Y) * sideX) / cross;
            float onSide = ((a.X - center.X) * dy - (a.Y - center.Y) * dx) / cross;

            if (along > 0 && along < nearest && onSide >= -0.001f && onSide <= 1.001f)
            {
                nearest = along;
            }
        }

        return nearest;
    }

    private static PointF PointAlong(IReadOnlyList<PointF> route, float part)
    {
        float left = LengthOf(route) * part;

        for (int i = 1; i < route.Count; i++)
        {
            float length = Distance(route[i - 1], route[i]);

            if (length > 0 && left <= length)
            {
                float along = left / length;

                return new PointF(
                    route[i - 1].X + (route[i].X - route[i - 1].X) * along,
                    route[i - 1].Y + (route[i].Y - route[i - 1].Y) * along);
            }

            left -= length;
        }

        return route[^1];
    }

    private static float LengthOf(IReadOnlyList<PointF> route)
    {
        float total = 0;

        for (int i = 1; i < route.Count; i++)
        {
            total += Distance(route[i - 1], route[i]);
        }

        return total;
    }

    private static float Distance(PointF a, PointF b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;

        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    ///     Makes the triangle at one end of the route, and moves that end to the triangle's base.
    /// </summary>
    /// <returns>The triangle; null when the last piece of the line has no direction.</returns>
    private static PointF[]? AddHead(List<PointF> route, float length, bool atEnd)
    {
        int tipIndex = atEnd ? route.Count - 1 : 0;
        PointF tip = route[tipIndex];
        PointF before = route[atEnd ? route.Count - 2 : 1];
        float distance = Distance(before, tip);

        if (distance == 0)
        {
            return null;
        }

        float unitX = (tip.X - before.X) / distance;
        float unitY = (tip.Y - before.Y) / distance;

        // A short piece can't lose more than it has, or the line would turn back
        float size = Math.Min(length, Math.Max(distance, 4));
        float baseX = tip.X - unitX * size;
        float baseY = tip.Y - unitY * size;
        float halfWidth = size * 0.45f;

        route[tipIndex] = size < distance ? new PointF(baseX, baseY) : before;

        return
        [
            tip,
            new PointF(baseX - unitY * halfWidth, baseY + unitX * halfWidth),
            new PointF(baseX + unitY * halfWidth, baseY - unitX * halfWidth),
        ];
    }
}
