using System.Drawing;
using System.Text.Json;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Help.Interfaces;
using CincoVertice.MyTools.Ui.Help.Models;

namespace CincoVertice.MyTools.Ui.Help.Services;

/// <summary>
///     The help window's settings in a JSON file, read when it opens and written when it closes. A missing or broken
///     file gives no settings, so the window opens as at first.
/// </summary>
public sealed class JsonFileHelpSettingsStore : IHelpSettingsStore
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public JsonFileHelpSettingsStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public HelpSettingsModel? Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            var file = JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(FilePath), _options);

            if (file is null)
            {
                return null;
            }

            Rectangle? bounds = file.Width > 0 && file.Height > 0
                ? new Rectangle(file.Left, file.Top, file.Width, file.Height)
                : null;

            return new HelpSettingsModel(file.Page, bounds, file.Maximized);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    public void Save(HelpSettingsModel settings)
    {
        Rectangle bounds = settings.Bounds ?? Rectangle.Empty;
        var file = new SettingsFile(
            settings.Page,
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height,
            settings.IsMaximized);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            AtomicFile.WriteAllText(FilePath, JsonSerializer.Serialize(file, _options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The window opens as at first next time, rather than not closing
        }
    }

    private sealed record SettingsFile(
        string? Page,
        int Left,
        int Top,
        int Width,
        int Height,
        bool Maximized);
}
