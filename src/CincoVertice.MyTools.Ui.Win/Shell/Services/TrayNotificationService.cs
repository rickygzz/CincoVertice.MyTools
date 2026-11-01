using CincoVertice.MyTools.Ui.Win.Shell.Models;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     Lets code that doesn't own the tray icon show a notification, e.g. notify() in scripts.
///     <see cref="TrayApplicationContext"/> owns the icon and shows each <see cref="Requested"/> notification.
/// </summary>
public sealed class TrayNotificationService
{
    public event EventHandler<TrayNotificationModel>? Requested;

    public void Show(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        Requested?.Invoke(this, new TrayNotificationModel(title, text, icon));
    }
}
