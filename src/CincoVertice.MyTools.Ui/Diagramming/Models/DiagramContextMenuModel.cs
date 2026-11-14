namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     What the menu over the selection can do to it.
/// </summary>
/// <param name="CanPasteStyle">False until a style has been copied.</param>
/// <param name="CanGroup">True when two or more shapes are selected, and not already one group.</param>
/// <param name="CanUngroup">True when a selected shape is in a group.</param>
/// <param name="CanAlign">True when two or more shapes, or groups, are selected.</param>
/// <param name="CanDistribute">True when three or more shapes, or groups, are selected.</param>
public sealed record DiagramContextMenuModel(
    bool CanPasteStyle,
    bool CanGroup,
    bool CanUngroup,
    bool CanAlign = false,
    bool CanDistribute = false);
