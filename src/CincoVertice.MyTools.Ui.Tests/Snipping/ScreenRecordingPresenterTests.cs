using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Presenters;
using CincoVertice.MyTools.Ui.Snipping.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Snipping;

public class ScreenRecordingPresenterTests
{
    private static readonly Rectangle _area = new(100, 50, 640, 360);

    private static readonly RecordingSettingsModel _noCountdown =
        RecordingSettingsModel.Default with { CountdownSeconds = 0 };

    private static readonly RecordingSettingsModel _threeSeconds =
        RecordingSettingsModel.Default with { CountdownSeconds = 3 };

    private readonly FakeView _view = new();
    private readonly FakeRecorder _recorder = new();
    private readonly FakeFiles _files = new();
    private readonly ManualTime _time = new(new DateTimeOffset(2026, 11, 12, 18, 30, 5, TimeSpan.Zero));
    private readonly ScreenRecordingPresenter _presenter;

    public ScreenRecordingPresenterTests()
    {
        _presenter = new ScreenRecordingPresenter(_view, _recorder, _files, _time);
    }

    [Fact]
    public async Task Start_RecordsTheAreaToANewFile_WithTheSettings_AndShowsItIsRecording()
    {
        // Act
        bool started = await _presenter.StartAsync(_area, _noCountdown);

        // Assert
        Assert.True(started);
        Assert.True(_presenter.IsRecording);
        Assert.Equal((_area, "temp1.mp4", _noCountdown), _recorder.Started);
        Assert.Equal(_area, _view.Area);
        Assert.Equal([0], _view.Countdown);
        Assert.Equal(TimeSpan.Zero, _view.Elapsed);
    }

    [Fact]
    public async Task Start_CountsDown_ASecondAtATime_ThenRecords()
    {
        // Act
        Task<bool> starting = _presenter.StartAsync(_area, _threeSeconds);

        // Assert: the frame and the bar show while it counts, and nothing is recorded yet
        Assert.Equal(_area, _view.Area);
        Assert.Equal([3], _view.Countdown);
        Assert.Null(_recorder.Started);

        _time.Fire();
        _time.Fire();
        Assert.Equal([3, 2, 1], _view.Countdown);

        _time.Fire();
        Assert.True(await starting);
        Assert.Equal([3, 2, 1, 0], _view.Countdown);
        Assert.NotNull(_recorder.Started);
    }

    [Fact]
    public async Task Stop_DuringTheCountdown_RecordsNothing()
    {
        // Arrange
        Task<bool> starting = _presenter.StartAsync(_area, _threeSeconds);

        // Act
        await _presenter.StopAsync();
        _time.Fire();

        // Assert
        Assert.False(await starting);
        Assert.Null(_recorder.Started);
        Assert.Empty(_files.Kept);
        Assert.Null(_view.SuggestedName);
        Assert.True(_view.IsClosed);
    }

    [Fact]
    public async Task WhileRecording_EachTick_ShowsHowLong()
    {
        // Arrange
        await _presenter.StartAsync(_area, _noCountdown);
        _recorder.Elapsed = TimeSpan.FromSeconds(12);

        // Act
        _view.Tick();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(12), _view.Elapsed);
    }

    [Fact]
    public async Task Pause_PausesTheRecorder_AndAgain_Resumes()
    {
        // Arrange
        await _presenter.StartAsync(_area, _noCountdown);

        // Act and assert
        _view.RequestPause();
        Assert.Equal(["pause"], _recorder.Calls);
        Assert.True(_view.IsPaused);

        _view.RequestPause();
        Assert.Equal(["pause", "resume"], _recorder.Calls);
        Assert.False(_view.IsPaused);
    }

    [Fact]
    public async Task AtTheMostMinutes_ItStopsByItself()
    {
        // Arrange
        await _presenter.StartAsync(_area, _noCountdown with { MaxMinutes = 2 });

        // Act
        _recorder.Elapsed = TimeSpan.FromSeconds(119);
        _view.Tick();
        bool stoppedBefore = _recorder.IsStopped;
        _recorder.Elapsed = TimeSpan.FromMinutes(2);
        _view.Tick();
        await Task.Yield();

        // Assert
        Assert.False(stoppedBefore);
        Assert.True(_recorder.IsStopped);
    }

    [Fact]
    public async Task Stop_FinishesTheVideo_ThenSavesItWhereTheUserSays_InTheFolderOfTheSettings()
    {
        // Arrange
        var settings = _noCountdown with { Folder = @"D:\Clips" };
        await _presenter.StartAsync(_area, settings);
        _view.SavePath = @"D:\Clips\demo.gif";

        // Act
        await _presenter.StopAsync();

        // Assert
        Assert.True(_recorder.IsStopped);
        Assert.True(_view.IsSaving);
        Assert.Equal(("Recording 2026-11-12 183005.mp4", @"D:\Clips"), (_view.SuggestedName, _view.Folder));
        Assert.Equal(("temp1.mp4", @"D:\Clips\demo.gif", settings), Assert.Single(_files.Kept));
        Assert.Equal(@"D:\Clips\demo.gif", _view.Saved);
        Assert.True(_view.IsClosed);
        Assert.False(_presenter.IsRecording);
    }

    [Fact]
    public async Task Stop_ThenCancellingTheSave_DeletesTheVideo()
    {
        // Arrange
        await _presenter.StartAsync(_area, _noCountdown);

        // Act
        _view.RequestStop();
        await Task.Yield();

        // Assert
        Assert.Equal(["temp1.mp4"], _files.Discarded);
        Assert.Empty(_files.Kept);
        Assert.True(_view.IsClosed);
    }

    [Fact]
    public async Task Stop_Twice_StopsOnce()
    {
        // Arrange
        await _presenter.StartAsync(_area, _noCountdown);

        // Act
        await _presenter.StopAsync();
        await _presenter.StopAsync();

        // Assert
        Assert.Equal(1, _recorder.Stops);
    }

    [Fact]
    public async Task Start_WhenWindowsCantRecord_SaysWhy_AndLeavesNoFile()
    {
        // Arrange
        _recorder.StartProblem = "Windows can't record video here.";

        // Act
        bool started = await _presenter.StartAsync(_area, _noCountdown);

        // Assert
        Assert.False(started);
        Assert.False(_presenter.IsRecording);
        Assert.Equal("Windows can't record video here.", _view.Problem);
        Assert.Equal(["temp1.mp4"], _files.Discarded);
        Assert.True(_view.IsClosed);
    }

    [Fact]
    public async Task Stop_WhenTheVideoCantBeSaved_SaysWhy_AndDeletesIt()
    {
        // Arrange
        await _presenter.StartAsync(_area, _noCountdown);
        _view.SavePath = @"C:\Locked\demo.mp4";
        _files.KeepProblem = "Access denied.";

        // Act
        await _presenter.StopAsync();

        // Assert
        Assert.Equal(@"Could not save C:\Locked\demo.mp4: Access denied.", _view.Problem);
        Assert.Equal(["temp1.mp4"], _files.Discarded);
        Assert.Null(_view.Saved);
    }

    /// <summary>A clock whose timers fire when the test says, all at once.</summary>
    private sealed class ManualTime(DateTimeOffset now) : TimeProvider
    {
        private readonly List<(TimerCallback Callback, object? State)> _timers = [];

        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _timers.Add((callback, state));

            return new NoTimer();
        }

        public void Fire()
        {
            List<(TimerCallback Callback, object? State)> due = [.. _timers];
            _timers.Clear();

            foreach ((TimerCallback callback, object? state) in due)
            {
                callback(state);
            }
        }

        private sealed class NoTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class FakeRecorder : IScreenRecorder
    {
        public TimeSpan Elapsed { get; set; }

        public string? StartProblem { get; set; }

        public (Rectangle Area, string Path, RecordingSettingsModel Settings)? Started { get; private set; }

        public List<string> Calls { get; } = [];

        public bool IsStopped { get; private set; }

        public int Stops { get; private set; }

        public Task StartAsync(Rectangle area, string path, RecordingSettingsModel settings)
        {
            if (StartProblem is not null)
            {
                throw new InvalidOperationException(StartProblem);
            }

            Started = (area, path, settings);

            return Task.CompletedTask;
        }

        public void Pause() => Calls.Add("pause");

        public void Resume() => Calls.Add("resume");

        public Task StopAsync()
        {
            IsStopped = true;
            Stops++;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeFiles : IRecordingFileStore
    {
        private int _count;

        public string? KeepProblem { get; set; }

        public List<(string From, string To, RecordingSettingsModel Settings)> Kept { get; } = [];

        public List<string> Discarded { get; } = [];

        public string CreateTemporaryPath() => $"temp{++_count}.mp4";

        public Task KeepAsync(string temporaryPath, string path, RecordingSettingsModel settings)
        {
            if (KeepProblem is not null)
            {
                throw new IOException(KeepProblem);
            }

            Kept.Add((temporaryPath, path, settings));

            return Task.CompletedTask;
        }

        public void Discard(string temporaryPath) => Discarded.Add(temporaryPath);
    }

    private sealed class FakeView : IRecordingView
    {
        public event EventHandler? StopRequested;

        public event EventHandler? PauseRequested;

        public event EventHandler? Ticked;

        public Rectangle? Area { get; private set; }

        public List<int> Countdown { get; } = [];

        public TimeSpan? Elapsed { get; private set; }

        public bool IsPaused { get; private set; }

        public bool IsSaving { get; private set; }

        public string? SavePath { get; set; }

        public string? SuggestedName { get; private set; }

        public string? Folder { get; private set; }

        public string? Saved { get; private set; }

        public string? Problem { get; private set; }

        public bool IsClosed { get; private set; }

        public void RequestStop() => StopRequested?.Invoke(this, EventArgs.Empty);

        public void RequestPause() => PauseRequested?.Invoke(this, EventArgs.Empty);

        public void Tick() => Ticked?.Invoke(this, EventArgs.Empty);

        public void ShowRecording(Rectangle area) => Area = area;

        public void ShowCountdown(int seconds) => Countdown.Add(seconds);

        public void ShowElapsed(TimeSpan elapsed) => Elapsed = elapsed;

        public void ShowPaused(bool isPaused) => IsPaused = isPaused;

        public void ShowSaving() => IsSaving = true;

        public string? AskSavePath(string suggestedFileName, string folder)
        {
            SuggestedName = suggestedFileName;
            Folder = folder;

            return SavePath;
        }

        public void ShowSaved(string path) => Saved = path;

        public void ShowProblem(string message) => Problem = message;

        public void CloseView() => IsClosed = true;
    }
}
