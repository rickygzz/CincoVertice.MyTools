using CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

namespace CincoVertice.MyTools.Ui.Win.Shell.Actions;

/// <summary>
///     Opens hotkeys.cfg in the config editor. MyTools reloads the hotkeys when the file is saved
///     (<see cref="Services.HotkeyFilesWatcher"/>).
/// </summary>
public sealed class EditHotkeysAction : IAppAction
{
    private readonly ConfigEditorWindowService _editor;
    private readonly HotkeyConfigService _hotkeyConfigService;

    public EditHotkeysAction(ConfigEditorWindowService editor, HotkeyConfigService hotkeyConfigService)
    {
        _editor = editor;
        _hotkeyConfigService = hotkeyConfigService;
    }

    public string Name => "EditHotkeys";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        _editor.Open(_hotkeyConfigService.FilePath);

        return Task.CompletedTask;
    }
}
