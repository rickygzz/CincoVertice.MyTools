using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A shape on the canvas.
/// </summary>
/// <param name="Id">Unique in its diagram; connectors refer to it.</param>
/// <param name="Kind">Which shape it is.</param>
/// <param name="Bounds">Where it is, in diagram pixels.</param>
/// <param name="Text">The text inside it, which can have several lines.</param>
/// <param name="Style">How it looks.</param>
/// <param name="GroupId">The shapes with the same value are selected and moved together; null for no group.</param>
/// <param name="CornerRadius">How round a rectangle's corners are, in pixels; null for its kind's usual ones.</param>
public sealed record DiagramShapeModel(
    int Id,
    DiagramShapeKind Kind,
    Rectangle Bounds,
    string Text,
    DiagramStyleModel Style,
    int? GroupId = null,
    int? CornerRadius = null);
