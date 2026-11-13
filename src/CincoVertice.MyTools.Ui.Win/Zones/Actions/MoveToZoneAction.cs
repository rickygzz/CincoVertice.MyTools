using System.Globalization;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Zones.Services;

namespace CincoVertice.MyTools.Ui.Win.Zones.Actions;

/// <summary>
///     Moves the active window to a zone of its monitor, given by its number after the shortcut in hotkeys.cfg:
///     "MoveToZone = Ctrl+Alt+1, 1". One line per zone.
/// </summary>
public sealed class MoveToZoneAction : IArgumentAppAction
{
    public const string NotificationTitle = "My Tools";

    private readonly ZoneMover _mover;
    private readonly TrayNotificationService _notifications;

    public MoveToZoneAction(ZoneMover mover, TrayNotificationService notifications)
    {
        _mover = mover;
        _notifications = notifications;
    }

    public string Name => "MoveToZone";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        Report($"{Name} needs the zone's number after the shortcut, e.g. {Name} = Ctrl+Alt+1, 1.");

        return Task.CompletedTask;
    }

    /// <param name="argument">The zone's number, from 1.</param>
    public Task ExecuteAsync(string argument)
    {
        if (!int.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture, out int number))
        {
            Report($"{Name}: '{argument}' is not a zone's number, e.g. {Name} = Ctrl+Alt+1, 1.");
        }
        else if (_mover.MoveActiveWindowTo(number) is string problem)
        {
            Report(problem);
        }

        return Task.CompletedTask;
    }

    private void Report(string problem) => _notifications.Show(NotificationTitle, problem, ToolTipIcon.Warning);
}
