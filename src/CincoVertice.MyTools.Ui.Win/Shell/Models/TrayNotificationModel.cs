namespace CincoVertice.MyTools.Ui.Win.Shell.Models;

/// <summary>A notification for the tray icon to show.</summary>
/// <param name="Clicked">What a click on it does; null for nothing.</param>
public record TrayNotificationModel(
    string Title,
    string Text,
    ToolTipIcon Icon,
    Action? Clicked = null);
