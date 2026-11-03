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
        Assert.False(File.Exists(_path + ".tmp"));
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
        // Arrange: a folder where the temporary file goes
        File.WriteAllText(_path, "old");
        Directory.CreateDirectory(_path + ".tmp");

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => _store.Write(_path, new EditableFileContentModel("new", "\n", false)));
        Assert.Equal("old", File.ReadAllText(_path));
    }
}
