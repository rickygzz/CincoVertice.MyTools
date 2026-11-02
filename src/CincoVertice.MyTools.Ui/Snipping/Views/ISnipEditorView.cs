using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Snipping.Views;

/// <summary>
///     Shows a snip with the annotations drawn over it, and a toolbar to pick the tool and its color, size and
///     transparency.
///     Points are in snip pixels: (0, 0) is the snip's top left corner.
/// </summary>
public interface ISnipEditorView
{
    /// <summary>Raised when the user presses the left button on the snip.</summary>
    event EventHandler<Point>? PointerPressed;

    /// <summary>Raised while the user moves the mouse with the left button down.</summary>
    event EventHandler<Point>? PointerMoved;

    /// <summary>Raised when the user releases the left button.</summary>
    event EventHandler<Point>? PointerReleased;

    event EventHandler<SnipTool>? ToolSelected;

    event EventHandler<Color>? ColorSelected;

    /// <summary>Raised when the user wants a color that isn't in the palette.</summary>
    event EventHandler? MoreColorsRequested;

    /// <summary>Raised with the line width, or for the text tool the font size, in pixels.</summary>
    event EventHandler<int>? SizeSelected;

    /// <summary>Raised with how see-through to draw, in percent.</summary>
    event EventHandler<int>? TransparencySelected;

    /// <summary>Raised with which ends of an arrow should have a head.</summary>
    event EventHandler<SnipArrowHeads>? ArrowHeadsSelected;

    /// <summary>Raised when the user ends a text begun by <see cref="BeginTextInput"/>; it can be empty.</summary>
    event EventHandler<TextInputModel>? TextCommitted;

    event EventHandler? UndoRequested;

    event EventHandler? RedoRequested;

    event EventHandler? CopyRequested;

    event EventHandler? SaveRequested;

    void ShowTool(SnipTool tool);

    /// <summary>
    ///     Shows the colors to choose from: the fixed ones, and below them the ones picked with More colors. Called
    ///     before the first <see cref="ShowColor"/>, and again when the custom colors change.
    /// </summary>
    void ShowPalette(IReadOnlyList<Color> colors, IReadOnlyList<Color> customColors);

    /// <summary>Shows the current tool's color, which may not be in the palette.</summary>
    void ShowColor(Color color);

    /// <summary>Lets the user pick any color, starting from the current one.</summary>
    /// <param name="current">The color selected first.</param>
    /// <param name="customColors">The colors picked before, offered again.</param>
    /// <returns>The color picked; null when the user cancels.</returns>
    Color? AskColor(Color current, IReadOnlyList<Color> customColors);

    /// <summary>Shows the sizes the current tool can use, in pixels, and selects one of them.</summary>
    void ShowSizes(IReadOnlyList<int> sizes, int selected);

    /// <summary>Shows the transparencies to choose from, in percent, and selects one of them.</summary>
    void ShowTransparencies(IReadOnlyList<int> transparencies, int selected);

    /// <summary>Shows the choice of arrow heads with one selected; null hides it, for the other tools.</summary>
    void ShowArrowHeads(SnipArrowHeads? selected);

    /// <summary>Draws these annotations over the snip, in order, instead of the ones shown before.</summary>
    void ShowAnnotations(IReadOnlyList<AnnotationModel> annotations);

    void ShowUndoRedo(bool canUndo, bool canRedo);

    /// <summary>Lets the user type a text at the location; ends with <see cref="TextCommitted"/>.</summary>
    void BeginTextInput(Point location, Color color, int fontSize);

    /// <summary>Copies the snip with the annotations shown to the clipboard.</summary>
    void CopyImageToClipboard();

    /// <summary>Asks where to save the image.</summary>
    /// <param name="suggestedFileName">The file name offered first.</param>
    /// <returns>The chosen path; null when the user cancels.</returns>
    string? AskSavePath(string suggestedFileName);

    /// <summary>Saves the snip with the annotations shown; the format follows the file extension.</summary>
    void SaveImage(string path);
}
