namespace CincoVertice.MyTools.Ui.Win.Behaviors;

/// <summary>
///     Runs a control's setup that depends on its DPI now, and again each time the DPI changes: when its window is
///     made on a monitor with other scaling, or moved to one. The app is per-monitor DPI aware, so WinForms rescales
///     controls and their fonts by itself, but not what a form made once from the DPI: icons drawn at a size, list
///     rows, fonts copied from another.
/// </summary>
public static class DpiFollower
{
    /// <param name="control">The form, or a control in it.</param>
    /// <param name="apply">The setup, given the DPI: 96 at 100%, 120 at 125%.</param>
    public static void Attach(Control control, Action<int> apply)
    {
        int applied = control.DeviceDpi;

        apply(applied);

        void Check(object? sender, EventArgs e)
        {
            if (control.DeviceDpi != applied)
            {
                applied = control.DeviceDpi;
                apply(applied);
            }
        }

        // A form's DPI is known once its window is made, on its monitor; a form raises DpiChanged when it moves, and
        // a control inside it DpiChangedAfterParent
        control.HandleCreated += Check;
        control.DpiChangedAfterParent += Check;

        if (control is Form form)
        {
            // First, so a change while the form is dragged is held until the drop, and not set up here either
            DpiRescaleOnDrop.Attach(form);

            // Raised before WinForms rescales the form's controls: the setup reads their new fonts and sizes after
            form.DpiChanged += (_, e) =>
            {
                if (!e.Cancel)
                {
                    form.BeginInvoke(() => Check(form, EventArgs.Empty));
                }
            };
        }
    }

    /// <returns>A size in pixels at 100% scaling, at the DPI.</returns>
    public static int Scaled(int pixels, int dpi) => (int)Math.Round(pixels * dpi / 96.0);

    /// <returns>
    ///     How much WinForms rescaled the control's font from its size as designed, on a monitor with other scaling than
    ///     the main one: the zoom for what shows at sizes of its own, which don't follow, such as RTF.
    /// </returns>
    public static float FontScale(Control control, float designedSize) => control.Font.Size / designedSize;
}
