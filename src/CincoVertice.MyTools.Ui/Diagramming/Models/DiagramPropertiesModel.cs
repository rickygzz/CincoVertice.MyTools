using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     What the properties panel shows about the selection. A null value is one that doesn't apply to it, so its field
///     is hidden.
/// </summary>
/// <param name="Title">What's selected.</param>
/// <param name="Bounds">Where the selected shapes are, all of them together.</param>
/// <param name="CornerRadius">How round the corners are, in pixels; only rectangles have this.</param>
/// <param name="Fill">The inside of the shapes; <see cref="Color.Empty"/> for no fill.</param>
/// <param name="Line">The border of the shapes, or the line of a connector.</param>
/// <param name="LineWidth">In pixels.</param>
/// <param name="Dashed">True for a dashed line.</param>
/// <param name="TextColor">The color of the text.</param>
/// <param name="FontSize">In pixels.</param>
/// <param name="Text">A shape's text or a connector's label, when only one is selected.</param>
/// <param name="Routing">How the selected connectors go between shapes.</param>
/// <param name="Heads">Which of their ends have a head.</param>
/// <param name="FontFamily">The font of the text.</param>
public sealed record DiagramPropertiesModel(
    string Title,
    Rectangle? Bounds = null,
    int? CornerRadius = null,
    Color? Fill = null,
    Color? Line = null,
    int? LineWidth = null,
    bool? Dashed = null,
    Color? TextColor = null,
    int? FontSize = null,
    string? Text = null,
    DiagramRouting? Routing = null,
    DiagramArrowHeads? Heads = null,
    string? FontFamily = null);
