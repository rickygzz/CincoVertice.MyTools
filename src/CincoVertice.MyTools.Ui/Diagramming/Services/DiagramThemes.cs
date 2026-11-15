using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Services;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     The color themes, laid out as Office lays them out: each theme color in a column, with five lighter and darker
///     versions under it. A color picked from that grid is found again in another theme at the same place, so a
///     diagram can be given another theme's colors.
/// </summary>
public static class DiagramThemes
{
    /// <summary>The theme of a diagram that names none, e.g. one from before themes.</summary>
    public const string DefaultName = "Office";

    /// <summary>How many colors a theme has, and how many columns the grid has.</summary>
    public const int Columns = 10;

    /// <summary>How many rows the grid has: the theme's colors, then five rows of versions.</summary>
    public const int Rows = 6;

    // Where new shapes' colors come from: text 1, and accent 1 lighter 80% and darker 25%
    private const int TextColumn = 1;
    private const int AccentColumn = 4;
    private const int LighterRow = 1;
    private const int DarkerRow = 4;

    private static readonly string[] _columnNames =
    [
        "Background 1", "Text 1", "Background 2", "Text 2",
        "Accent 1", "Accent 2", "Accent 3", "Accent 4", "Accent 5", "Accent 6",
    ];

    /// <summary>Every theme, in the order the menu shows them.</summary>
    public static IReadOnlyList<DiagramThemeModel> All { get; } =
    [
        Theme(
            "Office",
            0xFFFFFF, 0x000000, 0xE7E6E6, 0x44546A,
            0x4472C4, 0xED7D31, 0xA5A5A5, 0xFFC000, 0x5B9BD5, 0x70AD47),
        Theme(
            "Azure",
            0xFFFFFF, 0x000000, 0xF3F2F1, 0x243A5E,
            0x0078D4, 0x50E6FF, 0x773ADC, 0x107C10, 0xFFB900, 0xD83B01),
        Theme(
            "Pastel",
            0xFFFFFF, 0x3A3A3A, 0xF7F3EE, 0x5C6B73,
            0xA8D8EA, 0xFFB7B2, 0xB5EAD7, 0xFFDAC1, 0xC7CEEA, 0xE2F0CB),
        Theme(
            "Grayscale",
            0xFFFFFF, 0x000000, 0xF2F2F2, 0x404040,
            0x595959, 0x737373, 0x8C8C8C, 0xA6A6A6, 0xBFBFBF, 0xD9D9D9),
        Theme(
            "Vivid",
            0xFFFFFF, 0x000000, 0xF5F5F5, 0x1F1F1F,
            0xE81123, 0xFF8C00, 0xFFF100, 0x00B294, 0x0063B1, 0x8E44AD),
        Theme(
            "Earth",
            0xFFFFFF, 0x3E2C23, 0xF3EDE4, 0x5B4636,
            0x8B5E3C, 0xA68A64, 0x6B8E23, 0xC2B280, 0x7F4F24, 0x3F6B4F),
        Theme(
            "Ocean",
            0xFFFFFF, 0x0B2545, 0xEEF4F8, 0x13315C,
            0x1B98E0, 0x247BA0, 0x70C1B3, 0xB2DBBF, 0x006494, 0x4CB5AE),
    ];

    /// <summary>Ten colors offered in every theme, which a theme never changes.</summary>
    public static IReadOnlyList<DiagramSwatchModel> StandardColors { get; } =
    [
        Swatch(0xC00000, "Dark red"), Swatch(0xFF0000, "Red"), Swatch(0xFFC000, "Orange"),
        Swatch(0xFFFF00, "Yellow"), Swatch(0x92D050, "Light green"), Swatch(0x00B050, "Green"),
        Swatch(0x00B0F0, "Light blue"), Swatch(0x0070C0, "Blue"), Swatch(0x002060, "Dark blue"),
        Swatch(0x7030A0, "Purple"),
    ];

    /// <summary>The theme of that name, in any case; the default one for a name no theme has.</summary>
    public static DiagramThemeModel Of(string? name) =>
        All.FirstOrDefault(theme => string.Equals(theme.Name, name, StringComparison.OrdinalIgnoreCase)) ?? All[0];

    /// <summary>
    ///     The theme's grid, row by row: its colors, then five rows of versions. A light color gets darker versions,
    ///     a dark one lighter ones, and the others three lighter and two darker, as Office does.
    /// </summary>
    public static IReadOnlyList<DiagramSwatchModel> Grid(DiagramThemeModel theme)
    {
        var grid = new DiagramSwatchModel[Columns * Rows];

        for (int column = 0; column < Columns; column++)
        {
            Color color = theme.Colors[column];
            string name = _columnNames[column];
            (double hue, double saturation, double lightness) = ColorPalette.ToHsl(color);
            (bool IsLighter, int Percent)[] versions = lightness switch
            {
                > 0.95 => [(false, 5), (false, 15), (false, 25), (false, 35), (false, 50)],
                < 0.05 => [(true, 50), (true, 35), (true, 25), (true, 15), (true, 5)],
                _ => [(true, 80), (true, 60), (true, 40), (false, 25), (false, 50)],
            };

            grid[column] = new DiagramSwatchModel(color, name);

            for (int row = 1; row < Rows; row++)
            {
                (bool isLighter, int percent) = versions[row - 1];
                double amount = percent / 100.0;
                double changed = isLighter ? lightness + (1 - lightness) * amount : lightness * (1 - amount);

                grid[row * Columns + column] = new DiagramSwatchModel(
                    ColorPalette.FromHsl(hue, saturation, changed),
                    $"{name}, {(isLighter ? "lighter" : "darker")} {percent}%");
            }
        }

        return grid;
    }

    /// <summary>
    ///     The colors new shapes and connectors get with the theme: accent 1 lighter 80% inside, accent 1 darker 25%
    ///     for the border and the line, and text 1 for the text. The rest of the style stays.
    /// </summary>
    public static DiagramStyleModel DefaultsOf(DiagramThemeModel theme, DiagramStyleModel style)
    {
        IReadOnlyList<DiagramSwatchModel> grid = Grid(theme);

        return style with
        {
            Fill = grid[LighterRow * Columns + AccentColumn].Color,
            Line = grid[DarkerRow * Columns + AccentColumn].Color,
            TextColor = grid[TextColumn].Color,
        };
    }

    /// <summary>
    ///     The diagram with each color picked from one theme's grid in the color at the same place in the other's.
    ///     Other colors, such as the standard ones, those picked otherwise, no fill and no title fill, stay.
    /// </summary>
    public static DiagramModel Recolored(DiagramModel diagram, DiagramThemeModel from, DiagramThemeModel to)
    {
        IReadOnlyList<DiagramSwatchModel> fromGrid = Grid(from);
        IReadOnlyList<DiagramSwatchModel> toGrid = Grid(to);
        Dictionary<int, Color> map = [];

        // A color twice in the grid goes where it's first
        for (int i = 0; i < fromGrid.Count; i++)
        {
            map.TryAdd(fromGrid[i].Color.ToArgb(), toGrid[i].Color);
        }

        Color Mapped(Color color) =>
            color.IsEmpty || color.A == 0 || !map.TryGetValue(color.ToArgb(), out Color mapped) ? color : mapped;

        DiagramStyleModel Restyled(DiagramStyleModel style) => style with
        {
            Fill = Mapped(style.Fill),
            Line = Mapped(style.Line),
            TextColor = Mapped(style.TextColor),
            TextOutlineColor = Mapped(style.TextOutlineColor),
            TitleFill = Mapped(style.TitleFill),
        };

        return diagram with
        {
            Shapes = [.. diagram.Shapes.Select(shape => shape with { Style = Restyled(shape.Style) })],
            Connectors =
            [
                .. diagram.Connectors.Select(connector => connector with { Style = Restyled(connector.Style) }),
            ],
        };
    }

    /// <summary>Whether a color is offered already: in the theme's grid or among the standard colors.</summary>
    public static bool Offers(DiagramThemeModel theme, Color color) =>
        Grid(theme).Concat(StandardColors).Any(swatch => swatch.Color.ToArgb() == color.ToArgb());

    private static DiagramThemeModel Theme(string name, params int[] colors) =>
        new(name, [.. colors.Select(rgb => Color.FromArgb(255, Color.FromArgb(rgb)))]);

    private static DiagramSwatchModel Swatch(int rgb, string name) =>
        new(Color.FromArgb(255, Color.FromArgb(rgb)), name);
}
