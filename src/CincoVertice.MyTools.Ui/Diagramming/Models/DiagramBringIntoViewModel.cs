using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A request to move the view so the diagram, or what's selected, is where the user wants it.
/// </summary>
/// <param name="Placement">Where in the window.</param>
/// <param name="CanvasSize">How large the canvas is, in canvas pixels.</param>
public sealed record DiagramBringIntoViewModel(DiagramPlacement Placement, Size CanvasSize);
