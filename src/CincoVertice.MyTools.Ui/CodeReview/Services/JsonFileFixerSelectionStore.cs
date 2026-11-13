using System.Text.Json;
using System.Text.Json.Serialization;
using CincoVertice.Common.Application.CodeStandard.Models;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.CodeReview.Interfaces;
using CincoVertice.MyTools.Ui.CodeReview.Models;

namespace CincoVertice.MyTools.Ui.CodeReview.Services;

/// <summary>
///     The choices for Fix folder in a JSON file, written on every change. A missing or broken file gives the
///     defaults: every fixer on, line endings and encodings kept.
/// </summary>
public sealed class JsonFileFixerSelectionStore : IFixerSelectionStore
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public JsonFileFixerSelectionStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public FixerSettingsModel Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return FixerSettingsModel.Default;
            }

            var file = JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(FilePath), _options);

            return file is null
                ? FixerSettingsModel.Default
                : new FixerSettingsModel([.. file.Off ?? []], file.LineEnding, file.Encoding);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return FixerSettingsModel.Default;
        }
    }

    public void Save(FixerSettingsModel settings)
    {
        var file = new SettingsFile(
            [.. settings.Off.Order(StringComparer.Ordinal)],
            settings.LineEnding,
            settings.Encoding);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            AtomicFile.WriteAllText(FilePath, JsonSerializer.Serialize(file, _options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The choice still holds until the app ends; it's just not kept for the next time
        }
    }

    private sealed record SettingsFile(
        string[]? Off,
        LineEndingEnum LineEnding = LineEndingEnum.None,
        FileEncodingEnum Encoding = FileEncodingEnum.Keep);
}
