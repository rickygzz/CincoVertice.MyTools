using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Actions;

/// <summary>
///     Freezes the screen, lets the user drag a rectangle over it, and copies that part to the clipboard.
/// </summary>
public sealed class SnipRegionAction : IAppAction
{
    private readonly ScreenSnipper _snipper;
    private readonly SnipEditorWindowService _editorWindow;

    public SnipRegionAction(ScreenSnipper snipper, SnipEditorWindowService editorWindow)
    {
        _snipper = snipper;
        _editorWindow = editorWindow;
    }

    public string Name => "SnipRegion";

    public string? DefaultShortcut => "Ctrl+Alt+Shift+S";

    public async Task ExecuteAsync()
    {
        Bitmap? snip;

        // The open snip editor, if any, stays out of this one too
        using (_editorWindow.KeepOutOfSnip())
        {
            snip = await _snipper.SnipAsync();
        }

        if (snip is not null)
        {
            using (snip)
            {
                Clipboard.SetImage(snip);
            }
        }
    }
}
