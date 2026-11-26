using System.Text.Json;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.JsonEditor.Interfaces;
using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>
///     The JSON editor's settings in a JSON file, read when a window opens and written when the user changes one. A
///     missing or broken file gives no settings, and one that can't be written keeps the choice for this window only.
/// </summary>
public sealed class JsonFileJsonEditorSettingsStore : IJsonEditorSettingsStore
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public JsonFileJsonEditorSettingsStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public JsonEditorSettingsModel? Load()
    {
        try
        {
            return File.Exists(FilePath)
                && JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(FilePath), _options) is SettingsFile file
                ? new JsonEditorSettingsModel(file.Dark, file.Tree)
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    public void Save(JsonEditorSettingsModel settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var file = new SettingsFile(settings.IsDark, settings.IsTreeShown);
            AtomicFile.WriteAllText(FilePath, JsonSerializer.Serialize(file, _options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Kept for this window only
        }
    }

    private sealed record SettingsFile(bool? Dark = null, bool Tree = false);
}
