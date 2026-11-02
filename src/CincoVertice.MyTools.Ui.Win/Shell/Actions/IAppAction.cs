namespace CincoVertice.MyTools.Ui.Win.Shell.Actions;

/// <summary>
///     Something the user starts, from a hotkey or a menu. Registered actions can be bound to a shortcut in
///     hotkeys.cfg by <see cref="Name"/>.
/// </summary>
public interface IAppAction
{
    /// <summary>The name used in hotkeys.cfg, e.g. "ToggleActivity". Keep it stable.</summary>
    string Name { get; }

    /// <summary>Shortcut written to a new hotkeys.cfg, e.g. "Ctrl+Alt+M"; null for no hotkey by default.</summary>
    string? DefaultShortcut { get; }

    Task ExecuteAsync();
}
