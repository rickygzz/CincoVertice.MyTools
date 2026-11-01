using CincoVertice.MyTools.Ui.Shell.Models;

namespace CincoVertice.MyTools.Ui.Shell.Interfaces;

/// <summary>
///     The application-wide hotkeys, as the tray sees them: registered from hotkeys.cfg, for the actions and scripts
///     there are now.
/// </summary>
public interface IHotkeyRegistrar
{
    /// <summary>Raised on the UI thread after hotkeys.cfg changed, or a script was added, removed or renamed.</summary>
    event EventHandler? FilesChanged;

    /// <summary>
    ///     Replaces the registered hotkeys with the ones in hotkeys.cfg. Must be called on the UI thread.
    /// </summary>
    HotkeyRegistrationModel Register();

    /// <summary>
    ///     Starts raising <see cref="FilesChanged"/>. Must be called on the UI thread.
    /// </summary>
    void WatchFiles();
}
