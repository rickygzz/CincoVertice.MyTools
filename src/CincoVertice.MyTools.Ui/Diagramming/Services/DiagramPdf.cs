using System.Drawing;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     Turns a diagram into a one-page PDF, drawn as the SVG file draws it: the same figures, as lines and curves that
///     stay sharp at any zoom. The page is as large as the SVG image, a diagram pixel being 1/96 inch. Text goes as the
///     outlines of its letters, so it looks the same on any computer, though it can't be selected or searched.
/// </summary>
public static class DiagramPdf
{
    /// <summary>PDF points (1/72 inch) in a diagram pixel (1/96 inch).</summary>
    public const float PointsPerPixel = 0.75f;

    // Around a connector's label, as on the screen and in the SVG file
    private const float LabelPadding = 2;

    // An image goes as pixels, 4 to a diagram pixel, sharp in print; a large one has fewer, to keep the file small
    private const int ImagePixelsPerPixel = 4;
    private const int MaxImageSide = 2048;

    /// <param name="diagram">What to write.</param>
    /// <param name="textOutliner">Gives the letters' outlines.</param>
    /// <param name="imageRasterizer">Draws the images as pixels; without it, they're left out.</param>
    public static byte[] Write(
        DiagramModel diagram,
        IDiagramTextOutliner textOutliner,
        IDiagramImageRasterizer? imageRasterizer = null)
    {
        List<DiagramRasterModel> images = [];
        Rectangle area = DiagramArea.Of(diagram);
        var content = new StringBuilder();

        // Diagram pixels from the top left of the area, as PDF draws from the bottom left in points
        content.Append(
            $"{Number(PointsPerPixel)} 0 0 {Number(-PointsPerPixel)} {Number(-area.X * PointsPerPixel)} "
            + $"{Number(area.Bottom * PointsPerPixel)} cm\n1 j\n");

        foreach (DiagramShapeModel shape in diagram.Shapes)
        {
            if (shape is { Kind: DiagramShapeKind.Image, Image: string markup }
                && imageRasterizer is not null
                && Rasterize(imageRasterizer, DiagramSvgImage.Filling(markup), shape.Bounds) is { } image)
            {
                images.Add(image);
                WriteImage(content, shape.Bounds, images.Count);
            }

            WriteShape(content, shape, textOutliner);
        }

        foreach (DiagramConnectorModel connector in diagram.Connectors)
        {
            if (DiagramRouter.PathOf(connector, diagram) is { } path)
            {
                WriteConnector(content, connector, path, textOutliner);
            }
        }

        return WriteDocument(
            area.Width * PointsPerPixel,
            area.Height * PointsPerPixel,
            Compress(Encoding.ASCII.GetBytes(content.ToString())),
            images);
    }

    private static DiagramRasterModel? Rasterize(IDiagramImageRasterizer rasterizer, string markup, Rectangle bounds)
    {
        float scale = Math.Min(ImagePixelsPerPixel, (float)MaxImageSide / Math.Max(bounds.Width, bounds.Height));

        return rasterizer.Rasterize(
            markup,
            Math.Max((int)Math.Round(bounds.Width * scale), 1),
            Math.Max((int)Math.Round(bounds.Height * scale), 1));
    }

    /// <summary>The image numbered so, stretched over the bounds; its first row at their top.</summary>
    private static void WriteImage(StringBuilder content, Rectangle bounds, int number) =>
        content.Append(
            $"q {bounds.Width} 0 0 {-bounds.Height} {bounds.X} {bounds.Bottom} cm /Im{number} Do Q\n");

    private static void WriteShape(StringBuilder content, DiagramShapeModel shape, IDiagramTextOutliner textOutliner)
    {
        DiagramStyleModel style = shape.Style;
        bool hasLine = style.LineWidth > 0;

        content.Append("q\n");
        WriteLineStyle(content, style);

        foreach (DiagramFigureModel figure in DiagramFigures.Of(shape))
        {
            // Each figure its own fill: a container's title band has another
            Color fill = DiagramFigures.FillOf(figure, style);

            if (!fill.IsEmpty)
            {
                content.Append($"{Color(fill)} rg\n");
            }

            // Filled, then the line over it, as on the screen
            string? paint = (!fill.IsEmpty, hasLine && figure.Stroked) switch
            {
                (true, true) => "B",
                (true, false) => "f",
                (false, true) => "S",
                _ => null,
            };

            if (paint is not null)
            {
                WritePath(content, figure);
                content.Append(paint).Append('\n');
            }
        }

        content.Append("Q\n");

        if (shape.Text.Length > 0)
        {
            WriteText(content, DiagramTextLayout.OfShape(shape), style, textOutliner);
        }
    }

    private static void WriteConnector(
        StringBuilder content,
        DiagramConnectorModel connector,
        DiagramConnectorPathModel path,
        IDiagramTextOutliner textOutliner)
    {
        DiagramStyleModel style = connector.Style;

        content.Append("q\n");

        if (style.LineWidth > 0)
        {
            WriteLineStyle(content, style);
            WritePath(content, path.Line);
            content.Append("S\n");
        }

        content.Append($"{Color(style.Line)} rg\n");

        foreach (IReadOnlyList<PointF>? head in new[] { path.StartHead, path.EndHead })
        {
            if (head is { Count: > 0 })
            {
                content.Append($"{Point(head[0])} m\n");

                foreach (PointF point in head.Skip(1))
                {
                    content.Append($"{Point(point)} l\n");
                }

                content.Append("h f\n");
            }
        }

        content.Append("Q\n");

        if (connector.Label.Length > 0)
        {
            DiagramTextBlockModel block = DiagramTextLayout.LayoutAt(
                connector.Label,
                path.LabelCenter,
                style.FontSize,
                style.TextAlignment);
            RectangleF back = block.Bounds;

            // So the line doesn't run through the words
            back.Inflate(LabelPadding, 0);
            content.Append(
                $"1 1 1 rg {Number(back.X)} {Number(back.Y)} {Number(back.Width)} {Number(back.Height)} re f\n");
            WriteText(content, block, style, textOutliner);
        }
    }

    private static void WriteText(
        StringBuilder content,
        DiagramTextBlockModel block,
        DiagramStyleModel style,
        IDiagramTextOutliner textOutliner)
    {
        IReadOnlyList<DiagramFigureModel> letters = textOutliner.Outline(block, style);

        if (letters.Count == 0)
        {
            return;
        }

        content.Append("q\n");

        if (style.TextOutlineWidth > 0)
        {
            // Under the letters, twice as wide, since they cover its inner half; as on the screen
            content.Append($"{Color(style.TextOutlineColor)} RG {style.TextOutlineWidth * 2} w [] 0 d\n");
            WriteFigures(content, letters);
            content.Append("S\n");
        }

        content.Append($"{Color(style.TextColor)} rg\n");
        WriteFigures(content, letters);
        content.Append("f\nQ\n");
    }

    private static void WriteLineStyle(StringBuilder content, DiagramStyleModel style)
    {
        content.Append($"{Color(style.Line)} RG {style.LineWidth} w ");

        // In line widths, like the SVG file's
        content.Append(style.Dashed ? $"[{style.LineWidth * 4} {style.LineWidth * 3}] 0 d\n" : "[] 0 d\n");
    }

    private static void WriteFigures(StringBuilder content, IReadOnlyList<DiagramFigureModel> figures)
    {
        foreach (DiagramFigureModel figure in figures)
        {
            WritePath(content, figure);
        }
    }

    private static void WritePath(StringBuilder content, DiagramFigureModel figure)
    {
        foreach (DiagramPathSegmentModel segment in figure.Segments)
        {
            switch (segment.Operation)
            {
                case DiagramPathOperation.Move:
                    content.Append($"{Point(segment.Points[0])} m\n");
                    break;
                case DiagramPathOperation.Line:
                    content.Append($"{Point(segment.Points[0])} l\n");
                    break;
                case DiagramPathOperation.Curve:
                    content.Append(
                        $"{Point(segment.Points[0])} {Point(segment.Points[1])} {Point(segment.Points[2])} c\n");
                    break;
                default:
                    content.Append("h\n");
                    break;
            }
        }
    }

    private static string Point(PointF point) => $"{Number(point.X)} {Number(point.Y)}";

    private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Color(Color color) =>
        $"{Number(color.R / 255f)} {Number(color.G / 255f)} {Number(color.B / 255f)}";

    private static byte[] Compress(byte[] data)
    {
        using var compressed = new MemoryStream();

        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal))
        {
            zlib.Write(data);
        }

        return compressed.ToArray();
    }

    /// <summary>The PDF file around the page's drawing: its objects, and the table of where each one starts.</summary>
    /// <param name="width">The page's, in points.</param>
    /// <param name="height">The page's, in points.</param>
    /// <param name="pageContent">What the page draws, compressed.</param>
    /// <param name="images">The images it draws, as /Im1, /Im2 and so on.</param>
    private static byte[] WriteDocument(
        float width,
        float height,
        byte[] pageContent,
        IReadOnlyList<DiagramRasterModel> images)
    {
        using var file = new MemoryStream();
        var offsets = new List<long>();

        void Write(string text) => file.Write(Encoding.ASCII.GetBytes(text));

        void WriteObject(string body)
        {
            offsets.Add(file.Position);
            Write($"{offsets.Count} 0 obj\n{body}\nendobj\n");
        }

        // The second line's bytes above 127 tell programs the file is binary
        Write("%PDF-1.4\n");
        file.Write([(byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n']);

        WriteObject("<< /Type /Catalog /Pages 2 0 R >>");
        WriteObject("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        void WriteStream(string dictionary, byte[] data)
        {
            offsets.Add(file.Position);
            Write($"{offsets.Count} 0 obj\n<< {dictionary}/Length {data.Length} /Filter /FlateDecode >>\nstream\n");
            file.Write(data);
            Write("\nendstream\nendobj\n");
        }

        // Each image is two objects after the page's content: its colors, then its transparency
        string xObjects = string.Concat(images.Select((_, i) => $" /Im{i + 1} {5 + (i * 2)} 0 R"));

        WriteObject(
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Number(width)} {Number(height)}] "
            + $"/Resources << /XObject <<{xObjects} >> >> /Contents 4 0 R >>");
        WriteStream(string.Empty, pageContent);

        foreach (DiagramRasterModel image in images)
        {
            string size = $"/Width {image.Width} /Height {image.Height} /BitsPerComponent 8";

            WriteStream(
                $"/Type /XObject /Subtype /Image {size} /ColorSpace /DeviceRGB /SMask {offsets.Count + 2} 0 R ",
                Compress(image.Rgb));
            WriteStream($"/Type /XObject /Subtype /Image {size} /ColorSpace /DeviceGray ", Compress(image.Alpha));
        }

        WriteObject("<< /Producer (My Tools) >>");

        long table = file.Position;
        var xref = new StringBuilder($"xref\n0 {offsets.Count + 1}\n0000000000 65535 f\r\n");

        foreach (long offset in offsets)
        {
            // Each entry is 20 bytes, ending in a space and a line feed or in CR LF
            xref.Append($"{offset:D10} 00000 n\r\n");
        }

        xref.Append($"trailer\n<< /Size {offsets.Count + 1} /Root 1 0 R /Info {offsets.Count} 0 R >>\n");
        xref.Append($"startxref\n{table}\n%%EOF\n");
        Write(xref.ToString());

        return file.ToArray();
    }
}
