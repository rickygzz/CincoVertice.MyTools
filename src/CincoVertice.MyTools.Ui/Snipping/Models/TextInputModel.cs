using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     A text the user typed in the snip editor.
/// </summary>
/// <param name="Location">Where the text input was started.</param>
public sealed record TextInputModel(Point Location, string Text);
