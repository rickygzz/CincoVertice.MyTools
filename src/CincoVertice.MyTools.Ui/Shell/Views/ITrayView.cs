using CincoVertice.MyTools.Ui.Shell.Models;

namespace CincoVertice.MyTools.Ui.Shell.Views;

/// <summary>
///     The tray icon and its menu.
/// </summary>
public interface ITrayView
{
    /// <summary>Raised when the user asks to reload the hotkeys, e.g. with the menu's Reload hotkeys.</summary>
    event EventHandler? ReloadHotkeysRequested;

    /// <summary>Raised when the menu is about to open, since what it shows may have changed elsewhere.</summary>
    event EventHandler? MenuOpening;

    /// <summary>Raised with what the user asked for with the menu's Start with Windows.</summary>
    event EventHandler<bool>? StartWithWindowsRequested;

    void ShowStartWithWindows(bool isEnabled);

    /// <summary>Fills the Scripts menu with one item per script, in order.</summary>
    void ShowScripts(IReadOnlyList<ScriptMenuItemModel> scripts);

    /// <summary>
    ///     Shows a Hotkey problems menu item that lists all of them; with no problems, there is no such item.
    /// </summary>
    void ShowHotkeyProblems(IReadOnlyList<string> problems);

    /// <summary>Shows a notification that goes away by itself.</summary>
    /// <param name="title">The notification's title.</param>
    /// <param name="text">The notification's text.</param>
    /// <param name="isWarning">True for a warning, false for information.</param>
    void ShowNotification(string title, string text, bool isWarning);
}
