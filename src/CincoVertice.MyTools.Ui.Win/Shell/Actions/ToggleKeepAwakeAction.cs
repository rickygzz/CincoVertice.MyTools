using System.ComponentModel;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Shell.Actions;

/// <summary>
///     Turns keeping the computer awake on or off, e.g. from the global hotkey, whether the mouse mover is on or not:
///     no sleep, and the screen stays on, even locked. Run on the UI thread: Windows ties keeping awake to the thread
///     that asks, until that thread asks again.
/// </summary>
public sealed class ToggleKeepAwakeAction : IAppAction
{
    public const string NotificationTitle = "Keep awake";
    public const string OnMessage = "The computer stays awake, with the screen on, until you turn this off.";
    public const string OffMessage = "The computer can sleep again, as Windows' settings say.";

    private readonly AwakeKeeper _awakeKeeper;
    private readonly AppIconService _appIconService;
    private readonly TrayNotificationService _notifications;

    public ToggleKeepAwakeAction(
        AwakeKeeper awakeKeeper,
        AppIconService appIconService,
        TrayNotificationService notifications)
    {
        _awakeKeeper = awakeKeeper;
        _appIconService = appIconService;
        _notifications = notifications;
    }

    public string Name => "ToggleKeepAwake";

    public string? DefaultShortcut => "Ctrl+Alt+K";

    public bool IsOn => _awakeKeeper.IsOn;

    public Task ExecuteAsync()
    {
        if (IsOn)
        {
            _awakeKeeper.TurnOff();
            _notifications.Show(NotificationTitle, OffMessage);
        }
        else
        {
            try
            {
                _awakeKeeper.TurnOn();
                _notifications.Show(NotificationTitle, OnMessage);
            }
            catch (Win32Exception ex)
            {
                _notifications.Show(NotificationTitle, $"{ex.Message} The computer may sleep.", ToolTipIcon.Warning);
            }
        }

        _appIconService.ShowKeepingAwake(IsOn);

        return Task.CompletedTask;
    }
}
