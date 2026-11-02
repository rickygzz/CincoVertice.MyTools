using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Shell.Actions;

/// <summary>
///     Opens activity.cfg, creating it first if needed. The settings apply the next time activity is turned on.
/// </summary>
public sealed class EditActivitySettingsAction : IAppAction
{
    private readonly ActivitySettingsService _settingsService;

    public EditActivitySettingsAction(ActivitySettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public string Name => "EditActivitySettings";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        // Only to create the file: problems in it are reported when activity is turned on
        _settingsService.Load();

        if (File.Exists(_settingsService.FilePath))
        {
            ConfigFileOpener.Open(_settingsService.FilePath);
        }
        else
        {
            MessageBox.Show(
                $"{_settingsService.FilePath} could not be created.",
                "Edit activity settings",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        return Task.CompletedTask;
    }
}
