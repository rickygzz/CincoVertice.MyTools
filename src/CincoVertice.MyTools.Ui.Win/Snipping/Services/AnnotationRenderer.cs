using System.Drawing.Drawing2D;
using System.Drawing.Text;
using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     Draws snip annotations, the same way on the editor's screen and in the copied or saved image.
/// </summary>
public static class AnnotationRenderer
{
    public const string FontFamilyName = "Segoe UI";

    public static void Draw(Graphics graphics, IReadOnlyList<AnnotationModel> annotations)
    {
        SmoothingMode smoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        foreach (AnnotationModel annotation in annotations)
        {
            switch (annotation)
            {
                case StrokeAnnotationModel stroke:
                    DrawStroke(graphics, stroke);
                    break;
                case ArrowAnnotationModel arrow:
                    DrawArrow(graphics, arrow);
                    break;
                case RectangleAnnotationModel rectangle:
                    DrawRectangle(graphics, rectangle);
                    break;
                case TextAnnotationModel text:
                    DrawText(graphics, text);
                    break;
            }
        }

        graphics.SmoothingMode = smoothing;
    }

    /// <summary>The font of a text annotation, sized in pixels; the caller disposes it.</summary>
    public static Font CreateFont(int fontSize) => new(FontFamilyName, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);

    private static Pen CreatePen(Color color, int width) => new(color, width)
    {
        StartCap = LineCap.Round,
        EndCap = LineCap.Round,
        LineJoin = LineJoin.Round,
    };

    private static void DrawStroke(Graphics graphics, StrokeAnnotationModel stroke)
    {
        if (stroke.Points.Count == 1)
        {
            // A dot as wide as the line
            Point center = stroke.Points[0];
            float radius = stroke.Width / 2f;
            using var brush = new SolidBrush(stroke.Color);

            graphics.FillEllipse(brush, center.X - radius, center.Y - radius, stroke.Width, stroke.Width);

            return;
        }

        // One path, so a see-through highlighter isn't darker where the line crosses itself
        using var pen = CreatePen(stroke.Color, stroke.Width);
        using var path = new GraphicsPath();

        path.AddLines(stroke.Points.ToArray());
        graphics.DrawPath(pen, path);
    }

    private static void DrawArrow(Graphics graphics, ArrowAnnotationModel arrow)
    {
        using var pen = CreatePen(arrow.Color, arrow.Width);

        // Scaled by the pen width; thin lines get a larger head, so it can be seen
        float headSize = arrow.Width <= 2 ? 5 : 3.5f;
        using var head = new AdjustableArrowCap(headSize, headSize, isFilled: true);

        if (arrow.Heads is SnipArrowHeads.Start or SnipArrowHeads.Both)
        {
            pen.CustomStartCap = head;
        }

        if (arrow.Heads is SnipArrowHeads.End or SnipArrowHeads.Both)
        {
            pen.CustomEndCap = head;
        }

        graphics.DrawLine(pen, arrow.From, arrow.To);
    }

    private static void DrawRectangle(Graphics graphics, RectangleAnnotationModel rectangle)
    {
        int corner = 2 * (6 + 2 * rectangle.Width);
        using var pen = CreatePen(rectangle.Color, rectangle.Width);
        using var path = new GraphicsPath();

        path.AddRoundedRectangle(rectangle.Bounds, new Size(corner, corner));
        graphics.DrawPath(pen, path);
    }

    private static void DrawText(Graphics graphics, TextAnnotationModel text)
    {
        using var font = CreateFont(text.FontSize);
        using var brush = new SolidBrush(text.Color);
        TextRenderingHint hint = graphics.TextRenderingHint;

        // GDI+, not TextRenderer: TextRenderer ignores the color's transparency
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.DrawString(text.Text, font, brush, text.Location, StringFormat.GenericTypographic);
        graphics.TextRenderingHint = hint;
    }
}
