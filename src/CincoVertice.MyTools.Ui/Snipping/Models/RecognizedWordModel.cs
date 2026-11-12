namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>A word read from an image, and where it is in it.</summary>
/// <param name="Left">Its left edge, in pixels of the image.</param>
/// <param name="Width">In pixels of the image.</param>
public sealed record RecognizedWordModel(string Text, double Left, double Width);
