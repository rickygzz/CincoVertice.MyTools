using System.Text;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Win.Shell.Models;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

/// <summary>
///     Owns activity.cfg: creates it with the default settings the first time, and reads it.
/// </summary>
public sealed class ActivitySettingsService
{
    public ActivitySettingsService(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>config\activity.cfg next to the executable.</summary>
    public static string DefaultFilePath { get; } = Path.Combine(
        AppContext.BaseDirectory,
        "config",
        "activity.cfg");

    public string FilePath { get; }

    /// <summary>
    ///     Reads the settings, creating the file with the defaults when it doesn't exist. When the file can't be read
    ///     or written, the defaults are used and the reason is reported as a problem.
    /// </summary>
    public ActivitySettingsResultModel Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, CreateDefaultText());
            }

            return ActivitySettingsReader.Read(File.ReadAllText(FilePath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ActivitySettingsResultModel(
                MouseMoveSettings.Default,
                [$"Could not use {FilePath}, so the default settings are used: {ex.Message}"]);
        }
    }

    /// <summary>The content of a new activity.cfg.</summary>
    public static string CreateDefaultText()
    {
        MouseMoveSettings defaults = MouseMoveSettings.Default;
        var sb = new StringBuilder();

        sb.AppendLine("# MyTools activity (the ToggleActivity hotkey, Ctrl+Alt+M).");
        sb.AppendLine("# Changes apply the next time you turn it on.");
        sb.AppendLine("#");
        sb.AppendLine("# MoveMouse: true to move the mouse while you are away, so you look present: no screen saver,");
        sb.AppendLine("#   no lock after the time without input, no Away in chat programs.");
        sb.AppendLine("# KeepAwake: true to keep the computer from sleeping and the screen on, even with the screen");
        sb.AppendLine("#   locked, where the mouse can't be moved. Nothing touches the mouse or the keyboard.");
        sb.AppendLine("#   One of the two must be true.");
        sb.AppendLine("#");
        sb.AppendLine("# How the mouse is moved:");
        sb.AppendLine("# Interval: milliseconds between checks of the mouse (100 to 3600000).");
        sb.AppendLine("# IdleTicksBeforeMove: checks to wait after you move the mouse before it moves again.");
        sb.AppendLine("#   The wait is about Interval x IdleTicksBeforeMove: 5000 x 20 is 100 seconds.");
        sb.AppendLine("# SpeedMin, SpeedMax: how slow a move is, picked at random between the two (20 to 100).");
        sb.AppendLine("#   Higher is slower, so SpeedMin is the fastest. SpeedMin can't be above SpeedMax.");
        sb.AppendLine("# MoveImmediately: true to move the mouse as soon as you turn it on; false to wait first,");
        sb.AppendLine("#   as after you move the mouse.");
        sb.AppendLine();
        sb.AppendLine($"{ActivitySettingsReader.MoveMouseName} = true");
        sb.AppendLine($"{ActivitySettingsReader.KeepAwakeName} = true");
        sb.AppendLine($"{ActivitySettingsReader.IntervalName} = {defaults.Interval}");
        sb.AppendLine($"{ActivitySettingsReader.IdleTicksName} = {defaults.IdleTicksBeforeMove}");
        sb.AppendLine($"{ActivitySettingsReader.SpeedMinName} = {defaults.SpeedMin}");
        sb.AppendLine($"{ActivitySettingsReader.SpeedMaxName} = {defaults.SpeedMax}");
        sb.AppendLine(
            $"{ActivitySettingsReader.MoveImmediatelyName} = {defaults.MoveImmediately.ToString().ToLowerInvariant()}");

        return sb.ToString();
    }
}
