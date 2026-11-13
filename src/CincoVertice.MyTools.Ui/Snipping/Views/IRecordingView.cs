using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Views;

/// <summary>
///     While the screen is recorded: a frame around the area, and a bar with how long it has recorded and a way to
///     stop. Neither is in the video.
/// </summary>
public interface IRecordingView
{
    /// <summary>Raised when the user wants the recording stopped.</summary>
    event EventHandler? StopRequested;

    /// <summary>Raised a few times a second while recording, to show the time.</summary>
    event EventHandler? Ticked;

    /// <summary>Shows the frame around the area and the bar.</summary>
    /// <param name="area">In screen pixels.</param>
    void ShowRecording(Rectangle area);

    void ShowElapsed(TimeSpan elapsed);

    /// <summary>Shows that the video is being finished, which takes a moment.</summary>
    void ShowSaving();

    /// <summary>Asks where to save the video.</summary>
    /// <returns>The chosen path; null when the user cancels.</returns>
    string? AskSavePath(string suggestedFileName);

    /// <summary>Tells the user something without waiting for them.</summary>
    void ShowMessage(string message, bool isProblem);

    void CloseView();
}
