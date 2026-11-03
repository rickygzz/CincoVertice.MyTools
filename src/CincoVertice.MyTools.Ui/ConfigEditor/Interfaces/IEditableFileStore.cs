using CincoVertice.MyTools.Ui.ConfigEditor.Models;

namespace CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;

/// <summary>Reads and writes the edited files.</summary>
public interface IEditableFileStore
{
    /// <exception cref="IOException">It can't be read, or it isn't valid UTF-8.</exception>
    /// <exception cref="UnauthorizedAccessException">It can't be read.</exception>
    EditableFileContentModel Read(string path);

    /// <exception cref="IOException">It can't be written; the file stays as it was.</exception>
    /// <exception cref="UnauthorizedAccessException">It can't be written; the file stays as it was.</exception>
    void Write(string path, EditableFileContentModel content);
}
