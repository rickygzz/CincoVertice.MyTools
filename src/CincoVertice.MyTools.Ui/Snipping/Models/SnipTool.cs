namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     What a drag (or, for <see cref="Text"/>, a click) does in the snip editor.
/// </summary>
public enum SnipTool
{
    Pen,
    Highlighter,
    Arrow,
    Rectangle,
    Text,
    Blur,

    /// <summary>Doesn't draw: a click selects what's there, to move, scale, change or delete it.</summary>
    Select,
}
