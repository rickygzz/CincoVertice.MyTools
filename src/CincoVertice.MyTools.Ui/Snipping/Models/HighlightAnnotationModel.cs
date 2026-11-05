using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     A whole area highlighted: what a highlighter's line becomes when it's scaled.
/// </summary>
/// <param name="Bounds">The area.</param>
/// <param name="Color">Its color, see-through.</param>
public sealed record HighlightAnnotationModel(Rectangle Bounds, Color Color) : AnnotationModel(Color);
