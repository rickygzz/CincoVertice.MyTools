using CincoVertice.MyTools.Ui.ColorPicker.Models;
using CincoVertice.MyTools.Ui.ColorPicker.Services;
using CincoVertice.MyTools.Ui.Win.ColorPicker.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.ColorPicker.Actions;

/// <summary>
///     Freezes the screen and copies the color of the pixel the user clicks, as HEX (Ctrl+click: RGB), and tells it
///     in a notification with the other ways to write it.
/// </summary>
public sealed class PickColorAction : IAppAction
{
    public const string NotificationTitle = "My Tools";

    private readonly ScreenColorPicker _picker;
    private readonly TrayNotificationService _notifications;

    public PickColorAction(ScreenColorPicker picker, TrayNotificationService notifications)
    {
        _picker = picker;
        _notifications = notifications;
    }

    public string Name => "PickColor";

    public string? DefaultShortcut => "Ctrl+Alt+P";

    public async Task ExecuteAsync()
    {
        PickedColorModel? picked = await _picker.PickAsync();

        if (picked is null)
        {
            return;
        }

        Clipboard.SetText(picked.Text);
        _notifications.Show(NotificationTitle, ColorText.CopiedMessage(picked));
    }
}
