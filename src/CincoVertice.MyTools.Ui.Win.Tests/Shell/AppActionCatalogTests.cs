using CincoVertice.Common.Application.Scripting;
using CincoVertice.MyTools.Ui.Win.Scripting.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Shell;

public class AppActionCatalogTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "AppActionCatalogTests_" + Guid.NewGuid());

    private readonly AppActionCatalog _catalog;

    public AppActionCatalogTests()
    {
        Directory.CreateDirectory(_folder);
        _catalog = new AppActionCatalog(
            [new FakeAction("ToggleActivity")],
            new ScriptEngine(),
            new TrayNotificationService(),
            _folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GetActions_ListsBuiltInActionsThenScripts()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_folder, "Search.mts"), "");

        // Act
        var actions = _catalog.GetActions();

        // Assert
        Assert.Equal(["ToggleActivity", "Search"], actions.Select(a => a.Name));
        Assert.IsType<ScriptAction>(actions[1]);
    }

    [Fact]
    public void GetActions_LeavesOutScriptsThatAreOnlyForInclude()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_folder, "Search.mts"), "");
        File.WriteAllText(Path.Combine(_folder, "_Common.mts"), "");

        // Act
        var actions = _catalog.GetActions();

        // Assert
        Assert.Equal(["ToggleActivity", "Search"], actions.Select(a => a.Name));
    }

    [Fact]
    public void GetActions_ReadsTheScriptsFolderAgain()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_folder, "Old.mts"), "");
        var before = _catalog.GetActions();
        File.Delete(Path.Combine(_folder, "Old.mts"));
        File.WriteAllText(Path.Combine(_folder, "New.mts"), "");

        // Act
        var after = _catalog.GetActions();

        // Assert
        Assert.Equal(["ToggleActivity", "Old"], before.Select(a => a.Name));
        Assert.Equal(["ToggleActivity", "New"], after.Select(a => a.Name));
    }

    [Fact]
    public void GetActions_KeepsTheSameActionForAScript()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_folder, "Search.mts"), "");

        // Act
        var first = _catalog.GetActions();
        var second = _catalog.GetActions();

        // Assert
        Assert.Same(first[1], second[1]);
    }

    private sealed class FakeAction(string name) : IAppAction
    {
        public string Name => name;

        public string? DefaultShortcut => null;

        public Task ExecuteAsync() => Task.CompletedTask;
    }
}
