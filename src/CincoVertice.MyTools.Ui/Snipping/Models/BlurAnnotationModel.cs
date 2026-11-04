using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     A part of the snip hidden for good: it looks blurred, but nothing in it comes from the pixels it covers.
/// </summary>
public sealed record BlurAnnotationModel(Rectangle Bounds) : AnnotationModel(Color.Empty);
