using CincoVertice.MyTools.Ui.Shell.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Shell;

public sealed class JsonFileUpdateSettingsStoreTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "UpdateSettingsTests", Guid.NewGuid().ToString("N"));

    private readonly JsonFileUpdateSettingsStore _store;

    public JsonFileUpdateSettingsStoreTests()
    {
        // In a folder that doesn't exist yet: saving creates it
        _store = new JsonFileUpdateSettingsStore(Path.Combine(_folder, "config", "updates.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Load_WithoutAFile_IsOff()
    {
        // Looking goes on the network, so only when asked for
        Assert.False(_store.LoadCheckAtStart());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SaveThenLoad_GivesWhatWasSaved(bool checkAtStart)
    {
        // Act
        _store.SaveCheckAtStart(checkAtStart);

        // Assert
        Assert.Equal(checkAtStart, _store.LoadCheckAtStart());
        Assert.Contains("\"checkAtStart\"", File.ReadAllText(_store.FilePath));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{ \"checkAtStart\": \"yes\" }")]
    [InlineData("null")]
    [InlineData("{}")]
    public void Load_ABrokenFile_IsOff(string text)
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(_store.FilePath)!);
        File.WriteAllText(_store.FilePath, text);

        // Act & Assert
        Assert.False(_store.LoadCheckAtStart());
    }
}
