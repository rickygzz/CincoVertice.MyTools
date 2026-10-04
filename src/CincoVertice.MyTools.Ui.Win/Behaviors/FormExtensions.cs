namespace CincoVertice.MyTools.Ui.Win.Behaviors;

public static class FormExtensions
{
    /// <summary>
    ///     Centers the form on the screen the mouse cursor is on.
    /// </summary>
    public static void CenterOnCursorScreen(this Form form)
    {
        Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;

        form.Location = new Point(
            area.Left + (area.Width - form.Width) / 2,
            area.Top + (area.Height - form.Height) / 2);
    }

    /// <summary>
    ///     Makes a user close (X, Alt+F4, Close()) hide the form instead, so a singleton form is not disposed and can
    ///     be shown again. Windows shutdown, Task Manager, Application.Exit, etc. still close it.
    /// </summary>
    public static void HideOnUserClose(this Form form)
    {
        form.FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                form.Hide();
            }
        };
    }
}
