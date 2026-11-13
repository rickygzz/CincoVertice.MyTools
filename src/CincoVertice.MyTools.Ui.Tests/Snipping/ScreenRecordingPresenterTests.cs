using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Presenters;
using CincoVertice.MyTools.Ui.Snipping.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Snipping;

public class ScreenRecordingPresenterTests
{
    private static readonly Rectangle _area = new(100, 50, 640, 360);

    private readonly FakeView _view = new();
    private readonly FakeRecorder _recorder = new();
    private readonly FakeFiles _files = new();
    private readonly ScreenRecordingPresenter _presenter;

    public ScreenRecordingPresenterTests()
    {
        _presenter = new ScreenRecordingPresenter(
            _view,
            _recorder,
            _files,
            new FixedTime(new DateTimeOffset(2026, 11, 12, 18, 30, 5, TimeSpan.Zero)));
    }

    [Fact]
    public async Task Start_RecordsTheAreaToANewFile_AndShowsItIsRecording()
    {
        // Act
        bool started = await _presenter.StartAsync(_area);

        // Assert
        Assert.True(started);
        Assert.True(_presenter.IsRecording);
        Assert.Equal((_area, "temp1.mp4"), _recorder.Started);
        Assert.Equal(_area, _view.Area);
        Assert.Equal(TimeSpan.Zero, _view.Elapsed);
    }

    [Fact]
    public async Task WhileRecording_EachTick_ShowsHowLong()
    {
        // Arrange
        await _presenter.StartAsync(_area);
        _recorder.Elapsed = TimeSpan.FromSeconds(12);

        // Act
        _view.Tick();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(12), _view.Elapsed);
    }

    [Fact]
    public async Task Stop_FinishesTheVideo_ThenSavesItWhereTheUserSays()
    {
        // Arrange
        await _presenter.StartAsync(_area);
        _view.SavePath = @"C:\Videos\demo.mp4";

        // Act
        await _presenter.StopAsync();

        // Assert
        Assert.True(_recorder.IsStopped);
        Assert.True(_view.IsSaving);
        Assert.Equal("Recording 2026-11-12 183005.mp4", _view.SuggestedName);
        Assert.Equal(("temp1.mp4", @"C:\Videos\demo.mp4"), Assert.Single(_files.Kept));
        Assert.Equal(("Saved demo.mp4.", false), _view.Message);
        Assert.True(_view.IsClosed);
        Assert.False(_presenter.IsRecording);
    }

    [Fact]
    public async Task Stop_ThenCancellingTheSave_DeletesTheVideo()
    {
        // Arrange
        await _presenter.StartAsync(_area);

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
        await _presenter.StartAsync(_area);

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
        bool started = await _presenter.StartAsync(_area);

        // Assert
        Assert.False(started);
        Assert.Equal(("Windows can't record video here.", true), _view.Message);
        Assert.Equal(["temp1.mp4"], _files.Discarded);
        Assert.True(_view.IsClosed);
    }

    [Fact]
    public async Task Stop_WhenTheVideoCantBeMoved_SaysWhy_AndDeletesIt()
    {
        // Arrange
        await _presenter.StartAsync(_area);
        _view.SavePath = @"C:\Locked\demo.mp4";
        _files.KeepProblem = "Access denied.";

        // Act
        await _presenter.StopAsync();

        // Assert
        Assert.Equal((@"Could not save C:\Locked\demo.mp4: Access denied.", true), _view.Message);
        Assert.Equal(["temp1.mp4"], _files.Discarded);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class FakeRecorder : IScreenRecorder
    {
        public TimeSpan Elapsed { get; set; }

        public string? StartProblem { get; set; }

        public (Rectangle Area, string Path)? Started { get; private set; }

        public bool IsStopped { get; private set; }

        public int Stops { get; private set; }

        public Task StartAsync(Rectangle area, string path)
        {
            if (StartProblem is not null)
            {
                throw new InvalidOperationException(StartProblem);
            }

            Started = (area, path);

            return Task.CompletedTask;
        }

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

        public List<(string From, string To)> Kept { get; } = [];

        public List<string> Discarded { get; } = [];

        public string CreateTemporaryPath() => $"temp{++_count}.mp4";

        public void Keep(string temporaryPath, string path)
        {
            if (KeepProblem is not null)
            {
                throw new IOException(KeepProblem);
            }

            Kept.Add((temporaryPath, path));
        }

        public void Discard(string temporaryPath) => Discarded.Add(temporaryPath);
    }

    private sealed class FakeView : IRecordingView
    {
        public event EventHandler? StopRequested;

        public event EventHandler? Ticked;

        public Rectangle? Area { get; private set; }

        public TimeSpan? Elapsed { get; private set; }

        public bool IsSaving { get; private set; }

        public string? SavePath { get; set; }

        public string? SuggestedName { get; private set; }

        public (string Text, bool IsProblem)? Message { get; private set; }

        public bool IsClosed { get; private set; }

        public void RequestStop() => StopRequested?.Invoke(this, EventArgs.Empty);

        public void Tick() => Ticked?.Invoke(this, EventArgs.Empty);

        public void ShowRecording(Rectangle area) => Area = area;

        public void ShowElapsed(TimeSpan elapsed) => Elapsed = elapsed;

        public void ShowSaving() => IsSaving = true;

        public string? AskSavePath(string suggestedFileName)
        {
            SuggestedName = suggestedFileName;

            return SavePath;
        }

        public void ShowMessage(string message, bool isProblem) => Message = (message, isProblem);

        public void CloseView() => IsClosed = true;
    }
}
