namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A name typed for a row of the layers panel.
/// </summary>
/// <param name="Id">The shape's or the connector's id.</param>
/// <param name="Name">The name; empty to have none again.</param>
public sealed record DiagramLayerRenameModel(int Id, string Name);
