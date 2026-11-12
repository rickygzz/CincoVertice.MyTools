using System.Globalization;
using CincoVertice.Common.Utils.Config;
using CincoVertice.MyTools.Ui.Win.Zones.Models;
using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Win.Zones.Services;

/// <summary>
///     Reads zones.cfg with the config lexer: a Layout line (Columns, Rows or Grid) or Zone lines in percent, and a
///     Gap in pixels; # for comments. Every line that can't be used is reported as a problem and skipped; without a
///     layout, the default one is used.
/// </summary>
public static class ZonesConfigReader
{
    public const string LayoutName = "Layout";
    public const string ZoneName = "Zone";
    public const string GapName = "Gap";

    /// <summary>The most zones a layout has: window.tozone takes up to 99.</summary>
    public const int MaxZones = 99;

    public const int MaxGap = 100;

    // How far from 100 the widths or heights can add up to, for thirds such as 33.33
    private const double PercentTolerance = 0.5;

    private const string LayoutExample = "e.g. Layout = Columns 3, Columns 25 50 25, Rows 2 or Grid 3 x 2";
    private const string ZoneExample = "e.g. Zone = 0, 0, 50, 100";

    public static ZonesConfigResultModel Read(string text)
    {
        var result = new ZonesConfigResultModel();
        ConfigLexer lexer = new(text);
        ConfigItems items = lexer.Items;

        IReadOnlyList<ZoneModel>? layout = null;
        int layoutLine = 0;
        List<ZoneModel> zones = [];
        int? gap = null;

        for (int i = 0; i < items.KeysCount; i++)
        {
            string name = items.GetKeyName(i);
            int line = items.GetLine(i);

            if (items.GetValueType(i) == ConfigTokenType.ErrorQuotedStringNotClosed)
            {
                result.AddProblem(line, $"{name}: the [\" is never closed, so the rest of the file is ignored.");

                continue;
            }

            string value = items.GetValue(i).Split('#')[0].Trim();

            if (name.Equals(LayoutName, StringComparison.OrdinalIgnoreCase))
            {
                if (layout is not null)
                {
                    result.AddProblem(line, $"{LayoutName}: there's one already, line {layoutLine}, which is used.");
                }
                else if (TryReadLayout(value, out IReadOnlyList<ZoneModel> read, out string problem))
                {
                    layout = read;
                    layoutLine = line;
                }
                else
                {
                    result.AddProblem(line, $"{LayoutName}: {problem}");
                }
            }
            else if (name.Equals(ZoneName, StringComparison.OrdinalIgnoreCase))
            {
                if (zones.Count == MaxZones)
                {
                    result.AddProblem(line, $"{ZoneName}: there can be {MaxZones} zones at most.");
                }
                else if (TryReadZone(value, out ZoneModel? zone, out string problem))
                {
                    zones.Add(zone!);
                }
                else
                {
                    result.AddProblem(line, $"{ZoneName}: {problem}");
                }
            }
            else if (name.Equals(GapName, StringComparison.OrdinalIgnoreCase))
            {
                if (gap is not null)
                {
                    result.AddProblem(line, $"{GapName}: there's one already, which is used.");
                }
                else if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int pixels)
                    && pixels <= MaxGap)
                {
                    gap = pixels;
                }
                else
                {
                    result.AddProblem(line, $"{GapName}: a whole number of pixels from 0 to {MaxGap}, not '{value}'.");
                }
            }
            else
            {
                result.AddProblem(
                    line,
                    $"{name}: each line is {LayoutName} = ..., {ZoneName} = ... or {GapName} = ....");
            }
        }

        foreach (var (number, badLine) in lexer.BadLines)
        {
            result.AddProblem(number, $"'{badLine}' is not 'Name = Value'.");
        }

        if (zones.Count > 0 && layout is not null)
        {
            result.AddProblem(layoutLine, $"{LayoutName}: there are {ZoneName} lines too, and they're used instead.");
        }

        IReadOnlyList<ZoneModel> used = zones.Count > 0 ? zones : layout ?? ZoneLayoutModel.Default.Zones;
        result.Layout = new ZoneLayoutModel(used, gap ?? ZoneLayoutModel.DefaultGap);

        return result;
    }

    // "Columns 3", "Columns 25 50 25", "Rows 2", "Grid 3 x 2"
    private static bool TryReadLayout(string value, out IReadOnlyList<ZoneModel> zones, out string problem)
    {
        zones = [];
        problem = string.Empty;

        string[] words = value.Replace("x", " x ", StringComparison.OrdinalIgnoreCase)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string kind = words.Length > 0 ? words[0] : string.Empty;
        string[] numbers = words.Length > 0 ? words[1..] : [];

        if (kind.Equals("Columns", StringComparison.OrdinalIgnoreCase)
            || kind.Equals("Rows", StringComparison.OrdinalIgnoreCase))
        {
            bool isColumns = kind.Equals("Columns", StringComparison.OrdinalIgnoreCase);

            if (!TryReadSizes(numbers, isColumns ? "columns" : "rows", out IReadOnlyList<double> sizes, out problem))
            {
                return false;
            }

            zones = isColumns ? ZoneLayoutModel.Columns(sizes) : ZoneLayoutModel.Rows(sizes);

            return true;
        }

        if (kind.Equals("Grid", StringComparison.OrdinalIgnoreCase))
        {
            string[] counts = [.. numbers.Where(word => !word.Equals("x", StringComparison.OrdinalIgnoreCase))];

            if (counts.Length != 2
                || !TryReadCount(counts[0], out int columns)
                || !TryReadCount(counts[1], out int rows)
                || columns * rows > MaxZones)
            {
                problem = $"Grid takes columns x rows, e.g. Grid 3 x 2, with {MaxZones} zones at most.";

                return false;
            }

            zones = ZoneLayoutModel.Grid(ZoneLayoutModel.Equal(columns), ZoneLayoutModel.Equal(rows));

            return true;
        }

        problem = value.Length == 0
            ? $"which layout? {LayoutExample}."
            : $"'{value}' is not Columns, Rows or Grid; {LayoutExample}.";

        return false;
    }

    // One number is a count; more are the sizes in percent, adding up to 100
    private static bool TryReadSizes(
        string[] numbers,
        string what,
        out IReadOnlyList<double> sizes,
        out string problem)
    {
        sizes = [];
        problem = string.Empty;

        if (numbers.Length == 1 && TryReadCount(numbers[0], out int count))
        {
            sizes = ZoneLayoutModel.Equal(count);

            return true;
        }

        List<double> percents = [];

        foreach (string number in numbers)
        {
            if (!TryReadPercent(number, out double percent) || percent <= 0)
            {
                percents.Clear();

                break;
            }

            percents.Add(percent);
        }

        if (numbers.Length < 2 || percents.Count == 0 || percents.Count > MaxZones)
        {
            problem = $"how many {what} from 1 to {MaxZones}, or the size of each in percent, "
                + $"e.g. {Capitalized(what)} 3 or {Capitalized(what)} 25 50 25.";

            return false;
        }

        double total = percents.Sum();

        if (Math.Abs(total - 100) > PercentTolerance)
        {
            problem = $"the {what} add up to {Format(total)} percent, not 100.";

            return false;
        }

        sizes = percents;

        return true;
    }

    // "left, top, width, height" in percent
    private static bool TryReadZone(string value, out ZoneModel? zone, out string problem)
    {
        zone = null;
        problem = string.Empty;

        string[] parts = value.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
        List<double> numbers = [];

        foreach (string part in parts)
        {
            if (TryReadPercent(part, out double number))
            {
                numbers.Add(number);
            }
        }

        if (parts.Length != 4 || numbers.Count != 4)
        {
            problem = $"four numbers in percent of the screen: left, top, width, height; {ZoneExample}.";

            return false;
        }

        var read = new ZoneModel(numbers[0], numbers[1], numbers[2], numbers[3]);

        if (read.Width <= 0 || read.Height <= 0)
        {
            problem = "its width and height must be more than 0.";

            return false;
        }

        if (read.Left + read.Width > 100 + PercentTolerance)
        {
            problem = $"it goes past the right edge: left + width is {Format(read.Left + read.Width)}, more than 100.";

            return false;
        }

        if (read.Top + read.Height > 100 + PercentTolerance)
        {
            problem = $"it goes past the bottom: top + height is {Format(read.Top + read.Height)}, more than 100.";

            return false;
        }

        zone = read;

        return true;
    }

    private static bool TryReadCount(string text, out int count) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out count)
        && count >= 1
        && count <= MaxZones;

    private static bool TryReadPercent(string text, out double percent) =>
        double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out percent)
        && percent <= 100;

    private static string Format(double number) => Math.Round(number, 2).ToString(CultureInfo.InvariantCulture);

    private static string Capitalized(string word) => char.ToUpperInvariant(word[0]) + word[1..];
}
