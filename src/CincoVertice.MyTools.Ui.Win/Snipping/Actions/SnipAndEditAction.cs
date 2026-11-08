using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Actions;

/// <summary>
///     Like <see cref="SnipRegionAction"/>, then shows the snip in the <see cref="SnipEditorForm"/> to draw over it,
///     which is the one already open, if any. The snip is on the clipboard already, and again after every change in
///     the editor.
/// </summary>
public sealed class SnipAndEditAction : IAppAction
{
    private readonly ScreenSnipper _snipper;
    private readonly SnipEditorWindowService _editorWindow;

    public SnipAndEditAction(ScreenSnipper snipper, SnipEditorWindowService editorWindow)
    {
        _snipper = snipper;
        _editorWindow = editorWindow;
    }

    /// <summary>Where the editor keeps its tool, color and tool settings: config\snipeditor.json.</summary>
    public static string SettingsFilePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "config", "snipeditor.json");

    public string Name => "SnipAndEdit";

    public string? DefaultShortcut => "Ctrl+Alt+S";

    public async Task ExecuteAsync()
    {
        Bitmap? snip;

        using (_editorWindow.KeepOutOfSnip())
        {
            snip = await _snipper.SnipAsync();
        }

        if (snip is not null)
        {
            _editorWindow.Show(snip);
        }
    }
}
