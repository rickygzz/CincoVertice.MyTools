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

    /// <summary>Something that changes when the file does, to tell that it was changed elsewhere.</summary>
    /// <returns>Its version; null when it's gone or can't be read.</returns>
    string? VersionOf(string path);

    /// <summary>Reads an SVG image to insert in a diagram.</summary>
    /// <returns>Its markup as it is in the file.</returns>
    /// <exception cref="IOException">The file can't be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The file can't be read.</exception>
    /// <exception cref="InvalidDataException">The file is too large to be an image to insert.</exception>
    string ReadImage(string path);
}
