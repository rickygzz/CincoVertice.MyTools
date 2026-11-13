using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Interfaces;

/// <summary>Records an area of the screen to a video file, as it changes, until stopped.</summary>
public interface IScreenRecorder
{
    /// <summary>How long it has recorded.</summary>
    TimeSpan Elapsed { get; }

    /// <param name="area">The area, in screen pixels.</param>
    /// <param name="path">The file to write the video to.</param>
    /// <exception cref="InvalidOperationException">
    ///     When it can't record; the message says why, for the user.
    /// </exception>
    Task StartAsync(Rectangle area, string path);

    /// <summary>Stops, and finishes writing the file.</summary>
    /// <exception cref="InvalidOperationException">
    ///     When the video couldn't be written; the message says why.
    /// </exception>
    Task StopAsync();
}
