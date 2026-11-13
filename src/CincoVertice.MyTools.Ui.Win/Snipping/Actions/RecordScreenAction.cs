using CincoVertice.MyTools.Ui.Snipping.Presenters;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Actions;

/// <summary>
///     Records a video of an area of the screen: the first press freezes the screen to drag the area, as for a snip,
///     and starts recording; the next press, or the bar's Stop, stops it and asks where to save the MP4.
/// </summary>
public sealed class RecordScreenAction : IAppAction
{
    private readonly ScreenSnipper _snipper;
    private readonly SnipEditorWindowService _editorWindow;
    private readonly TrayNotificationService _notifications;

    // The recording going on; null when there's none
    private ScreenRecordingPresenter? _recording;

    public RecordScreenAction(
        ScreenSnipper snipper,
        SnipEditorWindowService editorWindow,
        TrayNotificationService notifications)
    {
        _snipper = snipper;
        _editorWindow = editorWindow;
        _notifications = notifications;
    }

    public string Name => "RecordScreen";

    public string? DefaultShortcut => "Ctrl+Alt+R";

    public async Task ExecuteAsync()
    {
        if (_recording is { IsRecording: true } recording)
        {
            await recording.StopAsync();

            return;
        }

        Rectangle? area;

        using (_editorWindow.KeepOutOfSnip())
        {
            area = await _snipper.SelectAreaAsync();
        }

        if (area is not Rectangle chosen)
        {
            return;
        }

        // A form per recording, which disposes itself when closed
        var bar = new RecordingBarForm(_notifications);
        _recording = new ScreenRecordingPresenter(
            bar,
            new ScreenRecorder(),
            new RecordingFileStore(),
            TimeProvider.System);
        await _recording.StartAsync(chosen);
    }
}
