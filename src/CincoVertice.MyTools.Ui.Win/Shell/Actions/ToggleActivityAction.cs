using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

namespace CincoVertice.MyTools.Ui.Win.Shell.Actions;

/// <summary>
///     Turns the mouse mover on or off, e.g. from the global hotkey, and shows the red icon while it's on: it moves the
///     mouse while the user is away, as activity.cfg says. Keeping the computer awake is separate
///     (<see cref="ToggleKeepAwakeAction"/>).
/// </summary>
public sealed class ToggleActivityAction : IAppAction
{
    private readonly MouseMoveSimulator _mouseMoveSimulator;
    private readonly AppIconService _appIconService;
    private readonly ActivitySettingsService _settingsService;
    private readonly TrayNotificationService _notifications;

    public ToggleActivityAction(
        MouseMoveSimulator mouseMoveSimulator,
        AppIconService appIconService,
        ActivitySettingsService settingsService,
        TrayNotificationService notifications)
    {
        _mouseMoveSimulator = mouseMoveSimulator;
        _appIconService = appIconService;
        _settingsService = settingsService;
        _notifications = notifications;
    }

    public string Name => "ToggleActivity";

    public string? DefaultShortcut => "Ctrl+Alt+M";

    /// <summary>Whether the mouse mover is on.</summary>
    public bool IsOn => _mouseMoveSimulator.IsRunning;

    public Task ExecuteAsync()
    {
        if (IsOn)
        {
            _mouseMoveSimulator.Stop();
        }
        else
        {
            TurnOn();
        }

        _appIconService.ShowActive(IsOn);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Reads activity.cfg on every start, so edits apply the next time the mouse mover is turned on.
    /// </summary>
    private void TurnOn()
    {
        ActivitySettingsResultModel result = _settingsService.Load();

        if (result.Problems.Count > 0)
        {
            _notifications.Show("activity.cfg", string.Join(Environment.NewLine, result.Problems), ToolTipIcon.Warning);
        }

        _mouseMoveSimulator.Settings = result.Settings;
        _mouseMoveSimulator.Start();
    }
}
