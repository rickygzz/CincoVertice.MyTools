using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Snipping.Services;

/// <summary>
///     The snip editor's settings in a JSON file, read when an editor opens and written on every change:
///     { "tool": "Arrow", "tools": { "Pen": { "size": 4, "transparency": 0, "color": "#FF0000" }, ... },
///     "customColors": ["#123456", ...], "arrowHeads": "Both", "textOutline": { "width": 2, "color": "#FFFFFF" } }.
///     A missing or broken file gives no settings, so the editor starts with its defaults; a missing or broken
///     color gives <see cref="Color.Empty"/>, so that tool keeps its default color. A broken custom color is skipped.
/// </summary>
public sealed class JsonFileSnipEditorSettingsStore : ISnipEditorSettingsStore
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public JsonFileSnipEditorSettingsStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public SnipEditorSettingsModel? Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            var file = JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(FilePath), _options);

            if (file is null || file.Tools?.Values.Any(tool => tool is null) == true)
            {
                return null;
            }

            return new SnipEditorSettingsModel(
                file.Tool,
                (file.Tools ?? []).ToDictionary(
                    tool => tool.Key,
                    tool => new SnipToolSettingsModel(
                        tool.Value.Size,
                        tool.Value.Transparency,
                        ParseColor(tool.Value.Color))),
                [.. (file.CustomColors ?? []).Select(ParseColor).Where(color => !color.IsEmpty)],
                file.ArrowHeads,
                file.TextOutline is { } outline
                    ? new SnipTextOutlineModel(outline.Width, ParseColor(outline.Color))
                    : null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    public void Save(SnipEditorSettingsModel settings)
    {
        var file = new SettingsFile(
            settings.Tool,
            settings.Tools.ToDictionary(
                tool => tool.Key,
                tool => new ToolFile(tool.Value.Size, tool.Value.Transparency, FormatColor(tool.Value.Color))),
            [.. settings.CustomColors.Select(FormatColor).OfType<string>()],
            settings.ArrowHeads,
            settings.TextOutline is { } outline
                ? new TextOutlineFile(outline.Width, FormatColor(outline.Color))
                : null);

        try
        {
            // A new file first, then it replaces the old one, so a failure never leaves half a file
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            AtomicFile.WriteAllText(FilePath, JsonSerializer.Serialize(file, _options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Losing the settings is better than stopping the editor, e.g. when another editor is saving them too
        }
    }

    /// <summary>Writes "#RRGGBB"; an empty color as null, so it's left out.</summary>
    private static string? FormatColor(Color color) =>
        color.IsEmpty ? null : $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>Reads "#RRGGBB" as a solid color; anything else is <see cref="Color.Empty"/>.</summary>
    private static Color ParseColor(string? text)
    {
        if (text is not { Length: 7 } || text[0] != '#'
            || !int.TryParse(text.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int rgb))
        {
            return Color.Empty;
        }

        return Color.FromArgb(255, Color.FromArgb(rgb));
    }

    private sealed record SettingsFile(
        SnipTool Tool,
        Dictionary<SnipTool, ToolFile>? Tools,
        List<string?>? CustomColors,
        SnipArrowHeads ArrowHeads = SnipArrowHeads.End,
        TextOutlineFile? TextOutline = null);

    private sealed record ToolFile(
        int Size,
        int Transparency,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Color);

    private sealed record TextOutlineFile(int Width, string? Color);
}
