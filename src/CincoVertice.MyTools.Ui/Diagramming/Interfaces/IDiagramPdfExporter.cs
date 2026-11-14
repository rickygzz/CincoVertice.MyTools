using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Interfaces;

/// <summary>
///     Writes a diagram as a PDF file, to share or print. The diagram editor can't open it again.
/// </summary>
public interface IDiagramPdfExporter
{
    /// <exception cref="IOException">The file can't be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The file can't be written.</exception>
    void Export(string path, DiagramModel diagram);
}
