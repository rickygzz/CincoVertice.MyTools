using System.Text;
using CincoVertice.MyTools.Ui.Diff.Interfaces;
using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.Tests.JsonEditor;

internal sealed class FakeJsonFiles : IDiffFileReader, IDiffFileWriter
{
    public Dictionary<string, string> Texts { get; } = [];

    public DiffFileModel Read(string path) =>
        Texts.TryGetValue(path, out string? text)
            ? new DiffFileModel(text, DateTime.UtcNow, new UTF8Encoding(false))
            : throw new FileNotFoundException("Not found", path);

    public DateTime? WrittenUtc(string path) => null;

    public DateTime Write(string path, string text, Encoding encoding)
    {
        Texts[path] = text;

        return DateTime.UtcNow;
    }
}
