namespace CincoVertice.MyTools.Ui.Win.Behaviors;

/// <summary>
///     Moves a form with Ctrl+Arrow keys; holding Shift too moves it faster.
/// </summary>
public static class KeyboardMover
{
    // Pixels per key press
    private const int Step = 10;
    private const int FastStepMultiplier = 12;

    /// <summary>
    ///     Moves the form when the key is Ctrl+Arrow. Call it from the form's KeyDown, with KeyPreview on.
    /// </summary>
    /// <returns>True when the key moved the form and was handled.</returns>
    public static bool TryMove(Form form, KeyEventArgs e)
    {
        if (!e.Control)
        {
            return false;
        }

        int step = e.Shift ? Step * FastStepMultiplier : Step;

        Size offset = e.KeyCode switch
        {
            Keys.Up => new Size(0, -step),
            Keys.Down => new Size(0, step),
            Keys.Left => new Size(-step, 0),
            Keys.Right => new Size(step, 0),
            _ => Size.Empty,
        };

        if (offset.IsEmpty)
        {
            return false;
        }

        form.Location += offset;
        e.Handled = true;
        e.SuppressKeyPress = true;

        return true;
    }
}
