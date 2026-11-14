using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Interfaces;

/// <summary>
///     Turns text into the outlines of its letters, in the font the editor draws it with, so a file can show it the
///     same on a computer without that font.
/// </summary>
public interface IDiagramTextOutliner
{
    /// <returns>
    ///     The letters of every line, placed as the editor draws them, in diagram pixels; filled with the nonzero rule.
    ///     None for text without anything to draw, e.g. only spaces.
    /// </returns>
    IReadOnlyList<DiagramFigureModel> Outline(DiagramTextBlockModel block, DiagramStyleModel style);
}
