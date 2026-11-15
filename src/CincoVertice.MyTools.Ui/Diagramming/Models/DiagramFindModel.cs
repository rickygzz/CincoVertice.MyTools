using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     What the user looks for in the diagram's texts.
/// </summary>
/// <param name="Text">What a shape's text or a connector's label must have, in any case; empty to stop looking.</param>
/// <param name="Step">Which of them to go to.</param>
/// <param name="CanvasSize">How large the canvas is, in canvas pixels, to bring what's found into view.</param>
public sealed record DiagramFindModel(string Text, DiagramFindStep Step, Size CanvasSize);
