using System.Drawing.Drawing2D;
using System.Drawing.Text;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;

namespace CincoVertice.MyTools.Ui.Win.Diagramming.Services;

/// <summary>
///     Draws a diagram editor's canvas: the grid, the diagram, and on top of it what only the editor shows, like the
///     selection and the alignment guides.
/// </summary>
public static class DiagramRenderer
{
    // In canvas pixels, so they stay the same size at any zoom
    private const float HandleSize = 8;
    private const float OverlayLineWidth = 1;
    private const float AnchorRadius = 5;
    private const float AnchorHighlightRadius = 7;

    private static readonly Color _gridColor = Color.FromArgb(235, 235, 235);
    private static readonly Color _selectionColor = Color.FromArgb(0, 120, 215);
    private static readonly Color _guideColor = Color.FromArgb(230, 0, 140);

    public static void Draw(Graphics graphics, DiagramSceneModel scene, Size canvasSize)
    {
        DiagramViewportModel viewport = scene.Viewport;

        graphics.Clear(Color.White);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        DrawGrid(graphics, viewport, canvasSize);

        graphics.TranslateTransform(viewport.Pan.X, viewport.Pan.Y);
        graphics.ScaleTransform(viewport.Zoom, viewport.Zoom);

        foreach (DiagramShapeModel shape in scene.Diagram.Shapes)
        {
            DrawShape(graphics, shape);
        }

        foreach (DiagramConnectorModel connector in scene.Diagram.Connectors)
        {
            DrawConnector(graphics, connector, scene.Diagram, scene.SelectedConnectorIds.Contains(connector.Id), viewport.Zoom);
        }

        DrawOverlay(graphics, scene);
    }

    /// <summary>A path for GDI+ from a figure, the same one the SVG file writes.</summary>
    public static GraphicsPath ToPath(DiagramFigureModel figure)
    {
        var path = new GraphicsPath();
        PointF current = PointF.Empty;

        foreach (DiagramPathSegmentModel segment in figure.Segments)
        {
            switch (segment.Operation)
            {
                case DiagramPathOperation.Move:
                    path.StartFigure();
                    current = segment.Points[0];
                    break;
                case DiagramPathOperation.Line:
                    path.AddLine(current, segment.Points[0]);
                    current = segment.Points[0];
                    break;
                case DiagramPathOperation.Curve:
                    path.AddBezier(current, segment.Points[0], segment.Points[1], segment.Points[2]);
                    current = segment.Points[2];
                    break;
                default:
                    path.CloseFigure();
                    break;
            }
        }

        return path;
    }

    private static Font CreateFont(int fontSize) =>
        new(DiagramSvg.FontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);

    private static void DrawGrid(Graphics graphics, DiagramViewportModel viewport, Size canvasSize)
    {
        const int spacing = DiagramSnapper.GridSize * 5;

        float step = spacing * viewport.Zoom;
        float startX = viewport.Pan.X % step;
        float startY = viewport.Pan.Y % step;
        using var pen = new Pen(_gridColor);
        SmoothingMode smoothing = graphics.SmoothingMode;

        graphics.SmoothingMode = SmoothingMode.None;

        for (float x = startX; x < canvasSize.Width; x += step)
        {
            graphics.DrawLine(pen, x, 0, x, canvasSize.Height);
        }

        for (float y = startY; y < canvasSize.Height; y += step)
        {
            graphics.DrawLine(pen, 0, y, canvasSize.Width, y);
        }

        graphics.SmoothingMode = smoothing;
    }

    private static Pen CreateLinePen(DiagramStyleModel style)
    {
        var pen = new Pen(style.Line, style.LineWidth) { LineJoin = LineJoin.Round };

        if (style.Dashed)
        {
            // In line widths, like the SVG file's 4 and 3 times the width
            pen.DashPattern = [4, 3];
        }

        return pen;
    }

    private static void DrawShape(Graphics graphics, DiagramShapeModel shape)
    {
        using Pen pen = CreateLinePen(shape.Style);

        foreach (DiagramFigureModel figure in DiagramFigures.Of(shape.Kind, shape.Bounds))
        {
            using GraphicsPath path = ToPath(figure);

            if (figure.Filled && !shape.Style.Fill.IsEmpty)
            {
                using var brush = new SolidBrush(shape.Style.Fill);

                graphics.FillPath(brush, path);
            }

            if (shape.Style.LineWidth > 0)
            {
                graphics.DrawPath(pen, path);
            }
        }

        if (shape.Text.Length > 0)
        {
            RectangleF area = DiagramFigures.TextArea(shape.Kind, shape.Bounds);

            DrawText(graphics, DiagramTextLayout.Layout(shape.Text, area, shape.Style.FontSize), shape.Style);
        }
    }

    private static void DrawConnector(
        Graphics graphics,
        DiagramConnectorModel connector,
        DiagramModel diagram,
        bool isSelected,
        float zoom)
    {
        if (DiagramRouter.PathOf(connector, diagram) is not { } route)
        {
            return;
        }

        using GraphicsPath line = ToPath(route.Line);

        if (isSelected)
        {
            using var halo = new Pen(Color.FromArgb(90, _selectionColor), connector.Style.LineWidth + 6 / zoom);

            graphics.DrawPath(halo, line);
        }

        if (connector.Style.LineWidth > 0)
        {
            using Pen pen = CreateLinePen(connector.Style);

            graphics.DrawPath(pen, line);
        }

        using var brush = new SolidBrush(connector.Style.Line);

        foreach (IReadOnlyList<PointF>? head in new[] { route.StartHead, route.EndHead })
        {
            if (head is not null)
            {
                graphics.FillPolygon(brush, [.. head]);
            }
        }

        if (connector.Label.Length > 0)
        {
            DiagramTextBlockModel block = DiagramTextLayout.LayoutAt(
                connector.Label,
                route.LabelCenter,
                connector.Style.FontSize);

            // So the line doesn't run through the words
            RectangleF back = block.Bounds;

            back.Inflate(2, 0);
            graphics.FillRectangle(Brushes.White, back);
            DrawText(graphics, block, connector.Style);
        }
    }

    private static void DrawText(Graphics graphics, DiagramTextBlockModel block, DiagramStyleModel style)
    {
        using Font font = CreateFont(style.FontSize);
        using var brush = new SolidBrush(style.TextColor);
        using var format = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.MeasureTrailingSpaces,
        };

        for (int i = 0; i < block.Lines.Count; i++)
        {
            graphics.DrawString(block.Lines[i], font, brush, block.CenterX, block.Top + block.LineHeight * i, format);
        }
    }

    private static void DrawOverlay(Graphics graphics, DiagramSceneModel scene)
    {
        float zoom = scene.Viewport.Zoom;
        float lineWidth = OverlayLineWidth / zoom;
        using var selection = new Pen(_selectionColor, lineWidth);
        using var guide = new Pen(_guideColor, lineWidth);

        foreach (DiagramGuideModel line in scene.Guides)
        {
            graphics.DrawLine(guide, line.From, line.To);
        }

        foreach (DiagramShapeModel shape in scene.Diagram.Shapes)
        {
            if (scene.SelectedShapeIds.Contains(shape.Id))
            {
                graphics.DrawRectangle(selection, shape.Bounds);
            }

            if (shape.Id == scene.ConnectTargetId)
            {
                using var target = new Pen(_selectionColor, 3 / zoom);

                graphics.DrawRectangle(target, shape.Bounds);
            }
        }

        if (scene.SelectionBand is Rectangle band)
        {
            using var fill = new SolidBrush(Color.FromArgb(40, _selectionColor));

            graphics.FillRectangle(fill, band);
            graphics.DrawRectangle(selection, band);
        }

        if (scene.DraftConnector is var (from, to))
        {
            using var draft = new Pen(_selectionColor, 2 / zoom) { DashStyle = DashStyle.Dash };

            graphics.DrawLine(draft, from, to);
        }

        if (scene.HandleBounds is Rectangle box)
        {
            DrawHandles(graphics, box, zoom, selection);
        }

        if (scene.Anchors is { } anchors)
        {
            DrawAnchors(graphics, anchors, zoom, selection);
        }

        DrawConnectorEnds(graphics, scene.ConnectorEnds ?? [], zoom);
        DrawBends(graphics, scene.ConnectorBends ?? [], zoom, selection);
    }

    private static void DrawBends(Graphics graphics, IReadOnlyList<Point> bends, float zoom, Pen pen)
    {
        float size = HandleSize / zoom;

        foreach (Point bend in bends)
        {
            // A diamond, unlike the squares that scale and the dots at a connector's ends
            PointF[] diamond =
            [
                new(bend.X, bend.Y - size * 0.7f),
                new(bend.X + size * 0.7f, bend.Y),
                new(bend.X, bend.Y + size * 0.7f),
                new(bend.X - size * 0.7f, bend.Y),
            ];

            graphics.FillPolygon(Brushes.White, diamond);
            graphics.DrawPolygon(pen, diamond);
        }
    }

    private static void DrawConnectorEnds(Graphics graphics, IReadOnlyList<PointF> ends, float zoom)
    {
        float radius = AnchorRadius / zoom;
        using var fill = new SolidBrush(_selectionColor);
        using var outline = new Pen(Color.White, OverlayLineWidth / zoom);

        foreach (PointF end in ends)
        {
            var circle = new RectangleF(end.X - radius, end.Y - radius, radius * 2, radius * 2);

            graphics.FillEllipse(fill, circle);
            graphics.DrawEllipse(outline, circle);
        }
    }

    private static void DrawAnchors(Graphics graphics, DiagramAnchorsModel anchors, float zoom, Pen pen)
    {
        using var highlight = new SolidBrush(_selectionColor);

        foreach (PointF center in anchors.Points)
        {
            bool isHighlighted = center == anchors.Highlighted;
            float radius = (isHighlighted ? AnchorHighlightRadius : AnchorRadius) / zoom;
            var circle = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);

            graphics.FillEllipse(isHighlighted ? highlight : Brushes.White, circle);
            graphics.DrawEllipse(pen, circle);
        }
    }

    private static void DrawHandles(Graphics graphics, Rectangle box, float zoom, Pen pen)
    {
        float size = HandleSize / zoom;

        graphics.DrawRectangle(pen, box);

        foreach (DiagramHandle handle in Enum.GetValues<DiagramHandle>())
        {
            Point center = DiagramHitTester.HandleCenter(box, handle);
            var square = new RectangleF(center.X - size / 2, center.Y - size / 2, size, size);

            graphics.FillRectangle(Brushes.White, square);
            graphics.DrawRectangle(pen, square.X, square.Y, square.Width, square.Height);
        }
    }
}
