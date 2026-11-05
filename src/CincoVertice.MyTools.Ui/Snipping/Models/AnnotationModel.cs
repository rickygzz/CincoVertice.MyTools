using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     Something drawn over a snip. Coordinates are snip pixels: (0, 0) is the snip's top left corner.
/// </summary>
/// <param name="Color">Its color; a highlighter's is see-through.</param>
public abstract record AnnotationModel(Color Color)
{
    /// <summary>What it was when first drawn, to go back to; null when it hasn't changed since.</summary>
    public AnnotationModel? Original { get; init; }
}
