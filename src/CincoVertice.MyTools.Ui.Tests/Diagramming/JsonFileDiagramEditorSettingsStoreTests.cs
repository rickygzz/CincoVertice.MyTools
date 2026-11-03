using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diagramming;

public sealed class JsonFileDiagramEditorSettingsStoreTests : IDisposable
{
    private static readonly DiagramStyleModel _style = new(
        Color.FromArgb(255, 255, 255, 0),
        Color.FromArgb(255, 1, 2, 3),
        3,
        true,
        Color.FromArgb(255, 4, 5, 6),
        18,
        "Consolas");

    private readonly string _folder = Path.Combine(
        Path.GetTempPath(),
        "JsonFileDiagramEditorSettingsStoreTests_" + Guid.NewGuid());

    private readonly JsonFileDiagramEditorSettingsStore _store;

    public JsonFileDiagramEditorSettingsStoreTests()
    {
        _store = new JsonFileDiagramEditorSettingsStore(Path.Combine(_folder, "config", "diagrameditor.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private void WriteFile(string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_store.FilePath)!);
        File.WriteAllText(_store.FilePath, text);
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
        var settings = new DiagramEditorSettingsModel(
            _style,
            DiagramRouting.Straight,
            DiagramArrowHeads.Both,
            false,
            new DiagramWindowSizeModel(new Size(900, 600), true));

        // Act
        _store.Save(settings);

        // Assert
        Assert.Equal(settings, _store.Load());
        Assert.False(File.Exists(_store.FilePath + ".tmp"));
    }

    [Fact]
    public void SaveThenLoad_NoFillAndNoWindow_StayThatWay()
    {
        // Arrange
        var settings = new DiagramEditorSettingsModel(
            _style with { Fill = Color.Empty },
            DiagramRouting.Elbow,
            DiagramArrowHeads.End);

        // Act
        _store.Save(settings);

        // Assert
        Assert.Equal(settings, _store.Load());
    }

    [Fact]
    public void Save_WritesAFileThatIsEasyToRead()
    {
        // Act
        _store.Save(new DiagramEditorSettingsModel(
            _style,
            DiagramRouting.Elbow,
            DiagramArrowHeads.End,
            Window: new DiagramWindowSizeModel(new Size(900, 600), false)));

        // Assert
        string json = File.ReadAllText(_store.FilePath);
        Assert.Contains("\"fill\": \"#FFFF00\"", json);
        Assert.Contains("\"fontFamily\": \"Consolas\"", json);
        Assert.Contains("\"routing\": \"Elbow\"", json);
        Assert.Contains("\"width\": 900", json);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{ \"routing\": \"Elbow\" }")]
    [InlineData("{ \"style\": { }, \"routing\": \"Sideways\" }")]
    public void Load_ABrokenFile_GivesNothing(string text)
    {
        // Arrange
        WriteFile(text);

        // Act & Assert
        Assert.Null(_store.Load());
    }

    [Fact]
    public void Load_ReadsAFileEditedByHand()
    {
        // Arrange
        WriteFile("""
            {
                // comments and trailing commas are fine
                "style": { "fill": "#00ff00", "lineWidth": 9999, "fontSize": 1, "fontFamily": "Georgia" },
                "routing": "Straight",
            }
            """);

        // Act
        DiagramEditorSettingsModel? settings = _store.Load();

        // Assert: what's out of range is brought into range; what's left out has its default
        Assert.NotNull(settings);
        Assert.Equal(Color.FromArgb(255, 0, 255, 0), settings.Style.Fill);
        Assert.Equal(100, settings.Style.LineWidth);
        Assert.Equal(6, settings.Style.FontSize);
        Assert.Equal("Georgia", settings.Style.FontFamily);
        Assert.Equal(DiagramRouting.Straight, settings.Routing);
        Assert.Equal(DiagramArrowHeads.End, settings.Heads);
        Assert.True(settings.IsPropertiesPanelVisible);
        Assert.Null(settings.Window);
    }

    [Fact]
    public void Save_WhenItCantWrite_DoesntStopTheEditor()
    {
        // Arrange: a folder where the file should be
        Directory.CreateDirectory(_store.FilePath);

        // Act & Assert: reaching the end is the point
        _store.Save(new DiagramEditorSettingsModel(_style, DiagramRouting.Elbow, DiagramArrowHeads.End));
        Assert.True(Directory.Exists(_store.FilePath));
    }
}
