using System.ComponentModel;
using System.Drawing;
using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Zones.Views;

/// <summary>
///     The window zones editor: a monitor drawn at its shape, with the zones over it, numbered from 1, and a toolbar.
///     Points are in percent of the monitor's work area: (0, 0) is its top left corner, (100, 100) its bottom right.
/// </summary>
public interface IZoneEditorView
{
    /// <summary>Raised when the user presses the left button on the monitor.</summary>
    event EventHandler<ZoneEditorPointerModel>? PointerPressed;

    /// <summary>Raised while the user moves the mouse with the left button down.</summary>
    event EventHandler<ZoneEditorPointerModel>? PointerMoved;

    /// <summary>Raised when the user releases the left button.</summary>
    event EventHandler<ZoneEditorPointerModel>? PointerReleased;

    /// <summary>Raised while the user moves the mouse with no button down.</summary>
    event EventHandler<ZoneEditorPointerModel>? PointerHovered;

    /// <summary>Raised when the user clicks with the right button, as it's released.</summary>
    event EventHandler<ZoneEditorPointerModel>? PointerRightClicked;

    event EventHandler<ZoneLayoutTemplate>? TemplateSelected;

    /// <summary>Raised with the gap the user picked, in pixels.</summary>
    event EventHandler<int>? GapSelected;

    /// <summary>Raised with the layout to edit: 0 for the default one, 1 for the first monitor's, and so on.</summary>
    event EventHandler<int>? TargetSelected;

    /// <summary>Raised when the monitor edited should use the default layout again, instead of its own.</summary>
    event EventHandler? UseDefaultRequested;

    /// <summary>Raised to split the selected zone into a left and a right one, where it was right-clicked.</summary>
    event EventHandler? SplitVerticallyRequested;

    /// <summary>Raised to split the selected zone into a top and a bottom one, where it was right-clicked.</summary>
    event EventHandler? SplitHorizontallyRequested;

    event EventHandler? DeleteRequested;

    event EventHandler? UndoRequested;

    event EventHandler? RedoRequested;

    event EventHandler? SaveRequested;

    /// <summary>Raised when the user wants zones.cfg in the config editor instead.</summary>
    event EventHandler? EditAsTextRequested;

    /// <summary>Raised when the user closes the window; cancel to keep it open.</summary>
    event EventHandler<CancelEventArgs>? CloseRequested;

    /// <summary>Lists the layouts to edit, the default one first, then a monitor each, and shows one.</summary>
    /// <param name="names">What to call each.</param>
    /// <param name="selected">The one being edited.</param>
    /// <param name="shape">The size of the work area to draw the zones on, in pixels.</param>
    void ShowTargets(IReadOnlyList<string> names, int selected, Size shape);

    /// <summary>Shows whose layout is being edited.</summary>
    /// <param name="isMonitor">A monitor's; false for the default layout.</param>
    /// <param name="isOwn">The monitor has its own; false when it uses the default one until it's changed.</param>
    void ShowOwnLayout(bool isMonitor, bool isOwn);

    /// <summary>Shows the gaps to choose from, in pixels, and selects one of them.</summary>
    void ShowGaps(IReadOnlyList<int> gaps, int selected);

    /// <summary>
    ///     Draws the zones, numbered from 1 in this order, as they'll be on the monitor shown, and the selected one's
    ///     handles, at its corners and the middles of its sides.
    /// </summary>
    /// <param name="zones">In percent of the work area.</param>
    /// <param name="selected">The index of the zone selected; null for none.</param>
    /// <param name="gap">The gap around each zone, in pixels of the monitor.</param>
    void ShowZones(IReadOnlyList<ZoneModel> zones, int? selected, int gap);

    void ShowCursor(ZoneEditorCursor cursor);

    /// <summary>Shows what can be done to the selected zone, at a point of the monitor, in percent.</summary>
    void ShowContextMenu(double x, double y);

    void ShowUndoRedo(bool canUndo, bool canRedo);

    /// <summary>Shows whether there are changes that aren't saved.</summary>
    void ShowUnsaved(bool hasUnsaved);

    /// <summary>Tells the user something went wrong, without waiting for them.</summary>
    void ShowProblem(string message);

    /// <summary>Asks whether to save the changes before going on.</summary>
    /// <returns>True to save them, false to drop them, null to stay.</returns>
    bool? AskSaveChanges();

    /// <summary>Closes the window, without raising <see cref="CloseRequested"/>.</summary>
    void CloseView();
}
