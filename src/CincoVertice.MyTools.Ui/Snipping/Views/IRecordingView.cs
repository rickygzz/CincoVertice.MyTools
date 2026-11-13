using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Views;

/// <summary>
///     While the screen is recorded: a frame around the area, and a bar with how long it has recorded and ways to
///     pause and stop. Neither is in the video.
/// </summary>
public interface IRecordingView
{
    /// <summary>Raised when the user wants the recording stopped.</summary>
    event EventHandler? StopRequested;

    /// <summary>Raised when the user wants the recording paused, or going on again.</summary>
    event EventHandler? PauseRequested;

    /// <summary>Raised a few times a second, to show the time.</summary>
    event EventHandler? Ticked;

    /// <summary>Shows the frame around the area and the bar.</summary>
    /// <param name="area">In screen pixels.</param>
    void ShowRecording(Rectangle area);

    /// <summary>Shows the seconds left before recording starts; 0 when it has.</summary>
    void ShowCountdown(int seconds);

    void ShowElapsed(TimeSpan elapsed);

    /// <summary>Shows whether the recording is paused.</summary>
    void ShowPaused(bool isPaused);

    /// <summary>Shows that the video is being finished, which takes a moment.</summary>
    void ShowSaving();

    /// <summary>Asks where to save the video, as an MP4 or a GIF.</summary>
    /// <param name="suggestedFileName">The name offered first.</param>
    /// <param name="folder">Where the dialog starts; empty for the Videos folder.</param>
    /// <returns>The chosen path; null when the user cancels.</returns>
    string? AskSavePath(string suggestedFileName, string folder);

    /// <summary>Tells the user the video was saved; a click on the message shows it in its folder.</summary>
    void ShowSaved(string path);

    /// <summary>Tells the user something went wrong, without waiting for them.</summary>
    void ShowProblem(string message);

    void CloseView();
}
