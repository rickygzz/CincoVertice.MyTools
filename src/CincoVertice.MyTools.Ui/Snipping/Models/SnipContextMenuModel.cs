namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     What the menu over a selected annotation can do to it.
/// </summary>
/// <param name="CanReset">True when it has changed since it was drawn.</param>
/// <param name="CanBringToFront">False when it's on top of all the others already.</param>
/// <param name="CanSendToBack">False when it's under all the others already.</param>
public sealed record SnipContextMenuModel(bool CanReset, bool CanBringToFront, bool CanSendToBack);
