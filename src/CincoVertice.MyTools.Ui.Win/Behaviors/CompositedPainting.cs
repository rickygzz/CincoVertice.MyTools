using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;

namespace CincoVertice.MyTools.Ui.Win.Behaviors;

/// <summary>
///     Has Windows paint a window and all its controls into one buffer, shown when they're all painted
///     (WS_EX_COMPOSITED), so they don't flicker. While the user resizes a form, each of its controls is otherwise erased
///     and painted on its own, one after the other, which a slower computer shows: a panel of fields blinked at every
///     step. Only while the form is moved or resized (<see cref="DpiRescaleOnDrop"/> turns it on and off): always on,
///     every caret blink and hover would paint the whole window again.
/// </summary>
public static class CompositedPainting
{
    public static void Set(nint window, bool isOn)
    {
        long style = User32.GetWindowLongPtr(window, User32.GWL_EXSTYLE);
        long changed = isOn ? style | (long)WS.WS_EX_COMPOSITED : style & ~(long)WS.WS_EX_COMPOSITED;

        if (changed != style)
        {
            User32.SetWindowLongPtr(window, User32.GWL_EXSTYLE, (nint)changed);
        }
    }

    public static bool IsOn(nint window) =>
        (User32.GetWindowLongPtr(window, User32.GWL_EXSTYLE) & (long)WS.WS_EX_COMPOSITED) != 0;
}
