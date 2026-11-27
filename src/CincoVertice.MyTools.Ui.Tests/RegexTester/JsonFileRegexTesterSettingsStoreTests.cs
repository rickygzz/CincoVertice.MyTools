using CincoVertice.MyTools.Ui.RegexTester.Models;
using CincoVertice.MyTools.Ui.RegexTester.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.RegexTester;

public sealed class JsonFileRegexTesterSettingsStoreTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "RegexTesterSettingsTests", Guid.NewGuid().ToString("N"));

    private readonly JsonFileRegexTesterSettingsStore _store;

    public JsonFileRegexTesterSettingsStoreTests()
    {
        // In a folder that doesn't exist yet: Save creates it
        _store = new JsonFileRegexTesterSettingsStore(Path.Combine(_folder, "config", "regextester.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void SaveThenLoad_GivesItBack()
    {
        // Arrange
        var input = new RegexInputModel(@"(\d+)\s*""", "line 1\nline \"2\"", "$1", true, false, true);

        // Act
        _store.Save(input);

        // Assert
        Assert.Equal(input, _store.Load());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1]")]
    public void Load_WithoutAFile_OrABrokenOne_GivesNothing(string json)
    {
        // Arrange
        RegexInputModel? missing = _store.Load();
        Directory.CreateDirectory(Path.GetDirectoryName(_store.FilePath)!);
        File.WriteAllText(_store.FilePath, json);

        // Act / Assert
        Assert.Null(missing);
        Assert.Null(_store.Load());
    }

    [Fact]
    public void Load_AFileWithSomeOfIt_GivesTheRestEmpty()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(_store.FilePath)!);
        File.WriteAllText(_store.FilePath, """{ "pattern": "a+" }""");

        // Act / Assert
        Assert.Equal(new RegexInputModel("a+", "", ""), _store.Load());
    }
}
