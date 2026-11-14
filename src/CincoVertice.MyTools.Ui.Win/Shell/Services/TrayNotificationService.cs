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

    /// <param name="title">Its title.</param>
    /// <param name="text">What it says.</param>
    /// <param name="clicked">What a click on it does, e.g. show a file in its folder.</param>
    public void Show(string title, string text, Action clicked)
    {
        Requested?.Invoke(this, new TrayNotificationModel(title, text, ToolTipIcon.Info, clicked));
    }
}
