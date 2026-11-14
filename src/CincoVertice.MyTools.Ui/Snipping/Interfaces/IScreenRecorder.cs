using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Snipping.Interfaces;

/// <summary>Records an area of the screen to an MP4 file, as it changes, until stopped.</summary>
public interface IScreenRecorder
{
    /// <summary>How long it has recorded, pauses left out.</summary>
    TimeSpan Elapsed { get; }

    /// <param name="area">The area, in screen pixels.</param>
    /// <param name="path">The file to write the video to.</param>
    /// <param name="settings">Frame rate, quality, and whether to show clicks and shortcuts.</param>
    /// <exception cref="InvalidOperationException">
    ///     When it can't record; the message says why, for the user.
    /// </exception>
    Task StartAsync(Rectangle area, string path, RecordingSettingsModel settings);

    /// <summary>Stops adding to the video until <see cref="Resume"/>; the time paused isn't in it.</summary>
    void Pause();

    void Resume();

    /// <summary>Stops, and finishes writing the file.</summary>
    /// <exception cref="InvalidOperationException">
    ///     When the video couldn't be written; the message says why.
    /// </exception>
    Task StopAsync();
}
