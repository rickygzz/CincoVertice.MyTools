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

    /// <summary>Like the pen, with the wobble of the hand evened out.</summary>
    SmoothPen,

    /// <summary>A click drops a circle with the next number, for the steps of a how-to.</summary>
    Step,

    /// <summary>A drag keeps only that part of the snip.</summary>
    Crop,

    /// <summary>Doesn't draw: a click selects what's there, to move, scale, change or delete it.</summary>
    Select,

    /// <summary>A click drops the emoji chosen, in its own colors.</summary>
    Emoji,
}
