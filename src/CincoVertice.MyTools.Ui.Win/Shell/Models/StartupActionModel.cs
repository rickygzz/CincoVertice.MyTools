namespace CincoVertice.MyTools.Ui.Win.Shell.Models;

/// <summary>
///     One line of startup.cfg: an action to run when Windows starts the app.
/// </summary>
/// <param name="ActionName">The action's name, spelled as the action spells it.</param>
/// <param name="Argument">The value after the action, for script.argument(); null for none.</param>
public sealed record StartupActionModel(string ActionName, string? Argument);
