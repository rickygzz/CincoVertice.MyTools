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
/// <param name="FontFamily">The font of the text.</param>
/// <param name="TextOutlineWidth">How far the edge around the text's letters goes out from them, in pixels; 0 for none.</param>
/// <param name="TextOutlineColor">The color of that edge; <see cref="Color.Empty"/> for the default one.</param>
/// <param name="TitleFill">
///     A container's title band: <see cref="Color.Empty"/> for <paramref name="Fill"/>, and
///     <see cref="NoTitleFill"/> for none, so the band is see-through.
/// </param>
/// <param name="TextAlignment">Where the lines of the text go: at the left, in the middle or at the right.</param>
public sealed record DiagramStyleModel(
    Color Fill,
    Color Line,
    int LineWidth,
    bool Dashed,
    Color TextColor,
    int FontSize,
    string FontFamily = DiagramStyleModel.DefaultFontFamily,
    int TextOutlineWidth = 0,
    Color TextOutlineColor = default,
    DiagramTextAlignment TextAlignment = DiagramTextAlignment.Center,
    Color TitleFill = default)
{
    /// <summary>The font of new text.</summary>
    public const string DefaultFontFamily = "Roboto";

    public const int MaxTextOutlineWidth = 50;

    /// <summary>A title fill of none: the band is see-through, while an empty one has the inside's fill.</summary>
    public static Color NoTitleFill => Color.Transparent;

    public static Color DefaultTextOutlineColor { get; } = Color.FromArgb(255, 255, 255);

    public Color TextOutlineColor { get; init; } =
        TextOutlineColor.IsEmpty ? DefaultTextOutlineColor : TextOutlineColor;
}
