using System.Drawing;
using System.Globalization;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Views;

namespace CincoVertice.MyTools.Ui.Snipping.Presenters;

/// <summary>
///     One screen recording: counts down, records the area to a file of its own, shows how long while it does, can be
///     paused, and stops by itself at the most minutes the settings allow. When stopped, it asks where to save the
///     video, as an MP4 or a GIF; cancelled, the video is deleted. Stopped during the countdown, nothing is recorded.
/// </summary>
public sealed class ScreenRecordingPresenter
{
    private static readonly TimeSpan _second = TimeSpan.FromSeconds(1);

    private readonly IRecordingView _view;
    private readonly IScreenRecorder _recorder;
    private readonly IRecordingFileStore _files;
    private readonly TimeProvider _timeProvider;

    private RecordingSettingsModel _settings = RecordingSettingsModel.Default;
    private string _temporaryPath = string.Empty;
    private bool _isCountingDown;
    private bool _isPaused;

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

    /// <summary>Whether it's counting down or recording: stopping it means something.</summary>
    public bool IsRecording { get; private set; }

    /// <param name="area">In screen pixels.</param>
    /// <param name="settings">From recording.cfg.</param>
    /// <returns>
    ///     True when it records; false when it couldn't start, which the user was told, or was stopped first.
    /// </returns>
    public async Task<bool> StartAsync(Rectangle area, RecordingSettingsModel settings)
    {
        _settings = settings;
        IsRecording = true;
        _view.StopRequested += OnStopRequested;
        _view.PauseRequested += OnPauseRequested;
        _view.ShowRecording(area);

        _isCountingDown = true;

        for (int left = settings.CountdownSeconds; left > 0 && IsRecording; left--)
        {
            _view.ShowCountdown(left);
            await Task.Delay(_second, _timeProvider);
        }

        _isCountingDown = false;

        // Stopped during the countdown
        if (!IsRecording)
        {
            _view.CloseView();

            return false;
        }

        _view.ShowCountdown(0);
        _temporaryPath = _files.CreateTemporaryPath();

        try
        {
            await _recorder.StartAsync(area, _temporaryPath, settings);
        }
        catch (InvalidOperationException ex)
        {
            IsRecording = false;
            _files.Discard(_temporaryPath);
            _view.ShowProblem(ex.Message);
            _view.CloseView();

            return false;
        }

        _view.Ticked += OnTicked;
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

        // The countdown ends at its next second, and closes the view itself
        if (_isCountingDown)
        {
            return;
        }

        _view.Ticked -= OnTicked;
        _view.ShowSaving();

        try
        {
            await _recorder.StopAsync();
        }
        catch (InvalidOperationException ex)
        {
            _files.Discard(_temporaryPath);
            _view.ShowProblem(ex.Message);
            _view.CloseView();

            return;
        }

        DateTimeOffset now = _timeProvider.GetLocalNow();
        string name = $"Recording {now.ToString("yyyy-MM-dd HHmmss", CultureInfo.InvariantCulture)}.mp4";
        string? path = _view.AskSavePath(name, _settings.Folder);

        if (path is null)
        {
            _files.Discard(_temporaryPath);
            _view.CloseView();

            return;
        }

        try
        {
            await _files.KeepAsync(_temporaryPath, path, _settings);
            _view.ShowSaved(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _view.ShowProblem($"Could not save {path}: {ex.Message}");
            _files.Discard(_temporaryPath);
        }

        _view.CloseView();
    }

    private async void OnStopRequested(object? sender, EventArgs e) => await StopAsync();

    private void OnPauseRequested(object? sender, EventArgs e)
    {
        if (!IsRecording || _isCountingDown)
        {
            return;
        }

        _isPaused = !_isPaused;

        if (_isPaused)
        {
            _recorder.Pause();
        }
        else
        {
            _recorder.Resume();
        }

        _view.ShowPaused(_isPaused);
    }

    private async void OnTicked(object? sender, EventArgs e)
    {
        TimeSpan elapsed = _recorder.Elapsed;
        _view.ShowElapsed(elapsed);

        if (_settings.MaxMinutes > 0 && elapsed >= TimeSpan.FromMinutes(_settings.MaxMinutes))
        {
            await StopAsync();
        }
    }
}
