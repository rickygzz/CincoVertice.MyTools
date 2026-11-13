using System.Globalization;
using CincoVertice.Common.Utils.Config;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     Reads recording.cfg with the config lexer: one "Name = Value" per line, # for comments. Every line that can't be
///     used is reported as a problem, and a setting without a usable line keeps its default.
/// </summary>
public static class RecordingSettingsReader
{
    public const string FramesPerSecondName = "FramesPerSecond";
    public const string QualityName = "Quality";
    public const string MaxMinutesName = "MaxMinutes";
    public const string CountdownName = "Countdown";
    public const string ShowClicksName = "ShowClicks";
    public const string ShowKeysName = "ShowKeys";
    public const string FolderName = "Folder";
    public const string GifFramesPerSecondName = "GifFramesPerSecond";
    public const string GifMaxWidthName = "GifMaxWidth";

    private static readonly Dictionary<string, (int Min, int Max)> _numbers = new(StringComparer.OrdinalIgnoreCase)
    {
        // What copying the screen keeps up with
        [FramesPerSecondName] = (5, 30),
        [MaxMinutesName] = (0, 240),
        [CountdownName] = (0, 10),
        [GifFramesPerSecondName] = (2, 20),
        [GifMaxWidthName] = (200, 1920),
    };

    private static readonly string[] _switches = [ShowClicksName, ShowKeysName];

    public static RecordingSettingsResultModel Read(string text)
    {
        List<string> problems = [];
        List<ConfigLineProblemModel> lineProblems = [];

        void AddProblem(int line, string message)
        {
            problems.Add($"Line {line}: {message}");
            lineProblems.Add(new ConfigLineProblemModel(line, message));
        }

        ConfigLexer lexer = new(text);
        ConfigItems items = lexer.Items;
        RecordingSettingsModel defaults = RecordingSettingsModel.Default;
        Dictionary<string, int> numbers = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, bool> switches = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        RecordingQuality quality = defaults.Quality;
        string folder = defaults.Folder;

        for (int i = 0; i < items.KeysCount; i++)
        {
            string name = items.GetKeyName(i);
            int line = items.GetLine(i);

            if (items.GetValueType(i) == ConfigTokenType.ErrorQuotedStringNotClosed)
            {
                AddProblem(line, $"{name}: the [\" is never closed, so the rest of the file is ignored.");

                continue;
            }

            string value = WithoutComment(items.GetValue(i));
            bool isSwitch = _switches.Contains(name, StringComparer.OrdinalIgnoreCase);
            bool isKnown = _numbers.ContainsKey(name)
                || isSwitch
                || name.Equals(QualityName, StringComparison.OrdinalIgnoreCase)
                || name.Equals(FolderName, StringComparison.OrdinalIgnoreCase);

            if (!isKnown)
            {
                string all = string.Join(", ", [.. _numbers.Keys, .. _switches, QualityName, FolderName]);
                AddProblem(line, $"{name}: unknown setting. Settings: {all}.");
            }
            else if (!seen.Add(name))
            {
                AddProblem(line, $"{name}: set more than once; the first line is used.");
            }
            else if (_numbers.TryGetValue(name, out var range))
            {
                if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int number)
                    && number >= range.Min
                    && number <= range.Max)
                {
                    numbers[name] = number;
                }
                else
                {
                    AddProblem(
                        line,
                        $"{name}: must be a whole number from {range.Min} to {range.Max}; the default is used.");
                }
            }
            else if (isSwitch)
            {
                if (bool.TryParse(value, out bool on))
                {
                    switches[name] = on;
                }
                else
                {
                    AddProblem(line, $"{name}: must be true or false; the default is used.");
                }
            }
            else if (name.Equals(QualityName, StringComparison.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse(value, ignoreCase: true, out quality) || !Enum.IsDefined(quality))
                {
                    quality = defaults.Quality;
                    AddProblem(line, $"{name}: must be Low, Medium or High; the default is used.");
                }
            }
            else if (value.Length > 0 && !Directory.Exists(Environment.ExpandEnvironmentVariables(value)))
            {
                AddProblem(line, $"{name}: there's no folder '{value}'; the Videos folder is used.");
            }
            else
            {
                folder = Environment.ExpandEnvironmentVariables(value);
            }
        }

        foreach (var (number, badLine) in lexer.BadLines)
        {
            AddProblem(number, $"'{badLine}' is not 'Name = Value'.");
        }

        var settings = new RecordingSettingsModel(
            numbers.GetValueOrDefault(FramesPerSecondName, defaults.FramesPerSecond),
            quality,
            numbers.GetValueOrDefault(MaxMinutesName, defaults.MaxMinutes),
            numbers.GetValueOrDefault(CountdownName, defaults.CountdownSeconds),
            switches.GetValueOrDefault(ShowClicksName, defaults.ShowClicks),
            switches.GetValueOrDefault(ShowKeysName, defaults.ShowKeys),
            folder,
            numbers.GetValueOrDefault(GifFramesPerSecondName, defaults.GifFramesPerSecond),
            numbers.GetValueOrDefault(GifMaxWidthName, defaults.GifMaxWidth));

        return new RecordingSettingsResultModel(settings, problems, lineProblems);
    }

    // A folder can have a # in its name, but no setting has " #" in its value
    private static string WithoutComment(string value)
    {
        int comment = value.IndexOf(" #", StringComparison.Ordinal);

        return (comment >= 0 ? value[..comment] : value).Trim();
    }
}
