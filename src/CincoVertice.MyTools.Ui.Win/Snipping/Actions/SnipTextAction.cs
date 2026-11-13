using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Actions;

/// <summary>
///     Like <see cref="SnipRegionAction"/>, but copies the text in the snip, as text, and tells how many lines it
///     copied.
/// </summary>
public sealed class SnipTextAction : IAppAction
{
    public const string NotificationTitle = "My Tools";

    private readonly ScreenSnipper _snipper;
    private readonly SnipEditorWindowService _editorWindow;
    private readonly ITextRecognizer _textRecognizer;
    private readonly TrayNotificationService _notifications;

    public SnipTextAction(
        ScreenSnipper snipper,
        SnipEditorWindowService editorWindow,
        ITextRecognizer textRecognizer,
        TrayNotificationService notifications)
    {
        _snipper = snipper;
        _editorWindow = editorWindow;
        _textRecognizer = textRecognizer;
        _notifications = notifications;
    }

    public string Name => "SnipText";

    public string? DefaultShortcut => "Ctrl+Alt+T";

    public async Task ExecuteAsync()
    {
        Bitmap? snip;

        using (_editorWindow.KeepOutOfSnip())
        {
            snip = await _snipper.SnipAsync();
        }

        if (snip is null)
        {
            return;
        }

        byte[] image;

        using (snip)
        {
            image = PngEncoder.Encode(snip);
        }

        IReadOnlyList<string> lines;

        try
        {
            lines = RecognizedText.Format(await _textRecognizer.ReadLinesAsync(image));
        }
        catch (TextRecognitionException ex)
        {
            _notifications.Show(NotificationTitle, ex.Message, ToolTipIcon.Warning);

            return;
        }

        if (lines.Count == 0)
        {
            _notifications.Show(NotificationTitle, RecognizedText.NoneFoundMessage);

            return;
        }

        Clipboard.SetText(RecognizedText.Join(lines));
        _notifications.Show(NotificationTitle, RecognizedText.CopiedMessage(lines.Count));
    }
}
