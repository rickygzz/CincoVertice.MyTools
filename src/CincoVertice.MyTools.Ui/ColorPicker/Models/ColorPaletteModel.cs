using System.Drawing;

namespace CincoVertice.MyTools.Ui.ColorPicker.Models;

/// <summary>The colors the color window offers for a color.</summary>
/// <param name="Base">The color they're made from.</param>
/// <param name="Shades">Darkest first, <paramref name="Base"/> in the middle, lightest last.</param>
/// <param name="Harmonies">Dark and light colors that go well with it.</param>
public sealed record ColorPaletteModel(
    Color Base,
    IReadOnlyList<Color> Shades,
    IReadOnlyList<ColorHarmonyModel> Harmonies);
