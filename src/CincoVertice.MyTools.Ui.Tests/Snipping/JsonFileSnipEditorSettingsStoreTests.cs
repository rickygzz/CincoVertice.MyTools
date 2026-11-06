using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Snipping;

public sealed class JsonFileSnipEditorSettingsStoreTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "SnipSettingsTests", Guid.NewGuid().ToString("N"));

    private readonly JsonFileSnipEditorSettingsStore _store;

    public JsonFileSnipEditorSettingsStoreTests()
    {
        // In a folder that doesn't exist yet: Save creates it
        _store = new JsonFileSnipEditorSettingsStore(Path.Combine(_folder, "config", "snipeditor.json"));
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
    public void SaveThenLoad_GivesTheSameSettings()
    {
        // Arrange
        var settings = new SnipEditorSettingsModel(
            SnipTool.Highlighter,
            new Dictionary<SnipTool, SnipToolSettingsModel>
            {
                [SnipTool.Pen] = new(6, 0, Color.FromArgb(255, 30, 144, 255)),
                [SnipTool.Highlighter] = new(24, 40, Color.FromArgb(255, 255, 255, 0)),
            },
            [Color.FromArgb(255, 18, 52, 86), Color.FromArgb(255, 171, 205, 239)]);

        // Act
        _store.Save(settings);
        SnipEditorSettingsModel? loaded = _store.Load();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(settings.Tool, loaded.Tool);
        Assert.Equal(settings.Tools, loaded.Tools);
        Assert.Equal(settings.CustomColors, loaded.CustomColors);
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(_store.FilePath)!, "*.tmp"));
    }

    [Fact]
    public void SaveThenLoad_KeepsTheTextOutline()
    {
        // Arrange
        var outline = new SnipTextOutlineModel(3, Color.FromArgb(255, 18, 52, 86));

        // Act
        _store.Save(new SnipEditorSettingsModel(
            SnipTool.Text,
            new Dictionary<SnipTool, SnipToolSettingsModel>(),
            [],
            TextOutline: outline));

        // Assert
        Assert.Equal(outline, _store.Load()!.TextOutline);
    }

    [Fact]
    public void SaveThenLoad_KeepsTheCornerRadius()
    {
        // Act
        _store.Save(new SnipEditorSettingsModel(
            SnipTool.Rectangle,
            new Dictionary<SnipTool, SnipToolSettingsModel>(),
            [],
            CornerRadius: 0));

        // Assert: 0 is square corners, not a missing value
        Assert.Equal(0, _store.Load()!.CornerRadius);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SaveThenLoad_KeepsHideWhileSnipping(bool isHidden)
    {
        // Act
        _store.Save(new SnipEditorSettingsModel(
            SnipTool.Pen,
            new Dictionary<SnipTool, SnipToolSettingsModel>(),
            [],
            HideWhileSnipping: isHidden));

        // Assert
        Assert.Equal(isHidden, _store.Load()!.HideWhileSnipping);
    }

    [Fact]
    public void Load_AFileFromBeforeTextOutlines_HasNone()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(_store.FilePath)!);
        File.WriteAllText(_store.FilePath, """{ "tool": "Text", "tools": {}, "customColors": [] }""");

        // Assert
        Assert.Null(_store.Load()!.TextOutline);
        Assert.Null(_store.Load()!.CornerRadius);

        // On, as for anyone who never chose
        Assert.True(_store.Load()!.HideWhileSnipping);
    }

    [Fact]
    public void SaveThenLoad_KeepsTheArrowHeads()
    {
        // Act
        _store.Save(new SnipEditorSettingsModel(
            SnipTool.Arrow,
            new Dictionary<SnipTool, SnipToolSettingsModel>(),
            [],
            SnipArrowHeads.Both));

        // Assert
        Assert.Contains("\"arrowHeads\": \"Both\"", File.ReadAllText(_store.FilePath));
        Assert.Equal(SnipArrowHeads.Both, _store.Load()!.ArrowHeads);
    }

    [Fact]
    public void Load_AFileFromBeforeArrowHeads_HasTheHeadAtTheEnd()
    {
        // Arrange
        WriteFile("""{ "tool": "Arrow" }""");

        // Act
        SnipEditorSettingsModel? loaded = _store.Load();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(SnipArrowHeads.End, loaded.ArrowHeads);
    }

    [Fact]
    public void Save_WritesAFileThatIsEasyToRead()
    {
        // Act
        _store.Save(new SnipEditorSettingsModel(
            SnipTool.Arrow,
            new Dictionary<SnipTool, SnipToolSettingsModel> { [SnipTool.Arrow] = new(4, 10, Color.Red) },
            [Color.FromArgb(255, 18, 52, 86)]));

        // Assert
        string json = File.ReadAllText(_store.FilePath);
        Assert.Contains("\"tool\": \"Arrow\"", json);
        Assert.Contains("\"Arrow\": {", json);
        Assert.Contains("\"transparency\": 10", json);
        Assert.Contains("\"color\": \"#FF0000\"", json);
        Assert.Contains("\"customColors\": [", json);
        Assert.Contains("\"#123456\"", json);
    }

    [Fact]
    public void Load_ABrokenCustomColor_IsSkipped()
    {
        // Arrange
        WriteFile("""{ "tool": "Pen", "customColors": ["#123456", "teal", null, "#ABCDEF"] }""");

        // Act
        SnipEditorSettingsModel? loaded = _store.Load();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal([Color.FromArgb(255, 18, 52, 86), Color.FromArgb(255, 171, 205, 239)], loaded.CustomColors);
    }

    [Fact]
    public void Save_AnEmptyColor_LeavesItOut()
    {
        // Act
        _store.Save(new SnipEditorSettingsModel(
            SnipTool.Pen,
            new Dictionary<SnipTool, SnipToolSettingsModel> { [SnipTool.Pen] = new(4, 0, Color.Empty) },
            []));

        // Assert
        Assert.DoesNotContain("color", File.ReadAllText(_store.FilePath));
        Assert.Equal(Color.Empty, _store.Load()!.Tools[SnipTool.Pen].Color);
    }

    [Fact]
    public void Load_ReadsAFileEditedByHand()
    {
        // Arrange
        WriteFile("""
            {
                // comments and trailing commas are fine
                "tool": "Text",
                "tools": { "Text": { "size": 36, "transparency": 0, "color": "#00ff00" }, },
            }
            """);

        // Act
        SnipEditorSettingsModel? loaded = _store.Load();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(SnipTool.Text, loaded.Tool);
        Assert.Equal(new SnipToolSettingsModel(36, 0, Color.FromArgb(255, 0, 255, 0)), loaded.Tools[SnipTool.Text]);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#F00")]
    [InlineData("#GG0000")]
    public void Load_ABrokenColor_GivesAnEmptyColorForThatToolOnly(string color)
    {
        // Arrange
        WriteFile($$"""
            {
                "tool": "Pen",
                "tools": {
                    "Pen": { "size": 4, "transparency": 0, "color": "{{color}}" },
                    "Arrow": { "size": 4, "transparency": 0, "color": "#0000FF" }
                }
            }
            """);

        // Act
        SnipEditorSettingsModel? loaded = _store.Load();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(Color.Empty, loaded.Tools[SnipTool.Pen].Color);
        Assert.Equal(Color.FromArgb(255, 0, 0, 255), loaded.Tools[SnipTool.Arrow].Color);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "tool": "Brush" }""")]
    [InlineData("""{ "tool": "Pen", "tools": { "Brush": { "size": 4 } } }""")]
    [InlineData("""{ "tool": "Pen", "tools": { "Pen": null } }""")]
    [InlineData("null")]
    public void Load_ABrokenFile_GivesNothing(string json)
    {
        // Arrange
        WriteFile(json);

        // Act and assert
        Assert.Null(_store.Load());
    }

    [Fact]
    public void Load_WithoutTools_GivesNoToolSettings()
    {
        // Arrange
        WriteFile("""{ "tool": "Pen" }""");

        // Act
        SnipEditorSettingsModel? loaded = _store.Load();

        // Assert
        Assert.NotNull(loaded);
        Assert.Empty(loaded.Tools);
        Assert.Empty(loaded.CustomColors);
    }

    private void WriteFile(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_store.FilePath)!);
        File.WriteAllText(_store.FilePath, json);
    }
}
