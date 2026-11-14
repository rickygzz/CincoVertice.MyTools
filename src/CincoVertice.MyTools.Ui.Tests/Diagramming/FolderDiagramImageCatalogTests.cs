using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diagramming;

public sealed class FolderDiagramImageCatalogTests : IDisposable
{
    private const string Icon =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 18 18"><rect width="18" height="18"/></svg>""";

    private const string Other =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 9 9"><circle r="4"/></svg>""";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ImageCatalogTests_" + Guid.NewGuid());
    private readonly FolderDiagramImageCatalog _catalog;

    public FolderDiagramImageCatalogTests()
    {
        _catalog = new FolderDiagramImageCatalog(_folder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private void CopyIn(string path, string text)
    {
        string fullPath = Path.Combine(_folder, path);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, text);
    }

    [Fact]
    public void Add_KeepsACopyAsItWas_AtTheTop_AsUsedLast()
    {
        // Act
        _catalog.Add(Icon, "vm.svg");

        // Assert
        Assert.Equal(Icon, File.ReadAllText(Path.Combine(_folder, "vm.svg")));
        DiagramCatalogImageModel image = Assert.Single(_catalog.Images());
        Assert.Equal(("vm.svg", "vm", FolderDiagramImageCatalog.InsertedGroup), (image.Path, image.Name, image.Group));
        Assert.Equal(DiagramSvgImage.Prepare(Icon), image.Markup);
        Assert.Equal(["vm.svg"], _catalog.Recent());
    }

    [Fact]
    public void Add_TheSameImageAgain_IsOnlyUsedAgain_WhereverItIs_WhateverItsLineEndings()
    {
        // Arrange
        CopyIn("Azure/Compute/vm.svg", Icon.Replace("><", ">\r\n<", StringComparison.Ordinal));
        string withLineFeeds = Icon.Replace("><", ">\n<", StringComparison.Ordinal);
        _catalog.Add(Other, "other.svg");

        // Act
        _catalog.Add(withLineFeeds, "copy of vm.svg");

        // Assert
        Assert.Equal(2, _catalog.Images().Count);
        Assert.Equal(["Azure/Compute/vm.svg", "other.svg"], _catalog.Recent());
    }

    [Fact]
    public void Add_ADifferentImageOfTheSameName_GetsANumber()
    {
        // Act
        _catalog.Add(Icon, "vm.svg");
        _catalog.Add(Other, "vm.svg");
        _catalog.Add(Other.Replace("4", "3", StringComparison.Ordinal), "vm.SVG");

        // Assert
        Assert.Equal(["vm (3).svg", "vm (2).svg", "vm.svg"], _catalog.Recent());
    }

    [Theory]
    [InlineData("pasted.svg", "pasted.svg")]
    [InlineData(".svg", "image.svg")]
    [InlineData("a<b>.svg", "ab.svg")]
    public void Add_TheName_IsOneAFileCanHave(string fileName, string expected)
    {
        // Act
        _catalog.Add(Icon, fileName);

        // Assert
        Assert.Equal([expected], _catalog.Recent());
    }

    [Fact]
    public void Images_CopiedInByHand_AreInTheGroupsOfTheirFolders_InsertedFirst_ThenByName()
    {
        // Arrange: a file that isn't an image, and one that isn't SVG, are left out
        CopyIn("zebra.svg", Icon);
        CopyIn("Azure/Web/10021-icon-service-App-Services.svg", Icon);
        CopyIn("Azure/Compute/Virtual_Machine.svg", Icon);
        CopyIn("Azure/Compute/notes.svg", "not markup");
        CopyIn("Azure/readme.txt", "text");

        // Act
        IReadOnlyList<DiagramCatalogImageModel> images = _catalog.Images();

        // Assert
        Assert.Equal(
            [
                ("Inserted", "zebra"),
                ("Azure/Compute", "Virtual Machine"),
                ("Azure/Web", "App Services"),
            ],
            images.Select(image => (image.Group, image.Name)));
    }

    [Fact]
    public void Images_AFileChanged_IsReadAgain_AndOneGone_IsLeftOut()
    {
        // Arrange
        CopyIn("a.svg", Icon);
        CopyIn("b.svg", Icon);
        _ = _catalog.Images();
        string a = Path.Combine(_folder, "a.svg");

        // Act
        File.WriteAllText(a, Other);
        File.SetLastWriteTimeUtc(a, DateTime.UtcNow.AddMinutes(1));
        File.Delete(Path.Combine(_folder, "b.svg"));

        // Assert
        Assert.Equal(DiagramSvgImage.Prepare(Other), Assert.Single(_catalog.Images()).Markup);
    }

    [Fact]
    public void Recent_KeepsTheLastFifty()
    {
        // Act
        for (int i = 0; i < FolderDiagramImageCatalog.MaxRecent + 5; i++)
        {
            _catalog.MarkUsed($"{i}.svg");
        }

        // Assert
        IReadOnlyList<string> recent = _catalog.Recent();
        Assert.Equal(FolderDiagramImageCatalog.MaxRecent, recent.Count);
        Assert.Equal("54.svg", recent[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not json")]
    public void NoFolder_OrABrokenRecentFile_GivesNothing(string? recent)
    {
        // Arrange
        if (recent is not null)
        {
            CopyIn("recent.json", recent);
        }

        // Act / Assert
        Assert.Empty(_catalog.Images());
        Assert.Empty(_catalog.Recent());
    }

    [Theory]
    [InlineData("10021-icon-service-Virtual-Machine.svg", "Virtual Machine")]
    [InlineData("my_logo.svg", "my logo")]
    [InlineData("Kubernetes.svg", "Kubernetes")]
    public void NameOf_IsEasierToReadThanTheFileName(string fileName, string expected)
    {
        // Act / Assert
        Assert.Equal(expected, FolderDiagramImageCatalog.NameOf(fileName));
    }
}
