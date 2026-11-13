using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Zones.Services;

namespace CincoVertice.MyTools.Ui.Win.Zones.Actions;

/// <summary>Moves the active window to the next zone of its monitor, from the last one to the first.</summary>
public sealed class MoveToNextZoneAction : IAppAction
{
    private readonly ZoneMover _mover;
    private readonly TrayNotificationService _notifications;

    public MoveToNextZoneAction(ZoneMover mover, TrayNotificationService notifications)
    {
        _mover = mover;
        _notifications = notifications;
    }

    public string Name => "MoveToNextZone";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        if (_mover.MoveActiveWindowBy(1) is string problem)
        {
            _notifications.Show(MoveToZoneAction.NotificationTitle, problem, ToolTipIcon.Warning);
        }

        return Task.CompletedTask;
    }
}
