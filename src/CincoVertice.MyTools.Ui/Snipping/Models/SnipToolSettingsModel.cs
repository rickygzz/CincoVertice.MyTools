using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     What a snip editor tool remembers, so each tool keeps its own when the user switches between them.
/// </summary>
/// <param name="Size">The line width, or for the text tool the font size, in pixels.</param>
/// <param name="Transparency">How see-through it draws, in percent: 0 is solid.</param>
/// <param name="Color">The color it draws with, solid; <see cref="Color.Empty"/> when unknown (e.g. not saved).</param>
public sealed record SnipToolSettingsModel(int Size, int Transparency, Color Color);
