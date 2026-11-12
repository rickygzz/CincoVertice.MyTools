namespace CincoVertice.MyTools.Ui.Zones.Models;

/// <summary>The zones of every monitor, numbered from 1 in this order, and the space between them.</summary>
/// <param name="Gap">Pixels between zones, and between them and the edges of the work area.</param>
public sealed record ZoneLayoutModel(IReadOnlyList<ZoneModel> Zones, int Gap)
{
    public const int DefaultGap = 8;

    /// <summary>Three columns as wide as each other.</summary>
    public static ZoneLayoutModel Default { get; } = new(Columns(Equal(3)), DefaultGap);

    /// <param name="widths">Each column's width in percent, left to right, adding up to 100.</param>
    public static IReadOnlyList<ZoneModel> Columns(IReadOnlyList<double> widths) => Grid(widths, [100]);

    /// <param name="heights">Each row's height in percent, top to bottom, adding up to 100.</param>
    public static IReadOnlyList<ZoneModel> Rows(IReadOnlyList<double> heights) => Grid([100], heights);

    /// <returns>A zone per cell, row by row, each left to right.</returns>
    public static IReadOnlyList<ZoneModel> Grid(IReadOnlyList<double> widths, IReadOnlyList<double> heights)
    {
        List<ZoneModel> zones = [];
        double top = 0;

        foreach (double height in heights)
        {
            double left = 0;

            foreach (double width in widths)
            {
                zones.Add(new ZoneModel(left, top, width, height));
                left += width;
            }

            top += height;
        }

        return zones;
    }

    /// <returns>As many percent of the same size as the count, adding up to 100.</returns>
    public static IReadOnlyList<double> Equal(int count) => [.. Enumerable.Repeat(100d / count, count)];
}
