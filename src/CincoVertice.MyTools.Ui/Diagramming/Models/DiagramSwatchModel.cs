using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A color offered in a color menu.
/// </summary>
/// <param name="Color">The color.</param>
/// <param name="Name">What it is, e.g. "Accent 1, lighter 40%" or "Dark red", shown with its hex code.</param>
public sealed record DiagramSwatchModel(Color Color, string Name);
