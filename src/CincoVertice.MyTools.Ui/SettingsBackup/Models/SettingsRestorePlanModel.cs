namespace CincoVertice.MyTools.Ui.SettingsBackup.Models;

/// <summary>What restoring a backup would do, file by file, before anything is changed.</summary>
/// <param name="BackupPath">The backup's zip file.</param>
/// <param name="Files">Every file of the backup and of the settings, by path.</param>
public sealed record SettingsRestorePlanModel(string BackupPath, IReadOnlyList<SettingsFileModel> Files)
{
    /// <summary>The files the restore writes: added or changed.</summary>
    public IReadOnlyList<SettingsFileModel> ToWrite =>
        [.. Files.Where(file => file.Change is SettingsChangeKind.Added or SettingsChangeKind.Changed)];

    /// <summary>The files only the settings have, kept unless the user asks to delete them.</summary>
    public IReadOnlyList<SettingsFileModel> NotInBackup =>
        [.. Files.Where(file => file.Change == SettingsChangeKind.NotInBackup)];
}
