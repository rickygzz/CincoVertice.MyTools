using System.Text.Json;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     Keeps the copies in a folder, one file per window (config\recovery\&lt;id&gt;.json), each with the diagram as
///     its file has it. It knows which ids this run of the app made or took, so a window's copy is never offered while
///     the window is open.
/// </summary>
public sealed class FolderDiagramRecoveryStore : IDiagramRecoveryStore
{
    private const string Extension = ".json";

    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    private readonly string _folder;
    private readonly HashSet<string> _ours = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    public FolderDiagramRecoveryStore(string folder)
    {
        _folder = folder;
    }

    public string NewId()
    {
        string id = Guid.NewGuid().ToString("N");

        lock (_lock)
        {
            _ours.Add(id);
        }

        return id;
    }

    public void Adopt(string id)
    {
        lock (_lock)
        {
            _ours.Add(id);
        }
    }

    public void Save(DiagramRecoveryModel recovery)
    {
        Directory.CreateDirectory(_folder);

        var file = new RecoveryFile(
            recovery.Path,
            recovery.Version,
            recovery.SavedAt,
            DiagramJson.Write(recovery.Diagram));

        AtomicFile.WriteAllText(PathOf(recovery.Id), JsonSerializer.Serialize(file, _options));
    }

    public void Delete(string id)
    {
        try
        {
            File.Delete(PathOf(id));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Offered again the next time; nothing is lost
        }
    }

    public IReadOnlyList<DiagramRecoveryModel> Left()
    {
        if (!Directory.Exists(_folder))
        {
            return [];
        }

        List<DiagramRecoveryModel> left = [];

        foreach (string path in Directory.EnumerateFiles(_folder, "*" + Extension))
        {
            string id = Path.GetFileNameWithoutExtension(path);

            lock (_lock)
            {
                if (_ours.Contains(id))
                {
                    continue;
                }
            }

            if (Read(id, path) is DiagramRecoveryModel recovery)
            {
                left.Add(recovery);
            }
        }

        return [.. left.OrderBy(recovery => recovery.SavedAt)];
    }

    private static DiagramRecoveryModel? Read(string id, string path)
    {
        try
        {
            RecoveryFile? file = JsonSerializer.Deserialize<RecoveryFile>(File.ReadAllText(path), _options);

            return file?.Diagram is string diagram
                ? new DiagramRecoveryModel(
                    id,
                    file.Path,
                    file.Version,
                    DiagramJson.Read(diagram),
                    DateTime.SpecifyKind(file.SavedAt, DateTimeKind.Utc))
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException
            or InvalidDataException)
        {
            return null;
        }
    }

    // Only ids this store made, or found in its folder, so never a path elsewhere
    private string PathOf(string id) => Path.Combine(_folder, Path.GetFileName(id) + Extension);

    private sealed record RecoveryFile(string? Path, string? Version, DateTime SavedAt, string? Diagram);
}
