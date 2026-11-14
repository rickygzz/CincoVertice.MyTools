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

    /// <summary>Writes a copy of the diagram as a PDF file, which can't be opened here again.</summary>
    ExportPdf,

    /// <summary>Asks where, and how large, then writes the diagram as a PNG picture.</summary>
    ExportPng,
    Undo,
    Redo,
    Cut,
    Copy,
    Paste,
    Duplicate,

    /// <summary>Asks for SVG images, such as icons, and adds them where the mouse was last.</summary>
    InsertImage,

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

    /// <summary>Lines up the left edges of the selected shapes with the leftmost; a group moves as one.</summary>
    AlignLeft,

    /// <summary>Lines up the middles of the selected shapes, left to right, with the middle of all of them.</summary>
    AlignCenter,

    AlignRight,

    AlignTop,

    /// <summary>Lines up the middles of the selected shapes, top to bottom, with the middle of all of them.</summary>
    AlignMiddle,

    AlignBottom,

    /// <summary>The same space between each two of the selected shapes, side by side; the outer two stay.</summary>
    DistributeHorizontally,

    /// <summary>The same space between each two of the selected shapes, top to bottom; the outer two stay.</summary>
    DistributeVertically,

    /// <summary>Gives the selected images the size they were inserted at, with their own proportions.</summary>
    ResetSize,
    ZoomIn,
    ZoomOut,
    ZoomReset,

    /// <summary>Esc: ends what's in progress, or else goes back to the select tool and clears the selection.</summary>
    Cancel,
}
