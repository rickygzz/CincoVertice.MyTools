using System.Text;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Diff.Interfaces;

namespace CincoVertice.MyTools.Ui.Diff.Services;

/// <summary>Saves through <see cref="AtomicFile"/>, so a failure halfway never leaves half a file.</summary>
public sealed class DiskDiffFileWriter : IDiffFileWriter
{
    public DateTime Write(string path, string text, Encoding encoding)
    {
        AtomicFile.WriteAllText(path, text, encoding);

        return File.GetLastWriteTimeUtc(path);
    }
}
