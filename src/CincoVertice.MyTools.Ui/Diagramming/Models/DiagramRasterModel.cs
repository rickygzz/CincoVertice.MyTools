namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     An image as pixels, row by row from the top left.
/// </summary>
/// <param name="Width">In pixels.</param>
/// <param name="Height">In pixels.</param>
/// <param name="Rgb">Red, green and blue of each pixel, one byte each, not multiplied by its alpha.</param>
/// <param name="Alpha">How opaque each pixel is, from 0 for transparent to 255.</param>
public sealed record DiagramRasterModel(int Width, int Height, byte[] Rgb, byte[] Alpha);
