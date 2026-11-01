using CincoVertice.Common.WinApi.Hotkey;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     Registers the application-wide hotkeys from hotkeys.cfg, each running its <see cref="IAppAction"/>.
///     Must be started on the UI thread, since the hotkey window receives its messages there.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private readonly AppActionCatalog _actionCatalog;
    private readonly HotkeyConfigService _hotkeyConfig;
    private readonly Hotkey _hotkeys = new();

    public GlobalHotkeyService(
        AppActionCatalog actionCatalog,
        HotkeyConfigService hotkeyConfig)
    {
        _actionCatalog = actionCatalog;
        _hotkeyConfig = hotkeyConfig;
    }

    /// <summary>
    ///     The actions as of the last <see cref="Register"/>, including one per script.
    /// </summary>
    public IReadOnlyList<IAppAction> Actions { get; private set; } = [];

    /// <summary>
    ///     Replaces the registered hotkeys with the ones in hotkeys.cfg, for the scripts in the scripts folder now.
    /// </summary>
    /// <returns>
    ///     One problem per hotkey that is not active: a line that can't be used, or a shortcut that Windows refused
    ///     (e.g. another application already uses it). Empty when all are active.
    /// </returns>
    public List<string> Register()
    {
        _hotkeys.UnregisterAll();
        Actions = _actionCatalog.GetActions();

        var config = _hotkeyConfig.Load(Actions);
        List<string> problems = [.. config.Problems];

        foreach (var binding in config.Bindings)
        {
            var action = Actions.First(a => a.Name.Equals(binding.ActionName, StringComparison.OrdinalIgnoreCase));

            // No repeat: holding the keys down runs the action once, which matters for toggles
            var hotkey = new HotkeyData(
                binding.Modifiers | FSModifiers.MOD_NOREPEAT,
                binding.Key,
                async (_, _) => await action.ExecuteAsync());

            if (!_hotkeys.TryRegisterHotKey(hotkey, out string error))
            {
                problems.Add($"{binding.ActionName}: {binding.Shortcut} is not available. {error}");
            }
        }

        return problems;
    }

    public void Dispose()
    {
        _hotkeys.Dispose();
    }
}
