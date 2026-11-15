using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A set of colors that go together, as Office's themes have: the colors offered first, in their lighter and
///     darker versions, and what new shapes get.
/// </summary>
/// <param name="Name">Its name, as the menu shows it and the file keeps it.</param>
/// <param name="Colors">
///     Ten colors, in Office's order: background 1, text 1, background 2, text 2, then accents 1 to 6.
/// </param>
public sealed record DiagramThemeModel(string Name, IReadOnlyList<Color> Colors);
