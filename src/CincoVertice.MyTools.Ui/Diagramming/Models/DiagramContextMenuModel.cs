namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     What the menu over the selection can do to it.
/// </summary>
/// <param name="CanPasteStyle">False until a style has been copied.</param>
/// <param name="CanGroup">True when two or more shapes are selected, and not already one group.</param>
/// <param name="CanUngroup">True when a selected shape is in a group.</param>
public sealed record DiagramContextMenuModel(bool CanPasteStyle, bool CanGroup, bool CanUngroup);
