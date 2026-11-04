using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     Draws snip annotations, the same way on the editor's screen and in the copied or saved image.
/// </summary>
public static class AnnotationRenderer
{
    public const string FontFamilyName = "Segoe UI";

    /// <param name="graphics">Where to draw them.</param>
    /// <param name="annotations">What to draw, in order.</param>
    /// <param name="snip">The snip they go over, without annotations; blurs take their color from around them on it.</param>
    public static void Draw(Graphics graphics, IReadOnlyList<AnnotationModel> annotations, Bitmap snip)
    {
        SmoothingMode smoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        List<Rectangle> blurs = [.. annotations.OfType<BlurAnnotationModel>().Select(blur => blur.Bounds)];

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
                case BlurAnnotationModel blur:
                    BlurRenderer.Draw(graphics, blur, blurs, snip);
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

        if (text.Outline is { } outline)
        {
            DrawOutlinedText(graphics, text, font, outline);

            return;
        }

        using var brush = new SolidBrush(text.Color);
        TextRenderingHint hint = graphics.TextRenderingHint;

        // GDI+, not TextRenderer: TextRenderer ignores the color's transparency
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.DrawString(text.Text, font, brush, text.Location, StringFormat.GenericTypographic);
        graphics.TextRenderingHint = hint;
    }

    /// <summary>
    ///     Solid on a layer of its own, which goes on as see-through as the text as a whole: drawn directly, the edge
    ///     would show through see-through letters.
    /// </summary>
    private static void DrawOutlinedText(
        Graphics graphics,
        TextAnnotationModel text,
        Font font,
        SnipTextOutlineModel outline)
    {
        // Winding: a letter can be made of overlapping pieces, and the default fill leaves a hole where they overlap
        using var path = new GraphicsPath(FillMode.Winding);

        path.AddString(
            text.Text,
            font.FontFamily,
            (int)font.Style,
            font.Size,
            text.Location,
            StringFormat.GenericTypographic);

        // Twice as wide, since the letters cover the inner half of the line
        using var pen = new Pen(outline.Color, outline.Width * 2) { LineJoin = LineJoin.Round };
        using var brush = new SolidBrush(Color.FromArgb(255, text.Color));
        var area = Rectangle.Ceiling(path.GetBounds(null, pen));

        area.Inflate(1, 1);

        using var layer = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppPArgb);

        using (var layerGraphics = Graphics.FromImage(layer))
        {
            layerGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            layerGraphics.TranslateTransform(-area.X, -area.Y);
            layerGraphics.DrawPath(pen, path);
            layerGraphics.FillPath(brush, path);
        }

        using var attributes = new ImageAttributes();

        attributes.SetColorMatrix(new ColorMatrix { Matrix33 = text.Color.A / 255f });
        graphics.DrawImage(layer, area, 0, 0, area.Width, area.Height, GraphicsUnit.Pixel, attributes);
    }
}
