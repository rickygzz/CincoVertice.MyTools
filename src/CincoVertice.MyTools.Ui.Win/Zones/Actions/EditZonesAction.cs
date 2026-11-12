using CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Zones.Services;

namespace CincoVertice.MyTools.Ui.Win.Zones.Actions;

/// <summary>Opens zones.cfg in the config editor. A change applies at the next drag.</summary>
public sealed class EditZonesAction : IAppAction
{
    private readonly ConfigEditorWindowService _editor;
    private readonly ZonesConfigService _zones;

    public EditZonesAction(ConfigEditorWindowService editor, ZonesConfigService zones)
    {
        _editor = editor;
        _zones = zones;
    }

    public string Name => "EditZones";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        // Created first, with the default layout, so there's something to edit
        _zones.LoadResult();
        _editor.Open(_zones.FilePath);

        return Task.CompletedTask;
    }
}
