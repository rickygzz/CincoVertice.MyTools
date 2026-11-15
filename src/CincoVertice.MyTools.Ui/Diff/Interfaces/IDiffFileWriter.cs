using System.Text;

namespace CincoVertice.MyTools.Ui.Diff.Interfaces;

/// <summary>Saves a side's text to its file.</summary>
public interface IDiffFileWriter
{
    /// <returns>When the file was written, to tell a later change from this one.</returns>
    /// <exception cref="IOException">When it can't be written.</exception>
    /// <exception cref="UnauthorizedAccessException">When the user may not write it.</exception>
    DateTime Write(string path, string text, Encoding encoding);
}
