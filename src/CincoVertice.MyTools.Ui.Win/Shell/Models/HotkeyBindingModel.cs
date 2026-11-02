using CincoVertice.Common.WinApi.Libs.Enums;

namespace CincoVertice.MyTools.Ui.Win.Shell.Models;

/// <summary>
///     One line of hotkeys.cfg: the action to run and its shortcut.
/// </summary>
/// <param name="ActionName">The action's name, as the action spells it.</param>
/// <param name="Shortcut">The shortcut as written in the file, e.g. "Ctrl+Alt+M".</param>
/// <param name="Modifiers">The shortcut's modifier keys.</param>
/// <param name="Key">The shortcut's key.</param>
/// <param name="Argument">
///     The value after the shortcut, e.g. "app1" in "PrTemplate = Ctrl+Alt+1, app1"; null for none.
/// </param>
public sealed record HotkeyBindingModel(
    string ActionName,
    string Shortcut,
    FSModifiers Modifiers,
    KeyCode Key,
    string? Argument = null);
