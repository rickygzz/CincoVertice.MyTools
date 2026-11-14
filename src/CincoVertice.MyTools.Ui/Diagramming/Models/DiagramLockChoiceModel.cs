namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A lock the user turned on or off for the selected shapes.
/// </summary>
/// <param name="Lock">One of X, Y, Width, Height and AspectRatio.</param>
/// <param name="IsLocked">True to lock it, false to free it.</param>
public sealed record DiagramLockChoiceModel(DiagramLock Lock, bool IsLocked);
