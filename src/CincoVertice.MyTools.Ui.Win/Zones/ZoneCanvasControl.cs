namespace CincoVertice.MyTools.Ui.Win.Zones;

/// <summary>
///     The surface the zone editor draws the monitor and its zones on: double buffered so it doesn't flicker.
/// </summary>
public sealed class ZoneCanvasControl : Control
{
    public ZoneCanvasControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = true;
    }
}
