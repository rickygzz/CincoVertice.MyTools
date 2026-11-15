using CincoVertice.Common.WinApi.Helpers;

namespace CincoVertice.MyTools.Ui.Win.Behaviors;

/// <summary>
///     A message box for an action started from a hotkey or the tray, before any window of its own: it shows in the
///     middle of the screen the mouse pointer is on, in front and with the keyboard focus. Without a window of the
///     app as its owner, Windows would put it on the main screen, maybe behind the window in use.
/// </summary>
public static class CursorScreenMessageBox
{
    public static DialogResult Show(
        string text,
        string caption,
        MessageBoxButtons buttons,
        MessageBoxIcon icon,
        MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
    {
        using Form owner = CreateOwner();

        owner.Show();
        WindowHelper.ForceForeground(owner.Handle);
        owner.Activate();

        return MessageBox.Show(owner, text, caption, buttons, icon, defaultButton);
    }

    /// <summary>
    ///     An owner that can't be seen, in the middle of the working area of the screen the pointer is on: Windows
    ///     centers a message box on its owner.
    /// </summary>
    internal static Form CreateOwner()
    {
        Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;

        return new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Size = new Size(1, 1),
            Location = new Point(area.Left + area.Width / 2, area.Top + area.Height / 2),
            Opacity = 0,
            TopMost = true,
        };
    }
}
