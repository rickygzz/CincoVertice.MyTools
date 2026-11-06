using System.Drawing.Drawing2D;
using System.Drawing.Text;
using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     Draws the snip editor's tool icons, at any size so they're sharp at any DPI. Each icon is drawn on a 16 by 16
///     grid, scaled to the size asked for.
/// </summary>
public static class ToolIcons
{
    private const float Grid = 16f;
    private const float LineWidth = 1.4f;

    private static readonly Color _ink = Color.FromArgb(50, 50, 50);
    private static readonly Color _marker = Color.FromArgb(255, 210, 0);

    /// <returns>A new square image; the caller disposes it.</returns>
    public static Bitmap Create(SnipTool tool, int size)
    {
        var icon = new Bitmap(size, size);

        using var graphics = Graphics.FromImage(icon);
        using var pen = new Pen(_ink, LineWidth) { LineJoin = LineJoin.Round, StartCap = LineCap.Round };

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.ScaleTransform(size / Grid, size / Grid);

        switch (tool)
        {
            case SnipTool.Pen:
                DrawPen(graphics, pen);
                break;
            case SnipTool.Highlighter:
                DrawHighlighter(graphics, pen);
                break;
            case SnipTool.Arrow:
                DrawArrow(graphics, pen);
                break;
            case SnipTool.Rectangle:
                DrawRectangle(graphics, pen);
                break;
            case SnipTool.Text:
                DrawText(graphics);
                break;
            case SnipTool.Blur:
                DrawBlur(graphics);
                break;
            case SnipTool.Select:
                DrawSelect(graphics, pen);
                break;
            case SnipTool.Step:
                DrawStep(graphics);
                break;
            case SnipTool.Crop:
                DrawCrop(graphics, pen);
                break;
        }

        return icon;
    }

    /// <returns>An arrow turning back to the left; the caller disposes it.</returns>
    public static Bitmap CreateUndo(int size) => CreateHistory(size, mirrored: false);

    /// <returns>The undo icon mirrored, turning to the right; the caller disposes it.</returns>
    public static Bitmap CreateRedo(int size) => CreateHistory(size, mirrored: true);

    /// <returns>A level arrow with the heads asked for: End points right; the caller disposes it.</returns>
    public static Bitmap CreateArrowHeads(SnipArrowHeads heads, int size)
    {
        var icon = new Bitmap(size, size);

        using var graphics = Graphics.FromImage(icon);
        using var head = new AdjustableArrowCap(3.2f, 3.2f, isFilled: true);
        using var pen = new Pen(_ink, LineWidth);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.ScaleTransform(size / Grid, size / Grid);

        if (heads is SnipArrowHeads.Start or SnipArrowHeads.Both)
        {
            pen.CustomStartCap = head;
        }

        if (heads is SnipArrowHeads.End or SnipArrowHeads.Both)
        {
            pen.CustomEndCap = head;
        }

        graphics.DrawLine(pen, 2.5f, 8, 13.5f, 8);

        return icon;
    }

    private static Bitmap CreateHistory(int size, bool mirrored)
    {
        var icon = new Bitmap(size, size);

        using var graphics = Graphics.FromImage(icon);
        using var head = new AdjustableArrowCap(3.2f, 3.2f, isFilled: true);
        using var pen = new Pen(_ink, LineWidth) { LineJoin = LineJoin.Round, EndCap = LineCap.Round };
        using var path = new GraphicsPath();

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.ScaleTransform(size / Grid, size / Grid);

        if (mirrored)
        {
            graphics.TranslateTransform(Grid, 0);
            graphics.ScaleTransform(-1, 1);
        }

        // From the head on the left: along the top, round the right, and back along the bottom
        pen.CustomStartCap = head;
        path.AddLine(3.5f, 5.5f, 9.5f, 5.5f);
        path.AddArc(6.25f, 5.5f, 7, 7, 270, 180);
        path.AddLine(9.75f, 12.5f, 5.5f, 12.5f);
        graphics.DrawPath(pen, path);

        return icon;
    }

    /// <summary>A pencil from the bottom left (its tip) to the top right.</summary>
    private static void DrawPen(Graphics graphics, Pen pen)
    {
        graphics.TranslateTransform(8, 8);
        graphics.RotateTransform(-45);

        PointF[] tip = [new(-4.5f, -2), new(-8, 0), new(-4.5f, 2)];
        using var tipBrush = new SolidBrush(_ink);

        graphics.DrawRectangle(pen, -4.5f, -2, 10, 4);
        graphics.DrawLine(pen, 3, -2, 3, 2);
        graphics.DrawPolygon(pen, tip);
        graphics.FillPolygon(tipBrush, [new PointF(-6.8f, -0.7f), new PointF(-8, 0), new PointF(-6.8f, 0.7f)]);
    }

    /// <summary>A marker with a chisel tip over a yellow stroke.</summary>
    private static void DrawHighlighter(Graphics graphics, Pen pen)
    {
        using var stroke = new Pen(_marker, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var tipBrush = new SolidBrush(_marker);

        graphics.DrawLine(stroke, 2.5f, 14, 13.5f, 14);

        graphics.TranslateTransform(8.5f, 6.5f);
        graphics.RotateTransform(-45);

        PointF[] tip = [new(-3, -2.5f), new(-6.5f, -1.2f), new(-6.5f, 1.2f), new(-3, 2.5f)];

        graphics.FillPolygon(tipBrush, tip);
        graphics.DrawPolygon(pen, tip);
        graphics.DrawRectangle(pen, -3, -2.5f, 8.5f, 5);
    }

    private static void DrawArrow(Graphics graphics, Pen pen)
    {
        // Scaled by the pen width, like the arrows drawn on a snip
        using var head = new AdjustableArrowCap(3.2f, 3.2f, isFilled: true);

        pen.CustomEndCap = head;
        graphics.DrawLine(pen, 3, 13, 12.5f, 3.5f);
    }

    private static void DrawRectangle(Graphics graphics, Pen pen)
    {
        using var path = new GraphicsPath();

        path.AddRoundedRectangle(new RectangleF(2, 3.5f, 12, 9), new SizeF(5, 5));
        graphics.DrawPath(pen, path);
    }

    /// <summary>Two crossing corners, as cropping tools usually show.</summary>
    private static void DrawCrop(Graphics graphics, Pen pen)
    {
        graphics.DrawLines(pen, [new PointF(4.5f, 1.5f), new PointF(4.5f, 11.5f), new PointF(14.5f, 11.5f)]);
        graphics.DrawLines(pen, [new PointF(1.5f, 4.5f), new PointF(11.5f, 4.5f), new PointF(11.5f, 14.5f)]);
    }

    /// <summary>A circle with a 1 in it, as the steps look.</summary>
    private static void DrawStep(Graphics graphics)
    {
        using var brush = new SolidBrush(_ink);
        using var font = new Font(AnnotationRenderer.FontFamilyName, 9, FontStyle.Bold, GraphicsUnit.Pixel);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        graphics.FillEllipse(brush, 1.5f, 1.5f, 13, 13);
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.DrawString("1", font, Brushes.White, new RectangleF(1.5f, 2, 13, 13), format);
    }

    /// <summary>A mouse pointer.</summary>
    private static void DrawSelect(Graphics graphics, Pen pen)
    {
        PointF[] pointer =
        [
            new(4, 2), new(4, 13), new(6.8f, 10.5f), new(8.7f, 14.5f), new(10.6f, 13.6f), new(8.7f, 9.7f), new(12.2f, 9.5f),
        ];

        graphics.FillPolygon(Brushes.White, pointer);
        graphics.DrawPolygon(pen, pointer);
    }

    /// <summary>A square of blocks in different grays, as a blurred picture looks.</summary>
    private static void DrawBlur(Graphics graphics)
    {
        int[] grays = [70, 170, 110, 200, 90, 140, 190, 60, 150];

        for (int i = 0; i < grays.Length; i++)
        {
            using var brush = new SolidBrush(Color.FromArgb(grays[i], grays[i], grays[i]));

            graphics.FillRectangle(brush, 2.5f + 3.67f * (i % 3), 2.5f + 3.67f * (i / 3), 3.67f, 3.67f);
        }
    }

    private static void DrawText(Graphics graphics)
    {
        using var font = new Font(AnnotationRenderer.FontFamilyName, 13, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(_ink);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.DrawString("A", font, brush, new RectangleF(0, 0, Grid, Grid + 1), format);
    }
}
