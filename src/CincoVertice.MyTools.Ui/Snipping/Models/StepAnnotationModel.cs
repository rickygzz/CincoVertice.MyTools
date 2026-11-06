using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     A numbered circle, for the steps of a how-to.
/// </summary>
/// <param name="Center">The middle of the circle.</param>
/// <param name="Number">The number in it.</param>
/// <param name="Color">The circle's color; the number is white or black, whichever shows better on it.</param>
/// <param name="Diameter">How wide the circle is, in pixels.</param>
public sealed record StepAnnotationModel(Point Center, int Number, Color Color, int Diameter) : AnnotationModel(Color);
