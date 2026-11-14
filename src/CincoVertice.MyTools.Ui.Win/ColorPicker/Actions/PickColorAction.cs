using CincoVertice.MyTools.Ui.ColorPicker.Models;
using CincoVertice.MyTools.Ui.Win.ColorPicker.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.ColorPicker.Actions;

/// <summary>
///     Freezes the screen and copies the color of the pixel the user clicks, as HEX (Ctrl+click: RGB), then shows it
///     in the color window, with its values, shades and the colors that go well with it.
/// </summary>
public sealed class PickColorAction : IAppAction
{
    private readonly ScreenColorPicker _picker;
    private readonly ColorWindowService _window;

    public PickColorAction(ScreenColorPicker picker, ColorWindowService window)
    {
        _picker = picker;
        _window = window;
    }

    public string Name => "PickColor";

    public string? DefaultShortcut => "Ctrl+Alt+P";

    public async Task ExecuteAsync()
    {
        PickedColorModel? picked = await _picker.PickAsync();

        if (picked is null)
        {
            return;
        }

        Clipboard.SetText(picked.Text);
        _window.Show(picked.Color);
    }
}
