using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.JsonEditor;

public sealed class JsonSectionsAndSettingsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"jsoneditor-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 0)]
    [InlineData(6, 0)]
    public void SectionAt_IsTheInnermostTheLineIsIn_ItsFirstAndLastLinesIncluded(int line, int startLine)
    {
        // Arrange: an object over lines 0 to 6, with an array over 2 to 4
        JsonFoldModel[] folds = [new(0, 6, "{ 2 keys }"), new(2, 4, "[ 1 item ]")];

        // Act / Assert
        Assert.Equal(startLine, JsonSections.SectionAt(folds, line)?.StartLine);
    }

    [Fact]
    public void OpenSectionAt_OnTheFirstLineOfACollapsedOne_IsTheOneAroundIt()
    {
        // Arrange: the array over 2 to 4 collapsed, in the object over 0 to 6
        JsonFoldModel[] folds = [new(0, 6, "{ 2 keys }"), new(2, 4, "[ 1 item ]")];

        // Act / Assert
        Assert.Equal(0, JsonSections.OpenSectionAt(folds, [2], 2)?.StartLine);
        Assert.Null(JsonSections.OpenSectionAt(folds, [0, 2], 2));
    }

    [Fact]
    public void SectionAt_OutsideEverySection_IsNone()
    {
        // Act / Assert
        Assert.Null(JsonSections.SectionAt([new JsonFoldModel(1, 3, "{ 1 key }")], 5));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SettingsStore_KeepsTheTheme_MakingTheFolder(bool isDark)
    {
        // Arrange
        string path = Path.Combine(_folder, "config", "jsoneditor.json");

        // Act
        new JsonFileJsonEditorSettingsStore(path).Save(new JsonEditorSettingsModel(isDark));

        // Assert: a new store, as in the next window
        Assert.Equal(isDark, new JsonFileJsonEditorSettingsStore(path).Load()?.IsDark);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{ broken")]
    public void SettingsStore_OfAMissingOrBrokenFile_IsNone(string? content)
    {
        // Arrange
        string path = Path.Combine(_folder, "jsoneditor.json");

        if (content is not null)
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(path, content);
        }

        // Act / Assert
        Assert.Null(new JsonFileJsonEditorSettingsStore(path).Load());
    }
}
