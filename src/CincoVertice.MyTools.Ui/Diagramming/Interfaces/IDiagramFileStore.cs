using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Interfaces;

/// <summary>
///     Where diagrams are saved.
/// </summary>
public interface IDiagramFileStore
{
    /// <exception cref="IOException">The file can't be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The file can't be written.</exception>
    void Save(string path, DiagramModel diagram);

    /// <exception cref="IOException">The file can't be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The file can't be read.</exception>
    /// <exception cref="InvalidDataException">The file isn't a diagram.</exception>
    DiagramModel Load(string path);
}
