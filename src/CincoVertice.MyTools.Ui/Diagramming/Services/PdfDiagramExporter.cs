using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     Writes diagrams as PDF files through <see cref="DiagramPdf"/>. A failed export leaves the old file as it was.
/// </summary>
public sealed class PdfDiagramExporter : IDiagramPdfExporter
{
    private readonly IDiagramTextOutliner _textOutliner;

    public PdfDiagramExporter(IDiagramTextOutliner textOutliner)
    {
        _textOutliner = textOutliner;
    }

    public void Export(string path, DiagramModel diagram)
    {
        byte[] pdf = DiagramPdf.Write(diagram, _textOutliner);

        AtomicFile.Write(path, temporaryPath => File.WriteAllBytes(temporaryPath, pdf));
    }
}
