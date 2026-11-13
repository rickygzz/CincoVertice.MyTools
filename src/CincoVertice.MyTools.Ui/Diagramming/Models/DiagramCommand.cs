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

    /// <summary>Keeps the style of what's selected, for <see cref="PasteStyle"/>.</summary>
    CopyStyle,

    /// <summary>Gives what's selected, and what comes next, the style kept by <see cref="CopyStyle"/>.</summary>
    PasteStyle,
    Delete,
    SelectAll,
    Group,
    Ungroup,
    BringToFront,
    SendToBack,

    /// <summary>Makes the selected connectors work out their own corners again.</summary>
    ResetRoute,
    ZoomIn,
    ZoomOut,
    ZoomReset,

    /// <summary>Esc: ends what's in progress, or else goes back to the select tool and clears the selection.</summary>
    Cancel,
}
