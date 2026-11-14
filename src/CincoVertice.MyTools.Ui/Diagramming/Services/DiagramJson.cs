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

    // A title fill of none, a see-through band
    private const string NoColor = "none";

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
                shape.CornerRadius,
                shape.Locks == DiagramLock.None ? null : shape.Locks,
                shape.Image,
                shape.BottomCornerRadius))],
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
                connector.Bends?.Select(bend => new PointFile(bend.X, bend.Y)).ToList(),
                connector.FromPoint is Point from ? new PointFile(from.X, from.Y) : null,
                connector.ToPoint is Point to ? new PointFile(to.X, to.Y) : null,
                connector.Direction == DiagramLineDirection.Free ? null : connector.Direction))]);

        return JsonSerializer.Serialize(file, _options);
    }

    /// <summary>
    ///     Reads a diagram. Sizes and fonts out of range are brought into range; a connector without both of its
    ///     shapes, and a line without both of its points or of no length, are left out.
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

        if (file?.Format != FormatName || !HasOnlyKnownEntries(file))
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
        List<DiagramConnectorModel> connectors = [];

        foreach (ConnectorFile c in file.Connectors ?? [])
        {
            if (c is { FromShapeId: null, ToShapeId: null, FromPoint: { } from, ToPoint: { } to })
            {
                // A line of no length has no direction for its heads
                if (from != to)
                {
                    DiagramConnectorModel line = DiagramConnectorModel.Line(
                        c.Id,
                        new Point(from.X, from.Y),
                        new Point(to.X, to.Y),
                        c.Heads,
                        ToStyle(c.Style)) with { Label = c.Label ?? string.Empty, Direction = DirectionOf(c) };

                    connectors.Add(line.Aligned());
                }
            }
            else if (c is { FromShapeId: int fromId, ToShapeId: int toId }
                && fromId != toId
                && shapeIds.Contains(fromId)
                && shapeIds.Contains(toId))
            {
                connectors.Add(new DiagramConnectorModel(
                    c.Id,
                    fromId,
                    toId,
                    c.Routing,
                    c.Heads,
                    c.Label ?? string.Empty,
                    ToStyle(c.Style),
                    c.FromSide,
                    c.ToSide,
                    c.Bends is { Count: > 0 } bends ? [.. bends.Select(bend => new Point(bend.X, bend.Y))] : null));
            }
        }

        return new DiagramModel(shapes, connectors);
    }

    /// <summary>
    ///     JSON can have null in a list, and a number for a value no kind has, which the code after this doesn't expect:
    ///     a form's list of arrows, for one, throws on an index it doesn't have.
    /// </summary>
    private static bool HasOnlyKnownEntries(DiagramFile file) =>
        (file.Shapes ?? []).All(shape => shape is not null && Enum.IsDefined(shape.Kind))
        && (file.Connectors ?? []).All(connector => connector is not null
            && Enum.IsDefined(connector.Routing)
            && Enum.IsDefined(connector.Heads)
            && (connector.FromSide is not DiagramSide from || Enum.IsDefined(from))
            && (connector.ToSide is not DiagramSide to || Enum.IsDefined(to))
            && (connector.Bends ?? []).All(bend => bend is not null));

    /// <summary>A value no direction has, from a file written by hand, leaves the line free.</summary>
    private static DiagramLineDirection DirectionOf(ConnectorFile connector) =>
        connector.Direction is DiagramLineDirection direction && Enum.IsDefined(direction)
            ? direction
            : DiagramLineDirection.Free;

    private static DiagramShapeModel ToShape(ShapeFile shape) => new(
        shape.Id,
        shape.Kind,
        new Rectangle(shape.X, shape.Y, Math.Max(shape.Width, 1), Math.Max(shape.Height, 1)),
        shape.Text ?? string.Empty,
        ToStyle(shape.Style),
        shape.GroupId,
        shape.CornerRadius is int radius ? Math.Max(radius, 0) : null,

        // Values no lock has, from a file written by hand, are left out
        (shape.Locks ?? DiagramLock.None) & DiagramLock.All,
        shape.Kind == DiagramShapeKind.Image ? shape.Image ?? string.Empty : null,
        shape.BottomCornerRadius is int bottom ? Math.Max(bottom, 0) : null);

    /// <summary>A style read from a file, with values out of range brought into range.</summary>
    internal static DiagramStyleModel ToStyle(StyleFile? style) => new(
        ParseColor(style?.Fill, Color.Empty),
        ParseColor(style?.Line, Color.Black),
        Math.Clamp(style?.LineWidth ?? 2, 0, MaxLineWidth),
        style?.Dashed ?? false,
        ParseColor(style?.TextColor, Color.Black),
        Math.Clamp(style?.FontSize ?? 14, MinFontSize, MaxFontSize),
        string.IsNullOrWhiteSpace(style?.FontFamily) ? OlderFilesFontFamily : style.FontFamily,
        Math.Clamp(style?.TextOutlineWidth ?? 0, 0, DiagramStyleModel.MaxTextOutlineWidth),
        ParseColor(style?.TextOutlineColor, DiagramStyleModel.DefaultTextOutlineColor),

        // Centered in a file from before it could be chosen, or with a value no alignment has
        style?.TextAlignment is DiagramTextAlignment alignment && Enum.IsDefined(alignment)
            ? alignment
            : DiagramTextAlignment.Center,
        ParseTitleFill(style?.TitleFill));

    /// <summary>None, a see-through title band, is "none": a color has no way to say so.</summary>
    private static string? FormatTitleFill(Color color) =>
        color is { IsEmpty: false, A: 0 } ? NoColor : FormatColor(color);

    private static Color ParseTitleFill(string? text) =>
        string.Equals(text, NoColor, StringComparison.OrdinalIgnoreCase)
            ? DiagramStyleModel.NoTitleFill
            : ParseColor(text, Color.Empty);

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
        int? CornerRadius,
        DiagramLock? Locks = null,
        string? Image = null,
        int? BottomCornerRadius = null);

    private sealed record ConnectorFile(
        int Id,
        int? FromShapeId,
        int? ToShapeId,
        DiagramRouting Routing,
        DiagramArrowHeads Heads,
        string? Label,
        StyleFile? Style,
        DiagramSide? FromSide,
        DiagramSide? ToSide,
        List<PointFile>? Bends,
        PointFile? FromPoint = null,
        PointFile? ToPoint = null,
        DiagramLineDirection? Direction = null);

    private sealed record PointFile(int X, int Y);

    /// <summary>A style as it's written in a file.</summary>
    internal sealed record StyleFile(
        string? Fill,
        string? Line,
        int LineWidth,
        bool Dashed,
        string? TextColor,
        int FontSize,
        string? FontFamily,
        int TextOutlineWidth = 0,
        string? TextOutlineColor = null,
        DiagramTextAlignment? TextAlignment = null,
        string? TitleFill = null)
    {
        public static StyleFile From(DiagramStyleModel style) => new(
            FormatColor(style.Fill),
            FormatColor(style.Line),
            style.LineWidth,
            style.Dashed,
            FormatColor(style.TextColor),
            style.FontSize,
            style.FontFamily,
            style.TextOutlineWidth,
            FormatColor(style.TextOutlineColor),

            // Written only when not centered, so older diagrams' files are as they were
            style.TextAlignment == DiagramTextAlignment.Center ? null : style.TextAlignment,
            FormatTitleFill(style.TitleFill));
    }
}
