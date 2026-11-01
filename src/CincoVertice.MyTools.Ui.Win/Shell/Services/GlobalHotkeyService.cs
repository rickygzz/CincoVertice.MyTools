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
    private readonly IReadOnlyList<IAppAction> _actions;
    private readonly HotkeyConfigService _hotkeyConfig;
    private readonly Hotkey _hotkeys = new();

    public GlobalHotkeyService(
        IEnumerable<IAppAction> actions,
        HotkeyConfigService hotkeyConfig)
    {
        _actions = [.. actions];
        _hotkeyConfig = hotkeyConfig;
    }

    /// <summary>
    ///     Registers every hotkey in hotkeys.cfg.
    /// </summary>
    /// <returns>
    ///     One problem per hotkey that is not active: a line that can't be used, or a shortcut that Windows refused
    ///     (e.g. another application already uses it). Empty when all are active.
    /// </returns>
    public List<string> Start()
    {
        var config = _hotkeyConfig.Load(_actions);
        List<string> problems = [.. config.Problems];

        foreach (var binding in config.Bindings)
        {
            var action = _actions.First(a => a.Name.Equals(binding.ActionName, StringComparison.OrdinalIgnoreCase));

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
