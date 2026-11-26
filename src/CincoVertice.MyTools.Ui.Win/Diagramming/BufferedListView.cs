namespace CincoVertice.MyTools.Ui.Win.Diagramming;

/// <summary>
///     A list view drawn into a buffer first (LVS_EX_DOUBLEBUFFER), so its rows don't blink when the selection changes:
///     the layers panel's selection follows the canvas's at every click.
/// </summary>
public sealed class BufferedListView : ListView
{
    public BufferedListView()
    {
        DoubleBuffered = true;
    }
}
