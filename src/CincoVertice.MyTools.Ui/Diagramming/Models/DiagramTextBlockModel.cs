using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A text split into lines, each centered on <paramref name="CenterX"/>.
/// </summary>
/// <param name="Lines">The lines, wrapped to fit.</param>
/// <param name="CenterX">The middle of every line.</param>
/// <param name="Top">The top of the first line.</param>
/// <param name="LineHeight">From the top of a line to the top of the next.</param>
/// <param name="Bounds">Around all the lines; an estimate, since it depends on the font's real widths.</param>
public sealed record DiagramTextBlockModel(
    IReadOnlyList<string> Lines,
    float CenterX,
    float Top,
    float LineHeight,
    RectangleF Bounds);
