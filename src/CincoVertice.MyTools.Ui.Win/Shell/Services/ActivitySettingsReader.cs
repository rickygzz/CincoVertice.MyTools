using System.Globalization;
using CincoVertice.Common.Utils.Config;
using CincoVertice.MyTools.Ui.Win.Shell.Models;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     Reads activity.cfg with the config lexer: one "Name = Number" per line, # for comments. Every line that can't
///     be used is reported as a problem, and a setting without a usable line keeps its default. So the settings it
///     returns are always safe to give to the mouse move timer, where a bad value would end the app.
/// </summary>
public static class ActivitySettingsReader
{
    public const string IntervalName = "Interval";
    public const string IdleTicksName = "IdleTicksBeforeMove";
    public const string SpeedMinName = "SpeedMin";
    public const string SpeedMaxName = "SpeedMax";

    private static readonly Dictionary<string, (int Min, int Max)> _ranges = new(StringComparer.OrdinalIgnoreCase)
    {
        // Up to an hour; a move takes a while, so a much shorter interval would only skip ticks
        [IntervalName] = (100, 3_600_000),
        [IdleTicksName] = (0, 100_000),

        // What InputSimulator.IncrementalMouseMove accepts
        [SpeedMinName] = (20, 100),
        [SpeedMaxName] = (20, 100),
    };

    public static ActivitySettingsResultModel Read(string text)
    {
        List<string> problems = [];
        ConfigItems items = new ConfigLexer(text).Items;
        Dictionary<string, int> values = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < items.KeysCount; i++)
        {
            string name = items.GetKeyName(i);

            if (!_ranges.TryGetValue(name, out var range))
            {
                problems.Add($"{name}: unknown setting. Settings: {string.Join(", ", _ranges.Keys)}.");
            }
            else if (!seen.Add(name))
            {
                problems.Add($"{name}: set more than once; the first line is used.");
            }
            else if (TryReadNumber(items.GetValue(i), range, out int number))
            {
                values[name] = number;
            }
            else
            {
                problems.Add($"{name}: must be a whole number from {range.Min} to {range.Max}; the default is used.");
            }
        }

        foreach (var (number, line) in ConfigLineChecker.FindUnreadLines(text, items))
        {
            problems.Add($"Line {number}: '{line}' is not 'Name = Number'.");
        }

        ActivitySettingsModel defaults = ActivitySettingsModel.Default;
        int speedMin = values.GetValueOrDefault(SpeedMinName, defaults.SpeedMin);
        int speedMax = values.GetValueOrDefault(SpeedMaxName, defaults.SpeedMax);

        if (speedMin > speedMax)
        {
            problems.Add($"{SpeedMinName} ({speedMin}) is above {SpeedMaxName} ({speedMax}); the defaults are used.");
            speedMin = defaults.SpeedMin;
            speedMax = defaults.SpeedMax;
        }

        var settings = new ActivitySettingsModel(
            values.GetValueOrDefault(IntervalName, defaults.Interval),
            values.GetValueOrDefault(IdleTicksName, defaults.IdleTicksBeforeMove),
            speedMin,
            speedMax);

        return new ActivitySettingsResultModel(settings, problems);
    }

    // A # after the number is a comment; the lexer keeps it in the value
    private static bool TryReadNumber(string value, (int Min, int Max) range, out int number)
    {
        string text = value.Split('#')[0].Trim();

        return int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number)
            && number >= range.Min
            && number <= range.Max;
    }
}
