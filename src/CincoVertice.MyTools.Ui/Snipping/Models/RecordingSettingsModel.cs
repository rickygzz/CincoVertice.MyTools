namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>How the screen is recorded, from recording.cfg.</summary>
/// <param name="FramesPerSecond">How many times a second the area is copied into the video.</param>
/// <param name="Quality">How sharp the video is, against how large its file is.</param>
/// <param name="MaxMinutes">After how long a recording stops by itself; 0 for never.</param>
/// <param name="CountdownSeconds">Seconds counted down before it starts; 0 for none.</param>
/// <param name="ShowClicks">True to show a ring where the mouse is clicked.</param>
/// <param name="ShowKeys">True to show the shortcuts pressed, e.g. Ctrl+Shift+P, in a corner.</param>
/// <param name="Folder">Where the save dialog starts; empty for the Videos folder.</param>
/// <param name="GifFramesPerSecond">How many frames a second a GIF keeps of the video.</param>
/// <param name="GifMaxWidth">How wide a GIF can be, in pixels; a wider video is made smaller.</param>
public sealed record RecordingSettingsModel(
    int FramesPerSecond,
    RecordingQuality Quality,
    int MaxMinutes,
    int CountdownSeconds,
    bool ShowClicks,
    bool ShowKeys,
    string Folder,
    int GifFramesPerSecond,
    int GifMaxWidth)
{
    public static RecordingSettingsModel Default { get; } =
        new(20, RecordingQuality.Medium, 0, 3, true, true, "", 10, 800);
}
