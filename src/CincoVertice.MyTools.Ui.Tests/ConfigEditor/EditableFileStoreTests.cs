using System.Text;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.ConfigEditor.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.ConfigEditor;

public sealed class EditableFileStoreTests : IDisposable
{
    private static readonly byte[] _bom = [0xEF, 0xBB, 0xBF];

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "EditableFileStoreTests_" + Guid.NewGuid());
    private readonly EditableFileStore _store = new();
    private readonly string _path;

    public EditableFileStoreTests()
    {
        Directory.CreateDirectory(_folder);
        _path = Path.Combine(_folder, "file.cfg");
    }

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
    }

    [Theory]
    [InlineData("a\r\nb\r\n", false)]
    [InlineData("a\nb\n", false)]
    [InlineData("a\r\nñ\r\n", true)]
    [InlineData("a\rb\r", false)]
    public void ReadThenWrite_GivesTheSameBytes(string text, bool hasBom)
    {
        // Arrange
        byte[] bytes = [.. hasBom ? _bom : [], .. Encoding.UTF8.GetBytes(text)];
        File.WriteAllBytes(_path, bytes);

        // Act
        EditableFileContentModel content = _store.Read(_path);
        _store.Write(_path, content);

        // Assert
        Assert.DoesNotContain('\r', content.Text);
        Assert.Equal(hasBom, content.HasBom);
        Assert.Equal(bytes, File.ReadAllBytes(_path));
        Assert.Single(Directory.GetFiles(_folder));
    }

    [Fact]
    public void Write_LeavesAFileNamedLikeItsTemporaryOne()
    {
        // Arrange
        File.WriteAllText(_path, "old");
        File.WriteAllText(_path + ".tmp", "mine");

        // Act
        _store.Write(_path, new EditableFileContentModel("new", "\n", false));

        // Assert
        Assert.Equal("new", File.ReadAllText(_path));
        Assert.Equal("mine", File.ReadAllText(_path + ".tmp"));
    }

    [Fact]
    public void Read_ALoneReturn_IsANewLine()
    {
        // Arrange
        File.WriteAllText(_path, "a\rb");

        // Act & Assert
        Assert.Equal("a\nb", _store.Read(_path).Text);
    }

    [Fact]
    public void Read_NotUtf8_IsRefused()
    {
        // Arrange: ñ in Latin-1, as an old editor saves it
        File.WriteAllBytes(_path, [0x61, 0xF1, 0x62]);

        // Act & Assert: and it isn't IOException's subclass for a missing file, which means something else
        IOException error = Assert.Throws<IOException>(() => _store.Read(_path));
        Assert.Contains("UTF-8", error.Message);
    }

    [Fact]
    public void Write_WhenItCantWrite_KeepsTheFile()
    {
        // Arrange
        File.WriteAllText(_path, "old");
        File.SetAttributes(_path, FileAttributes.ReadOnly);

        try
        {
            // Act & Assert: and nothing else is left in the folder, or every failed save would add a file
            Assert.ThrowsAny<Exception>(() => _store.Write(_path, new EditableFileContentModel("new", "\n", false)));
            Assert.Equal("old", File.ReadAllText(_path));
            Assert.Single(Directory.GetFiles(_folder));
        }
        finally
        {
            File.SetAttributes(_path, FileAttributes.Normal);
        }
    }
}
