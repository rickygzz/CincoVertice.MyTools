namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A row of the layers panel: a shape, a connector or a line, front first.
/// </summary>
/// <param name="Id">The shape's or the connector's id; they never share one.</param>
/// <param name="Text">Its name, or else its kind and the start of its text, e.g. "Rectangle: Start".</param>
/// <param name="Name">The name the user gave it, to start renaming from; empty for none.</param>
/// <param name="IsConnector">True for a connector or a line, which are always in front of the shapes.</param>
/// <param name="GroupId">The group it's selected and moved with; null for none.</param>
/// <param name="IsHidden">True when it isn't drawn.</param>
/// <param name="IsLocked">True when clicks on the canvas go through it.</param>
public sealed record DiagramLayerModel(
    int Id,
    string Text,
    string Name,
    bool IsConnector,
    int? GroupId = null,
    bool IsHidden = false,
    bool IsLocked = false);
