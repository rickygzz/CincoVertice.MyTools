namespace CincoVertice.MyTools.Ui.Zones.Models;

/// <summary>
///     Where the mouse is on the editor's monitor, in percent of its work area, and how near counts as on a border.
/// </summary>
/// <param name="ToleranceX">How far from a border, across, still grabs it, in percent of the width.</param>
/// <param name="ToleranceY">The same, in percent of the height.</param>
public sealed record ZoneEditorPointerModel(double X, double Y, double ToleranceX, double ToleranceY);
