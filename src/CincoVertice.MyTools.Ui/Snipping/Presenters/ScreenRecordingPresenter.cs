using System.Drawing;
using System.Globalization;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Views;

namespace CincoVertice.MyTools.Ui.Snipping.Presenters;

/// <summary>
///     One screen recording: records the area to a file of its own, shows how long while it does, and when stopped,
///     asks where to save the video; cancelled, the video is deleted.
/// </summary>
public sealed class ScreenRecordingPresenter
{
    private readonly IRecordingView _view;
    private readonly IScreenRecorder _recorder;
    private readonly IRecordingFileStore _files;
    private readonly TimeProvider _timeProvider;

    private string _temporaryPath = string.Empty;

    public ScreenRecordingPresenter(
        IRecordingView view,
        IScreenRecorder recorder,
        IRecordingFileStore files,
        TimeProvider timeProvider)
    {
        _view = view;
        _recorder = recorder;
        _files = files;
        _timeProvider = timeProvider;
    }

    public bool IsRecording { get; private set; }

    /// <param name="area">In screen pixels.</param>
    /// <returns>True when it records; false when it couldn't start, which the user was told.</returns>
    public async Task<bool> StartAsync(Rectangle area)
    {
        _temporaryPath = _files.CreateTemporaryPath();

        try
        {
            await _recorder.StartAsync(area, _temporaryPath);
        }
        catch (InvalidOperationException ex)
        {
            _files.Discard(_temporaryPath);
            _view.ShowMessage(ex.Message, isProblem: true);
            _view.CloseView();

            return false;
        }

        IsRecording = true;
        _view.StopRequested += OnStopRequested;
        _view.Ticked += OnTicked;
        _view.ShowRecording(area);
        _view.ShowElapsed(TimeSpan.Zero);

        return true;
    }

    /// <summary>Stops, then asks where to save the video. Stopping when it's stopped already does nothing.</summary>
    public async Task StopAsync()
    {
        if (!IsRecording)
        {
            return;
        }

        IsRecording = false;
        _view.Ticked -= OnTicked;
        _view.ShowSaving();

        try
        {
            await _recorder.StopAsync();
        }
        catch (InvalidOperationException ex)
        {
            _files.Discard(_temporaryPath);
            _view.ShowMessage(ex.Message, isProblem: true);
            _view.CloseView();

            return;
        }

        DateTimeOffset now = _timeProvider.GetLocalNow();
        string? path = _view.AskSavePath(
            $"Recording {now.ToString("yyyy-MM-dd HHmmss", CultureInfo.InvariantCulture)}.mp4");

        if (path is null)
        {
            _files.Discard(_temporaryPath);
        }
        else
        {
            try
            {
                _files.Keep(_temporaryPath, path);
                _view.ShowMessage($"Saved {Path.GetFileName(path)}.", isProblem: false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _view.ShowMessage($"Could not save {path}: {ex.Message}", isProblem: true);
                _files.Discard(_temporaryPath);
            }
        }

        _view.CloseView();
    }

    private async void OnStopRequested(object? sender, EventArgs e) => await StopAsync();

    private void OnTicked(object? sender, EventArgs e) => _view.ShowElapsed(_recorder.Elapsed);
}
