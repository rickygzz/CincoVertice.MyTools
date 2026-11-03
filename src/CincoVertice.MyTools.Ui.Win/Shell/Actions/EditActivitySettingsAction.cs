using CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

namespace CincoVertice.MyTools.Ui.Win.Shell.Actions;

/// <summary>
///     Opens activity.cfg in the config editor. The settings apply the next time activity is turned on.
/// </summary>
public sealed class EditActivitySettingsAction : IAppAction
{
    private readonly ConfigEditorWindowService _editor;
    private readonly ActivitySettingsService _settingsService;

    public EditActivitySettingsAction(ConfigEditorWindowService editor, ActivitySettingsService settingsService)
    {
        _editor = editor;
        _settingsService = settingsService;
    }

    public string Name => "EditActivitySettings";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        _editor.Open(_settingsService.FilePath);

        return Task.CompletedTask;
    }
}
