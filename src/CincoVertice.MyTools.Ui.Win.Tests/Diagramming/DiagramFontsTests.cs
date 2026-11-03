using System.Drawing;
using CincoVertice.MyTools.Ui.Win.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Diagramming;

public sealed class DiagramFontsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "DiagramFontsTests_" + Guid.NewGuid());
    private readonly List<(string Name, string Path)> _registered = [];
    private readonly List<string> _loaded = [];

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    // Never the real folder, registry or font table: these tests must not install anything on the computer
    private RobotoFontInstaller Installer(bool isInstalled = false, Func<Stream>? openFont = null) => new(
        _folder,
        () => isInstalled,
        openFont ?? (() => new MemoryStream([1, 2, 3])),
        (name, path) => _registered.Add((name, path)),
        _loaded.Add);

    [Fact]
    public void Create_Roboto_IsThatFont_WithoutItBeingInstalled()
    {
        // Act
        using Font font = DiagramFonts.Create("Roboto", 14);

        // Assert
        Assert.Equal("Roboto", font.FontFamily.Name);
        Assert.Equal(14, font.Size);
        Assert.Equal(GraphicsUnit.Pixel, font.Unit);
    }

    [Fact]
    public void Create_AFontThatDoesntExist_GivesADefaultOne()
    {
        // Act
        using Font font = DiagramFonts.Create("No Such Font At All", 14);

        // Assert
        Assert.NotEqual("No Such Font At All", font.FontFamily.Name);
    }

    [Fact]
    public void Names_HaveRoboto_InOrder_WithoutRepeats()
    {
        // Act
        IReadOnlyList<string> names = DiagramFonts.Names();

        // Assert
        Assert.Contains("Roboto", names);
        Assert.Equal(names.Order(StringComparer.OrdinalIgnoreCase), names);
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void BundledFont_IsATrueTypeFile()
    {
        // Act
        using Stream font = DiagramFonts.OpenBundledFont();
        byte[] header = new byte[4];

        font.ReadExactly(header);

        // Assert
        Assert.Equal(new byte[] { 0, 1, 0, 0 }, header);
    }

    [Fact]
    public void EnsureInstalled_WhenItsThereAlready_DoesNothing()
    {
        // Act
        bool result = Installer(isInstalled: true).EnsureInstalled();

        // Assert
        Assert.True(result);
        Assert.False(Directory.Exists(_folder));
        Assert.Empty(_registered);
        Assert.Empty(_loaded);
    }

    [Fact]
    public void EnsureInstalled_CopiesTheFile_RegistersIt_AndLoadsIt()
    {
        // Act
        bool result = Installer().EnsureInstalled();

        // Assert
        string path = Path.Combine(_folder, "Roboto.ttf");

        Assert.True(result);
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(path));
        Assert.Equal(("Roboto (TrueType)", path), Assert.Single(_registered));
        Assert.Equal(path, Assert.Single(_loaded));
    }

    [Fact]
    public void EnsureInstalled_WhenTheFileIsThereAlready_LeavesIt_AndStillRegistersIt()
    {
        // Arrange: it may be in use, so writing it could fail
        Directory.CreateDirectory(_folder);
        File.WriteAllBytes(Path.Combine(_folder, "Roboto.ttf"), [9]);

        // Act
        bool result = Installer().EnsureInstalled();

        // Assert
        Assert.True(result);
        Assert.Equal(new byte[] { 9 }, File.ReadAllBytes(Path.Combine(_folder, "Roboto.ttf")));
        Assert.Single(_registered);
    }

    [Fact]
    public void EnsureInstalled_WhenItFails_SaysSo_WithoutStoppingTheEditor()
    {
        // Act
        bool result = Installer(openFont: () => throw new IOException("Disk full")).EnsureInstalled();

        // Assert
        Assert.False(result);
        Assert.Empty(_registered);
    }

    [Fact]
    public void EnsureInstalled_WhenTheCopyFailsHalfway_LeavesNothing_AndCanTryAgain()
    {
        // Act
        bool failed = Installer(openFont: () => new FailsHalfwayStream()).EnsureInstalled();

        // Assert: a partial file would count as installed from then on
        Assert.False(failed);
        Assert.Empty(_registered);
        Assert.Empty(Directory.GetFiles(_folder));

        // Act
        bool retried = Installer().EnsureInstalled();

        // Assert
        Assert.True(retried);
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(Path.Combine(_folder, "Roboto.ttf")));
        Assert.Single(Directory.GetFiles(_folder));
    }

    [Fact]
    public void EnsureInstalled_LeavesAFileNamedLikeItsTemporaryOne()
    {
        // Arrange
        Directory.CreateDirectory(_folder);
        File.WriteAllBytes(Path.Combine(_folder, "Roboto.ttf.tmp"), [9]);

        // Act
        Installer().EnsureInstalled();

        // Assert
        Assert.Equal(new byte[] { 9 }, File.ReadAllBytes(Path.Combine(_folder, "Roboto.ttf.tmp")));
    }

    private sealed class FailsHalfwayStream : MemoryStream
    {
        public override void CopyTo(Stream destination, int bufferSize)
        {
            destination.WriteByte(1);

            throw new IOException("Disk full");
        }
    }
}
