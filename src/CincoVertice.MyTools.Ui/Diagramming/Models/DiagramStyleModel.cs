using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     How a shape or a connector looks. A connector doesn't use <paramref name="Fill"/>.
/// </summary>
/// <param name="Fill">The inside of a shape; <see cref="Color.Empty"/> for none.</param>
/// <param name="Line">The outline of a shape, or a connector's line.</param>
/// <param name="LineWidth">In pixels.</param>
/// <param name="Dashed">True for a dashed line.</param>
/// <param name="TextColor">The color of a shape's text or a connector's label.</param>
/// <param name="FontSize">In pixels.</param>
public sealed record DiagramStyleModel(
    Color Fill,
    Color Line,
    int LineWidth,
    bool Dashed,
    Color TextColor,
    int FontSize);
