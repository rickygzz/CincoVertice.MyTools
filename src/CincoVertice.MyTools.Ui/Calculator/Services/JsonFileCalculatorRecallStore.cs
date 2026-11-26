using System.Text.Json;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Calculator.Interfaces;

namespace CincoVertice.MyTools.Ui.Calculator.Services;

/// <summary>
///     The calculator's lines in a JSON file, { "lines": [ ... ] }, the newest last. A missing or broken file gives
///     none, so the calculator starts as at first; one that can't be written is left as it was.
/// </summary>
public sealed class JsonFileCalculatorRecallStore : ICalculatorRecallStore
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public JsonFileCalculatorRecallStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public IReadOnlyList<string> Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return [];
            }

            RecallFile? file = JsonSerializer.Deserialize<RecallFile>(File.ReadAllText(FilePath), _options);

            return [.. (file?.Lines ?? []).Where(line => !string.IsNullOrWhiteSpace(line))];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    public void Save(IReadOnlyList<string> lines)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            AtomicFile.WriteAllText(FilePath, JsonSerializer.Serialize(new RecallFile([.. lines]), _options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Up brings back this run's lines all the same
        }
    }

    private sealed record RecallFile(List<string>? Lines);
}
