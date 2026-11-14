using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     The part of a diagram a file shows: the SVG image and the PDF page are this large, so both look the same.
/// </summary>
public static class DiagramArea
{
    private const int Margin = 20;

    /// <summary>Around everything, including text and lines that go past the shapes, with a margin.</summary>
    /// <returns>In diagram pixels; 100 by 100 at the origin for an empty diagram.</returns>
    public static Rectangle Of(DiagramModel diagram)
    {
        List<RectangleF> parts = [.. diagram.Shapes.Select(BoundsWithText)];

        foreach (DiagramConnectorModel connector in diagram.Connectors)
        {
            if (DiagramRouter.PathOf(connector, diagram) is not { } path)
            {
                continue;
            }

            IEnumerable<PointF> points = path.Points
                .Concat(path.StartHead ?? [])
                .Concat(path.EndHead ?? []);

            parts.AddRange(points.Select(point => new RectangleF(point, SizeF.Empty)));

            if (connector.Label.Length > 0)
            {
                DiagramTextBlockModel label =
                    DiagramTextLayout.LayoutAt(connector.Label, path.LabelCenter, connector.Style.FontSize);

                parts.Add(label.Bounds);
            }
        }

        if (parts.Count == 0)
        {
            return new Rectangle(0, 0, 100, 100);
        }

        RectangleF area = parts.Aggregate(RectangleF.Union);

        // Half of a line's width goes outside what it follows
        int widestLine = diagram.Shapes.Select(shape => shape.Style.LineWidth)
            .Concat(diagram.Connectors.Select(connector => connector.Style.LineWidth))
            .Max();
        int widestTextOutline = diagram.Shapes.Select(shape => shape.Style.TextOutlineWidth)
            .Concat(diagram.Connectors.Select(connector => connector.Style.TextOutlineWidth))
            .Max();
        int margin = Math.Max(Margin, Math.Max(widestLine / 2, widestTextOutline) + 1);
        var whole = Rectangle.Ceiling(area);

        whole.Inflate(margin, margin);

        return whole;
    }

    /// <summary>A shape's text isn't cut at its edges, so it can go past them.</summary>
    private static RectangleF BoundsWithText(DiagramShapeModel shape)
    {
        if (shape.Text.Length == 0)
        {
            return shape.Bounds;
        }

        RectangleF textArea = DiagramFigures.TextArea(shape.Kind, shape.Bounds);

        return RectangleF.Union(
            shape.Bounds,
            DiagramTextLayout.Layout(shape.Text, textArea, shape.Style.FontSize).Bounds);
    }
}
