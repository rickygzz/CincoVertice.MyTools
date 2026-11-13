using System.ComponentModel;
using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Views;

/// <summary>
///     Shows a diagram on a canvas, with toolbars to pick the tool and the style. Points in the events are canvas
///     pixels; the presenter converts them with the viewport it keeps.
/// </summary>
public interface IDiagramEditorView
{
    /// <summary>Raised when the user presses a mouse button on the canvas.</summary>
    event EventHandler<DiagramPointerModel>? PointerPressed;

    /// <summary>Raised when the mouse moves on the canvas, with or without a button down.</summary>
    event EventHandler<DiagramPointerModel>? PointerMoved;

    /// <summary>Raised when the user releases the button.</summary>
    event EventHandler<DiagramPointerModel>? PointerReleased;

    /// <summary>Raised when the user double-clicks on the canvas, after the press.</summary>
    event EventHandler<DiagramPointerModel>? PointerDoubleClicked;

    /// <summary>Raised when the user right-clicks the canvas, with where, in canvas pixels.</summary>
    event EventHandler<Point>? PointerRightClicked;

    /// <summary>Raised when the mouse leaves the canvas.</summary>
    event EventHandler? PointerLeft;

    event EventHandler<DiagramWheelModel>? WheelTurned;

    event EventHandler<DiagramTool>? ToolSelected;

    event EventHandler<DiagramCommand>? CommandRequested;

    /// <summary>Raised with how far to move the selection, in diagram pixels.</summary>
    event EventHandler<Size>? NudgeRequested;

    event EventHandler<DiagramColorChoiceModel>? ColorSelected;

    /// <summary>Raised when the user wants a color that isn't in the palette.</summary>
    event EventHandler<DiagramColorTarget>? MoreColorsRequested;

    event EventHandler<int>? LineWidthSelected;

    event EventHandler<bool>? DashedSelected;

    event EventHandler<int>? FontSizeSelected;

    /// <summary>Raised with the name of the font the text should have.</summary>
    event EventHandler<string>? FontFamilySelected;

    /// <summary>Raised with how wide the edge around the text's letters should be, in pixels; 0 for none.</summary>
    event EventHandler<int>? TextOutlineWidthSelected;

    event EventHandler<DiagramRouting>? RoutingSelected;

    event EventHandler<DiagramArrowHeads>? ArrowHeadsSelected;

    /// <summary>Raised when the user types where the selected shapes should be, all of them together.</summary>
    event EventHandler<Rectangle>? BoundsEdited;

    /// <summary>Raised when the user locks or frees the X, Y, width or height of the selected shapes.</summary>
    event EventHandler<DiagramLockChoiceModel>? LockToggled;

    /// <summary>Raised with how round the corners of the selected shapes should be, in pixels.</summary>
    event EventHandler<int>? CornerRadiusEdited;

    /// <summary>Raised with the text of the selected shape, or the label of the selected connector.</summary>
    event EventHandler<string>? TextEdited;

    /// <summary>Raised when the user ends a text begun by <see cref="BeginTextEdit"/>, not when they cancel it.</summary>
    event EventHandler<string>? TextCommitted;

    /// <summary>Raised with true when the user shows the properties panel, and false when they hide it.</summary>
    event EventHandler<bool>? PropertiesPanelToggled;

    /// <summary>Raised with the window's size as it closes, once closing wasn't cancelled.</summary>
    event EventHandler<DiagramWindowSizeModel>? WindowClosed;

    /// <summary>Raised before the window closes; cancel to keep it open.</summary>
    event EventHandler<CancelEventArgs>? Closing;

    /// <summary>Draws these instead of what was shown before.</summary>
    void ShowScene(DiagramSceneModel scene);

    void ShowTool(DiagramTool tool);

    /// <summary>Shows the colors to choose from. Called once, before the first <see cref="ShowStyle"/>.</summary>
    void ShowPalette(IReadOnlyList<Color> colors);

    /// <summary>Shows the choices for line widths, font sizes and text edges, in pixels. Called once.</summary>
    void ShowSizes(IReadOnlyList<int> lineWidths, IReadOnlyList<int> fontSizes, IReadOnlyList<int> textOutlineWidths);

    /// <summary>Shows the style of what's selected, or of what comes next when nothing is.</summary>
    void ShowStyle(DiagramStyleModel style, DiagramRouting routing, DiagramArrowHeads heads);

    /// <summary>Shows or hides the properties panel, as it was saved. Called once, before the window shows.</summary>
    void ShowPropertiesPanel(bool isVisible);

    /// <summary>Makes the window this large, as it was saved. Called once at most, before the window shows.</summary>
    void ShowWindowSize(DiagramWindowSizeModel size);

    /// <summary>Shows the properties of the selection, which change while it's dragged too.</summary>
    void ShowProperties(DiagramPropertiesModel properties);

    void ShowUndoRedo(bool canUndo, bool canRedo);

    /// <summary>Shows the menu over what's selected, whose items ask for their <see cref="DiagramCommand"/>.</summary>
    /// <param name="location">Where, in canvas pixels.</param>
    /// <param name="menu">Which of its items can be used.</param>
    void ShowContextMenu(Point location, DiagramContextMenuModel menu);

    /// <summary>Shows the name of the diagram.</summary>
    /// <param name="name">The file's name, or a default.</param>
    /// <param name="hasChanges">True when it has unsaved changes.</param>
    void ShowDocument(string name, bool hasChanges);

    /// <summary>Shows how large the diagram is drawn, in percent.</summary>
    void ShowZoom(int percent);

    void ShowCursor(DiagramCursor cursor);

    /// <summary>Lets the user type over a part of the canvas; ends with <see cref="TextCommitted"/>.</summary>
    /// <param name="bounds">Where, in canvas pixels.</param>
    /// <param name="text">The text to start from, selected.</param>
    /// <param name="fontSize">In canvas pixels.</param>
    /// <param name="fontFamily">The name of the font.</param>
    /// <param name="color">The color of the text.</param>
    void BeginTextEdit(Rectangle bounds, string text, int fontSize, string fontFamily, Color color);

    /// <summary>Lets the user pick any color.</summary>
    /// <returns>The color; null when the user cancels.</returns>
    Color? AskColor(Color current);

    /// <returns>The file to save to; null when the user cancels.</returns>
    string? AskSavePath(string suggestedFileName);

    /// <returns>The PDF file to export to; null when the user cancels.</returns>
    string? AskExportPdfPath(string suggestedFileName);

    /// <returns>The file to open; null when the user cancels.</returns>
    string? AskOpenPath();

    /// <summary>Asks what to do with the unsaved changes.</summary>
    /// <param name="name">The name of the diagram.</param>
    DiagramSaveChoice AskSaveChanges(string name);

    /// <returns>Whether to replace a file that was changed elsewhere since it was opened or saved here.</returns>
    bool AskOverwrite(string name);

    void ShowError(string message);

    void SetClipboardText(string text);

    /// <returns>The text on the clipboard; null when there's none.</returns>
    string? GetClipboardText();
}
