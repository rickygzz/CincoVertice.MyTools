using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diff;

public sealed class DiskDiffFileReaderTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "MyToolsDiffReaderTests", Guid.NewGuid().ToString("N"));

    public DiskDiffFileReaderTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void Decode_ReadsUtf8_AndOtherFilesAsLatin1()
    {
        // Act
        string utf8 = DiskDiffFileReader.Decode([0xEF, 0xBB, 0xBF, 0x63, 0x61, 0x66, 0xC3, 0xA9]);
        string noBom = DiskDiffFileReader.Decode([0x63, 0x61, 0x66, 0xC3, 0xA9]);
        string latin1 = DiskDiffFileReader.Decode([0x63, 0x61, 0x66, 0xE9]);
        string utf16 = DiskDiffFileReader.Decode([0xFF, 0xFE, 0x63, 0x00, 0xE9, 0x00]);

        // Assert
        Assert.Equal("café", utf8);
        Assert.Equal("café", noBom);
        Assert.Equal("café", latin1);
        Assert.Equal("cé", utf16);
    }

    [Fact]
    public void Read_GivesTheTextAndWhenItWasWritten()
    {
        // Arrange
        string path = Path.Combine(_folder, "a.txt");
        File.WriteAllText(path, "one\ntwo");
        var reader = new DiskDiffFileReader();

        // Act
        DiffFileModel file = reader.Read(path);

        // Assert
        Assert.Equal("one\ntwo", file.Text);
        Assert.Equal(File.GetLastWriteTimeUtc(path), file.WrittenUtc);
        Assert.Equal(file.WrittenUtc, reader.WrittenUtc(path));
        Assert.Null(reader.WrittenUtc(Path.Combine(_folder, "missing.txt")));
    }

    [Fact]
    public void AFolder_CantBeRead()
    {
        // Act / Assert
        Assert.Throws<IOException>(() => new DiskDiffFileReader().Read(_folder));
    }
}
