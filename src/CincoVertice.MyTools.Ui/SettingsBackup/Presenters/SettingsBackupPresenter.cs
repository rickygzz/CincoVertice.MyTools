using System.Globalization;
using CincoVertice.MyTools.Ui.SettingsBackup.Models;
using CincoVertice.MyTools.Ui.SettingsBackup.Services;
using CincoVertice.MyTools.Ui.SettingsBackup.Views;

namespace CincoVertice.MyTools.Ui.SettingsBackup.Presenters;

/// <summary>
///     Backs up the settings to a zip the user chooses, and restores one: what it would do is reviewed first, the
///     settings as they were are saved before anything is written, and nothing is deleted unless asked.
/// </summary>
public sealed class SettingsBackupPresenter
{
    private readonly ISettingsBackupView _view;
    private readonly SettingsBackupService _service;
    private readonly TimeProvider _clock;

    public SettingsBackupPresenter(ISettingsBackupView view, SettingsBackupService service, TimeProvider? clock = null)
    {
        _view = view;
        _service = service;
        _clock = clock ?? TimeProvider.System;
    }

    public void BackUp()
    {
        string date = _clock.GetLocalNow().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string name = $"MyTools-settings-{date}.zip";

        if (_view.AskBackupPath(name) is not string path)
        {
            return;
        }

        try
        {
            _service.Export(path);
            _view.ShowMessage($"Settings backed up to {path}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowProblem($"Could not back up the settings to {path}: {ex.Message}");
        }
    }

    public void Restore()
    {
        if (_view.AskRestorePath() is not string path)
        {
            return;
        }

        SettingsRestorePlanModel plan;

        try
        {
            plan = _service.Plan(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _view.ShowProblem($"Could not read {path}: {ex.Message}");

            return;
        }

        if (plan.ToWrite.Count == 0 && plan.NotInBackup.Count == 0)
        {
            _view.ShowMessage("The backup is the same as your settings: there's nothing to restore.");

            return;
        }

        if (_view.ReviewRestore(plan) is not SettingsRestoreChoiceModel choice)
        {
            return;
        }

        string? safetyCopy;

        try
        {
            safetyCopy = _service.SaveSafetyCopy();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowProblem($"Nothing was restored: your settings could not be saved first ({ex.Message}).");

            return;
        }

        string before = safetyCopy is null
            ? string.Empty
            : $" Your settings before the restore are saved in {safetyCopy}.";

        try
        {
            (int written, int deleted) = _service.Apply(plan, choice.DeleteFilesNotInBackup);
            string done = deleted > 0
                ? $"{Count(written, "file")} written, {Count(deleted, "file")} deleted"
                : $"{Count(written, "file")} written";
            _view.ShowMessage(
                $"Settings restored: {done}.{before} Exit and reopen My Tools so every setting is read again.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowProblem($"The restore stopped partway ({ex.Message}).{before}");
        }
    }

    private static string Count(int count, string what) => $"{count} {what}{(count == 1 ? string.Empty : "s")}";
}
