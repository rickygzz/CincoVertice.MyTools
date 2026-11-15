namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     What the color menus offer.
/// </summary>
/// <param name="Theme">The name of the diagram's theme.</param>
/// <param name="Themes">The names of every theme, to choose from.</param>
/// <param name="ThemeColors">
///     The theme's colors, row by row, ten to a row: its own colors, then five rows of lighter and darker versions.
/// </param>
/// <param name="StandardColors">Ten colors that are the same in every theme.</param>
/// <param name="RecentColors">Colors used last that aren't the others, newest first.</param>
public sealed record DiagramPaletteModel(
    string Theme,
    IReadOnlyList<string> Themes,
    IReadOnlyList<DiagramSwatchModel> ThemeColors,
    IReadOnlyList<DiagramSwatchModel> StandardColors,
    IReadOnlyList<DiagramSwatchModel> RecentColors);
