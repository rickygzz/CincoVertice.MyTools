using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Presenters;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Actions;

/// <summary>
///     Like <see cref="SnipRegionAction"/>, then opens the snip in a new <see cref="SnipEditorForm"/> to draw over it.
///     The snip is on the clipboard already, and again after every change in the editor.
/// </summary>
public sealed class SnipAndEditAction : IAppAction
{
    private readonly ScreenSnipper _snipper;
    private readonly ISnipEditorSettingsStore _settingsStore;

    public SnipAndEditAction(ScreenSnipper snipper, ISnipEditorSettingsStore settingsStore)
    {
        _snipper = snipper;
        _settingsStore = settingsStore;
    }

    /// <summary>Where the editor keeps its tool, color and tool settings: config\snipeditor.json.</summary>
    public static string SettingsFilePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "config", "snipeditor.json");

    public string Name => "SnipAndEdit";

    public string? DefaultShortcut => "Ctrl+Alt+S";

    public async Task ExecuteAsync()
    {
        Bitmap? snip = await _snipper.SnipAsync();

        if (snip is null)
        {
            return;
        }

        // The form owns the snip from here and disposes it; it is disposed itself when closed
        var editor = new SnipEditorForm(snip);
        var presenter = new SnipEditorPresenter(editor, TimeProvider.System, _settingsStore);

        presenter.Start();
        editor.Show();
    }
}
