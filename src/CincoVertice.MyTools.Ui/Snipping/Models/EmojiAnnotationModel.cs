using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     An emoji, in its own colors, as Windows draws it.
/// </summary>
/// <param name="Center">The middle of the emoji.</param>
/// <param name="Emoji">The emoji, e.g. "👍": one or more characters, as a skin tone or a flag takes.</param>
/// <param name="Color">Only its transparency is used: the emoji keeps its own colors.</param>
/// <param name="Size">How wide and tall it is, in pixels.</param>
public sealed record EmojiAnnotationModel(Point Center, string Emoji, Color Color, int Size) : AnnotationModel(Color);
