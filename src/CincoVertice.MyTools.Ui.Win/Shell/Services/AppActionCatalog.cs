using CincoVertice.Common.Application.Scripting;
using CincoVertice.MyTools.Ui.Win.Scripting.Actions;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     Every action that can be bound in hotkeys.cfg: the built-in ones, then one per script in the scripts folder as
///     it is now. Built-in actions come first, so they win a name clash with a script.
/// </summary>
public sealed class AppActionCatalog
{
    private readonly IReadOnlyList<IAppAction> _builtInActions;
    private readonly ScriptEngine _engine;
    private readonly TrayNotificationService _notifications;
    private readonly string _scriptsFolderPath;

    // Kept across reloads, so a script that is running isn't started again by its new hotkey
    private readonly Dictionary<string, ScriptAction> _scriptActions = new(StringComparer.OrdinalIgnoreCase);

    public AppActionCatalog(
        IEnumerable<IAppAction> builtInActions,
        ScriptEngine engine,
        TrayNotificationService notifications,
        string scriptsFolderPath)
    {
        _builtInActions = [.. builtInActions];
        _engine = engine;
        _notifications = notifications;
        _scriptsFolderPath = scriptsFolderPath;
    }

    /// <summary>
    ///     The actions, reading the scripts folder again.
    /// </summary>
    public IReadOnlyList<IAppAction> GetActions()
    {
        List<IAppAction> actions = [.. _builtInActions];

        foreach (string scriptFile in ScriptCatalog.GetScriptFiles(_scriptsFolderPath))
        {
            if (ScriptCatalog.IsLibrary(scriptFile) || ScriptCatalog.GetActionName(scriptFile).Length == 0)
            {
                continue;
            }

            if (!_scriptActions.TryGetValue(scriptFile, out var script))
            {
                script = new ScriptAction(scriptFile, _engine, _notifications);
                _scriptActions.Add(scriptFile, script);
            }

            actions.Add(script);
        }

        return actions;
    }
}
