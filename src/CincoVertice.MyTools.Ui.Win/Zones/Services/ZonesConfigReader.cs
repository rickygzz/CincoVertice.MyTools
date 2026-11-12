using System.Drawing;
using System.Globalization;
using CincoVertice.Common.Utils.Config;
using CincoVertice.MyTools.Ui.Win.Zones.Models;
using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Win.Zones.Services;

/// <summary>
///     Reads zones.cfg with the config lexer: a Layout line (Columns, Rows or Grid) or Zone lines in percent, and a
///     Gap in pixels; # for comments. That's the default layout; after a Monitor line (the monitor's bounds), the same
///     lines are that monitor's own layout, taking what it lacks from the default one. Every line that can't be used is
///     reported as a problem and skipped; without a layout, the default one is used.
/// </summary>
public static class ZonesConfigReader
{
    public const string LayoutName = "Layout";
    public const string ZoneName = "Zone";
    public const string GapName = "Gap";
    public const string MonitorName = "Monitor";

    /// <summary>The most zones a layout has: window.tozone takes up to 99.</summary>
    public const int MaxZones = 99;

    public const int MaxGap = 100;

    // How far from 100 the widths or heights can add up to, for thirds such as 33.33
    private const double PercentTolerance = 0.5;

    private const string LayoutExample = "e.g. Layout = Columns 3, Columns 25 50 25, Rows 2 or Grid 3 x 2";
    private const string ZoneExample = "e.g. Zone = 0, 0, 50, 100";
    private const string MonitorExample = "e.g. Monitor = 1920, 0, 2560, 1440";

    public static ZonesConfigResultModel Read(string text)
    {
        var result = new ZonesConfigResultModel();
        ConfigLexer lexer = new(text);
        ConfigItems items = lexer.Items;

        // The lines before the first Monitor line are the default layout's; each Monitor line starts a monitor's own
        var defaultSection = new Section(Rectangle.Empty);
        List<Section> monitors = [];
        Section section = defaultSection;

        foreach (int i in Enumerable.Range(0, items.KeysCount).OrderBy(items.GetLine))
        {
            string name = items.GetKeyName(i);
            int line = items.GetLine(i);

            if (items.GetValueType(i) == ConfigTokenType.ErrorQuotedStringNotClosed)
            {
                result.AddProblem(line, $"{name}: the [\" is never closed, so the rest of the file is ignored.");

                continue;
            }

            string value = items.GetValue(i).Split('#')[0].Trim();

            if (name.Equals(MonitorName, StringComparison.OrdinalIgnoreCase))
            {
                section = StartMonitor(result, monitors, value, line);
            }
            else if (name.Equals(LayoutName, StringComparison.OrdinalIgnoreCase))
            {
                ReadLayoutLine(result, section, value, line);
            }
            else if (name.Equals(ZoneName, StringComparison.OrdinalIgnoreCase))
            {
                ReadZoneLine(result, section, value, line);
            }
            else if (name.Equals(GapName, StringComparison.OrdinalIgnoreCase))
            {
                ReadGapLine(result, section, value, line);
            }
            else
            {
                result.AddProblem(
                    line,
                    $"{name}: each line is {LayoutName} = ..., {ZoneName} = ..., {GapName} = ... "
                    + $"or {MonitorName} = ....");
            }
        }

        foreach (var (number, badLine) in lexer.BadLines)
        {
            result.AddProblem(number, $"'{badLine}' is not 'Name = Value'.");
        }

        ZoneLayoutModel defaultLayout =
            defaultSection.Build(result, ZoneLayoutModel.Default.Zones, ZoneLayoutModel.DefaultGap);
        result.Layouts = new ZoneLayoutsModel(
            defaultLayout,
            [
                .. monitors.Select(monitor => new MonitorZoneLayoutModel(
                    monitor.Bounds,
                    monitor.Build(result, defaultLayout.Zones, defaultLayout.Gap))),
            ]);

        return result;
    }

    // A monitor's own layout from here on; a Monitor line that can't be used starts one that's read but not used
    private static Section StartMonitor(ZonesConfigResultModel result, List<Section> monitors, string value, int line)
    {
        if (!TryReadMonitor(value, out Rectangle bounds))
        {
            result.AddProblem(
                line,
                $"{MonitorName}: four whole numbers in pixels: left, top, width, height, "
                + "as Windows places the monitor; "
                + $"{MonitorExample}. Its lines are not used.");

            return new Section(Rectangle.Empty);
        }

        if (monitors.FirstOrDefault(monitor => monitor.Bounds == bounds) is Section other)
        {
            result.AddProblem(
                line,
                $"{MonitorName}: there's one already for it, line {other.Line}; these lines are not used.");

            return new Section(bounds);
        }

        var section = new Section(bounds) { Line = line };
        monitors.Add(section);

        return section;
    }

    private static void ReadLayoutLine(ZonesConfigResultModel result, Section section, string value, int line)
    {
        if (section.Layout is not null)
        {
            result.AddProblem(line, $"{LayoutName}: there's one already, line {section.LayoutLine}, which is used.");
        }
        else if (TryReadLayout(value, out IReadOnlyList<ZoneModel> read, out string problem))
        {
            section.Layout = read;
            section.LayoutLine = line;
        }
        else
        {
            result.AddProblem(line, $"{LayoutName}: {problem}");
        }
    }

    private static void ReadZoneLine(ZonesConfigResultModel result, Section section, string value, int line)
    {
        if (section.Zones.Count == MaxZones)
        {
            result.AddProblem(line, $"{ZoneName}: there can be {MaxZones} zones at most.");
        }
        else if (TryReadZone(value, out ZoneModel? zone, out string problem))
        {
            section.Zones.Add(zone!);
        }
        else
        {
            result.AddProblem(line, $"{ZoneName}: {problem}");
        }
    }

    private static void ReadGapLine(ZonesConfigResultModel result, Section section, string value, int line)
    {
        if (section.Gap is not null)
        {
            result.AddProblem(line, $"{GapName}: there's one already, which is used.");
        }
        else if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int pixels)
            && pixels <= MaxGap)
        {
            section.Gap = pixels;
        }
        else
        {
            result.AddProblem(line, $"{GapName}: a whole number of pixels from 0 to {MaxGap}, not '{value}'.");
        }
    }

    // "left, top, width, height", whole pixels; left and top can be below 0, for a monitor left of or above the main
    // one
    private static bool TryReadMonitor(string value, out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        string[] parts = value.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
        List<int> numbers = [];

        foreach (string part in parts)
        {
            if (int.TryParse(part, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number))
            {
                numbers.Add(number);
            }
        }

        if (parts.Length != 4 || numbers.Count != 4 || numbers[2] <= 0 || numbers[3] <= 0)
        {
            return false;
        }

        bounds = new Rectangle(numbers[0], numbers[1], numbers[2], numbers[3]);

        return true;
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

    /// <summary>The lines of one layout: the default one, or a monitor's own.</summary>
    private sealed class Section(Rectangle bounds)
    {
        public Rectangle Bounds { get; } = bounds;

        /// <summary>The Monitor line it starts at; 0 for the default layout.</summary>
        public int Line { get; init; }

        public IReadOnlyList<ZoneModel>? Layout { get; set; }

        public int LayoutLine { get; set; }

        public List<ZoneModel> Zones { get; } = [];

        public int? Gap { get; set; }

        /// <summary>
        ///     Its zones, from Zone lines or a Layout line, and its gap; what it lacks comes from the fallback.
        /// </summary>
        public ZoneLayoutModel Build(ZonesConfigResultModel result, IReadOnlyList<ZoneModel> zones, int gap)
        {
            if (Zones.Count > 0 && Layout is not null)
            {
                result.AddProblem(
                    LayoutLine,
                    $"{LayoutName}: there are {ZoneName} lines too, and they're used instead.");
            }

            return new ZoneLayoutModel(Zones.Count > 0 ? Zones : Layout ?? zones, Gap ?? gap);
        }
    }
}
