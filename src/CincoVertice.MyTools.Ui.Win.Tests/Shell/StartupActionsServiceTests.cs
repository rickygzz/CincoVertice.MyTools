using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Shell;

public sealed class StartupActionsServiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "StartupActionsTests_" + Guid.NewGuid());
    private readonly List<string> _runs = [];
    private readonly List<TrayNotificationModel> _notifications = [];
    private readonly TrayNotificationService _notificationService = new();

    public StartupActionsServiceTests()
    {
        _notificationService.Requested += (_, notification) => _notifications.Add(notification);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private string ConfigPath => Path.Combine(_folder, "startup.cfg");

    private StartupActionsService CreateService(params IAppAction[] actions) => new(
        new StartupConfigService(ConfigPath),
        () => actions,
        _notificationService);

    private void WriteConfig(string text)
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(ConfigPath, text);
    }

    [Fact]
    public async Task RunAsync_RunsEachInOrder_EachAfterTheOneBeforeEnds()
    {
        // Arrange
        WriteConfig("Run = Second\nRun = First, a");
        var gate = new TaskCompletionSource();
        var service = CreateService(
            new RecordingAction("First", _runs),
            new RecordingAction("Second", _runs, gate.Task));

        // Act
        Task running = service.RunAsync();

        // Assert: First waits for Second
        Assert.Equal(["Second"], _runs);
        gate.SetResult();
        await running;
        Assert.Equal(["Second", "First a"], _runs);
        Assert.Empty(_notifications);
    }

    [Fact]
    public async Task RunAsync_TheFirstTime_CreatesTheFile_AndRunsNothing()
    {
        // Act
        await CreateService(new RecordingAction("First", _runs)).RunAsync();

        // Assert
        Assert.True(File.Exists(ConfigPath));
        Assert.Empty(_runs);
        Assert.Empty(_notifications);
    }

    [Fact]
    public async Task RunAsync_ReportsTheLinesThatCantBeUsed_AndRunsTheOthers()
    {
        // Arrange
        WriteConfig("Run = Nope\nRun = First");

        // Act
        await CreateService(new RecordingAction("First", _runs)).RunAsync();

        // Assert
        Assert.Equal(["First"], _runs);
        var notification = Assert.Single(_notifications);
        Assert.Equal(StartupActionsService.ProblemsTitle, notification.Title);
        Assert.StartsWith("Line 1: Nope: unknown action.", notification.Text);
        Assert.Equal(ToolTipIcon.Warning, notification.Icon);
    }

    [Fact]
    public async Task RunAsync_AnActionThatFails_IsReported_AndTheNextOneRuns()
    {
        // Arrange
        WriteConfig("Run = Broken\nRun = First");

        // Act
        await CreateService(new FailingAction("Broken"), new RecordingAction("First", _runs)).RunAsync();

        // Assert
        Assert.Equal(["First"], _runs);
        Assert.Equal("Broken failed: Boom", Assert.Single(_notifications).Text);
    }

    [Fact]
    public async Task RunAsync_AValueForAnActionThatTakesNone_IsReported()
    {
        // Arrange
        WriteConfig("Run = Broken, a");

        // Act
        await CreateService(new FailingAction("Broken")).RunAsync();

        // Assert
        Assert.Equal("Broken: takes no value (a).", Assert.Single(_notifications).Text);
    }

    [Fact]
    public void SummarizeProblems_ShowsTheFirstThree_AndWhereToFixThem()
    {
        // Act
        string summary = StartupActionsService.SummarizeProblems(["P1", "P2", "P3", "P4", "P5"]);

        // Assert
        Assert.Equal(
            string.Join(
                Environment.NewLine,
                "P1",
                "P2",
                "P3",
                "...and 2 more.",
                "Fix them in startup.cfg: tray menu, Edit hotkeys."),
            summary);
    }

    private sealed class RecordingAction(string name, List<string> runs, Task? until = null) : IArgumentAppAction
    {
        public string Name => name;

        public string? DefaultShortcut => null;

        public Task ExecuteAsync()
        {
            runs.Add(name);

            return until ?? Task.CompletedTask;
        }

        public Task ExecuteAsync(string argument)
        {
            runs.Add($"{name} {argument}");

            return until ?? Task.CompletedTask;
        }
    }

    private sealed class FailingAction(string name) : IAppAction
    {
        public string Name => name;

        public string? DefaultShortcut => null;

        public Task ExecuteAsync() => throw new InvalidOperationException("Boom");
    }
}
