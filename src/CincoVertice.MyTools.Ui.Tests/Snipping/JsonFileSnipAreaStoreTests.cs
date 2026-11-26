using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Snipping;

public sealed class JsonFileSnipAreaStoreTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "SnipAreaTests", Guid.NewGuid().ToString("N"));

    private readonly JsonFileSnipAreaStore _store;

    public JsonFileSnipAreaStoreTests()
    {
        // In a folder that doesn't exist yet: Save creates it
        _store = new JsonFileSnipAreaStore(Path.Combine(_folder, "config", "snipping.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Load_WithoutAFile_GivesNothing()
    {
        Assert.Null(_store.Load());
    }

    [Fact]
    public void SaveThenLoad_GivesTheArea_LeftOfAndAboveTheMainMonitorToo()
    {
        // Act
        _store.Save(new Rectangle(-1920, -100, 640, 360));

        // Assert
        Assert.Equal(new Rectangle(-1920, -100, 640, 360), _store.Load());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{ "lastArea": { "x": 1, "y": 2, "width": 0, "height": 5 } }""")]
    [InlineData("""{ "lastArea": { "x": 1, "y": 2, "width": "wide", "height": 5 } }""")]
    public void Load_ABrokenFile_OrAnAreaWithoutASize_GivesNothing(string json)
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(_store.FilePath)!);
        File.WriteAllText(_store.FilePath, json);

        // Act / Assert
        Assert.Null(_store.Load());
    }

    [Fact]
    public void Load_ReadsTheFileAsWritten()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(_store.FilePath)!);
        File.WriteAllText(
            _store.FilePath,
            """{ "lastArea": { "x": 100, "y": 80, "width": 640, "height": 360 } }""");

        // Act / Assert
        Assert.Equal(new Rectangle(100, 80, 640, 360), _store.Load());
    }
}
