using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Interfaces;

/// <summary>
///     Writes a diagram as a PNG picture, for chats and emails, which show it at once. The diagram editor can't open
///     it again.
/// </summary>
public interface IDiagramPngExporter
{
    /// <param name="path">The file.</param>
    /// <param name="diagram">What to write: the same part of it as the SVG and PDF files.</param>
    /// <param name="scale">Pixels in the picture per diagram pixel: 1, or 2 for a sharper, larger one.</param>
    /// <exception cref="IOException">The file can't be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The file can't be written.</exception>
    void Export(string path, DiagramModel diagram, int scale);
}
