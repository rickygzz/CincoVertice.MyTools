using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Zones.Services;

namespace CincoVertice.MyTools.Ui.Win.Zones.Actions;

/// <summary>
///     Opens the window zones editor, which saves zones.cfg; its Edit as text opens the file in the config editor.
///     A change applies at the next drag.
/// </summary>
public sealed class EditZonesAction : IAppAction
{
    private readonly ZoneEditorWindowService _editor;

    public EditZonesAction(ZoneEditorWindowService editor)
    {
        _editor = editor;
    }

    public string Name => "EditZones";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        _editor.Open();

        return Task.CompletedTask;
    }
}
