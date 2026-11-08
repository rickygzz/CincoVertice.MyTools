using CincoVertice.MyTools.Ui.Shell.Interfaces;
using CincoVertice.MyTools.Ui.Shell.Models;
using CincoVertice.MyTools.Ui.Shell.Presenters;
using CincoVertice.MyTools.Ui.Shell.Services;
using CincoVertice.MyTools.Ui.Shell.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Shell;

public class UpdatesPresenterTests
{
    private static readonly Version Current = new(0, 0, 6);

    private static readonly AppReleaseModel Newer = new(new Version(0, 0, 7), "https://example.com/v0.0.7");

    private readonly FakeUpdatesView _view = new();
    private readonly FakeReleaseSource _releases = new();
    private readonly FakeUpdateSettingsStore _settings = new();
    private readonly UpdatesPresenter _presenter;

    public UpdatesPresenterTests()
    {
        _presenter = new UpdatesPresenter(_view, _releases, _settings, Current, TimeSpan.Zero);
    }

    [Fact]
    public void AboutRequested_ShowsTheVersionAndTheReleasesPage()
    {
        // Act
        _view.RequestAbout();

        // Assert
        Assert.Equal((Current, FakeReleaseSource.PageUrl), _view.About);
        Assert.Equal(0, _releases.Asked);
    }

    [Fact]
    public void CheckForUpdatesRequested_WithANewerVersion_AsksAndOpensItsPage()
    {
        // Arrange
        _releases.Latest = Newer;
        _view.OpensRelease = true;

        // Act
        _view.RequestCheck();

        // Assert
        Assert.Equal((Newer, Current), _view.AskedAbout);
        Assert.Equal([Newer.PageUrl], _view.OpenedPages);
    }

    [Fact]
    public void CheckForUpdatesRequested_WhenTheUserSaysNo_OpensNothing()
    {
        // Arrange
        _releases.Latest = Newer;
        _view.OpensRelease = false;

        // Act
        _view.RequestCheck();

        // Assert
        Assert.NotNull(_view.AskedAbout);
        Assert.Empty(_view.OpenedPages);
    }

    [Theory]
    [InlineData(0, 0, 6)]
    [InlineData(0, 0, 5)]
    public void CheckForUpdatesRequested_WithThisOrAnOlderVersion_SaysItsTheLatest(int major, int minor, int patch)
    {
        // Arrange: older, as when running a build that isn't published yet
        _releases.Latest = new AppReleaseModel(new Version(major, minor, patch), "https://x");

        // Act
        _view.RequestCheck();

        // Assert
        Assert.Equal(Current, _view.UpToDate);
        Assert.Null(_view.AskedAbout);
    }

    [Fact]
    public void CheckForUpdatesRequested_WithNoReleaseYet_SaysItsTheLatest()
    {
        // Act
        _view.RequestCheck();

        // Assert
        Assert.Equal(Current, _view.UpToDate);
    }

    [Fact]
    public void CheckForUpdatesRequested_WhenItFails_SaysWhy()
    {
        // Arrange
        _releases.Error = new UpdateCheckException("GitHub can't be reached: no network.");

        // Act
        _view.RequestCheck();

        // Assert
        Assert.Equal("GitHub can't be reached: no network.", _view.Failure);
    }

    [Fact]
    public async Task CheckNow_WhileStillAsking_DoesntAskAgain()
    {
        // Arrange: the first answer hasn't come yet
        var answer = new TaskCompletionSource<AppReleaseModel?>();
        _releases.Pending = answer.Task;
        Task first = _presenter.CheckNowAsync();

        // Act
        await _presenter.CheckNowAsync();
        answer.SetResult(null);
        await first;

        // Assert
        Assert.Equal(1, _releases.Asked);
        Assert.Equal(Current, _view.UpToDate);
    }

    [Fact]
    public async Task CheckNow_AfterAFailure_CanAskAgain()
    {
        // Arrange
        _releases.Error = new UpdateCheckException("Down.");
        await _presenter.CheckNowAsync();
        _releases.Error = null;
        _releases.Latest = Newer;

        // Act
        await _presenter.CheckNowAsync();

        // Assert
        Assert.Equal(2, _releases.Asked);
        Assert.NotNull(_view.AskedAbout);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Start_ShowsTheSetting(bool checkAtStart)
    {
        // Arrange
        _settings.CheckAtStart = checkAtStart;

        // Act
        await _presenter.Start();

        // Assert
        Assert.Equal(checkAtStart, _view.ChecksAtStart);
    }

    [Fact]
    public async Task Start_WhenItsOff_DoesntAsk()
    {
        // Arrange: looking goes on the network, so only when it's turned on
        _releases.Latest = Newer;

        // Act
        await _presenter.Start();

        // Assert
        Assert.Equal(0, _releases.Asked);
        Assert.Empty(_view.Notified);
    }

    [Fact]
    public async Task Start_WhenItsOn_WithANewerVersion_NotifiesOnly()
    {
        // Arrange
        _settings.CheckAtStart = true;
        _releases.Latest = Newer;

        // Act
        await _presenter.Start();

        // Assert: a notification, no dialog, since nobody asked
        Assert.Equal([Newer], _view.Notified);
        Assert.Null(_view.AskedAbout);
        Assert.Empty(_view.OpenedPages);
    }

    [Fact]
    public async Task Start_WhenItsOn_WithTheLatestVersion_SaysNothing()
    {
        // Arrange
        _settings.CheckAtStart = true;
        _releases.Latest = new AppReleaseModel(Current, "https://x");

        // Act
        await _presenter.Start();

        // Assert
        Assert.Equal(1, _releases.Asked);
        Assert.Empty(_view.Notified);
        Assert.Null(_view.UpToDate);
    }

    [Fact]
    public async Task Start_WhenItsOn_AndItFails_SaysNothing()
    {
        // Arrange
        _settings.CheckAtStart = true;
        _releases.Error = new UpdateCheckException("Down.");

        // Act
        await _presenter.Start();

        // Assert
        Assert.Empty(_view.Notified);
        Assert.Null(_view.Failure);
    }

    [Fact]
    public async Task Start_TurnedOffWhileItWaits_DoesntAsk()
    {
        // Arrange
        var presenter = new UpdatesPresenter(_view, _releases, _settings, Current, TimeSpan.FromMilliseconds(50));
        _settings.CheckAtStart = true;
        Task start = presenter.Start();

        // Act
        _view.RequestCheckAtStart(false);
        await start;

        // Assert
        Assert.Equal(0, _releases.Asked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CheckAtStartRequested_SavesItAndShowsIt(bool enabled)
    {
        // Arrange
        _settings.CheckAtStart = !enabled;

        // Act
        _view.RequestCheckAtStart(enabled);

        // Assert
        Assert.Equal(enabled, _settings.CheckAtStart);
        Assert.Equal(enabled, _view.ChecksAtStart);
    }

    [Fact]
    public void CheckAtStartRequested_WhenItCantBeSaved_ShowsItDidntChange()
    {
        // Arrange
        _settings.FailsToSave = true;

        // Act
        _view.RequestCheckAtStart(true);

        // Assert
        Assert.False(_view.ChecksAtStart);
    }

    private sealed class FakeReleaseSource : IReleaseSource
    {
        public const string PageUrl = "https://example.com/releases";

        public string ReleasesPageUrl => PageUrl;

        public AppReleaseModel? Latest { get; set; }

        public UpdateCheckException? Error { get; set; }

        /// <summary>An answer still to come, instead of <see cref="Latest"/>.</summary>
        public Task<AppReleaseModel?>? Pending { get; set; }

        public int Asked { get; private set; }

        public Task<AppReleaseModel?> GetLatestAsync(CancellationToken cancellationToken = default)
        {
            Asked++;

            if (Error is not null)
            {
                return Task.FromException<AppReleaseModel?>(Error);
            }

            return Pending ?? Task.FromResult(Latest);
        }
    }

    private sealed class FakeUpdateSettingsStore : IUpdateSettingsStore
    {
        public bool CheckAtStart { get; set; }

        public bool FailsToSave { get; set; }

        public bool LoadCheckAtStart() => CheckAtStart;

        public void SaveCheckAtStart(bool checkAtStart)
        {
            if (!FailsToSave)
            {
                CheckAtStart = checkAtStart;
            }
        }
    }

    private sealed class FakeUpdatesView : IUpdatesView
    {
        public event EventHandler? AboutRequested;

        public event EventHandler? CheckForUpdatesRequested;

        public event EventHandler<bool>? CheckAtStartRequested;

        public bool ChecksAtStart { get; private set; }

        public (Version Version, string Url)? About { get; private set; }

        public bool OpensRelease { get; set; }

        public (AppReleaseModel Release, Version Current)? AskedAbout { get; private set; }

        public Version? UpToDate { get; private set; }

        public string? Failure { get; private set; }

        public List<AppReleaseModel> Notified { get; } = [];

        public List<string> OpenedPages { get; } = [];

        public void RequestAbout() => AboutRequested?.Invoke(this, EventArgs.Empty);

        public void RequestCheck() => CheckForUpdatesRequested?.Invoke(this, EventArgs.Empty);

        public void RequestCheckAtStart(bool enabled) => CheckAtStartRequested?.Invoke(this, enabled);

        public void ShowCheckAtStart(bool isEnabled) => ChecksAtStart = isEnabled;

        public void ShowAbout(Version version, string releasesPageUrl) => About = (version, releasesPageUrl);

        public bool AskToOpenRelease(AppReleaseModel release, Version current)
        {
            AskedAbout = (release, current);

            return OpensRelease;
        }

        public void ShowUpToDate(Version current) => UpToDate = current;

        public void ShowUpdateCheckFailed(string reason) => Failure = reason;

        public void NotifyUpdateAvailable(AppReleaseModel release) => Notified.Add(release);

        public void OpenPage(string url) => OpenedPages.Add(url);
    }
}
