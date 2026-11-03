using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     A diagram as JSON: what's kept inside its SVG file to open it again, and what's on the clipboard between copy
///     and paste.
/// </summary>
public static class DiagramJson
{
    private const string FormatName = "mytools-diagram";
    private const int MinFontSize = 6;
    private const int MaxFontSize = 400;
    private const int MaxLineWidth = 100;

    // What every text used before a font could be chosen, so older diagrams keep their look
    private const string OlderFilesFontFamily = "Segoe UI";

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Write(DiagramModel diagram)
    {
        var file = new DiagramFile(
            FormatName,
            1,
            [.. diagram.Shapes.Select(shape => new ShapeFile(
                shape.Id,
                shape.Kind,
                shape.Bounds.X,
                shape.Bounds.Y,
                shape.Bounds.Width,
                shape.Bounds.Height,
                shape.Text,
                StyleFile.From(shape.Style),
                shape.GroupId,
                shape.CornerRadius))],
            [.. diagram.Connectors.Select(connector => new ConnectorFile(
                connector.Id,
                connector.FromShapeId,
                connector.ToShapeId,
                connector.Routing,
                connector.Heads,
                connector.Label,
                StyleFile.From(connector.Style),
                connector.FromSide,
                connector.ToSide,
                connector.Bends?.Select(bend => new PointFile(bend.X, bend.Y)).ToList()))]);

        return JsonSerializer.Serialize(file, _options);
    }

    /// <summary>
    ///     Reads a diagram. Sizes and fonts out of range are brought into range, and a connector without both of its
    ///     shapes is left out.
    /// </summary>
    /// <exception cref="InvalidDataException">The text isn't a diagram.</exception>
    public static DiagramModel Read(string json)
    {
        DiagramFile? file;

        try
        {
            file = JsonSerializer.Deserialize<DiagramFile>(json, _options);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("This isn't a diagram.", ex);
        }

        if (file?.Format != FormatName)
        {
            throw new InvalidDataException("This isn't a diagram.");
        }

        List<DiagramShapeModel> shapes = [.. (file.Shapes ?? []).Select(ToShape)];
        List<int> ids = [.. shapes.Select(shape => shape.Id), .. (file.Connectors ?? []).Select(c => c.Id)];

        if (ids.Count != ids.Distinct().Count())
        {
            throw new InvalidDataException("This diagram has two parts with the same id.");
        }

        HashSet<int> shapeIds = [.. shapes.Select(shape => shape.Id)];
        List<DiagramConnectorModel> connectors =
        [
            .. (file.Connectors ?? [])
                .Where(c => c.FromShapeId != c.ToShapeId
                    && shapeIds.Contains(c.FromShapeId)
                    && shapeIds.Contains(c.ToShapeId))
                .Select(c => new DiagramConnectorModel(
                    c.Id,
                    c.FromShapeId,
                    c.ToShapeId,
                    c.Routing,
                    c.Heads,
                    c.Label ?? string.Empty,
                    ToStyle(c.Style),
                    c.FromSide,
                    c.ToSide,
                    c.Bends is { Count: > 0 } bends ? [.. bends.Select(bend => new Point(bend.X, bend.Y))] : null)),
        ];

        return new DiagramModel(shapes, connectors);
    }

    private static DiagramShapeModel ToShape(ShapeFile shape) => new(
        shape.Id,
        shape.Kind,
        new Rectangle(shape.X, shape.Y, Math.Max(shape.Width, 1), Math.Max(shape.Height, 1)),
        shape.Text ?? string.Empty,
        ToStyle(shape.Style),
        shape.GroupId,
        shape.CornerRadius is int radius ? Math.Max(radius, 0) : null);

    /// <summary>A style read from a file, with values out of range brought into range.</summary>
    internal static DiagramStyleModel ToStyle(StyleFile? style) => new(
        ParseColor(style?.Fill, Color.Empty),
        ParseColor(style?.Line, Color.Black),
        Math.Clamp(style?.LineWidth ?? 2, 0, MaxLineWidth),
        style?.Dashed ?? false,
        ParseColor(style?.TextColor, Color.Black),
        Math.Clamp(style?.FontSize ?? 14, MinFontSize, MaxFontSize),
        string.IsNullOrWhiteSpace(style?.FontFamily) ? OlderFilesFontFamily : style.FontFamily);

    private static string? FormatColor(Color color) =>
        color.IsEmpty ? null : $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static Color ParseColor(string? text, Color fallback)
    {
        if (text is not { Length: 7 } || text[0] != '#'
            || !int.TryParse(text.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int rgb))
        {
            return fallback;
        }

        return Color.FromArgb(255, Color.FromArgb(rgb));
    }

    private sealed record DiagramFile(
        string? Format,
        int Version,
        List<ShapeFile>? Shapes,
        List<ConnectorFile>? Connectors);

    private sealed record ShapeFile(
        int Id,
        DiagramShapeKind Kind,
        int X,
        int Y,
        int Width,
        int Height,
        string? Text,
        StyleFile? Style,
        int? GroupId,
        int? CornerRadius);

    private sealed record ConnectorFile(
        int Id,
        int FromShapeId,
        int ToShapeId,
        DiagramRouting Routing,
        DiagramArrowHeads Heads,
        string? Label,
        StyleFile? Style,
        DiagramSide? FromSide,
        DiagramSide? ToSide,
        List<PointFile>? Bends);

    private sealed record PointFile(int X, int Y);

    /// <summary>A style as it's written in a file.</summary>
    internal sealed record StyleFile(
        string? Fill,
        string? Line,
        int LineWidth,
        bool Dashed,
        string? TextColor,
        int FontSize,
        string? FontFamily)
    {
        public static StyleFile From(DiagramStyleModel style) => new(
            FormatColor(style.Fill),
            FormatColor(style.Line),
            style.LineWidth,
            style.Dashed,
            FormatColor(style.TextColor),
            style.FontSize,
            style.FontFamily);
    }
}
