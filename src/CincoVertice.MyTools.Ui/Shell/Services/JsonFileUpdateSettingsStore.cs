using System.Text.Json;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Shell.Interfaces;

namespace CincoVertice.MyTools.Ui.Shell.Services;

/// <summary>
///     Whether to look for a newer version at start, in a JSON file: { "checkAtStart": true }. A missing or broken
///     file means no, since looking goes on the network.
/// </summary>
public sealed class JsonFileUpdateSettingsStore : IUpdateSettingsStore
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public JsonFileUpdateSettingsStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public bool LoadCheckAtStart()
    {
        try
        {
            return File.Exists(FilePath)
                && JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(FilePath), _options)?.CheckAtStart == true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    public void SaveCheckAtStart(bool checkAtStart)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            AtomicFile.WriteAllText(FilePath, JsonSerializer.Serialize(new SettingsFile(checkAtStart), _options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Asking again next time is better than stopping the app
        }
    }

    private sealed record SettingsFile(bool? CheckAtStart);
}
