using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A line attached to two shapes, which follows them when they move; or a line on its own, between two points,
///     drawn with the line tool, which isn't attached to anything.
/// </summary>
/// <param name="Id">Unique in its diagram.</param>
/// <param name="FromShapeId">The shape where the drag started; null for a line on its own.</param>
/// <param name="ToShapeId">The shape where the drag ended; null for a line on its own.</param>
/// <param name="Routing">How it goes between them. A line on its own is always straight.</param>
/// <param name="Heads">Which ends have a head.</param>
/// <param name="Label">The text in its middle; can be empty.</param>
/// <param name="Style">How it looks.</param>
/// <param name="FromSide">The side it leaves from; null to pick the one that fits best as the shapes move.</param>
/// <param name="ToSide">The side it arrives at; null to pick the one that fits best as the shapes move.</param>
/// <param name="Bends">
///     The corners of an elbow connector whose lines were moved by hand, in order; null for the corners it works out
///     by itself. They stay where they are when only one of its shapes moves. A straight connector doesn't use them.
/// </param>
/// <param name="FromPoint">Where a line on its own starts, in diagram pixels; null for a connector.</param>
/// <param name="ToPoint">Where a line on its own ends, in diagram pixels; null for a connector.</param>
/// <param name="Direction">Which way a line on its own may point; a connector doesn't use it.</param>
public sealed record DiagramConnectorModel(
    int Id,
    int? FromShapeId,
    int? ToShapeId,
    DiagramRouting Routing,
    DiagramArrowHeads Heads,
    string Label,
    DiagramStyleModel Style,
    DiagramSide? FromSide = null,
    DiagramSide? ToSide = null,
    IReadOnlyList<Point>? Bends = null,
    Point? FromPoint = null,
    Point? ToPoint = null,
    DiagramLineDirection Direction = DiagramLineDirection.Free)
{
    /// <summary>True for a line on its own, between two points, rather than between two shapes.</summary>
    public bool IsLine => FromPoint is not null && ToPoint is not null;

    /// <summary>A line on its own from one point to the other, straight.</summary>
    public static DiagramConnectorModel Line(
        int id,
        Point from,
        Point to,
        DiagramArrowHeads heads,
        DiagramStyleModel style) =>
        new(id, null, null, DiagramRouting.Straight, heads, string.Empty, style, FromPoint: from, ToPoint: to);

    /// <summary>
    ///     The line pointing its way: when it's level or upright and doesn't, its end is moved to where it does, as far
    ///     from the start. Anything else is as it was.
    /// </summary>
    public DiagramConnectorModel Aligned()
    {
        if (FromPoint is not Point from || ToPoint is not Point to || Direction == DiagramLineDirection.Free)
        {
            return this;
        }

        bool isHorizontal = Direction == DiagramLineDirection.Horizontal;

        if (isHorizontal ? from.Y == to.Y : from.X == to.X)
        {
            return this;
        }

        int dx = to.X - from.X;
        int dy = to.Y - from.Y;

        // It keeps going the way it went along that direction, or else forwards
        int along = isHorizontal ? dx : dy;
        int sign = along < 0 ? -1 : 1;
        int length = Math.Max((int)Math.Round(Math.Sqrt((double)dx * dx + (double)dy * dy)), 1);

        return this with
        {
            ToPoint = isHorizontal
                ? new Point(from.X + sign * length, from.Y)
                : new Point(from.X, from.Y + sign * length),
        };
    }
}
