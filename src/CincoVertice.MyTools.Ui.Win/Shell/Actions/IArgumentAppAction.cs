namespace CincoVertice.MyTools.Ui.Win.Shell.Actions;

/// <summary>
///     An action that can take a value from its hotkeys.cfg line, as in "PrTemplate = Ctrl+Alt+1, app1", so one
///     action can have several hotkeys that do slightly different things.
/// </summary>
public interface IArgumentAppAction : IAppAction
{
    /// <param name="argument">The value after the shortcut, e.g. "app1".</param>
    Task ExecuteAsync(string argument);
}
