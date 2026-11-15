using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diagramming;

public sealed class FolderDiagramRecoveryStoreTests : IDisposable
{
    private static readonly DiagramModel _diagram = new(
        [
            new DiagramShapeModel(
                1,
                DiagramShapeKind.Rectangle,
                new Rectangle(10, 20, 100, 50),
                "Start",
                new DiagramStyleModel(Color.White, Color.Black, 2, false, Color.Black, 14)),
        ],
        []);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "RecoveryTests_" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    private DiagramRecoveryModel Copy(string id, DateTime savedAt) =>
        new(id, @"C:\flow.svg", "v1", _diagram, savedAt);

    [Fact]
    public void Left_IsWhatAnotherRunSaved_OldestFirst_ButNotThisRunsOwn()
    {
        // Arrange: a run that ended with two windows' copies
        var before = new FolderDiagramRecoveryStore(_folder);
        string first = before.NewId();
        string second = before.NewId();
        before.Save(Copy(second, new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc)));
        before.Save(Copy(first, new DateTime(2026, 11, 14, 9, 0, 0, DateTimeKind.Utc)));

        // Act: this run's own window has a copy too
        var store = new FolderDiagramRecoveryStore(_folder);
        string own = store.NewId();
        store.Save(Copy(own, DateTime.UtcNow));
        IReadOnlyList<DiagramRecoveryModel> left = store.Left();

        // Assert
        Assert.Equal([first, second], left.Select(copy => copy.Id));
        Assert.Equal(
            (@"C:\flow.svg", "v1", "Start", DateTimeKind.Utc),
            (left[0].Path, left[0].Version, left[0].Diagram.Shapes[0].Text, left[0].SavedAt.Kind));
        Assert.Equal([own], before.Left().Select(copy => copy.Id));
    }

    [Fact]
    public void Adopt_AndDelete_TakeACopyOutOfWhatsLeft()
    {
        // Arrange
        var before = new FolderDiagramRecoveryStore(_folder);
        string first = before.NewId();
        string second = before.NewId();
        before.Save(Copy(first, DateTime.UtcNow));
        before.Save(Copy(second, DateTime.UtcNow));
        var store = new FolderDiagramRecoveryStore(_folder);

        // Act
        store.Adopt(first);
        store.Delete(second);

        // Assert: the adopted one is still kept, for its new window
        Assert.Empty(store.Left());
        Assert.Single(new FolderDiagramRecoveryStore(_folder).Left());
    }

    [Fact]
    public void Left_LeavesOutFilesThatCantBeRead_AndAFolderThatIsntThere()
    {
        // Act / Assert
        Assert.Empty(new FolderDiagramRecoveryStore(_folder).Left());

        // Arrange
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "broken.json"), "{ not json");
        File.WriteAllText(Path.Combine(_folder, "nodiagram.json"), """{ "path": null }""");

        // Act / Assert
        Assert.Empty(new FolderDiagramRecoveryStore(_folder).Left());
    }

    [Fact]
    public void Delete_OfACopyThatIsntThere_DoesNothing()
    {
        // Act / Assert
        new FolderDiagramRecoveryStore(_folder).Delete("nothing");
    }
}
