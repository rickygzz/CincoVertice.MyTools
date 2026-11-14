using System.Drawing;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     Turns a diagram into an SVG image any browser or program can show. The diagram itself goes along, as JSON in
///     the SVG's metadata, so this editor can open the file again with its shapes and connectors. An SVG file from
///     elsewhere has no such metadata, and can't be opened.
/// </summary>
public static class DiagramSvg
{
    private const string MetadataId = "mytools-diagram";

    private static readonly XNamespace _svg = "http://www.w3.org/2000/svg";
    private static readonly XNamespace _xlink = "http://www.w3.org/1999/xlink";

    public static string Write(DiagramModel diagram)
    {
        Rectangle area = DiagramArea.Of(diagram);
        var root = new XElement(
            _svg + "svg",
            new XAttribute("viewBox", $"{area.X} {area.Y} {area.Width} {area.Height}"),
            new XAttribute("width", area.Width),
            new XAttribute("height", area.Height),
            new XElement(_svg + "metadata", new XAttribute("id", MetadataId), DiagramJson.Write(diagram)));

        if (diagram.Shapes.Any(shape => shape.Image is not null))
        {
            root.Add(new XAttribute(XNamespace.Xmlns + "xlink", _xlink));
        }

        foreach (DiagramShapeModel shape in diagram.Shapes)
        {
            root.Add(ShapeElement(shape));
        }

        foreach (DiagramConnectorModel connector in diagram.Connectors)
        {
            if (DiagramRouter.PathOf(connector, diagram) is { } path)
            {
                root.Add(ConnectorElement(connector, path));
            }
        }

        return new XDocument(root).ToString();
    }

    /// <summary>Reads the diagram kept inside an SVG file.</summary>
    /// <exception cref="InvalidDataException">The text isn't an SVG file made by this editor.</exception>
    public static DiagramModel Read(string svg)
    {
        XDocument document;

        try
        {
            document = XDocument.Parse(svg);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException("This isn't an SVG file.", ex);
        }

        XElement? metadata = document.Root?.Elements(_svg + "metadata")
            .FirstOrDefault(element => (string?)element.Attribute("id") == MetadataId);

        if (metadata is null)
        {
            throw new InvalidDataException("This SVG file wasn't made by the diagram editor, so it can't be edited here.");
        }

        return DiagramJson.Read(metadata.Value);
    }

    /// <summary>A figure as an SVG path's data.</summary>
    public static string PathData(DiagramFigureModel figure)
    {
        var data = new StringBuilder();

        foreach (DiagramPathSegmentModel segment in figure.Segments)
        {
            data.Append(segment.Operation switch
            {
                DiagramPathOperation.Move => "M",
                DiagramPathOperation.Line => "L",
                DiagramPathOperation.Curve => "C",
                _ => "Z",
            });

            data.Append(string.Join(' ', segment.Points.Select(point => $"{Number(point.X)} {Number(point.Y)}")));
        }

        return data.ToString();
    }

    private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static XElement ShapeElement(DiagramShapeModel shape)
    {
        var group = new XElement(_svg + "g");

        // Its own SVG, inside a link of its own, so its ids and styles can't mix with another's
        if (shape is { Kind: DiagramShapeKind.Image, Image: string markup })
        {
            group.Add(new XElement(
                _svg + "image",
                new XAttribute("x", shape.Bounds.X),
                new XAttribute("y", shape.Bounds.Y),
                new XAttribute("width", shape.Bounds.Width),
                new XAttribute("height", shape.Bounds.Height),
                new XAttribute(_xlink + "href", DiagramSvgImage.DataUri(markup))));
        }

        foreach (DiagramFigureModel figure in DiagramFigures.Of(shape.Kind, shape.Bounds, shape.CornerRadius))
        {
            var path = new XElement(
                _svg + "path",
                new XAttribute("d", PathData(figure)),
                new XAttribute("fill", figure.Filled && !shape.Style.Fill.IsEmpty ? Hex(shape.Style.Fill) : "none"));

            AddLineStyle(path, shape.Style);
            group.Add(path);
        }

        if (shape.Text.Length > 0)
        {
            AddText(group, DiagramTextLayout.OfShape(shape), shape.Style);
        }

        return group;
    }

    private static XElement ConnectorElement(DiagramConnectorModel connector, DiagramConnectorPathModel path)
    {
        var group = new XElement(_svg + "g");
        var line = new XElement(
            _svg + "path",
            new XAttribute("d", PathData(path.Line)),
            new XAttribute("fill", "none"));

        AddLineStyle(line, connector.Style);
        group.Add(line);

        foreach (IReadOnlyList<PointF>? head in new[] { path.StartHead, path.EndHead })
        {
            if (head is not null)
            {
                group.Add(new XElement(
                    _svg + "polygon",
                    new XAttribute("points", string.Join(' ', head.Select(Coordinates))),
                    new XAttribute("fill", Hex(connector.Style.Line))));
            }
        }

        if (connector.Label.Length > 0)
        {
            DiagramTextBlockModel block = DiagramTextLayout.LayoutAt(
                connector.Label,
                path.LabelCenter,
                connector.Style.FontSize);

            // So the line doesn't run through the words
            group.Add(new XElement(
                _svg + "rect",
                new XAttribute("x", Number(block.Bounds.X - 2)),
                new XAttribute("y", Number(block.Bounds.Y)),
                new XAttribute("width", Number(block.Bounds.Width + 4)),
                new XAttribute("height", Number(block.Bounds.Height)),
                new XAttribute("fill", "#FFFFFF")));
            AddText(group, block, connector.Style);
        }

        return group;
    }

    private static string Coordinates(PointF point) => $"{Number(point.X)},{Number(point.Y)}";

    private static void AddLineStyle(XElement element, DiagramStyleModel style)
    {
        element.Add(
            new XAttribute("stroke", Hex(style.Line)),
            new XAttribute("stroke-width", style.LineWidth),
            new XAttribute("stroke-linejoin", "round"));

        if (style.Dashed)
        {
            element.Add(new XAttribute("stroke-dasharray", $"{style.LineWidth * 4} {style.LineWidth * 3}"));
        }
    }

    /// <summary>
    ///     XML can't hold some control characters, or half of a surrogate pair, and writing them would fail the whole save.
    /// </summary>
    private static string WithoutInvalidCharacters(string line)
    {
        var valid = new StringBuilder(line.Length);

        for (int i = 0; i < line.Length; i++)
        {
            if (i + 1 < line.Length && char.IsSurrogatePair(line[i], line[i + 1]))
            {
                valid.Append(line[i]).Append(line[i + 1]);
                i++;
            }
            else if (XmlConvert.IsXmlChar(line[i]))
            {
                valid.Append(line[i]);
            }
        }

        return valid.ToString();
    }

    private static void AddText(XElement group, DiagramTextBlockModel block, DiagramStyleModel style)
    {
        if (style.TextOutlineWidth > 0)
        {
            // A copy under the text instead of paint-order, which not every program that shows SVG knows
            XElement outline = TextElement(block, style);

            outline.SetAttributeValue("fill", Hex(style.TextOutlineColor));
            outline.Add(
                new XAttribute("stroke", Hex(style.TextOutlineColor)),

                // Twice as wide, since the letters cover the inner half of the line
                new XAttribute("stroke-width", style.TextOutlineWidth * 2),
                new XAttribute("stroke-linejoin", "round"));
            group.Add(outline);
        }

        group.Add(TextElement(block, style));
    }

    private static XElement TextElement(DiagramTextBlockModel block, DiagramStyleModel style)
    {
        var text = new XElement(
            _svg + "text",
            new XAttribute("text-anchor", "middle"),

            // In quotes for a name with spaces; on a computer without it, any sans serif font shows instead
            new XAttribute("font-family", $"'{style.FontFamily.Replace("'", string.Empty)}', sans-serif"),
            new XAttribute("font-size", style.FontSize),
            new XAttribute("fill", Hex(style.TextColor)),
            new XAttribute(XNamespace.Xml + "space", "preserve"));

        for (int i = 0; i < block.Lines.Count; i++)
        {
            // y is the baseline, a little above the bottom of the line
            float baseline = block.Top + block.LineHeight * i + block.LineHeight * 0.75f;

            text.Add(new XElement(
                _svg + "tspan",
                new XAttribute("x", Number(block.CenterX)),
                new XAttribute("y", Number(baseline)),
                WithoutInvalidCharacters(block.Lines[i])));
        }

        return text;
    }
}
