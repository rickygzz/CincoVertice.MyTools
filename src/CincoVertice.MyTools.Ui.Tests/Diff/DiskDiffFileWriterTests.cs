using System.Text;
using CincoVertice.MyTools.Ui.Diff.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diff;

public sealed class DiskDiffFileWriterTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "DiffWriter_" + Guid.NewGuid());

    [Theory]
    [InlineData("漢")]
    [InlineData("😀")]
    public void UnrepresentableCharacters_LeaveTheOriginalFileIntact(string text)
    {
        Directory.CreateDirectory(_folder);
        string path = Path.Combine(_folder, "text.txt");
        File.WriteAllBytes(path, [0xE9]);

        Assert.Throws<EncoderFallbackException>(() => new DiskDiffFileWriter().Write(path, text, Encoding.Latin1));

        Assert.Equal(new byte[] { 0xE9 }, File.ReadAllBytes(path));
        Assert.Single(Directory.GetFiles(_folder));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
