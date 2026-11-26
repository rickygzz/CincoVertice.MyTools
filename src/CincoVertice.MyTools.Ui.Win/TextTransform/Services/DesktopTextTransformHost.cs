using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.MyTools.Ui.TextTransform.Interfaces;
using CincoVertice.MyTools.Ui.TextTransform.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.TextTransform.Services;

/// <summary>
///     The text transforms on the desktop: the selection copied from the window in front, a menu at the mouse pointer,
///     the result pasted back in that window. One per use, on the UI thread, which the clipboard needs.
/// </summary>
public sealed class DesktopTextTransformHost : ITextTransformHost
{
    public const string NotificationTitle = "Transform text";

    private readonly TrayNotificationService _notifications;

    // The window the text was copied from, to give it back the focus the menu takes
    private nint _window;

    public DesktopTextTransformHost(TrayNotificationService notifications)
    {
        _notifications = notifications;
    }

    public Task<string?> CopySelectionAsync()
    {
        _window = WinUser.GetForegroundWindow();

        return DesktopClipboard.CopySelectionAsync();
    }

    public Task<TextTransformKind?> AskTransformAsync(IReadOnlyList<TextTransformModel> transforms)
    {
        var chosen = new TaskCompletionSource<TextTransformKind?>();
        Form owner = CreateOwner();
        ContextMenuStrip menu = CreateMenu(transforms);

        menu.ItemClicked += (_, e) =>
        {
            if (e.ClickedItem?.Tag is TextTransformKind kind)
            {
                chosen.TrySetResult(kind);
            }
        };

        // After the item's click, which comes first; the window the text came from gets the focus back either way
        menu.Closed += (_, _) => owner.BeginInvoke(() =>
        {
            chosen.TrySetResult(null);
            menu.Dispose();
            owner.Dispose();
            GiveBackFocus();
        });

        menu.Show(owner, Point.Empty);

        // The first transform selected, so the arrows and Enter work at once, as the letters do
        menu.Items[0].Select();

        return chosen.Task;
    }

    public async Task PasteAsync(string text)
    {
        GiveBackFocus();
        await DesktopClipboard.PasteAsync(text);
    }

    public void ShowProblem(string message) => _notifications.Show(NotificationTitle, message, ToolTipIcon.Warning);

    /// <summary>The menu: each transform, its letter underlined, the groups between lines.</summary>
    internal static ContextMenuStrip CreateMenu(IReadOnlyList<TextTransformModel> transforms)
    {
        var menu = new ContextMenuStrip { ShowImageMargin = false };

        for (int i = 0; i < transforms.Count; i++)
        {
            if (i > 0 && transforms[i].Group != transforms[i - 1].Group)
            {
                menu.Items.Add(new ToolStripSeparator());
            }

            menu.Items.Add(new ToolStripMenuItem(transforms[i].Title) { Tag = transforms[i].Kind });
        }

        return menu;
    }

    private void GiveBackFocus()
    {
        if (_window != 0 && WinUser.GetForegroundWindow() != _window)
        {
            WindowHelper.ForceForeground(_window);
        }
    }

    /// <summary>
    ///     A window of this app in front, for the menu to have the keyboard and close at a click elsewhere: a tray app's
    ///     menu otherwise gets neither. Transparent, 1 pixel big, at the mouse pointer, where the menu opens.
    /// </summary>
    private static Form CreateOwner()
    {
        var owner = new Form
        {
            TopMost = true,
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.None,
            Opacity = 0,
            StartPosition = FormStartPosition.Manual,
            Bounds = new Rectangle(Cursor.Position, new Size(1, 1)),
        };

        owner.Show();
        WindowHelper.ForceForeground(owner.Handle);

        return owner;
    }
}
