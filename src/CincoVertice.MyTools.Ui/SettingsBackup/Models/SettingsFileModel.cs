namespace CincoVertice.MyTools.Ui.SettingsBackup.Models;

/// <summary>A file of the settings, as a restore would treat it.</summary>
/// <param name="Path">Its path in the config folder: hotkeys.cfg, scripts\Hello.mts.</param>
/// <param name="Change">What the restore would do to it.</param>
/// <param name="BackupBytes">Its contents in the backup; null for one only in the settings.</param>
/// <param name="CurrentText">Its text now, when it's text that can be compared; null otherwise.</param>
/// <param name="BackupText">Its text in the backup, likewise.</param>
public sealed record SettingsFileModel(
    string Path,
    SettingsChangeKind Change,
    byte[]? BackupBytes,
    string? CurrentText,
    string? BackupText)
{
    /// <summary>Whether its two versions can be shown side by side: a text file that the restore changes.</summary>
    public bool CanCompare =>
        Change is SettingsChangeKind.Changed or SettingsChangeKind.Added
        && CurrentText is not null
        && BackupText is not null;
}
