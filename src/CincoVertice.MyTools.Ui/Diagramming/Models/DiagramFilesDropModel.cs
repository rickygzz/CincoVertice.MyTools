using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     Files dropped on the canvas.
/// </summary>
/// <param name="Paths">The files, in the order the user picked them.</param>
/// <param name="Location">Where they were dropped, in canvas pixels.</param>
public sealed record DiagramFilesDropModel(IReadOnlyList<string> Paths, Point Location);
