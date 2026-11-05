using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Snipping.Services;

/// <summary>
///     Where an annotation is, and what it becomes when moved or scaled. A text's size comes from the view, which knows
///     the font: measure gives it for a text and a font size.
/// </summary>
public static class AnnotationGeometry
{
    private const int MinFontSize = 6;
    private const int MaxFontSize = 400;

    // In pixels, around what's drawn: a thin line would be hard to hit otherwise
    private const int HitDistance = 4;
    private const int HandleDistance = 6;

    // How small dragging a handle can make a frame
    private const int MinSize = 2;

    private const int MaxLineWidth = 200;

    /// <summary>
    ///     The frame that's scaled to scale the annotation. A freehand line's goes around its thickness: around only the
    ///     middle of a nearly level line, it would be so thin that scaling it would stretch every wobble of the hand.
    /// </summary>
    public static Rectangle BoxOf(AnnotationModel annotation, Func<string, int, Size> measure) => annotation switch
    {
        StrokeAnnotationModel stroke => Inflated(BoxAround(stroke.Points), HalfOf(stroke.Width)),
        ArrowAnnotationModel arrow => BoxAround([arrow.From, arrow.To]),
        RectangleAnnotationModel rectangle => rectangle.Bounds,
        BlurAnnotationModel blur => blur.Bounds,
        HighlightAnnotationModel highlight => highlight.Bounds,
        TextAnnotationModel text => new Rectangle(text.Location, measure(text.Text, text.FontSize)),
        _ => Rectangle.Empty,
    };

    /// <summary>What shows it's selected: an arrow's two ends, or a frame with 8 handles, clockwise from its top left.</summary>
    public static SnipSelectionModel SelectionOf(AnnotationModel annotation, Func<string, int, Size> measure)
    {
        if (annotation is ArrowAnnotationModel arrow)
        {
            return new SnipSelectionModel(null, [arrow.From, arrow.To]);
        }

        Rectangle box = BoxOf(annotation, measure);
        int middleX = box.Left + box.Width / 2;
        int middleY = box.Top + box.Height / 2;

        return new SnipSelectionModel(
            box,
            [
                new Point(box.Left, box.Top),
                new Point(middleX, box.Top),
                new Point(box.Right, box.Top),
                new Point(box.Right, middleY),
                new Point(box.Right, box.Bottom),
                new Point(middleX, box.Bottom),
                new Point(box.Left, box.Bottom),
                new Point(box.Left, middleY),
            ]);
    }

    /// <returns>Which of the selection's handles is at the point; null when none is.</returns>
    public static int? HandleAt(SnipSelectionModel selection, Point point)
    {
        for (int i = 0; i < selection.Handles.Count; i++)
        {
            Point handle = selection.Handles[i];

            if (Math.Abs(handle.X - point.X) <= HandleDistance && Math.Abs(handle.Y - point.Y) <= HandleDistance)
            {
                return i;
            }
        }

        return null;
    }

    /// <summary>
    ///     Whether the point is on what's drawn: on a line or a rectangle's outline, not in the empty space they
    ///     surround, so what's under them can still be clicked.
    /// </summary>
    public static bool Hits(AnnotationModel annotation, Point point, Func<string, int, Size> measure)
    {
        switch (annotation)
        {
            case StrokeAnnotationModel stroke:
                return IsNearLine(stroke.Points, point, stroke.Width);
            case ArrowAnnotationModel arrow:
                return IsNearLine([arrow.From, arrow.To], point, arrow.Width);
            case RectangleAnnotationModel rectangle:
                Rectangle bounds = rectangle.Bounds;

                return IsNearLine(
                    [
                        new Point(bounds.Left, bounds.Top),
                        new Point(bounds.Right, bounds.Top),
                        new Point(bounds.Right, bounds.Bottom),
                        new Point(bounds.Left, bounds.Bottom),
                        new Point(bounds.Left, bounds.Top),
                    ],
                    point,
                    rectangle.Width);
            default:
                Rectangle box = BoxOf(annotation, measure);

                box.Inflate(HitDistance, HitDistance);

                return box.Contains(point);
        }
    }

    public static AnnotationModel Moved(AnnotationModel annotation, Size delta) => annotation switch
    {
        StrokeAnnotationModel stroke => stroke with { Points = [.. stroke.Points.Select(point => point + delta)] },
        ArrowAnnotationModel arrow => arrow with { From = arrow.From + delta, To = arrow.To + delta },
        RectangleAnnotationModel rectangle => rectangle with { Bounds = Offset(rectangle.Bounds, delta) },
        BlurAnnotationModel blur => blur with { Bounds = Offset(blur.Bounds, delta) },
        HighlightAnnotationModel highlight => highlight with { Bounds = Offset(highlight.Bounds, delta) },
        TextAnnotationModel text => text with { Location = text.Location + delta },
        _ => annotation,
    };

    /// <summary>What the annotation becomes with one of its handles dragged to the point.</summary>
    /// <param name="annotation">The annotation as it was before the drag.</param>
    /// <param name="handle">Which of the handles of <see cref="SelectionOf"/>.</param>
    /// <param name="point">Where it is now.</param>
    /// <param name="measure">Gives a text's size.</param>
    public static AnnotationModel Resized(
        AnnotationModel annotation,
        int handle,
        Point point,
        Func<string, int, Size> measure)
    {
        if (annotation is ArrowAnnotationModel arrow)
        {
            return handle == 0 ? arrow with { From = point } : arrow with { To = point };
        }

        Rectangle box = BoxOf(annotation, measure);
        int left = box.Left;
        int top = box.Top;
        int right = box.Right;
        int bottom = box.Bottom;

        if (handle is 0 or 6 or 7)
        {
            left = Math.Min(point.X, right - MinSize);
        }
        else if (handle is 2 or 3 or 4)
        {
            right = Math.Max(point.X, left + MinSize);
        }

        if (handle is 0 or 1 or 2)
        {
            top = Math.Min(point.Y, bottom - MinSize);
        }
        else if (handle is 4 or 5 or 6)
        {
            bottom = Math.Max(point.Y, top + MinSize);
        }

        return Scaled(annotation, box, Rectangle.FromLTRB(left, top, right, bottom), measure);
    }

    /// <returns>The pointer for a handle of <see cref="SelectionOf"/>.</returns>
    public static SnipCursor CursorOf(SnipSelectionModel selection, int handle)
    {
        if (selection.Box is null)
        {
            return SnipCursor.Cross;
        }

        return handle switch
        {
            0 or 4 => SnipCursor.SizeTopLeftBottomRight,
            2 or 6 => SnipCursor.SizeTopRightBottomLeft,
            1 or 5 => SnipCursor.SizeVertical,
            _ => SnipCursor.SizeHorizontal,
        };
    }

    private static AnnotationModel Scaled(
        AnnotationModel annotation,
        Rectangle from,
        Rectangle to,
        Func<string, int, Size> measure) => annotation switch
        {
            StrokeAnnotationModel { IsHighlight: true } line => new HighlightAnnotationModel(to, line.Color),
            StrokeAnnotationModel stroke => ScaledStroke(stroke, from, to),
            RectangleAnnotationModel rectangle => rectangle with { Bounds = to },
            BlurAnnotationModel blur => blur with { Bounds = to },
            HighlightAnnotationModel highlight => highlight with { Bounds = to },
            TextAnnotationModel text => ScaledText(text, from, to, measure),
            _ => annotation,
        };

    /// <summary>
    ///     The line's width changes with how it's scaled across its direction: dragging across a level line makes it
    ///     thicker, and dragging along it makes it longer. The rest of the frame is for the points.
    /// </summary>
    private static StrokeAnnotationModel ScaledStroke(StrokeAnnotationModel stroke, Rectangle from, Rectangle to)
    {
        Rectangle path = BoxAround(stroke.Points);
        double acrossWidth = to.Width / (double)from.Width;
        double acrossHeight = to.Height / (double)from.Height;
        int reach = path.Width + path.Height;
        double factor = reach == 0
            ? Math.Max(acrossWidth, acrossHeight)
            : (path.Width * acrossHeight + path.Height * acrossWidth) / reach;
        int width = Math.Clamp((int)Math.Round(stroke.Width * factor), 1, MaxLineWidth);
        int half = HalfOf(width);
        int left = to.Left + half;
        int top = to.Top + half;
        int pathWidth = Math.Max(to.Width - 2 * half, 0);
        int pathHeight = Math.Max(to.Height - 2 * half, 0);

        return stroke with
        {
            Width = width,
            Points = [.. stroke.Points.Select(point => new Point(
                Scale(point.X, path.Left, path.Width, left, pathWidth),
                Scale(point.Y, path.Top, path.Height, top, pathHeight)))],
        };
    }

    /// <summary>
    ///     Only the font's size changes, so the frame may not end up just as dragged: the side that wasn't dragged stays.
    /// </summary>
    private static TextAnnotationModel ScaledText(
        TextAnnotationModel text,
        Rectangle from,
        Rectangle to,
        Func<string, int, Size> measure)
    {
        double factor = to.Height != from.Height
            ? to.Height / (double)Math.Max(from.Height, 1)
            : to.Width / (double)Math.Max(from.Width, 1);
        int fontSize = Math.Clamp((int)Math.Round(text.FontSize * factor), MinFontSize, MaxFontSize);
        Size size = measure(text.Text, fontSize);

        return text with
        {
            FontSize = fontSize,
            Location = new Point(
                to.Left != from.Left ? to.Right - size.Width : from.Left,
                to.Top != from.Top ? to.Bottom - size.Height : from.Top),
        };
    }

    /// <summary>A level or upright line has no length to scale across it, and goes in the middle.</summary>
    private static int Scale(int value, int fromStart, int fromLength, int toStart, int toLength) =>
        fromLength == 0
            ? toStart + toLength / 2
            : toStart + (int)Math.Round((value - fromStart) * (toLength / (double)fromLength));

    private static Rectangle Offset(Rectangle rectangle, Size delta) =>
        new(rectangle.Location + delta, rectangle.Size);

    private static Rectangle Inflated(Rectangle rectangle, int by)
    {
        rectangle.Inflate(by, by);

        return rectangle;
    }

    /// <summary>How far a line of this width goes out from its middle, at least a pixel, so a frame always has a size.</summary>
    private static int HalfOf(int width) => Math.Max((width + 1) / 2, 1);

    private static Rectangle BoxAround(IReadOnlyList<Point> points) => Rectangle.FromLTRB(
        points.Min(point => point.X),
        points.Min(point => point.Y),
        points.Max(point => point.X),
        points.Max(point => point.Y));

    private static bool IsNearLine(IReadOnlyList<Point> points, Point point, int width)
    {
        double reach = width / 2.0 + HitDistance;

        if (points.Count == 1)
        {
            return Distance(point, points[0], points[0]) <= reach;
        }

        for (int i = 1; i < points.Count; i++)
        {
            if (Distance(point, points[i - 1], points[i]) <= reach)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>From a point to the nearest point of a line between two others.</summary>
    private static double Distance(Point point, Point from, Point to)
    {
        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        double lengthSquared = dx * dx + dy * dy;
        double along = lengthSquared == 0
            ? 0
            : Math.Clamp(((point.X - from.X) * dx + (point.Y - from.Y) * dy) / lengthSquared, 0, 1);

        return Math.Sqrt(Math.Pow(point.X - (from.X + along * dx), 2) + Math.Pow(point.Y - (from.Y + along * dy), 2));
    }
}
