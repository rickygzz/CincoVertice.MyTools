using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Models;

namespace CincoVertice.MyTools.Ui.ColorPicker.Views;

/// <summary>
///     The color window: a color's values, its shades, and colors that go well with it, each of which can be chosen
///     to see its values.
/// </summary>
public interface IColorView
{
    /// <summary>Raised when the user clicks a shade or a color that goes well.</summary>
    event EventHandler<Color>? ColorChosen;

    /// <summary>Raised to make the shades and the colors that go well from the color chosen.</summary>
    event EventHandler? UseAsBaseRequested;

    /// <summary>Raised to show the color picked again, with the colors made from it.</summary>
    event EventHandler? BackToPickedRequested;

    /// <summary>Raised to pick another color from the screen.</summary>
    event EventHandler? PickAnotherRequested;

    /// <summary>Shows the shades and the colors that go well with a color.</summary>
    void ShowPalette(ColorPaletteModel palette);

    /// <summary>Shows the values of the color chosen, and marks it among the others.</summary>
    void ShowChosen(ColorValuesModel values);

    /// <summary>Enables Back to picked: false while the color picked and its palette are what's shown.</summary>
    void SetBackToPickedEnabled(bool isEnabled);

    /// <summary>Keeps the window out of the frozen screen while a color is picked, until disposed.</summary>
    IDisposable KeepOutOfPick();

    void CopyToClipboard(string text);
}
