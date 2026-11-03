using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     The diagram editor's settings in a JSON file, read when an editor opens and written on every change. A missing
///     or broken file gives no settings, so the editor starts with its defaults.
/// </summary>
public sealed class JsonFileDiagramEditorSettingsStore : IDiagramEditorSettingsStore
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public JsonFileDiagramEditorSettingsStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public DiagramEditorSettingsModel? Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            var file = JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(FilePath), _options);

            if (file?.Style is null)
            {
                return null;
            }

            return new DiagramEditorSettingsModel(
                DiagramJson.ToStyle(file.Style),
                file.Routing,
                file.Heads,
                file.PropertiesPanel,
                file.Window is { } window
                    ? new DiagramWindowSizeModel(new Size(window.Width, window.Height), window.Maximized)
                    : null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    public void Save(DiagramEditorSettingsModel settings)
    {
        DiagramWindowSizeModel? window = settings.Window;
        var file = new SettingsFile(
            DiagramJson.StyleFile.From(settings.Style),
            settings.Routing,
            settings.Heads,
            settings.IsPropertiesPanelVisible,
            window is null ? null : new WindowFile(window.Size.Width, window.Size.Height, window.IsMaximized));

        try
        {
            // A new file first, then it replaces the old one, so a failure never leaves half a file
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            string tempPath = FilePath + ".tmp";

            File.WriteAllText(tempPath, JsonSerializer.Serialize(file, _options));
            File.Move(tempPath, FilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Losing the settings is better than stopping the editor, e.g. when another editor is saving them too
        }
    }

    private sealed record SettingsFile(
        DiagramJson.StyleFile? Style,
        DiagramRouting Routing,
        DiagramArrowHeads Heads,
        bool PropertiesPanel = true,
        WindowFile? Window = null);

    private sealed record WindowFile(int Width, int Height, bool Maximized);
}
