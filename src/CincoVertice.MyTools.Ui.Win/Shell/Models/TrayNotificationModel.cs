namespace CincoVertice.MyTools.Ui.Win.Shell.Models;

/// <summary>A notification for the tray icon to show.</summary>
public record TrayNotificationModel(
    string Title,
    string Text,
    ToolTipIcon Icon);
