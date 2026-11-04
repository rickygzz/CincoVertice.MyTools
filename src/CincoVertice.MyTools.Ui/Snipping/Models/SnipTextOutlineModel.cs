using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     The edge around a text's letters.
/// </summary>
/// <param name="Width">How far it goes out from the letters, in pixels; 0 for no edge.</param>
/// <param name="Color">Solid; <see cref="Color.Empty"/> when unknown (e.g. not saved).</param>
public sealed record SnipTextOutlineModel(int Width, Color Color);
