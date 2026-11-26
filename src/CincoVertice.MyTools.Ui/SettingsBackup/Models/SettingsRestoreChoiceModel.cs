namespace CincoVertice.MyTools.Ui.SettingsBackup.Models;

/// <summary>The user's choice in the restore's review: restore, and whether to delete files the backup lacks.</summary>
/// <param name="DeleteFilesNotInBackup">Delete the settings' files the backup doesn't have; kept when false.</param>
public sealed record SettingsRestoreChoiceModel(bool DeleteFilesNotInBackup);
