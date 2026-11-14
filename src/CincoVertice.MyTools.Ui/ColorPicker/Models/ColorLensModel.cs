using System.Drawing;

namespace CincoVertice.MyTools.Ui.ColorPicker.Models;

/// <summary>
///     What the magnifier shows: the pixel under the pointer, in screenshot pixels, how many times larger, and its
///     color.
/// </summary>
/// <param name="Cells">How many pixels across the magnifier shows, an odd number, so one is in the middle.</param>
public sealed record ColorLensModel(Point Pixel, int Zoom, int Cells, Color Color, string Hex, string Rgb);
