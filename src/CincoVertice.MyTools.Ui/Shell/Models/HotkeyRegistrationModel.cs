namespace CincoVertice.MyTools.Ui.Shell.Models;

/// <summary>
///     What registering the hotkeys gave.
/// </summary>
/// <param name="Problems">One per hotkeys.cfg line that is not active, saying why; empty when all are.</param>
/// <param name="Scripts">The scripts that can be run, for the Scripts menu.</param>
public sealed record HotkeyRegistrationModel(
    IReadOnlyList<string> Problems,
    IReadOnlyList<ScriptMenuItemModel> Scripts);
