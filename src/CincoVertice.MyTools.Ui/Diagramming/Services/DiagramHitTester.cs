using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     Finds what's under a point, in diagram pixels.
/// </summary>
public static class DiagramHitTester
{
    /// <returns>The shape on top at the point; null when there's none.</returns>
    public static DiagramShapeModel? ShapeAt(DiagramModel diagram, Point point) =>
        diagram.Shapes.LastOrDefault(shape => shape.Bounds.Contains(point));

    /// <returns>The connector on top within the distance of the point; null when there's none.</returns>
    public static DiagramConnectorModel? ConnectorAt(DiagramModel diagram, Point point, float distance)
    {
        return diagram.Connectors.LastOrDefault(connector =>
            DiagramRouter.PathOf(connector, diagram) is { } path && IsNear(path.Points, point, distance));
    }

    /// <returns>The line on its own on top within the distance of the point; null when there's none.</returns>
    public static DiagramConnectorModel? LineAt(DiagramModel diagram, Point point, float distance)
    {
        return diagram.Connectors.LastOrDefault(connector => connector.IsLine
            && DiagramRouter.PathOf(connector, diagram) is { } path
            && IsNear(path.Points, point, distance));
    }

    /// <summary>Where an end of a connector touches its shape: the tip of its head, if it has one there.</summary>
    public static PointF EndPoint(DiagramConnectorPathModel path, DiagramConnectorEnd end) => end == DiagramConnectorEnd.Start
        ? path.StartHead?[0] ?? path.Points[0]
        : path.EndHead?[0] ?? path.Points[^1];

    /// <returns>The end of the connector within the distance of the point; null when there's none.</returns>
    public static DiagramConnectorEnd? ConnectorEndAt(DiagramConnectorPathModel path, Point point, float distance)
    {
        foreach (DiagramConnectorEnd end in Enum.GetValues<DiagramConnectorEnd>())
        {
            PointF center = EndPoint(path, end);
            float x = center.X - point.X;
            float y = center.Y - point.Y;

            if (x * x + y * y <= distance * distance)
            {
                return end;
            }
        }

        return null;
    }

    /// <summary>The middle of the circle for a side of a shape: this far outside the edge's middle.</summary>
    public static PointF AnchorCenter(DiagramShapeModel shape, DiagramSide side, float offset)
    {
        PointF anchor = DiagramRouter.AnchorPoint(shape, side);
        (int x, int y) = DiagramRouter.DirectionOf(side);

        return new PointF(anchor.X + x * offset, anchor.Y + y * offset);
    }

    /// <returns>The side whose circle is within the distance of the point; null when there's none.</returns>
    public static DiagramSide? AnchorAt(DiagramShapeModel shape, Point point, float offset, float distance)
    {
        foreach (DiagramSide side in Enum.GetValues<DiagramSide>())
        {
            PointF center = AnchorCenter(shape, side, offset);
            float x = center.X - point.X;
            float y = center.Y - point.Y;

            if (x * x + y * y <= distance * distance)
            {
                return side;
            }
        }

        return null;
    }

    /// <returns>The index of the point that starts the piece of line within the distance; null when there's none.</returns>
    public static int? SegmentAt(IReadOnlyList<PointF> line, Point point, float distance)
    {
        for (int i = 1; i < line.Count; i++)
        {
            if (DistanceToSegment(point, line[i - 1], line[i]) <= distance)
            {
                return i - 1;
            }
        }

        return null;
    }

    /// <returns>The handle within the distance of the point; null when there's none.</returns>
    public static DiagramHandle? HandleAt(Rectangle box, Point point, float distance)
    {
        foreach (DiagramHandle handle in Enum.GetValues<DiagramHandle>())
        {
            Point center = HandleCenter(box, handle);

            if (Math.Abs(center.X - point.X) <= distance && Math.Abs(center.Y - point.Y) <= distance)
            {
                return handle;
            }
        }

        return null;
    }

    /// <returns>Where a handle goes on the box.</returns>
    public static Point HandleCenter(Rectangle box, DiagramHandle handle)
    {
        int centerX = box.Left + box.Width / 2;
        int centerY = box.Top + box.Height / 2;

        return handle switch
        {
            DiagramHandle.TopLeft => new Point(box.Left, box.Top),
            DiagramHandle.Top => new Point(centerX, box.Top),
            DiagramHandle.TopRight => new Point(box.Right, box.Top),
            DiagramHandle.Right => new Point(box.Right, centerY),
            DiagramHandle.BottomRight => new Point(box.Right, box.Bottom),
            DiagramHandle.Bottom => new Point(centerX, box.Bottom),
            DiagramHandle.BottomLeft => new Point(box.Left, box.Bottom),
            _ => new Point(box.Left, centerY),
        };
    }

    private static bool IsNear(IReadOnlyList<PointF> line, Point point, float distance) =>
        SegmentAt(line, point, distance) is not null;

    private static float DistanceToSegment(Point point, PointF a, PointF b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        float lengthSquared = dx * dx + dy * dy;
        float along = lengthSquared == 0
            ? 0
            : Math.Clamp(((point.X - a.X) * dx + (point.Y - a.Y) * dy) / lengthSquared, 0, 1);
        float offsetX = point.X - (a.X + dx * along);
        float offsetY = point.Y - (a.Y + dy * along);

        return MathF.Sqrt(offsetX * offsetX + offsetY * offsetY);
    }
}
