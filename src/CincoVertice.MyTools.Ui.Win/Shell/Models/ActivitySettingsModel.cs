namespace CincoVertice.MyTools.Ui.Win.Shell.Models;

/// <summary>
///     How activity moves the mouse, from activity.cfg.
/// </summary>
/// <param name="Interval">Milliseconds between checks of the mouse.</param>
/// <param name="IdleTicksBeforeMove">Checks to wait after the user moves the mouse before moving it again.</param>
/// <param name="SpeedMin">The fastest move: 20 to 100, where higher is slower.</param>
/// <param name="SpeedMax">The slowest move, at least <paramref name="SpeedMin"/>.</param>
/// <param name="MoveImmediately">
///     True to move the mouse as soon as activity is turned on; false to wait first, as after the user moves it.
/// </param>
public sealed record ActivitySettingsModel(
    int Interval,
    int IdleTicksBeforeMove,
    int SpeedMin,
    int SpeedMax,
    bool MoveImmediately)
{
    public static ActivitySettingsModel Default { get; } = new(5000, 20, 20, 100, true);
}
