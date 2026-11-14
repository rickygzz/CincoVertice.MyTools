using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     The image catalog in a folder: config\images next to the exe. Inserted images go in at its top, and the user may
///     add folders of their own, which are its groups. Which were used last is kept in recent.json there. Each file is
///     read and prepared once, then again only when it changes.
/// </summary>
public sealed partial class FolderDiagramImageCatalog : IDiagramImageCatalog
{
    /// <summary>The group of the images at the top of the folder: those inserted.</summary>
    public const string InsertedGroup = "Inserted";

    /// <summary>How many it remembers as used last.</summary>
    public const int MaxRecent = 50;

    private const string RecentFileName = "recent.json";

    private readonly Dictionary<string, CachedFile> _files = new(StringComparer.OrdinalIgnoreCase);

    public FolderDiagramImageCatalog(string folder)
    {
        Folder = Path.GetFullPath(folder);
    }

    public string Folder { get; }

    public void CreateFolder() => Directory.CreateDirectory(Folder);

    private string RecentFilePath => Path.Combine(Folder, RecentFileName);

    public IReadOnlyList<DiagramCatalogImageModel> Images() =>
    [
        .. Scan()
            .Where(file => file.Image is not null)
            .Select(file => file.Image!)
            .OrderBy(
                image => image.Group == InsertedGroup ? string.Empty : image.Group,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(image => image.Name, StringComparer.OrdinalIgnoreCase),
    ];

    public IReadOnlyList<string> Recent()
    {
        try
        {
            return File.Exists(RecentFilePath)
                ? JsonSerializer.Deserialize<List<string>>(File.ReadAllText(RecentFilePath))?
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .ToList() ?? []
                : [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    public void Add(string markup, string fileName)
    {
        try
        {
            string hash = HashOf(markup);

            // The same image, inserted again or copied in by hand
            if (Scan().FirstOrDefault(file => file.Hash == hash) is { } same)
            {
                MarkUsed(same.Path);

                return;
            }

            Directory.CreateDirectory(Folder);
            string name = FreeNameFor(fileName);

            AtomicFile.WriteAllText(Path.Combine(Folder, name), markup);
            MarkUsed(name);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The catalog is only a help to insert again; the image is in the diagram all the same
        }
    }

    public void MarkUsed(string path)
    {
        List<string> recent =
        [
            path,
            .. Recent().Where(other => !string.Equals(other, path, StringComparison.OrdinalIgnoreCase)),
        ];

        try
        {
            Directory.CreateDirectory(Folder);
            AtomicFile.WriteAllText(RecentFilePath, JsonSerializer.Serialize(recent.Take(MaxRecent)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Not remembered, which only changes the order
        }
    }

    /// <summary>
    ///     A name easier to read and to search than the file's: Azure's "10021-icon-service-Virtual-Machine" is
    ///     "Virtual Machine".
    /// </summary>
    public static string NameOf(string fileName)
    {
        string name = AzurePrefixRegex().Replace(Path.GetFileNameWithoutExtension(fileName), string.Empty);

        return name.Replace('-', ' ').Replace('_', ' ').Trim();
    }

    /// <summary>The SVG files in the folder now, read again only when changed; the others forgotten.</summary>
    private List<CachedFile> Scan()
    {
        if (!Directory.Exists(Folder))
        {
            _files.Clear();

            return [];
        }

        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
        List<CachedFile> found = [];

        foreach (string fullPath in Directory.EnumerateFiles(Folder, "*.svg", options))
        {
            if (Read(new FileInfo(fullPath)) is { } file)
            {
                found.Add(file);
            }
        }

        foreach (string gone in _files.Keys.Except(found.Select(file => file.Path)).ToList())
        {
            _files.Remove(gone);
        }

        return found;
    }

    private CachedFile? Read(FileInfo info)
    {
        string path = Path.GetRelativePath(Folder, info.FullName).Replace('\\', '/');

        try
        {
            if (_files.TryGetValue(path, out CachedFile? cached)
                && cached.Changed == info.LastWriteTimeUtc
                && cached.Length == info.Length)
            {
                return cached;
            }

            if (info.Length > SvgDiagramFileStore.MaxImageBytes)
            {
                return null;
            }

            string text = File.ReadAllText(info.FullName);
            DiagramCatalogImageModel? image = null;

            try
            {
                string folder = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? string.Empty;

                image = new DiagramCatalogImageModel(
                    path,
                    NameOf(info.Name),
                    folder.Length == 0 ? InsertedGroup : folder,
                    DiagramSvgImage.Prepare(text));
            }
            catch (InvalidDataException)
            {
                // Kept without an image, so it isn't read again until it changes
            }

            var file = new CachedFile(path, info.LastWriteTimeUtc, info.Length, HashOf(text), image);
            _files[path] = file;

            return file;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>The file name at the top of the folder, with " (2)" and so on when another file has it.</summary>
    private string FreeNameFor(string fileName)
    {
        string invalid = new(Path.GetInvalidFileNameChars());
        string name = string.Concat(Path.GetFileNameWithoutExtension(fileName).Where(c => !invalid.Contains(c))).Trim();

        if (name.Length == 0)
        {
            name = "image";
        }

        string candidate = name + ".svg";

        for (int number = 2; File.Exists(Path.Combine(Folder, candidate)); number++)
        {
            candidate = $"{name} ({number}).svg";
        }

        return candidate;
    }

    // The same image whatever its line endings, as Git may change them
    private static string HashOf(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ReplaceLineEndings("\n").Trim())));

    [GeneratedRegex(@"^\d+-icon-service-", RegexOptions.IgnoreCase)]
    private static partial Regex AzurePrefixRegex();

    private sealed record CachedFile(
        string Path,
        DateTime Changed,
        long Length,
        string Hash,
        DiagramCatalogImageModel? Image);
}
