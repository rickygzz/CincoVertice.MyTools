using CincoVertice.MyTools.Ui.SettingsBackup.Presenters;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.SettingsBackup.Actions;

/// <summary>
///     Restores the settings from a backup the user chooses, after reviewing what changes; the settings as they were
///     are saved to the backups folder first.
/// </summary>
public sealed class RestoreSettingsAction : IAppAction
{
    private readonly SettingsBackupPresenter _presenter;

    public RestoreSettingsAction(SettingsBackupPresenter presenter)
    {
        _presenter = presenter;
    }

    public string Name => "RestoreSettings";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        _presenter.Restore();

        return Task.CompletedTask;
    }
}
