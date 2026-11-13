using System.Text.Json;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.CodeReview.Interfaces;

namespace CincoVertice.MyTools.Ui.CodeReview.Services;

/// <summary>
///     The fixers turned off, in a JSON file, written on every change. A missing or broken file turns none off.
/// </summary>
public sealed class JsonFileFixerSelectionStore : IFixerSelectionStore
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public JsonFileFixerSelectionStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public IReadOnlyCollection<string> LoadOff()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return [];
            }

            var file = JsonSerializer.Deserialize<SelectionFile>(File.ReadAllText(FilePath), _options);

            return [.. file?.Off ?? []];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    public void SaveOff(IReadOnlyCollection<string> names)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            AtomicFile.WriteAllText(
                FilePath,
                JsonSerializer.Serialize(new SelectionFile([.. names.Order(StringComparer.Ordinal)]), _options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The choice still holds until the app ends; it's just not kept for the next time
        }
    }

    private sealed record SelectionFile(string[]? Off);
}
