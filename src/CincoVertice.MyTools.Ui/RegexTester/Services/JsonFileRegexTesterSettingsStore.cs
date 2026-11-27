using System.Text.Json;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.RegexTester.Interfaces;
using CincoVertice.MyTools.Ui.RegexTester.Models;

namespace CincoVertice.MyTools.Ui.RegexTester.Services;

/// <summary>
///     What the regex tester had last in a JSON file: the pattern, its options, the text and the replacement. A missing
///     or broken file gives nothing, so the window starts empty.
/// </summary>
public sealed class JsonFileRegexTesterSettingsStore : IRegexTesterSettingsStore
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public JsonFileRegexTesterSettingsStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public RegexInputModel? Load()
    {
        try
        {
            return File.Exists(FilePath)
                && JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(FilePath), _options) is SettingsFile file
                ? new RegexInputModel(
                    file.Pattern ?? string.Empty,
                    file.Text ?? string.Empty,
                    file.Replacement ?? string.Empty,
                    file.IgnoreCase,
                    file.Multiline,
                    file.DotAll)
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    public void Save(RegexInputModel input)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var file = new SettingsFile(
                input.Pattern,
                input.Text,
                input.Replacement,
                input.IgnoreCase,
                input.Multiline,
                input.DotAll);
            AtomicFile.WriteAllText(FilePath, JsonSerializer.Serialize(file, _options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Kept in the window only
        }
    }

    private sealed record SettingsFile(
        string? Pattern,
        string? Text,
        string? Replacement,
        bool IgnoreCase = false,
        bool Multiline = false,
        bool DotAll = false);
}
