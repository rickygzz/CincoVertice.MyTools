using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     Keeps diagrams in SVG files. A failed save leaves the old file as it was.
/// </summary>
public sealed class SvgDiagramFileStore : IDiagramFileStore
{
    public void Save(string path, DiagramModel diagram)
    {
        string tempPath = path + ".tmp";

        File.WriteAllText(tempPath, DiagramSvg.Write(diagram));
        File.Move(tempPath, path, overwrite: true);
    }

    public DiagramModel Load(string path) => DiagramSvg.Read(File.ReadAllText(path));
}
