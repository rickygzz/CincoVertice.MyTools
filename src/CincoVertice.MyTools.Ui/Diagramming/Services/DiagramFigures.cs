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

    // Between a shape's edge and its text
    private const float TextPadding = 4;

    // The least a container's title band is, in pixels
    private const float MinContainerTitle = 32;

    // Between a container's sides and its title, more than in other shapes: a title at the left is against a border
    private const float ContainerTitlePadding = 10;

    /// <summary>How round a shape's corners are with no radius of its own: square, but a rounded rectangle's.</summary>
    public static int DefaultCornerRadius(DiagramShapeKind kind) => kind == DiagramShapeKind.RoundedRectangle ? 12 : 0;

    /// <returns>True for the shapes whose corners can be rounded.</returns>
    public static bool HasCornerRadius(DiagramShapeKind kind) =>
        kind is DiagramShapeKind.Rectangle or DiagramShapeKind.RoundedRectangle or DiagramShapeKind.Container;

    /// <param name="kind">Which shape.</param>
    /// <param name="bounds">Where it is.</param>
    /// <param name="cornerRadius">How round a rectangle's corners are; null for the kind's usual ones.</param>
    /// <param name="titleHeight">How high a container's title band is; null for the usual one.</param>
    /// <returns>The paths to draw for a shape, in order; none for a text.</returns>
    public static IReadOnlyList<DiagramFigureModel> Of(
        DiagramShapeKind kind,
        Rectangle bounds,
        int? cornerRadius = null,
        float? titleHeight = null) => kind switch
    {
        DiagramShapeKind.Rectangle or DiagramShapeKind.RoundedRectangle when
            (cornerRadius ?? DefaultCornerRadius(kind)) > 0 =>
            [RoundedRectangle(bounds, cornerRadius ?? DefaultCornerRadius(kind))],
        DiagramShapeKind.Rectangle or DiagramShapeKind.RoundedRectangle =>
            [Polygon(PolygonOf(DiagramShapeKind.Rectangle, bounds)!)],
        DiagramShapeKind.Diamond or DiagramShapeKind.Parallelogram or DiagramShapeKind.Hexagon =>
            [Polygon(PolygonOf(kind, bounds)!)],
        DiagramShapeKind.Ellipse => [Ellipse(bounds)],
        DiagramShapeKind.Cylinder => Cylinder(bounds),
        DiagramShapeKind.Document => [Document(bounds)],
        DiagramShapeKind.Container => Container(bounds, titleHeight ?? ContainerTitle(bounds).Height),
        _ => [],
    };

    /// <summary>The band at the top of a container of no text: 32 pixels, or half a low one.</summary>
    public static RectangleF ContainerTitle(Rectangle bounds) =>
        new(bounds.Left, bounds.Top, bounds.Width, Math.Min(MinContainerTitle, bounds.Height / 2f));

    /// <summary>
    ///     The band at the top of a container, which holds its title: as high as its lines, at its font size, with
    ///     some room, and 32 pixels at least; never more than the container.
    /// </summary>
    public static RectangleF ContainerTitle(DiagramShapeModel container)
    {
        Rectangle bounds = container.Bounds;
        int fontSize = container.Style.FontSize;
        string text = container.Text.Length > 0 ? container.Text : " ";
        float width = Math.Max(bounds.Width - (2 * ContainerTitlePadding), 1);
        int lines = DiagramTextLayout.Wrap(text, width, fontSize).Count;
        float height = Math.Max(MinContainerTitle, (lines * DiagramTextLayout.LineHeightOf(fontSize)) + 12);

        return new RectangleF(bounds.Left, bounds.Top, bounds.Width, Math.Min(height, bounds.Height));
    }

    /// <summary>The paths to draw for a shape, in order; a container's title band as high as its title.</summary>
    public static IReadOnlyList<DiagramFigureModel> Of(DiagramShapeModel shape)
    {
        int top = TopCornerRadius(shape);
        int bottom = BottomCornerRadius(shape);

        return shape.Kind switch
        {
            DiagramShapeKind.Container => Container(
                shape.Bounds,
                ContainerTitle(shape).Height,
                top,
                bottom,
                shape.Style.TitleFill.IsEmpty),
            DiagramShapeKind.Rectangle or DiagramShapeKind.RoundedRectangle when top > 0 || bottom > 0 =>
                [RoundedBox(shape.Bounds, top, bottom)],
            _ => Of(shape.Kind, shape.Bounds, shape.CornerRadius),
        };
    }

    /// <summary>How round a shape's top corners are: its own radius, or its kind's usual one.</summary>
    public static int TopCornerRadius(DiagramShapeModel shape) =>
        Math.Max(shape.CornerRadius ?? DefaultCornerRadius(shape.Kind), 0);

    /// <summary>How round a shape's bottom corners are: as its top ones, until it has a radius for them.</summary>
    public static int BottomCornerRadius(DiagramShapeModel shape) =>
        Math.Max(shape.BottomCornerRadius ?? TopCornerRadius(shape), 0);

    /// <summary>The part of a shape its text fits in; a container's title band, as high as its title.</summary>
    public static RectangleF TextArea(DiagramShapeModel shape)
    {
        if (shape.Kind != DiagramShapeKind.Container)
        {
            return TextArea(shape.Kind, shape.Bounds);
        }

        RectangleF title = ContainerTitle(shape);

        title.Inflate(-ContainerTitlePadding, 0);

        return title;
    }

    /// <summary>What a figure is filled with: the style's fill, its title fill, or nothing.</summary>
    /// <returns><see cref="Color.Empty"/> for no fill.</returns>
    public static Color FillOf(DiagramFigureModel figure, DiagramStyleModel style)
    {
        if (!figure.Filled)
        {
            return Color.Empty;
        }

        if (!figure.IsTitleBand)
        {
            return style.Fill;
        }

        // Empty for the inside's fill, and transparent for none
        return style.TitleFill.IsEmpty ? style.Fill
            : style.TitleFill.A == 0 ? Color.Empty
            : style.TitleFill;
    }

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
        const float padding = TextPadding;

        // A container's title is in its band at the top
        if (kind == DiagramShapeKind.Container)
        {
            RectangleF title = ContainerTitle(bounds);

            title.Inflate(-padding, 0);

            return title;
        }

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

    /// <summary>
    ///     A box whose top corners are as round as one radius and its bottom ones as another; 0 for a square corner.
    ///     Each is at most half the box's width or height, unless the caller has limited them.
    /// </summary>
    private static DiagramFigureModel RoundedBox(
        RectangleF box,
        float topRadius,
        float bottomRadius,
        bool limitToHalf = true)
    {
        float limit = limitToHalf ? Math.Min(box.Width, box.Height) / 2 : float.MaxValue;
        float top = Math.Clamp(topRadius, 0, limit);
        float bottom = Math.Clamp(bottomRadius, 0, limit);
        List<DiagramPathSegmentModel> segments = [Move(box.Left + top, box.Top)];

        segments.Add(Line(box.Right - top, box.Top));
        AddCorner(segments, new PointF(box.Right, box.Top), top, -1, 0, 0, 1);
        segments.Add(Line(box.Right, box.Bottom - bottom));
        AddCorner(segments, new PointF(box.Right, box.Bottom), bottom, 0, -1, -1, 0);
        segments.Add(Line(box.Left + bottom, box.Bottom));
        AddCorner(segments, new PointF(box.Left, box.Bottom), bottom, 1, 0, 0, -1);
        segments.Add(Line(box.Left, box.Top + top));
        AddCorner(segments, new PointF(box.Left, box.Top), top, 0, 1, 1, 0);
        segments.Add(Close());

        return new DiagramFigureModel(segments);
    }

    /// <summary>
    ///     A quarter circle round a corner, from the point the radius before it, in the direction (inX, inY) back
    ///     towards where the line came from, to the point the radius after it, in the direction (outX, outY).
    /// </summary>
    private static void AddCorner(
        List<DiagramPathSegmentModel> segments,
        PointF corner,
        float radius,
        float inX,
        float inY,
        float outX,
        float outY)
    {
        if (radius <= 0)
        {
            return;
        }

        float control = radius * (1 - Kappa);

        segments.Add(Curve(
            corner.X + (inX * control),
            corner.Y + (inY * control),
            corner.X + (outX * control),
            corner.Y + (outY * control),
            corner.X + (outX * radius),
            corner.Y + (outY * radius)));
    }

    private static DiagramFigureModel RoundedRectangle(Rectangle bounds, int cornerRadius)
    {
        float radius = Math.Min(cornerRadius, Math.Min(bounds.Width, bounds.Height) / 2f);
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

    /// <summary>The box, then the line under its title band.</summary>
    /// <param name="bounds">Where it is.</param>
    /// <param name="titleHeight">How high its title band is.</param>
    /// <param name="topRadius">How round its top corners are, and its band's; at most as the band is high.</param>
    /// <param name="bottomRadius">How round its bottom corners are; at most as much as its inside is high.</param>
    /// <param name="isBandAsInside">True when the band has the inside's fill, which then goes under it too.</param>
    private static IReadOnlyList<DiagramFigureModel> Container(
        Rectangle bounds,
        float titleHeight,
        float topRadius = 0,
        float bottomRadius = 0,
        bool isBandAsInside = true)
    {
        float under = bounds.Top + titleHeight;
        float top = Math.Clamp(topRadius, 0, Math.Min(bounds.Width / 2f, titleHeight));
        float bottom = Math.Clamp(bottomRadius, 0, Math.Min(bounds.Width / 2f, bounds.Height - titleHeight));
        DiagramFigureModel box = RoundedBox(bounds, top, bottom, limitToHalf: false);
        DiagramFigureModel band = RoundedBox(
            new RectangleF(bounds.Left, bounds.Top, bounds.Width, titleHeight),
            top,
            0,
            limitToHalf: false);

        // Under a band of its own color, or of none, the inside is filled only below it
        DiagramFigureModel inside = isBandAsInside
            ? box
            : RoundedBox(
                RectangleF.FromLTRB(bounds.Left, under, bounds.Right, bounds.Bottom),
                0,
                bottom,
                limitToHalf: false);

        // Both fills first, then the border over them, so the title's fill covers none of it
        return
        [
            inside with { Stroked = false },
            band with { Stroked = false, IsTitleBand = true },
            box with { Filled = false },
            new DiagramFigureModel([Move(bounds.Left, under), Line(bounds.Right, under)], Filled: false),
        ];
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
