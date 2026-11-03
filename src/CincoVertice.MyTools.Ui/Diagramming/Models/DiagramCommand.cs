namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     What the user can ask for from the toolbar or the keyboard.
/// </summary>
public enum DiagramCommand
{
    New,
    Open,
    Save,
    SaveAs,
    Undo,
    Redo,
    Cut,
    Copy,
    Paste,
    Duplicate,
    Delete,
    SelectAll,
    Group,
    Ungroup,
    BringToFront,
    SendToBack,

    /// <summary>Takes the bends off the selected connectors, so they follow their routing again.</summary>
    ResetRoute,
    ZoomIn,
    ZoomOut,
    ZoomReset,

    /// <summary>Esc: ends what's in progress, or else goes back to the select tool and clears the selection.</summary>
    Cancel,
}
