using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     What the snip editor remembers between windows and runs: the last tool, each tool's settings, and the
///     colors picked with More colors.
/// </summary>
/// <param name="Tool">The tool selected last.</param>
/// <param name="Tools">Each tool's size, transparency and color; a tool that's missing keeps its defaults.</param>
/// <param name="CustomColors">The colors picked with More colors, newest first.</param>
/// <param name="ArrowHeads">Which ends of the arrow tool's arrows have a head.</param>
/// <param name="TextOutline">The edge of the text tool's letters; null keeps the default.</param>
/// <param name="CornerRadius">How round the rectangle tool's corners are, in pixels; null keeps the default.</param>
public sealed record SnipEditorSettingsModel(
    SnipTool Tool,
    IReadOnlyDictionary<SnipTool, SnipToolSettingsModel> Tools,
    IReadOnlyList<Color> CustomColors,
    SnipArrowHeads ArrowHeads = SnipArrowHeads.End,
    SnipTextOutlineModel? TextOutline = null,
    int? CornerRadius = null);
