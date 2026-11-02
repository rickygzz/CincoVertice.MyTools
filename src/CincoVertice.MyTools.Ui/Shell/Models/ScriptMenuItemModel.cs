namespace CincoVertice.MyTools.Ui.Shell.Models;

/// <summary>
///     A script in the tray's Scripts menu.
/// </summary>
/// <param name="Name">The script's action name, shown in the menu.</param>
/// <param name="RunAsync">Runs the script, as its hotkey would without an argument.</param>
public sealed record ScriptMenuItemModel(string Name, Func<Task> RunAsync);
