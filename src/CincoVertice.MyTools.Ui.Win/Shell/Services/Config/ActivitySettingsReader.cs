using System.Globalization;
using CincoVertice.Common.Utils.Config;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Win.Shell.Models;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

/// <summary>
///     Reads activity.cfg with the config lexer: one "Name = Value" per line, # for comments. Every line that can't
///     be used is reported as a problem, and a setting without a usable line keeps its default. So the settings it
///     returns are always safe to give to the mouse move timer, where a bad value would end the app.
/// </summary>
public static class ActivitySettingsReader
{
    public const string IntervalName = "Interval";
    public const string IdleTicksName = "IdleTicksBeforeMove";
    public const string SpeedMinName = "SpeedMin";
    public const string SpeedMaxName = "SpeedMax";
    public const string MoveImmediatelyName = "MoveImmediately";

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
        ConfigLexer lexer = new(text);
        ConfigItems items = lexer.Items;
        Dictionary<string, int> values = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        MouseMoveSettings defaults = MouseMoveSettings.Default;
        bool moveImmediately = defaults.MoveImmediately;

        for (int i = 0; i < items.KeysCount; i++)
        {
            string name = items.GetKeyName(i);

            if (items.GetValueType(i) == ConfigTokenType.ErrorQuotedStringNotClosed)
            {
                problems.Add($"{name}: the [\" is never closed, so the rest of the file is ignored.");

                continue;
            }

            string value = WithoutComment(items.GetValue(i));
            bool isMoveImmediately = name.Equals(MoveImmediatelyName, StringComparison.OrdinalIgnoreCase);

            if (!isMoveImmediately && !_ranges.ContainsKey(name))
            {
                string settings = string.Join(", ", [.. _ranges.Keys, MoveImmediatelyName]);
                problems.Add($"{name}: unknown setting. Settings: {settings}.");
            }
            else if (!seen.Add(name))
            {
                problems.Add($"{name}: set more than once; the first line is used.");
            }
            else if (isMoveImmediately)
            {
                if (!bool.TryParse(value, out moveImmediately))
                {
                    moveImmediately = defaults.MoveImmediately;
                    problems.Add($"{name}: must be true or false; the default is used.");
                }
            }
            else if (TryReadNumber(value, _ranges[name], out int number))
            {
                values[name] = number;
            }
            else
            {
                var (Min, Max) = _ranges[name];
                problems.Add($"{name}: must be a whole number from {Min} to {Max}; the default is used.");
            }
        }

        foreach (var (number, line) in lexer.BadLines)
        {
            problems.Add($"Line {number}: '{line}' is not 'Name = Value'.");
        }

        int speedMin = values.GetValueOrDefault(SpeedMinName, defaults.SpeedMin);
        int speedMax = values.GetValueOrDefault(SpeedMaxName, defaults.SpeedMax);

        if (speedMin > speedMax)
        {
            problems.Add($"{SpeedMinName} ({speedMin}) is above {SpeedMaxName} ({speedMax}); the defaults are used.");
            speedMin = defaults.SpeedMin;
            speedMax = defaults.SpeedMax;
        }

        var result = new MouseMoveSettings(
            values.GetValueOrDefault(IntervalName, defaults.Interval),
            values.GetValueOrDefault(IdleTicksName, defaults.IdleTicksBeforeMove),
            speedMin,
            speedMax,
            moveImmediately);

        return new ActivitySettingsResultModel(result, problems);
    }

    // A # after the value is a comment; the lexer keeps it in the value
    private static string WithoutComment(string value) => value.Split('#')[0].Trim();

    private static bool TryReadNumber(string text, (int Min, int Max) range, out int number) =>
        int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number)
        && number >= range.Min
        && number <= range.Max;
}
