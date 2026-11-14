namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     How far to shift something so it lines up, and the alignment lines to show.
/// </summary>
public sealed record DiagramSnapModel(int DeltaX, int DeltaY, IReadOnlyList<DiagramGuideModel> Guides);
