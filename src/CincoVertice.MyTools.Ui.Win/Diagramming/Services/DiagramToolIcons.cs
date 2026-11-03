using System.Drawing.Drawing2D;
using System.Drawing.Text;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;

namespace CincoVertice.MyTools.Ui.Win.Diagramming.Services;

/// <summary>
///     Draws the diagram editor's tool icons, at any size so they're sharp at any DPI. Each is drawn on a 16 by 16
///     grid, scaled to the size asked for; the shape tools show the shape's own figures.
/// </summary>
public static class DiagramToolIcons
{
    private const float Grid = 16f;
    private const float LineWidth = 1.2f;

    private static readonly Color _ink = Color.FromArgb(50, 50, 50);

    /// <returns>A new square image; the caller disposes it.</returns>
    public static Bitmap Create(DiagramTool tool, int size)
    {
        var icon = new Bitmap(size, size);

        using var graphics = Graphics.FromImage(icon);
        using var pen = new Pen(_ink, LineWidth) { LineJoin = LineJoin.Round };

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.ScaleTransform(size / Grid, size / Grid);

        switch (tool)
        {
            case DiagramTool.Select:
                DrawPointer(graphics);
                break;
            case DiagramTool.Connector:
                DrawConnector(graphics, pen);
                break;
            case DiagramTool.Text:
                DrawText(graphics);
                break;
            default:
                DrawShape(graphics, pen, Enum.Parse<DiagramShapeKind>(tool.ToString()));
                break;
        }

        return icon;
    }

    private static void DrawPointer(Graphics graphics)
    {
        using var brush = new SolidBrush(_ink);

        graphics.FillPolygon(
            brush,
            [
                new PointF(4, 2),
                new PointF(4, 13.5f),
                new PointF(7, 10.8f),
                new PointF(9, 14.5f),
                new PointF(10.8f, 13.6f),
                new PointF(8.9f, 10),
                new PointF(12.5f, 10),
            ]);
    }

    private static void DrawConnector(Graphics graphics, Pen pen)
    {
        using var head = new AdjustableArrowCap(3.2f, 3.2f, isFilled: true);

        pen.CustomEndCap = head;
        graphics.DrawLines(pen, [new PointF(2, 13), new PointF(8, 13), new PointF(8, 4), new PointF(13, 4)]);
    }

    private static void DrawText(Graphics graphics)
    {
        using var font = new Font(DiagramSvg.FontFamily, 13, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(_ink);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.DrawString("T", font, brush, new RectangleF(0, 0, Grid, Grid + 1), format);
    }

    private static void DrawShape(Graphics graphics, Pen pen, DiagramShapeKind kind)
    {
        // Figures have rounded parts of a fixed size, so one as large as a real shape is scaled down to look like it
        const int scale = 10;

        graphics.ScaleTransform(1f / scale, 1f / scale);
        pen.Width *= scale;

        foreach (DiagramFigureModel figure in DiagramFigures.Of(kind, new Rectangle(20, 30, 120, 100)))
        {
            using GraphicsPath path = DiagramRenderer.ToPath(figure);

            graphics.DrawPath(pen, path);
        }
    }
}
