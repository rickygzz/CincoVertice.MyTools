using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;
using CincoVertice.MyTools.Ui.Win.Zones.Services;

namespace CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;

/// <summary>
///     The names each config file can set, with what they mean and the values to offer, for the config editor's
///     suggestions. Tests check them against the readers: every name a new file is written with is here, and every
///     value offered is read without a problem.
/// </summary>
public static class ConfigSettingsHelp
{
    private static readonly IReadOnlyList<CodeSuggestionModel> _onOff =
    [
        new("true", "On"),
        new("false", "Off"),
    ];

    public static IReadOnlyList<ConfigSettingModel> Activity { get; } =
    [
        Number(ActivitySettingsReader.IntervalName, "Milliseconds between checks", 5000, "100 to 3,600,000"),
        Number(ActivitySettingsReader.IdleTicksName, "Checks to wait after you moved the mouse", 20, "0 to 100,000"),
        Number(ActivitySettingsReader.SpeedMinName, "Fastest move; higher is slower", 40, "20 to 100, up to SpeedMax"),
        Number(ActivitySettingsReader.SpeedMaxName, "Slowest move", 100, "20 to 100"),
        Switch(ActivitySettingsReader.MoveImmediatelyName, "Move as soon as turned on, or wait first", true),
    ];

    public static IReadOnlyList<ConfigSettingModel> Recording { get; } =
    [
        Number(RecordingSettingsReader.FramesPerSecondName, "How smooth the video is; more is larger", 20, "5 to 30"),
        new(
            RecordingSettingsReader.QualityName,
            "How sharp, against how large the file is (default Medium)",
            [new("Low", "Smaller files"), new("Medium", "Default"), new("High", "Sharper, larger files")]),
        Number(RecordingSettingsReader.MaxMinutesName, "Stops by itself after this long; 0 for never", 0, "0 to 240"),
        Number(RecordingSettingsReader.CountdownName, "Seconds counted down before it starts; 0 for none", 3, "0 to 10"),
        Switch(RecordingSettingsReader.ShowClicksName, "Show a ring where you click", true),
        Switch(RecordingSettingsReader.ShowKeysName, "Show the shortcuts you press", true),
        new(
            RecordingSettingsReader.FolderName,
            "Where the save dialog starts; empty for Videos",
            [new("%USERPROFILE%\\Videos", "Your Videos folder")]),
        Number(RecordingSettingsReader.GifFramesPerSecondName, "Frames a second kept in a GIF", 10, "2 to 20"),
        Number(RecordingSettingsReader.GifMaxWidthName, "A wider GIF is made smaller, in pixels", 800, "200 to 1920"),
    ];

    /// <param name="actions">The actions, with what each does.</param>
    /// <param name="zoneCount">The most zones a monitor has, for MoveToZone's value.</param>
    public static IReadOnlyList<ConfigSettingModel> Startup(IReadOnlyList<CodeSuggestionModel> actions, int zoneCount) =>
    [
        new(
            StartupConfigReader.RunName,
            "An action to run when Windows starts My Tools, in order: Run = Action, value",
            actions,
            IsRepeatable: true,
            ArgumentsOf: action => action.Equals("MoveToZone", StringComparison.OrdinalIgnoreCase)
                ? ZoneNumbers(zoneCount)
                : []),
    ];

    /// <param name="monitors">The monitors connected now, in physical pixels.</param>
    public static IReadOnlyList<ConfigSettingModel> Zones(IReadOnlyList<Rectangle> monitors) =>
    [
        new(
            ZonesConfigReader.LayoutName,
            "A ready layout; Zone lines instead set your own",
            [
                new("Columns 2", "2 columns as wide as each other"),
                new("Columns 3", "3 columns as wide as each other"),
                new("Columns 4", "4 columns as wide as each other"),
                new("Columns 25 50 25", "Columns of these widths, in percent"),
                new("Rows 2", "2 rows as tall as each other"),
                new("Rows 3", "3 rows as tall as each other"),
                new("Grid 2 x 2", "2 columns and 2 rows"),
                new("Grid 3 x 2", "3 columns and 2 rows"),
                new("Grid 3 x 3", "3 columns and 3 rows"),
            ]),
        new(
            ZonesConfigReader.ZoneName,
            "A zone: left, top, width and height, in percent of the work area",
            [new("0, 0, 50, 100", "The left half"), new("50, 0, 50, 100", "The right half")],
            IsRepeatable: true),
        new(
            ZonesConfigReader.GapName,
            "Pixels between zones and around them, 0 to 100 (default 8)",
            [new("8", "Default"), new("0", "No gap")]),
        new(
            ZonesConfigReader.MonitorName,
            "The lines after it are this monitor's own layout",
            [.. monitors.Select((bounds, i) => new CodeSuggestionModel(
                $"{bounds.Left}, {bounds.Top}, {bounds.Width}, {bounds.Height}",
                $"Monitor {i + 1} ({bounds.Width} x {bounds.Height}), connected now"))],
            IsRepeatable: true,
            StartsSection: true),
    ];

    public static IReadOnlyList<CodeSuggestionModel> ZoneNumbers(int zoneCount) =>
        [
            .. Enumerable.Range(1, Math.Max(0, zoneCount))
                .Select(zone => new CodeSuggestionModel(zone.ToString(), $"Zone {zone}")),
        ];

    private static ConfigSettingModel Number(string name, string meaning, int byDefault, string allowed) =>
        new(name, $"{meaning} ({allowed}; default {byDefault})", [new(byDefault.ToString(), "Default")]);

    private static ConfigSettingModel Switch(string name, string meaning, bool byDefault)
    {
        string text = byDefault ? "true" : "false";

        return new(
            name,
            $"{meaning} (default {text})",
            [.. _onOff.Select(value => value.Text == text ? value with { Detail = "Default" } : value)]);
    }
}
