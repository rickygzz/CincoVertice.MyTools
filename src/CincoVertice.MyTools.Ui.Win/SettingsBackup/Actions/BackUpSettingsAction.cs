using CincoVertice.MyTools.Ui.SettingsBackup.Presenters;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.SettingsBackup.Actions;

/// <summary>Saves the config folder to a zip the user chooses.</summary>
public sealed class BackUpSettingsAction : IAppAction
{
    private readonly SettingsBackupPresenter _presenter;

    public BackUpSettingsAction(SettingsBackupPresenter presenter)
    {
        _presenter = presenter;
    }

    public string Name => "BackUpSettings";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        _presenter.BackUp();

        return Task.CompletedTask;
    }
}
