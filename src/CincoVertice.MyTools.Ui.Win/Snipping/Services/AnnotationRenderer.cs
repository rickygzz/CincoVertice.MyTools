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

    private const int HandleSize = 8;

    // How tightly a smooth pen's curve follows its points: 0 is straight lines, and GDI+'s usual curve is 0.5
    private const float SmoothTension = 0.5f;

    private static readonly Color _selectionColor = Color.FromArgb(0, 120, 215);

    /// <param name="graphics">Where to draw them.</param>
    /// <param name="annotations">What to draw, in order.</param>
    /// <param name="snip">The snip they go over, without annotations; blurs take their color from around them on it.</param>
    /// <param name="shown">
    ///     The part of the snip that's kept, when it's cropped: what's cut away isn't in the image, so a blur can't take
    ///     its color from there either.
    /// </param>
    public static void Draw(
        Graphics graphics,
        IReadOnlyList<AnnotationModel> annotations,
        Bitmap snip,
        Rectangle? shown = null)
    {
        Rectangle kept = shown ?? new Rectangle(Point.Empty, snip.Size);

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
                    BlurRenderer.Draw(graphics, blur, blurs, snip, kept);
                    break;
                case HighlightAnnotationModel highlight:
                    DrawHighlight(graphics, highlight);
                    break;
                case StepAnnotationModel step:
                    DrawStep(graphics, step);
                    break;
            }
        }

        graphics.SmoothingMode = smoothing;
    }

    /// <summary>The font of a text annotation, sized in pixels; the caller disposes it.</summary>
    public static Font CreateFont(int fontSize) => new(FontFamilyName, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);

    /// <returns>How large a text annotation is, measured the way it's drawn.</returns>
    public static Size MeasureText(string text, int fontSize)
    {
        using var image = new Bitmap(1, 1);
        using var graphics = Graphics.FromImage(image);
        using Font font = CreateFont(fontSize);

        return Size.Ceiling(graphics.MeasureString(text, font, PointF.Empty, StringFormat.GenericTypographic));
    }

    /// <summary>What shows an annotation is selected; on the editor's screen only, never in the image.</summary>
    /// <param name="graphics">The screen, not scaled.</param>
    /// <param name="selection">What shows it, in snip pixels.</param>
    /// <param name="zoom">How large the snip is shown; the frame and the handles stay as thin and as small.</param>
    public static void DrawSelection(Graphics graphics, SnipSelectionModel selection, float zoom = 1)
    {
        using var pen = new Pen(_selectionColor);

        if (selection.Box is Rectangle box)
        {
            graphics.DrawRectangle(
                pen,
                (int)Math.Round(box.X * zoom),
                (int)Math.Round(box.Y * zoom),
                (int)Math.Round(box.Width * zoom),
                (int)Math.Round(box.Height * zoom));
        }

        foreach (Point handle in selection.Handles)
        {
            var square = new Rectangle(
                (int)Math.Round(handle.X * zoom) - HandleSize / 2,
                (int)Math.Round(handle.Y * zoom) - HandleSize / 2,
                HandleSize,
                HandleSize);

            graphics.FillRectangle(Brushes.White, square);
            graphics.DrawRectangle(pen, square);
        }
    }

    private static Pen CreatePen(Color color, int width) => new(color, width)
    {
        StartCap = LineCap.Round,
        EndCap = LineCap.Round,
        LineJoin = LineJoin.Round,
    };

    /// <summary>A rectangle with corners this round; one too small for them is as round as it can be.</summary>
    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int diameter = Math.Min(2 * radius, Math.Min(bounds.Width, bounds.Height));

        if (diameter > 0)
        {
            path.AddRoundedRectangle(bounds, new Size(diameter, diameter));
        }
        else
        {
            path.AddRectangle(bounds);
        }

        return path;
    }

    /// <summary>With rounded corners, as a highlighter's line has round ends.</summary>
    private static void DrawHighlight(Graphics graphics, HighlightAnnotationModel highlight)
    {
        using var brush = new SolidBrush(highlight.Color);
        using GraphicsPath path = RoundedRectangle(highlight.Bounds, highlight.CornerRadius);
        PixelOffsetMode offset = graphics.PixelOffsetMode;

        // Half: the straight edges then fall on the pixels' edges, so they're as strong as the rest, not fainter
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.FillPath(brush, path);
        graphics.PixelOffsetMode = offset;
    }

    /// <summary>
    ///     A filled circle with the number in the middle, white or black, whichever shows better on the circle's color.
    ///     A number too wide for the circle gets a smaller font.
    /// </summary>
    private static void DrawStep(Graphics graphics, StepAnnotationModel step)
    {
        float radius = step.Diameter / 2f;
        var circle = new RectangleF(step.Center.X - radius, step.Center.Y - radius, step.Diameter, step.Diameter);
        Color fill = step.Color;
        Color ink = Color.FromArgb(fill.A, IsLight(fill) ? Color.Black : Color.White);
        string number = step.Number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        float fontSize = step.Diameter * (number.Length <= 1 ? 0.6f : number.Length == 2 ? 0.5f : 0.38f);

        using var brush = new SolidBrush(fill);
        using var inkBrush = new SolidBrush(ink);
        using var font = new Font(FontFamilyName, Math.Max(fontSize, 1), FontStyle.Bold, GraphicsUnit.Pixel);
        using var format = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        TextRenderingHint hint = graphics.TextRenderingHint;

        graphics.FillEllipse(brush, circle);

        // GDI+, not TextRenderer: TextRenderer ignores the color's transparency
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.DrawString(number, font, inkBrush, circle, format);
        graphics.TextRenderingHint = hint;
    }

    /// <summary>Whether black shows better on the color than white, by how light it looks to the eye.</summary>
    private static bool IsLight(Color color) => 0.299 * color.R + 0.587 * color.G + 0.114 * color.B > 160;

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

        // A smooth pen's line is a curve through its points, which are far apart where it's straight
        if (stroke.IsSmooth && stroke.Points.Count > 2)
        {
            path.AddCurve(stroke.Points.ToArray(), SmoothTension);
        }
        else
        {
            path.AddLines(stroke.Points.ToArray());
        }

        graphics.DrawPath(pen, path);
    }

    private static void DrawArrow(Graphics graphics, ArrowAnnotationModel arrow)
    {
        using var pen = CreatePen(arrow.Color, arrow.Width);

        // Scaled by the pen width
        using var head = new AdjustableArrowCap(arrow.HeadSize, arrow.HeadSize, isFilled: true);

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
        using var pen = CreatePen(rectangle.Color, rectangle.Width);
        using GraphicsPath path = RoundedRectangle(rectangle.Bounds, rectangle.CornerRadius);

        graphics.DrawPath(pen, path);
    }

    private static void DrawText(Graphics graphics, TextAnnotationModel text)
    {
        using var font = CreateFont(text.FontSize);

        // A width of 0 is what a text keeps when its edge is taken off, with the color for when it's put back
        if (text.Outline is { Width: > 0 } outline)
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

        // Only what can show: a long pasted text can be larger than any bitmap can be. With a margin, since GDI+ fades
        // the outer pixels of an image, which would show as lines where the layer ends in the middle of a letter
        var visible = Rectangle.Ceiling(graphics.VisibleClipBounds);

        visible.Inflate(2, 2);
        area.Intersect(visible);

        if (area.Width <= 0 || area.Height <= 0)
        {
            return;
        }

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
