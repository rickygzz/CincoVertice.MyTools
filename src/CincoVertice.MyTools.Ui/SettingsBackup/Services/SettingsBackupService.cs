using System.Globalization;
using System.IO.Compression;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Diff.Services;
using CincoVertice.MyTools.Ui.SettingsBackup.Models;

namespace CincoVertice.MyTools.Ui.SettingsBackup.Services;

/// <summary>
///     Backs up the config folder to a zip, and restores one: first a plan of what would change, file by file, then
///     each file written in place (as the app writes all its files), the settings as they were saved to a zip of their
///     own before. A zip is read only when every entry is a file inside the folder (no .., drive or stream), each path
///     once, at most <see cref="MaxFiles"/> files and <see cref="MaxTotalBytes"/> unzipped, counted as they unzip.
/// </summary>
public sealed class SettingsBackupService
{
    public const int MaxFiles = 10_000;
    public const long MaxTotalBytes = 256L * 1024 * 1024;

    /// <summary>How many copies of the settings before a restore are kept, the newest.</summary>
    public const int SafetyCopiesKept = 10;

    public const string SafetyCopyPrefix = "settings-before-restore-";

    private readonly TimeProvider _clock;

    /// <param name="configFolder">The settings: the app's config folder.</param>
    /// <param name="safetyFolder">Where the settings are saved before a restore; not inside the config folder.</param>
    /// <param name="clock">Names the copies by the time; the computer's when null.</param>
    public SettingsBackupService(string configFolder, string safetyFolder, TimeProvider? clock = null)
    {
        ConfigFolder = Path.GetFullPath(configFolder);
        SafetyFolder = Path.GetFullPath(safetyFolder);
        _clock = clock ?? TimeProvider.System;
    }

    public string ConfigFolder { get; }

    public string SafetyFolder { get; }

    /// <summary>Writes the settings' files to a zip, all at once: a failure leaves no half-written zip.</summary>
    public void Export(string zipPath)
    {
        string target = Path.GetFullPath(zipPath);

        // Not the zip itself, when it's saved in the config folder
        List<string> files = [.. CurrentFiles().Where(file => !SamePath(Path.Combine(ConfigFolder, file), target))];
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        AtomicFile.Write(target, temporary =>
        {
            using ZipArchive archive = ZipFile.Open(temporary, ZipArchiveMode.Create);

            foreach (string file in files)
            {
                string entryName = file.Replace('\\', '/');
                archive.CreateEntryFromFile(Path.Combine(ConfigFolder, file), entryName, CompressionLevel.Optimal);
            }
        });
    }

    /// <summary>What restoring the backup would do, without changing anything.</summary>
    /// <exception cref="InvalidDataException">It isn't a zip, or not a safe one.</exception>
    public SettingsRestorePlanModel Plan(string zipPath)
    {
        Dictionary<string, byte[]> backup = ReadBackup(zipPath);
        HashSet<string> current = new(CurrentFiles(), StringComparer.OrdinalIgnoreCase);
        List<SettingsFileModel> files = [];

        foreach ((string path, byte[] bytes) in backup)
        {
            string currentPath = Path.Combine(ConfigFolder, path);

            if (!current.Contains(path))
            {
                files.Add(new SettingsFileModel(path, SettingsChangeKind.Added, bytes, string.Empty, TextOf(bytes)));

                continue;
            }

            byte[] now = File.ReadAllBytes(currentPath);
            SettingsChangeKind change = now.AsSpan().SequenceEqual(bytes)
                ? SettingsChangeKind.Same
                : SettingsChangeKind.Changed;
            files.Add(new SettingsFileModel(path, change, bytes, TextOf(now), TextOf(bytes)));
        }

        files.AddRange(current
            .Where(path => !backup.ContainsKey(path))
            .Select(path => new SettingsFileModel(path, SettingsChangeKind.NotInBackup, null, null, null)));

        return new SettingsRestorePlanModel(
            Path.GetFullPath(zipPath),
            [.. files.OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)]);
    }

    /// <summary>
    ///     Saves the settings as they are to a zip in the safety folder, keeping the newest
    ///     <see cref="SafetyCopiesKept"/>.
    /// </summary>
    /// <returns>The zip's path; null when there are no settings to save.</returns>
    public string? SaveSafetyCopy()
    {
        if (!CurrentFiles().Any())
        {
            return null;
        }

        string stamp = _clock.GetLocalNow().ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture);
        string path = Path.Combine(SafetyFolder, $"{SafetyCopyPrefix}{stamp}.zip");

        for (int i = 2; File.Exists(path); i++)
        {
            path = Path.Combine(SafetyFolder, $"{SafetyCopyPrefix}{stamp}-{i}.zip");
        }

        Export(path);

        foreach (FileInfo old in new DirectoryInfo(SafetyFolder)
            .GetFiles($"{SafetyCopyPrefix}*.zip")
            .OrderByDescending(file => file.Name, StringComparer.Ordinal)
            .Skip(SafetyCopiesKept))
        {
            old.Delete();
        }

        return path;
    }

    /// <summary>
    ///     Writes the backup's files that are added or changed, each in place and whole; deletes the files the backup
    ///     doesn't have only when asked. Files that are the same aren't touched.
    /// </summary>
    /// <returns>How many files were written, and how many deleted.</returns>
    public (int Written, int Deleted) Apply(SettingsRestorePlanModel plan, bool deleteFilesNotInBackup)
    {
        int written = 0;
        int deleted = 0;

        foreach (SettingsFileModel file in plan.ToWrite)
        {
            string path = Path.Combine(ConfigFolder, file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            AtomicFile.Write(path, temporary => File.WriteAllBytes(temporary, file.BackupBytes!));
            written++;
        }

        if (deleteFilesNotInBackup)
        {
            foreach (SettingsFileModel file in plan.NotInBackup)
            {
                File.Delete(Path.Combine(ConfigFolder, file.Path));
                deleted++;
            }
        }

        return (written, deleted);
    }

    // Each file of the backup, by its path in the config folder; refused whole when anything is unsafe
    private static Dictionary<string, byte[]> ReadBackup(string zipPath)
    {
        ZipArchive archive;

        try
        {
            archive = ZipFile.OpenRead(zipPath);
        }
        catch (InvalidDataException)
        {
            throw new InvalidDataException("That isn't a zip file.");
        }

        using (archive)
        {
            List<ZipArchiveEntry> entries = [.. archive.Entries.Where(entry => !entry.FullName.EndsWith('/'))];

            if (entries.Count > MaxFiles)
            {
                throw new InvalidDataException($"The backup has more than {MaxFiles:N0} files.");
            }

            Dictionary<string, byte[]> files = new(StringComparer.OrdinalIgnoreCase);
            long total = 0;

            foreach (ZipArchiveEntry entry in entries)
            {
                string path = SafePath(entry.FullName);

                if (files.ContainsKey(path))
                {
                    throw new InvalidDataException($"The backup has {path} more than once.");
                }

                byte[] bytes = Unzip(entry, MaxTotalBytes - total);
                total += bytes.Length;
                files[path] = bytes;
            }

            return files;
        }
    }

    // Counted as it unzips, not by the size the zip claims, which can lie
    private static byte[] Unzip(ZipArchiveEntry entry, long room)
    {
        using Stream source = entry.Open();
        using var target = new MemoryStream();
        byte[] buffer = new byte[81920];
        int read;

        while ((read = source.Read(buffer)) > 0)
        {
            if (target.Length + read > room)
            {
                throw new InvalidDataException($"The backup unzips to more than {MaxTotalBytes / (1024 * 1024)} MB.");
            }

            target.Write(buffer, 0, read);
        }

        return target.ToArray();
    }

    // A path inside the config folder: no root, drive, .., . or empty part, nor a : (a file's hidden stream)
    private static string SafePath(string entryName)
    {
        string[] parts = entryName.Replace('\\', '/').Split('/');

        if (Path.IsPathRooted(entryName) || parts.Any(part => part is "" or "." or ".." || part.Contains(':')))
        {
            throw new InvalidDataException($"The backup has a path that leads outside the settings: {entryName}");
        }

        return Path.Combine(parts);
    }

    // Text when it reads as text that can be compared, not too large nor with zeros, as a file of pictures has
    private static string? TextOf(byte[] bytes)
    {
        if (bytes.Length > DiskDiffFileReader.MaxBytes)
        {
            return null;
        }

        string text = DiskDiffFileReader.Decode(bytes).Text;

        return text.Contains('\0') ? null : text;
    }

    private IEnumerable<string> CurrentFiles() => Directory.Exists(ConfigFolder)
        ? Directory.EnumerateFiles(ConfigFolder, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(ConfigFolder, path))
        : [];

    private static bool SamePath(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
}
