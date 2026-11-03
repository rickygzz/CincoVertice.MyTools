using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     The outlines of the shapes, as paths with only lines and Bezier curves, so that the screen and the SVG file can
///     draw them the same way.
/// </summary>
public static class DiagramFigures
{
    // How far a Bezier curve's control points are from a quarter circle's ends, as a part of the radius
    private const float Kappa = 0.5522848f;

    /// <returns>The paths to draw for a shape, in order; none for a text.</returns>
    public static IReadOnlyList<DiagramFigureModel> Of(DiagramShapeKind kind, Rectangle bounds) => kind switch
    {
        DiagramShapeKind.Rectangle or DiagramShapeKind.Diamond or DiagramShapeKind.Parallelogram
            or DiagramShapeKind.Hexagon => [Polygon(PolygonOf(kind, bounds)!)],
        DiagramShapeKind.RoundedRectangle => [RoundedRectangle(bounds)],
        DiagramShapeKind.Ellipse => [Ellipse(bounds)],
        DiagramShapeKind.Cylinder => Cylinder(bounds),
        DiagramShapeKind.Document => [Document(bounds)],
        _ => [],
    };

    /// <returns>The corners of a shape that has only straight sides; null for the others.</returns>
    public static IReadOnlyList<PointF>? PolygonOf(DiagramShapeKind kind, Rectangle bounds)
    {
        float left = bounds.Left;
        float right = bounds.Right;
        float top = bounds.Top;
        float bottom = bounds.Bottom;
        float centerX = left + bounds.Width / 2f;
        float centerY = top + bounds.Height / 2f;
        float slant = Math.Min(bounds.Width / 4f, bounds.Height / 2f);

        return kind switch
        {
            DiagramShapeKind.Rectangle => [new(left, top), new(right, top), new(right, bottom), new(left, bottom)],
            DiagramShapeKind.Diamond => [new(centerX, top), new(right, centerY), new(centerX, bottom), new(left, centerY)],
            DiagramShapeKind.Parallelogram =>
                [new(left + slant, top), new(right, top), new(right - slant, bottom), new(left, bottom)],
            DiagramShapeKind.Hexagon =>
                [
                    new(left + slant, top),
                    new(right - slant, top),
                    new(right, centerY),
                    new(right - slant, bottom),
                    new(left + slant, bottom),
                    new(left, centerY),
                ],
            _ => null,
        };
    }

    /// <summary>
    ///     The part of a shape that a text fits in, so a long text stays inside a diamond or an ellipse.
    /// </summary>
    public static RectangleF TextArea(DiagramShapeKind kind, Rectangle bounds)
    {
        const float padding = 4;

        (float x, float y) = kind switch
        {
            DiagramShapeKind.Diamond => (bounds.Width / 4f, bounds.Height / 4f),
            DiagramShapeKind.Ellipse => (bounds.Width * 0.15f, bounds.Height * 0.15f),
            DiagramShapeKind.Parallelogram or DiagramShapeKind.Hexagon =>
                (Math.Min(bounds.Width / 4f, bounds.Height / 2f), 0f),
            _ => (0f, 0f),
        };

        // The rim at the top of a cylinder and the wave at the bottom of a document
        float topInset = kind == DiagramShapeKind.Cylinder ? CylinderRim(bounds) * 2 : 0;
        float bottomInset = kind == DiagramShapeKind.Document ? DocumentWave(bounds) : 0;

        return RectangleF.FromLTRB(
            bounds.Left + x + padding,
            bounds.Top + y + padding + topInset,
            Math.Max(bounds.Right - x - padding, bounds.Left + x + padding),
            Math.Max(bounds.Bottom - y - padding - bottomInset, bounds.Top + y + padding + topInset));
    }

    /// <summary>
    ///     A line through the points with a curve at every corner. A curve takes at most half of each piece of the line
    ///     next to it, so a short piece still has room for the corners at both of its ends.
    /// </summary>
    public static DiagramFigureModel RoundedLine(IReadOnlyList<PointF> points, float radius)
    {
        List<DiagramPathSegmentModel> segments = [Move(points[0].X, points[0].Y)];

        for (int i = 1; i < points.Count - 1; i++)
        {
            PointF corner = points[i];
            (float inX, float inY, float inLength) = Direction(points[i - 1], corner);
            (float outX, float outY, float outLength) = Direction(corner, points[i + 1]);
            float size = Math.Min(radius, Math.Min(inLength, outLength) / 2);
            float control = size * (1 - Kappa);

            segments.Add(Line(corner.X - inX * size, corner.Y - inY * size));
            segments.Add(Curve(
                corner.X - inX * control,
                corner.Y - inY * control,
                corner.X + outX * control,
                corner.Y + outY * control,
                corner.X + outX * size,
                corner.Y + outY * size));
        }

        segments.Add(Line(points[^1].X, points[^1].Y));

        return new DiagramFigureModel(segments, Filled: false);
    }

    /// <returns>Which way the line goes as a unit vector, and how long it is; no direction if it has no length.</returns>
    private static (float X, float Y, float Length) Direction(PointF from, PointF to)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float length = MathF.Sqrt(dx * dx + dy * dy);

        return length == 0 ? (0, 0, 0) : (dx / length, dy / length, length);
    }

    private static float CylinderRim(Rectangle bounds) => Math.Min(bounds.Height / 6f, 12);

    private static float DocumentWave(Rectangle bounds) => Math.Min(bounds.Height / 6f, 12);

    private static DiagramPathSegmentModel Move(float x, float y) =>
        new(DiagramPathOperation.Move, [new PointF(x, y)]);

    private static DiagramPathSegmentModel Line(float x, float y) =>
        new(DiagramPathOperation.Line, [new PointF(x, y)]);

    private static DiagramPathSegmentModel Curve(float x1, float y1, float x2, float y2, float x, float y) =>
        new(DiagramPathOperation.Curve, [new PointF(x1, y1), new PointF(x2, y2), new PointF(x, y)]);

    private static DiagramPathSegmentModel Close() => new(DiagramPathOperation.Close, []);

    private static DiagramFigureModel Polygon(IReadOnlyList<PointF> corners)
    {
        List<DiagramPathSegmentModel> segments = [Move(corners[0].X, corners[0].Y)];

        segments.AddRange(corners.Skip(1).Select(corner => Line(corner.X, corner.Y)));
        segments.Add(Close());

        return new DiagramFigureModel(segments);
    }

    private static DiagramFigureModel RoundedRectangle(Rectangle bounds)
    {
        float radius = Math.Min(12, Math.Min(bounds.Width, bounds.Height) / 2f);
        float control = radius * (1 - Kappa);
        float left = bounds.Left;
        float right = bounds.Right;
        float top = bounds.Top;
        float bottom = bounds.Bottom;

        return new DiagramFigureModel(
        [
            Move(left + radius, top),
            Line(right - radius, top),
            Curve(right - control, top, right, top + control, right, top + radius),
            Line(right, bottom - radius),
            Curve(right, bottom - control, right - control, bottom, right - radius, bottom),
            Line(left + radius, bottom),
            Curve(left + control, bottom, left, bottom - control, left, bottom - radius),
            Line(left, top + radius),
            Curve(left, top + control, left + control, top, left + radius, top),
            Close(),
        ]);
    }

    private static DiagramFigureModel Ellipse(Rectangle bounds)
    {
        float radiusX = bounds.Width / 2f;
        float radiusY = bounds.Height / 2f;
        float centerX = bounds.Left + radiusX;
        float centerY = bounds.Top + radiusY;
        float controlX = radiusX * Kappa;
        float controlY = radiusY * Kappa;
        float left = bounds.Left;
        float right = bounds.Right;
        float top = bounds.Top;
        float bottom = bounds.Bottom;

        return new DiagramFigureModel(
        [
            Move(centerX, top),
            Curve(centerX + controlX, top, right, centerY - controlY, right, centerY),
            Curve(right, centerY + controlY, centerX + controlX, bottom, centerX, bottom),
            Curve(centerX - controlX, bottom, left, centerY + controlY, left, centerY),
            Curve(left, centerY - controlY, centerX - controlX, top, centerX, top),
            Close(),
        ]);
    }

    private static IReadOnlyList<DiagramFigureModel> Cylinder(Rectangle bounds)
    {
        float rim = CylinderRim(bounds);
        float control = rim * 4 / 3;
        float left = bounds.Left;
        float right = bounds.Right;
        float top = bounds.Top + rim;
        float bottom = bounds.Bottom - rim;
        float third = bounds.Width / 3f;

        return
        [
            new DiagramFigureModel(
            [
                Move(left, top),
                Curve(left + third, top - control, right - third, top - control, right, top),
                Line(right, bottom),
                Curve(right - third, bottom + control, left + third, bottom + control, left, bottom),
                Close(),
            ]),
            new DiagramFigureModel(
            [
                Move(left, top),
                Curve(left + third, top + control, right - third, top + control, right, top),
            ],
            Filled: false),
        ];
    }

    private static DiagramFigureModel Document(Rectangle bounds)
    {
        float wave = DocumentWave(bounds);
        float left = bounds.Left;
        float right = bounds.Right;
        float bottom = bounds.Bottom - wave;
        float third = bounds.Width / 3f;

        return new DiagramFigureModel(
        [
            Move(left, bounds.Top),
            Line(right, bounds.Top),
            Line(right, bottom),
            Curve(right - third, bottom - wave, left + third, bottom + wave * 2, left, bottom),
            Close(),
        ]);
    }
}
