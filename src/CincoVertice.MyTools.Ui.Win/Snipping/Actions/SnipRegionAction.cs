using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Actions;

/// <summary>
///     Freezes the screen, lets the user drag a rectangle over it, and copies that part to the clipboard.
/// </summary>
public sealed class SnipRegionAction : IAppAction
{
    private readonly ScreenSnipper _snipper;

    public SnipRegionAction(ScreenSnipper snipper)
    {
        _snipper = snipper;
    }

    public string Name => "SnipRegion";

    public string? DefaultShortcut => "Ctrl+Alt+S";

    public async Task ExecuteAsync()
    {
        using Bitmap? snip = await _snipper.SnipAsync();

        if (snip is not null)
        {
            Clipboard.SetImage(snip);
        }
    }
}
