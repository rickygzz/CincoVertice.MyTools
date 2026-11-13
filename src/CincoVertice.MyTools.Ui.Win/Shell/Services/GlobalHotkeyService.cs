using CincoVertice.Common.WinApi.Hotkey;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Shell.Interfaces;
using CincoVertice.MyTools.Ui.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Scripting.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     Registers the application-wide hotkeys from hotkeys.cfg, each running its <see cref="IAppAction"/>, and
///     tells when hotkeys.cfg or the scripts change. Must be used on the UI thread, since the hotkey window receives
///     its messages there.
/// </summary>
public sealed class GlobalHotkeyService : IHotkeyRegistrar, IDisposable
{
    private readonly AppActionCatalog _actionCatalog;
    private readonly HotkeyConfigService _hotkeyConfig;
    private readonly HotkeyFilesWatcher _filesWatcher;
    private readonly Hotkey _hotkeys = new();

    public GlobalHotkeyService(
        AppActionCatalog actionCatalog,
        HotkeyConfigService hotkeyConfig,
        HotkeyFilesWatcher filesWatcher)
    {
        _actionCatalog = actionCatalog;
        _hotkeyConfig = hotkeyConfig;
        _filesWatcher = filesWatcher;

        _filesWatcher.Changed += FilesWatcher_Changed;
    }

    public event EventHandler? FilesChanged;

    public void WatchFiles()
    {
        _filesWatcher.Start();
    }

    /// <summary>
    ///     Replaces the registered hotkeys with the ones in hotkeys.cfg, for the scripts in the scripts folder now.
    /// </summary>
    /// <returns>
    ///     One problem per hotkey that is not active: a line that can't be used, or a shortcut that Windows refused
    ///     (e.g. another application already uses it); and the scripts and the other actions, with
    ///     their hotkeys, for the Scripts and Actions menus.
    /// </returns>
    public HotkeyRegistrationModel Register()
    {
        _hotkeys.UnregisterAll();
        var actions = _actionCatalog.GetActions();

        var config = _hotkeyConfig.Load(actions);
        List<string> problems = [.. config.Problems];

        // The hotkey of each action that works, for the menus: the one without a value, else the first with one
        Dictionary<string, string> shortcuts = new(StringComparer.OrdinalIgnoreCase);

        foreach (var binding in config.Bindings)
        {
            var action = actions.First(a => a.Name.Equals(binding.ActionName, StringComparison.OrdinalIgnoreCase));
            Func<Task> execute;

            if (binding.Argument is null)
            {
                execute = action.ExecuteAsync;
            }
            else if (action is IArgumentAppAction argumentAction)
            {
                string argument = binding.Argument;
                execute = () => argumentAction.ExecuteAsync(argument);
            }
            else
            {
                problems.Add($"{binding.ActionName}: takes no value after the shortcut ({binding.Argument}).");

                continue;
            }

            // No repeat: holding the keys down runs the action once, which matters for toggles
            var hotkey = new HotkeyData(
                binding.Modifiers | FSModifiers.MOD_NOREPEAT,
                binding.Key,
                async (_, _) => await execute());

            if (!_hotkeys.TryRegisterHotKey(hotkey, out string error))
            {
                problems.Add($"{binding.ActionName}: {binding.Shortcut} is not available. {error}");
            }
            else if (binding.Argument is null)
            {
                shortcuts[action.Name] = binding.Shortcut;
            }
            else
            {
                shortcuts.TryAdd(action.Name, $"{binding.Shortcut} ({binding.Argument})");
            }
        }

        List<ScriptMenuItemModel> scripts =
        [
            .. actions.OfType<ScriptAction>()
                .Select(s => new ScriptMenuItemModel(s.Name, s.ExecuteAsync, shortcuts.GetValueOrDefault(s.Name))),
        ];

        List<ActionMenuItemModel> others =
        [
            .. actions.Where(a => a is not ScriptAction)
                .Select(a => new ActionMenuItemModel(a.Name, shortcuts.GetValueOrDefault(a.Name), a.ExecuteAsync)),
        ];

        return new HotkeyRegistrationModel(problems, scripts, others);
    }

    private void FilesWatcher_Changed(object? sender, EventArgs e)
    {
        FilesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _filesWatcher.Changed -= FilesWatcher_Changed;
        _hotkeys.Dispose();
    }
}
