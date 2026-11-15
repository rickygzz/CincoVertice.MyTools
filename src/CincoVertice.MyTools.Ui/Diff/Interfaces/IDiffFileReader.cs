using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.Diff.Interfaces;

/// <summary>Reads the files compared.</summary>
public interface IDiffFileReader
{
    /// <exception cref="IOException">When it can't be read, or is a folder.</exception>
    /// <exception cref="UnauthorizedAccessException">When the user may not read it.</exception>
    DiffFileModel Read(string path);

    /// <returns>When the file was last written, or null when it isn't there.</returns>
    DateTime? WrittenUtc(string path);
}
