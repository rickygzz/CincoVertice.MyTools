using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Services;

namespace CincoVertice.MyTools.Ui.ColorPicker.Models;

/// <summary>The color the user picked, and how they asked for it to be written.</summary>
public sealed record PickedColorModel(Color Color, ColorTextFormat Format)
{
    /// <summary>What goes on the clipboard.</summary>
    public string Text => ColorText.Of(Color, Format);
}
