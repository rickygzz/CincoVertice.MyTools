using CincoVertice.MyTools.Ui.Shell.Interfaces;
using CincoVertice.MyTools.Ui.Shell.Models;
using CincoVertice.MyTools.Ui.Shell.Presenters;
using CincoVertice.MyTools.Ui.Shell.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Shell;

public class TrayPresenterTests
{
    private readonly FakeTrayView _view = new();
    private readonly FakeHotkeyRegistrar _hotkeys = new();
    private readonly TrayPresenter _presenter;

    public TrayPresenterTests()
    {
        _presenter = new TrayPresenter(_view, _hotkeys);
    }

    [Fact]
    public void Start_RegistersQuietlyShowsScriptsAndWatchesFiles()
    {
        // Arrange
        _hotkeys.Scripts = [new ScriptMenuItemModel("GoogleSearch", () => Task.CompletedTask)];

        // Act
        _presenter.Start();

        // Assert
        Assert.Equal(1, _hotkeys.RegisterCount);
        Assert.True(_hotkeys.IsWatching);
        Assert.Equal(["GoogleSearch"], _view.Scripts.Select(s => s.Name));
        Assert.Empty(_view.Problems);
        Assert.Empty(_view.Notifications);
    }

    [Fact]
    public void Start_WithProblems_ListsThemAndWarns()
    {
        // Arrange
        _hotkeys.Problems = ["Foo: unknown action."];

        // Act
        _presenter.Start();

        // Assert
        Assert.Equal(["Foo: unknown action."], _view.Problems);
        var notification = Assert.Single(_view.Notifications);
        Assert.Equal((TrayPresenter.HotkeyProblemsTitle, true), (notification.Title, notification.IsWarning));
    }

    [Fact]
    public void ReloadHotkeysRequested_RegistersAgainAndAnnouncesIt()
    {
        // Arrange
        _presenter.Start();
        _hotkeys.Scripts = [new ScriptMenuItemModel("NewScript", () => Task.CompletedTask)];

        // Act
        _view.RequestReloadHotkeys();

        // Assert
        Assert.Equal(2, _hotkeys.RegisterCount);
        Assert.Equal(["NewScript"], _view.Scripts.Select(s => s.Name));
        Assert.Equal(
            (TrayPresenter.AppTitle, "Hotkeys reloaded.", false),
            Assert.Single(_view.Notifications));
    }

    [Fact]
    public void FilesChanged_ReloadsAndClearsProblemsThatAreFixed()
    {
        // Arrange
        _hotkeys.Problems = ["Foo: unknown action."];
        _presenter.Start();
        _hotkeys.Problems = [];

        // Act
        _hotkeys.ChangeFiles();

        // Assert
        Assert.Equal(2, _hotkeys.RegisterCount);
        Assert.Empty(_view.Problems);
        Assert.Equal("Hotkeys reloaded.", _view.Notifications[^1].Text);
    }

    [Theory]
    [InlineData(1, new[] { "P1", "Tray menu: Hotkey problems, Edit hotkeys." })]
    [InlineData(3, new[] { "P1", "P2", "P3", "Tray menu: Hotkey problems, Edit hotkeys." })]
    [InlineData(5, new[] { "P1", "P2", "P3", "...and 2 more.", "Tray menu: Hotkey problems, Edit hotkeys." })]
    public void SummarizeProblems_ShowsTheFirstThreeAndHowManyMore(int count, string[] expectedLines)
    {
        // Arrange
        List<string> problems = [.. Enumerable.Range(1, count).Select(i => $"P{i}")];

        // Act
        string summary = TrayPresenter.SummarizeProblems(problems);

        // Assert
        Assert.Equal(string.Join(Environment.NewLine, expectedLines), summary);
    }

    private sealed class FakeTrayView : ITrayView
    {
        public event EventHandler? ReloadHotkeysRequested;

        public IReadOnlyList<ScriptMenuItemModel> Scripts { get; private set; } = [];

        public IReadOnlyList<string> Problems { get; private set; } = [];

        public List<(string Title, string Text, bool IsWarning)> Notifications { get; } = [];

        public void ShowScripts(IReadOnlyList<ScriptMenuItemModel> scripts) => Scripts = scripts;

        public void ShowHotkeyProblems(IReadOnlyList<string> problems) => Problems = problems;

        public void ShowNotification(string title, string text, bool isWarning) =>
            Notifications.Add((title, text, isWarning));

        public void RequestReloadHotkeys() => ReloadHotkeysRequested?.Invoke(this, EventArgs.Empty);
    }

    private sealed class FakeHotkeyRegistrar : IHotkeyRegistrar
    {
        public event EventHandler? FilesChanged;

        public IReadOnlyList<string> Problems { get; set; } = [];

        public IReadOnlyList<ScriptMenuItemModel> Scripts { get; set; } = [];

        public int RegisterCount { get; private set; }

        public bool IsWatching { get; private set; }

        public HotkeyRegistrationModel Register()
        {
            RegisterCount++;

            return new HotkeyRegistrationModel(Problems, Scripts);
        }

        public void WatchFiles() => IsWatching = true;

        public void ChangeFiles() => FilesChanged?.Invoke(this, EventArgs.Empty);
    }
}
