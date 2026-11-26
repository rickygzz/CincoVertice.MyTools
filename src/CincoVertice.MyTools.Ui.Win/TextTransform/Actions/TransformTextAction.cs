using CincoVertice.MyTools.Ui.TextTransform.Presenters;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.TextTransform.Services;

namespace CincoVertice.MyTools.Ui.Win.TextTransform.Actions;

/// <summary>
///     Transforms the text selected in any program: a menu at the mouse pointer offers UPPER, lower and Title case,
///     trimming, sorting and removing duplicate lines, base64 and URL encoding both ways, and JSON pretty-printed or
///     minified; the one chosen replaces the selection.
/// </summary>
public sealed class TransformTextAction : IAppAction
{
    private readonly TrayNotificationService _notifications;

    public TransformTextAction(TrayNotificationService notifications)
    {
        _notifications = notifications;
    }

    public string Name => "TransformText";

    public string? DefaultShortcut => "Ctrl+Alt+X";

    public Task ExecuteAsync() =>
        new TextTransformPresenter(new DesktopTextTransformHost(_notifications)).RunAsync();
}
