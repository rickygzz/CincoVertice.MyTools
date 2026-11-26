using CincoVertice.MyTools.Ui.SettingsBackup.Models;

namespace CincoVertice.MyTools.Ui.SettingsBackup.Views;

/// <summary>What backing up and restoring the settings asks and tells the user.</summary>
public interface ISettingsBackupView
{
    /// <returns>Where to save the backup; null when the user cancels.</returns>
    string? AskBackupPath(string fileName);

    /// <returns>The backup to restore; null when the user cancels.</returns>
    string? AskRestorePath();

    /// <summary>Shows what the restore would do, file by file, with each changed text to compare.</summary>
    /// <returns>The user's choice; null when they cancel.</returns>
    SettingsRestoreChoiceModel? ReviewRestore(SettingsRestorePlanModel plan);

    void ShowMessage(string message);

    void ShowProblem(string message);
}
