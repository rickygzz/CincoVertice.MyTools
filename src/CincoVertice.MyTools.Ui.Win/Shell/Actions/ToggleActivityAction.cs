using System.ComponentModel;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

namespace CincoVertice.MyTools.Ui.Win.Shell.Actions;

/// <summary>
///     Turns activity on or off, e.g. from the global hotkey, and shows the red icon while it's on. Activity moves the
///     mouse while the user is away, keeps the computer awake, or both, as activity.cfg says. Run on the UI thread:
///     Windows ties keeping awake to the thread that asks, until that thread asks again.
/// </summary>
public sealed class ToggleActivityAction : IAppAction
{
    private readonly MouseMoveSimulator _mouseMoveSimulator;
    private readonly AwakeKeeper _awakeKeeper;
    private readonly AppIconService _appIconService;
    private readonly ActivitySettingsService _settingsService;
    private readonly TrayNotificationService _notifications;

    public ToggleActivityAction(
        MouseMoveSimulator mouseMoveSimulator,
        AwakeKeeper awakeKeeper,
        AppIconService appIconService,
        ActivitySettingsService settingsService,
        TrayNotificationService notifications)
    {
        _mouseMoveSimulator = mouseMoveSimulator;
        _awakeKeeper = awakeKeeper;
        _appIconService = appIconService;
        _settingsService = settingsService;
        _notifications = notifications;
    }

    public string Name => "ToggleActivity";

    public string? DefaultShortcut => "Ctrl+Alt+M";

    /// <summary>Whether activity is on: the mouse is moved, the computer is kept awake, or both.</summary>
    public bool IsOn => _mouseMoveSimulator.IsRunning || _awakeKeeper.IsOn;

    public Task ExecuteAsync()
    {
        if (IsOn)
        {
            _mouseMoveSimulator.Stop();
            _awakeKeeper.TurnOff();
        }
        else
        {
            TurnOn();
        }

        _appIconService.ShowActive(IsOn);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Reads activity.cfg on every start, so edits apply the next time activity is turned on.
    /// </summary>
    private void TurnOn()
    {
        ActivitySettingsResultModel result = _settingsService.Load();

        if (result.Problems.Count > 0)
        {
            _notifications.Show("activity.cfg", string.Join(Environment.NewLine, result.Problems), ToolTipIcon.Warning);
        }

        if (result.MoveMouse)
        {
            _mouseMoveSimulator.Settings = result.Settings;
            _mouseMoveSimulator.Start();
        }

        if (result.KeepAwake)
        {
            try
            {
                _awakeKeeper.TurnOn();
            }
            catch (Win32Exception ex)
            {
                _notifications.Show("activity.cfg", $"{ex.Message} The computer may sleep.", ToolTipIcon.Warning);
            }
        }
    }
}
