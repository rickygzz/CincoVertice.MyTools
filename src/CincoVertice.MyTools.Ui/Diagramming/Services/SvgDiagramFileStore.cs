using System.Security.Cryptography;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     Keeps diagrams in SVG files. A failed save leaves the old file as it was.
/// </summary>
public sealed class SvgDiagramFileStore : IDiagramFileStore
{
    public void Save(string path, DiagramModel diagram) => AtomicFile.WriteAllText(path, DiagramSvg.Write(diagram));

    public DiagramModel Load(string path) => DiagramSvg.Read(File.ReadAllText(path));

    public string? VersionOf(string path)
    {
        try
        {
            return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
