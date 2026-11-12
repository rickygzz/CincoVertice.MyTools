namespace CincoVertice.MyTools.Ui.Zones.Models;

/// <summary>What the mouse would do in the zone editor, shown by its cursor.</summary>
public enum ZoneEditorCursor
{
    /// <summary>On no zone: a drag draws a new one.</summary>
    Draw,

    /// <summary>In a zone: a drag moves it.</summary>
    Move,

    /// <summary>On a left or right border: a drag moves it sideways.</summary>
    ResizeLeftRight,

    /// <summary>On a top or bottom border: a drag moves it up or down.</summary>
    ResizeUpDown,

    /// <summary>On a top left or bottom right handle.</summary>
    ResizeDiagonalDown,

    /// <summary>On a top right or bottom left handle.</summary>
    ResizeDiagonalUp,
}
