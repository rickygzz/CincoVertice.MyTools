namespace CincoVertice.MyTools.Ui.Win.Behaviors;

/// <summary>
///     Keeps a tray icon's menu at the pointer that opened it. WinForms places the menu, its corner at the pointer, with
///     the size it had last; on a monitor with other scaling, the menu is then scaled while it opens and grows or
///     shrinks away from the pointer (100 pixels and more, measured, from a monitor at 125% to one at 100%). The menu
///     is made at the scaling Windows had at sign-in, so it happened after docking or undocking too.
/// </summary>
public static class TrayMenuAnchor
{
    /// <summary>
    ///     Places the menu again, with its final size, each time it opens from a right click on the icon. Its corner goes
    ///     at the pointer, as WinForms puts it: above and to the left, or below or to the right where the screen ends.
    /// </summary>
    public static void Attach(NotifyIcon icon, ContextMenuStrip menu)
    {
        Point? anchor = null;

        // WinForms reads the pointer for the menu right after raising this, so both read the same place
        icon.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                anchor = Cursor.Position;
            }
        };

        void Place()
        {
            if (anchor is Point at && menu.Visible)
            {
                Point placed = PlacedAt(at, menu.Size, Screen.FromPoint(at).Bounds);

                if (menu.Location != placed)
                {
                    menu.Location = placed;
                }
            }
        }

        menu.SizeChanged += (_, _) => Place();
        menu.Opened += (_, _) => Place();
        menu.Closed += (_, _) => anchor = null;
    }

    /// <summary>
    ///     Where a menu of this size goes for a click at this point: its bottom right corner at it, else its top or left
    ///     edge at it where the screen ends above or to the left, and always inside the screen.
    /// </summary>
    public static Point PlacedAt(Point anchor, Size size, Rectangle screen)
    {
        int x = anchor.X - size.Width < screen.Left ? anchor.X : anchor.X - size.Width;
        int y = anchor.Y - size.Height < screen.Top ? anchor.Y : anchor.Y - size.Height;

        return new Point(
            Math.Max(screen.Left, Math.Min(x, screen.Right - size.Width)),
            Math.Max(screen.Top, Math.Min(y, screen.Bottom - size.Height)));
    }
}
