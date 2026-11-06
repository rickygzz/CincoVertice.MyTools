using System.ComponentModel;
using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Snipping.Views;

/// <summary>
///     Shows a snip with the annotations drawn over it, and a toolbar to pick the tool and its color, size and
///     transparency. It keeps the images of all its snips, in the order they arrived, and shows one at a time.
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

    /// <summary>Raised when the user double-clicks with the left button, after the press.</summary>
    event EventHandler<Point>? PointerDoubleClicked;

    /// <summary>Raised while the user moves the mouse with no button down.</summary>
    event EventHandler<Point>? PointerHovered;

    /// <summary>Raised when the user clicks with the right button, as it's released.</summary>
    event EventHandler<Point>? PointerRightClicked;

    /// <summary>Raised when the user wants what's selected gone.</summary>
    event EventHandler? DeleteRequested;

    /// <summary>Raised when the user wants what's selected as it was when first drawn.</summary>
    event EventHandler? ResetRequested;

    /// <summary>Raised when the user wants a copy of what's selected.</summary>
    event EventHandler? DuplicateRequested;

    /// <summary>Raised when the user wants what's selected on top of all the others.</summary>
    event EventHandler? BringToFrontRequested;

    /// <summary>Raised when the user wants what's selected under all the others.</summary>
    event EventHandler? SendToBackRequested;

    /// <summary>Raised with how far to move what's selected, in pixels.</summary>
    event EventHandler<Size>? NudgeRequested;

    /// <summary>Raised when the user wants nothing selected.</summary>
    event EventHandler? DeselectRequested;

    /// <summary>Raised when the user ends a text begun by <see cref="BeginTextInput"/> with Esc; it can be empty.</summary>
    event EventHandler<TextInputModel>? TextEscaped;

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

    /// <summary>Raised with how round corners should be, in pixels; 0 for square ones.</summary>
    event EventHandler<int>? CornerRadiusSelected;

    /// <summary>Raised with how wide the edge around the text's letters should be, in pixels; 0 for none.</summary>
    event EventHandler<int>? TextOutlineWidthSelected;

    event EventHandler<Color>? TextOutlineColorSelected;

    /// <summary>Raised when the user wants an edge color that isn't in the palette.</summary>
    event EventHandler? MoreTextOutlineColorsRequested;

    /// <summary>Raised when the user ends a text begun by <see cref="BeginTextInput"/>; it can be empty.</summary>
    event EventHandler<TextInputModel>? TextCommitted;

    /// <summary>Raised when the view was given another snip, after any text being typed has ended.</summary>
    event EventHandler? SnipAdded;

    /// <summary>Raised to go to another snip: -1 for the one before, 1 for the one after.</summary>
    event EventHandler<int>? SnipStepRequested;

    /// <summary>Raised when the user wants the crop being adjusted applied.</summary>
    event EventHandler? CropApplyRequested;

    /// <summary>Raised when the user drops the crop being adjusted.</summary>
    event EventHandler? CropCancelRequested;

    /// <summary>Raised to show the snip larger (1), smaller (-1), or at its own size (0).</summary>
    event EventHandler<int>? ZoomRequested;

    /// <summary>Raised with true when the user wants the window kept out of the next snips' screenshots.</summary>
    event EventHandler<bool>? HideWhileSnippingSelected;

    /// <summary>Raised when the user closes the window; cancel to keep it open.</summary>
    event EventHandler<CancelEventArgs>? CloseRequested;

    event EventHandler? UndoRequested;

    event EventHandler? RedoRequested;

    event EventHandler? CopyRequested;

    event EventHandler? SaveRequested;

    void ShowTool(SnipTool tool);

    /// <returns>How large one of the snips is, in pixels, before any crop.</returns>
    Size GetSnipSize(int index);

    /// <summary>
    ///     Shows only this part of the snip shown, the same in what's copied or saved; null shows all of it. Points stay in
    ///     the pixels of the whole snip. Called before <see cref="ShowSnip"/> for another snip, and on its own when the crop
    ///     changes.
    /// </summary>
    void ShowCrop(Rectangle? crop);

    /// <summary>
    ///     Shows the part a crop being adjusted would keep, with handles to change it and a way to apply or drop it, on the
    ///     screen only; null for none.
    /// </summary>
    void ShowCropPreview(Rectangle? area);

    /// <summary>Shows one of the snips, and which one it is. Its annotations follow in <see cref="ShowAnnotations"/>.</summary>
    /// <param name="index">Which one, from 0.</param>
    /// <param name="count">How many there are.</param>
    void ShowSnip(int index, int count);

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

    /// <summary>Shows or hides the choices of color, size and transparency, which the blur tool doesn't have.</summary>
    void ShowDrawingChoices(bool isVisible);

    /// <summary>Shows the choice of arrow heads with one selected; null hides it, for the other tools.</summary>
    void ShowArrowHeads(SnipArrowHeads? selected);

    /// <summary>Shows how round corners are, and the radii to choose from; null hides it, for what has no corners.</summary>
    void ShowCornerRadius(IReadOnlyList<int> radii, int? selected);

    /// <summary>Shows the edge of the text's letters and the widths to choose from; null hides it, for the other tools.</summary>
    void ShowTextOutline(IReadOnlyList<int> widths, SnipTextOutlineModel? outline);

    /// <summary>Draws these annotations over the snip, in order, instead of the ones shown before.</summary>
    void ShowAnnotations(IReadOnlyList<AnnotationModel> annotations);

    /// <summary>Shows what's selected, on screen only: it's never part of the copied or saved image. Null for nothing.</summary>
    void ShowSelection(SnipSelectionModel? selection);

    void ShowCursor(SnipCursor cursor);

    /// <summary>Shows what can be done to what's selected, at a point of the snip.</summary>
    /// <param name="location">Where to show it.</param>
    /// <param name="menu">Which of the items can be used.</param>
    void ShowContextMenu(Point location, SnipContextMenuModel menu);

    /// <returns>How large a text is when drawn with this font size.</returns>
    Size MeasureText(string text, int fontSize);

    void ShowUndoRedo(bool canUndo, bool canRedo);

    /// <summary>
    ///     Shows the snip this large, in percent. Points in the events stay in snip pixels; what's copied or saved stays
    ///     at the snip's own size.
    /// </summary>
    void ShowZoom(int percent);

    /// <summary>Shows whether the window is kept out of the next snips' screenshots.</summary>
    void ShowHideWhileSnipping(bool isHidden);

    /// <summary>Lets the user type a text at the location; ends with <see cref="TextCommitted"/>.</summary>
    /// <param name="location">Where it goes.</param>
    /// <param name="color">The color of the text.</param>
    /// <param name="fontSize">In pixels.</param>
    /// <param name="text">The text to start from.</param>
    void BeginTextInput(Point location, Color color, int fontSize, string text);

    /// <summary>Copies the snip with the annotations shown to the clipboard.</summary>
    void CopyImageToClipboard();

    /// <summary>Asks where to save the image.</summary>
    /// <param name="suggestedFileName">The file name offered first.</param>
    /// <returns>The chosen path; null when the user cancels.</returns>
    string? AskSavePath(string suggestedFileName);

    /// <summary>Saves the snip with the annotations shown; the format follows the file extension.</summary>
    void SaveImage(string path);

    /// <summary>Asks whether to close the window, losing the snips that weren't saved.</summary>
    /// <param name="count">How many weren't.</param>
    /// <returns>True to close it.</returns>
    bool AskDiscardSnips(int count);
}
