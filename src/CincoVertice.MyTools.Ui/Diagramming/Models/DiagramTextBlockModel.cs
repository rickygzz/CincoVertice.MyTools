using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A text split into lines, each placed at <paramref name="X"/> as <paramref name="Alignment"/> says.
/// </summary>
/// <param name="Lines">The lines, wrapped to fit.</param>
/// <param name="X">Where every line is: its left edge, its middle or its right edge.</param>
/// <param name="Top">The top of the first line.</param>
/// <param name="LineHeight">From the top of a line to the top of the next.</param>
/// <param name="Bounds">Around all the lines; an estimate, since it depends on the font's real widths.</param>
/// <param name="Alignment">Which part of each line is at <paramref name="X"/>.</param>
public sealed record DiagramTextBlockModel(
    IReadOnlyList<string> Lines,
    float X,
    float Top,
    float LineHeight,
    RectangleF Bounds,
    DiagramTextAlignment Alignment = DiagramTextAlignment.Center);
