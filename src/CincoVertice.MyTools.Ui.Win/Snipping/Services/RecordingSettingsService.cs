using System.Text;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Win.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>Owns recording.cfg: creates it with the defaults the first time, and reads it at each recording.</summary>
public sealed class RecordingSettingsService
{
    public RecordingSettingsService(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>config\recording.cfg next to the executable.</summary>
    public static string DefaultFilePath { get; } = Path.Combine(AppContext.BaseDirectory, "config", "recording.cfg");

    public string FilePath { get; }

    /// <summary>
    ///     Reads the settings, creating the file with the defaults when it doesn't exist. When the file can't be read
    ///     or written, the defaults are used and the reason is reported as a problem.
    /// </summary>
    public RecordingSettingsResultModel Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, CreateDefaultText());
            }

            return RecordingSettingsReader.Read(File.ReadAllText(FilePath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new RecordingSettingsResultModel(
                RecordingSettingsModel.Default,
                [$"Could not use {FilePath}, so the default settings are used: {ex.Message}"],
                []);
        }
    }

    /// <summary>The content of a new recording.cfg.</summary>
    public static string CreateDefaultText()
    {
        RecordingSettingsModel defaults = RecordingSettingsModel.Default;
        var sb = new StringBuilder();

        sb.AppendLine("# MyTools screen recording (the RecordScreen hotkey, Ctrl+Alt+R). Read at each recording.");
        sb.AppendLine("#");
        sb.AppendLine("# FramesPerSecond: how smooth the video is (5 to 30); more makes larger files.");
        sb.AppendLine("# Quality: Low, Medium or High: how sharp, against how large the file is.");
        sb.AppendLine("# MaxMinutes: stops by itself after this long (0 to 240); 0 for never.");
        sb.AppendLine("# Countdown: seconds counted down before it starts (0 to 10); 0 for none.");
        sb.AppendLine("# ShowClicks: true to show a ring where you click.");
        sb.AppendLine("# ShowKeys: true to show the shortcuts you press, such as Ctrl+Shift+P, in a corner.");
        sb.AppendLine("#   Only shortcuts with Ctrl, Alt or Win, and keys such as Enter, Esc and F1: never what");
        sb.AppendLine("#   you type.");
        sb.AppendLine("# Folder: where the save dialog starts; empty for your Videos folder.");
        sb.AppendLine("# GifFramesPerSecond: frames a second kept when you save as a GIF (2 to 20).");
        sb.AppendLine("# GifMaxWidth: a GIF wider than this is made smaller, in pixels (200 to 1920).");
        sb.AppendLine();
        sb.AppendLine($"{RecordingSettingsReader.FramesPerSecondName} = {defaults.FramesPerSecond}");
        sb.AppendLine($"{RecordingSettingsReader.QualityName} = {defaults.Quality}");
        sb.AppendLine($"{RecordingSettingsReader.MaxMinutesName} = {defaults.MaxMinutes}");
        sb.AppendLine($"{RecordingSettingsReader.CountdownName} = {defaults.CountdownSeconds}");
        sb.AppendLine($"{RecordingSettingsReader.ShowClicksName} = true");
        sb.AppendLine($"{RecordingSettingsReader.ShowKeysName} = true");
        sb.AppendLine($"{RecordingSettingsReader.FolderName} =");
        sb.AppendLine($"{RecordingSettingsReader.GifFramesPerSecondName} = {defaults.GifFramesPerSecond}");
        sb.AppendLine($"{RecordingSettingsReader.GifMaxWidthName} = {defaults.GifMaxWidth}");

        return sb.ToString();
    }
}
