using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Zones.Services;

namespace CincoVertice.MyTools.Ui.Win.Zones.Actions;

/// <summary>
///     Keeps the active window on top of the others, or no longer, and says which in a notification, as the window
///     doesn't show it.
/// </summary>
public sealed class ToggleAlwaysOnTopAction : IAppAction
{
    public const string NotificationTitle = "Always on top";

    private readonly TrayNotificationService _notifications;

    public ToggleAlwaysOnTopAction(TrayNotificationService notifications)
    {
        _notifications = notifications;
    }

    public string Name => "ToggleAlwaysOnTop";

    public string? DefaultShortcut => "Ctrl+Alt+Win+T";

    public Task ExecuteAsync()
    {
        try
        {
            (string title, bool isOnTop) = ActiveWindowOnTop.Set(null);
            string window = title.Length > 0 ? title : "The window";

            _notifications.Show(
                NotificationTitle,
                isOnTop ? $"{window} stays on top of the other windows." : $"{window} is no longer on top.");
        }
        catch (ArgumentException ex)
        {
            string message = char.ToUpperInvariant(ex.Message[0]) + ex.Message[1..];
            _notifications.Show(NotificationTitle, message, ToolTipIcon.Warning);
        }

        return Task.CompletedTask;
    }
}
