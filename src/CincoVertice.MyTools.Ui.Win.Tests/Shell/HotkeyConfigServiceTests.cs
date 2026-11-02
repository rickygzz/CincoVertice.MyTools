using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Shell;

public class HotkeyConfigServiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "HotkeyConfigServiceTests_" + Guid.NewGuid());

    private readonly IAppAction[] _actions =
    [
        new FakeAction("ToggleActivity", "Ctrl+Alt+M"),
        new FakeAction("CheckFolder", null),
    ];

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Load_WhenFileIsMissing_CreatesItWithDefaults()
    {
        // Arrange
        var service = new HotkeyConfigService(Path.Combine(_folder, "MyTools", "hotkeys.cfg"));

        // Act
        HotkeyConfigResultModel result = service.Load(_actions);

        // Assert
        Assert.True(File.Exists(service.FilePath));
        Assert.Empty(result.Problems);
        Assert.Equal("ToggleActivity", Assert.Single(result.Bindings).ActionName);
    }

    [Fact]
    public void Load_WhenFileExists_ReadsIt()
    {
        // Arrange
        Directory.CreateDirectory(_folder);
        var service = new HotkeyConfigService(Path.Combine(_folder, "hotkeys.cfg"));
        File.WriteAllText(service.FilePath, "CheckFolder = Ctrl+Shift+C\n");

        // Act
        HotkeyConfigResultModel result = service.Load(_actions);

        // Assert
        Assert.Equal("CheckFolder", Assert.Single(result.Bindings).ActionName);
    }

    [Fact]
    public void Load_WhenFileCantBeRead_UsesDefaultsAndReportsWhy()
    {
        // Arrange: a folder with the file's name can't be read as a file
        string path = Path.Combine(_folder, "hotkeys.cfg");
        Directory.CreateDirectory(path);
        var service = new HotkeyConfigService(path);

        // Act
        HotkeyConfigResultModel result = service.Load(_actions);

        // Assert
        Assert.Equal("ToggleActivity", Assert.Single(result.Bindings).ActionName);
        Assert.StartsWith("Could not use", Assert.Single(result.Problems));
    }

    [Fact]
    public void CreateDefaultText_HasLinePerActionAndCommentsOutActionsWithoutDefault()
    {
        // Act
        string text = HotkeyConfigService.CreateDefaultText(_actions);

        // Assert
        Assert.Contains("ToggleActivity = Ctrl+Alt+M", text);
        Assert.Contains("# CheckFolder = Ctrl+Alt+", text);
        Assert.Empty(HotkeyConfigReader.Read(text, ["ToggleActivity", "CheckFolder"]).Problems);
    }

    private sealed class FakeAction(string name, string? defaultShortcut) : IAppAction
    {
        public string Name => name;

        public string? DefaultShortcut => defaultShortcut;

        public Task ExecuteAsync() => Task.CompletedTask;
    }
}
