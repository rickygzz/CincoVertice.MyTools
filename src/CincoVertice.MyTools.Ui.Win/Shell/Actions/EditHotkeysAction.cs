using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Shell.Actions;

/// <summary>
///     Opens hotkeys.cfg for editing. MyTools reloads the hotkeys when the file is saved
///     (<see cref="Services.HotkeyFilesWatcher"/>).
/// </summary>
public sealed class EditHotkeysAction : IAppAction
{
    private readonly HotkeyConfigService _hotkeyConfigService;

    public EditHotkeysAction(HotkeyConfigService hotkeyConfigService)
    {
        _hotkeyConfigService = hotkeyConfigService;
    }

    public string Name => "EditHotkeys";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        string filePath = _hotkeyConfigService.FilePath;

        if (!File.Exists(filePath))
        {
            MessageBox.Show(
                $"{filePath} does not exist. It is created when MyTools starts.",
                "Edit hotkeys",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return Task.CompletedTask;
        }

        ConfigFileOpener.Open(filePath);

        return Task.CompletedTask;
    }
}
