using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;

namespace CincoVertice.MyTools.Ui.Win.Zones.Services;

/// <summary>
///     Keeps the window the user works in on top of the others, or no longer: for the ToggleAlwaysOnTop action and
///     window.ontop() in scripts. Windows keeps it so until the window closes, or this changes it again.
/// </summary>
public static class ActiveWindowOnTop
{
    public const string NoWindow = "there's no window to keep on top.";

    public const string Refused =
        "Windows didn't let My Tools change that window: it may run as administrator.";

    /// <param name="isOnTop">Which; null to switch it.</param>
    /// <returns>The window's title, and whether it is on top now.</returns>
    /// <exception cref="ArgumentException">There's no window to change, or Windows refused.</exception>
    public static (string Title, bool IsOnTop) Set(bool? isOnTop)
    {
        nint window = WinUser.GetForegroundWindow();

        if (!WindowHelper.IsMovableWindow(window))
        {
            throw new ArgumentException(NoWindow);
        }

        bool on = isOnTop ?? !WindowHelper.IsTopMost(window);

        if (!WindowHelper.SetTopMost(window, on))
        {
            throw new ArgumentException(Refused);
        }

        return (WindowHelper.GetWindowTitle(window), on);
    }
}
