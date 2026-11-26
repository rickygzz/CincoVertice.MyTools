using System.Drawing;
using System.Text.Json;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;

namespace CincoVertice.MyTools.Ui.Snipping.Services;

/// <summary>
///     The area snipped last in a JSON file, { "lastArea": { "x": 100, "y": 80, "width": 640, "height": 360 } },
///     in physical screen pixels. Its own file, not the snip editor's: an open editor saves its settings from what it
///     read, and would put back an older area. A missing or broken file, or an area without a size, gives none.
/// </summary>
public sealed class JsonFileSnipAreaStore : ISnipAreaStore
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public JsonFileSnipAreaStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public Rectangle? Load()
    {
        try
        {
            if (!File.Exists(FilePath)
                || JsonSerializer.Deserialize<AreaFile>(File.ReadAllText(FilePath), _options)?.LastArea is not { } area
                || area.Width <= 0
                || area.Height <= 0)
            {
                return null;
            }

            return new Rectangle(area.X, area.Y, area.Width, area.Height);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    public void Save(Rectangle area)
    {
        var file = new AreaFile(new RectangleFile(area.X, area.Y, area.Width, area.Height));

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            AtomicFile.WriteAllText(FilePath, JsonSerializer.Serialize(file, _options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Losing the area is better than losing the snip
        }
    }

    private sealed record AreaFile(RectangleFile? LastArea);

    private sealed record RectangleFile(int X, int Y, int Width, int Height);
}
