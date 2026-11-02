using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     A text, possibly of several lines.
/// </summary>
/// <param name="Location">The top left corner of the text.</param>
/// <param name="FontSize">The font size, in pixels.</param>
public sealed record TextAnnotationModel(Point Location, string Text, Color Color, int FontSize)
    : AnnotationModel(Color);
