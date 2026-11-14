using CincoVertice.MyTools.Ui.ColorPicker.Models;

namespace CincoVertice.MyTools.Ui.ColorPicker.Interfaces;

/// <summary>Freezes the screen and lets the user pick the color of a pixel on it.</summary>
public interface IScreenColorPicker
{
    /// <returns>The color picked; null when the pick was cancelled or another one is open.</returns>
    Task<PickedColorModel?> PickAsync();
}
