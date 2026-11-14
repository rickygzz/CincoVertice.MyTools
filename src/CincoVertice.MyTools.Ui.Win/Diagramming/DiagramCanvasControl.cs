namespace CincoVertice.MyTools.Ui.Win.Diagramming;

/// <summary>
///     The surface a diagram is drawn on: double buffered so it doesn't flicker, and taking the arrow keys, which would
///     otherwise only move the focus between controls.
/// </summary>
public sealed class DiagramCanvasControl : Control
{
    public DiagramCanvasControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = true;
    }

    protected override bool IsInputKey(Keys keyData) =>
        (keyData & Keys.KeyCode) is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);
}
