using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     A text, possibly of several lines.
/// </summary>
/// <param name="Location">The top left corner of the text.</param>
/// <param name="FontSize">The font size, in pixels.</param>
/// <param name="Outline">The edge around the letters, as see-through as the text; null for none.</param>
public sealed record TextAnnotationModel(
    Point Location,
    string Text,
    Color Color,
    int FontSize,
    SnipTextOutlineModel? Outline = null)
    : AnnotationModel(Color);
