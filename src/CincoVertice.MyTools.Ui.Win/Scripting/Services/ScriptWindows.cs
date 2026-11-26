using System.Diagnostics;
using CincoVertice.Common.Application.Scripting.Models;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Win.Zones.Services;

namespace CincoVertice.MyTools.Ui.Win.Scripting.Services;

/// <summary>
///     Other programs' windows for scripts, by their handle: found by program or title, hidden, shown, minimized and so
///     on. A window hidden by a script is still found, after those shown, so it can be shown again.
/// </summary>
public static class ScriptWindows
{
    // How long a change waits for the window's program to do it, and how often it looks
    private const int ChangeWaitMilliseconds = 1000;
    private const int ChangeCheckMilliseconds = 20;

    public const string Closed = "the window is closed.";

    public static bool IsProcessRunning(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);

        foreach (Process process in processes)
        {
            process.Dispose();
        }

        return processes.Length > 0;
    }

    /// <summary>The windows of the programs with the name: those shown, used last first, then those hidden.</summary>
    public static IReadOnlyList<nint> OfProcess(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        HashSet<uint> ids = [.. processes.Select(process => (uint)process.Id)];

        foreach (Process process in processes)
        {
            process.Dispose();
        }

        return ids.Count == 0
            ? []
            : Matching(window => WinUser.GetWindowThreadProcessId(window, out uint id) != 0 && ids.Contains(id));
    }

    /// <summary>
    ///     The window with the text in its title, ignoring case: the one used last of those shown, or a hidden one.
    /// </summary>
    /// <returns>The window, or 0 for none.</returns>
    public static nint ByTitle(string titlePart) =>
        Matching(window => WindowHelper.GetWindowTitle(window).Contains(titlePart, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

    public static ScriptWindowState StateOf(nint window) =>
        !User32.IsWindow(window) ? ScriptWindowState.Closed
        : !User32.IsWindowVisible(window) ? ScriptWindowState.Hidden
        : WinUser.IsIconic(window) ? ScriptWindowState.Minimized
        : User32.IsZoomed(window) ? ScriptWindowState.Maximized
        : ScriptWindowState.Normal;

    /// <summary>
    ///     Asks the window's program for the change without waiting for it, which a program that doesn't respond would
    ///     make forever, then waits up to <see cref="ChangeWaitMilliseconds"/> for the window to show it.
    /// </summary>
    /// <returns>False when the window is closed, or Windows refused.</returns>
    public static async Task<bool> ChangeAsync(nint window, ScriptWindowChange change)
    {
        if (!User32.IsWindow(window))
        {
            return false;
        }

        if (change == ScriptWindowChange.Close)
        {
            // As its X: the program may ask to save first, so it isn't waited for
            return User32.PostMessage(window, WM.WM_CLOSE, 0, 0);
        }

        SW command = change switch
        {
            ScriptWindowChange.Hide => SW.SW_HIDE,
            ScriptWindowChange.Show => SW.SW_SHOWNA,
            ScriptWindowChange.Minimize => SW.SW_MINIMIZE,
            ScriptWindowChange.Maximize => SW.SW_SHOWMAXIMIZED,
            _ => SW.SW_RESTORE,
        };

        if (!User32.ShowWindowAsync(window, command))
        {
            return false;
        }

        for (int waited = 0;
            waited < ChangeWaitMilliseconds && !IsDone(window, change);
            waited += ChangeCheckMilliseconds)
        {
            await Task.Delay(ChangeCheckMilliseconds);
        }

        return User32.IsWindow(window);
    }

    /// <summary>Shown if hidden, restored if minimized, then in front with the focus.</summary>
    /// <returns>Whether it's in front afterwards.</returns>
    public static bool Activate(nint window)
    {
        if (!User32.IsWindow(window))
        {
            return false;
        }

        if (!User32.IsWindowVisible(window))
        {
            WinUser.ShowWindow(window, SW.SW_SHOW);
        }

        return WindowHelper.BringToFront(window);
    }

    /// <param name="isOnTop">Which; null to switch it.</param>
    /// <returns>Whether it is on top afterwards.</returns>
    /// <exception cref="ArgumentException">The window is closed, or Windows refused.</exception>
    public static bool SetOnTop(nint window, bool? isOnTop)
    {
        if (!User32.IsWindow(window))
        {
            throw new ArgumentException(Closed);
        }

        bool on = isOnTop ?? !WindowHelper.IsTopMost(window);

        return WindowHelper.SetTopMost(window, on) ? on : throw new ArgumentException(ActiveWindowOnTop.Refused);
    }

    private static bool IsDone(nint window, ScriptWindowChange change) => StateOf(window) switch
    {
        ScriptWindowState.Closed => true,
        ScriptWindowState.Hidden => change == ScriptWindowChange.Hide,
        ScriptWindowState.Minimized => change is ScriptWindowChange.Minimize or ScriptWindowChange.Show,
        ScriptWindowState.Maximized => change is ScriptWindowChange.Maximize or ScriptWindowChange.Show,
        _ => change is ScriptWindowChange.Restore or ScriptWindowChange.Show,
    };

    /// <summary>
    ///     The top-level windows that match, from the top: first those the user moves around and sees (not cloaked, as
    ///     on another virtual desktop), then hidden ones that would be such a window if shown.
    /// </summary>
    private static List<nint> Matching(Func<nint, bool> isMatch)
    {
        List<nint> shown = [];
        List<nint> hidden = [];

        User32.EnumWindows(
            (window, _) =>
            {
                if (WindowHelper.IsMovableWindow(window))
                {
                    if (!WindowHelper.IsCloaked(window) && isMatch(window))
                    {
                        shown.Add(window);
                    }
                }
                else if (IsHiddenAppWindow(window) && isMatch(window))
                {
                    hidden.Add(window);
                }

                return true;
            },
            0);

        return [.. shown, .. hidden];
    }

    /// <summary>
    ///     A hidden window with a title and a title bar, of no other window, and not a tool window: as a window the user
    ///     works in is once hidden. Programs keep many hidden windows of their own, which have none of these.
    /// </summary>
    private static bool IsHiddenAppWindow(nint window)
    {
        if (User32.IsWindowVisible(window)
            || User32.GetWindow(window, User32.GetWindowCmd.GW_OWNER) != 0
            || WindowHelper.GetWindowTitle(window).Length == 0)
        {
            return false;
        }

        long style = User32.GetWindowLongPtr(window, User32.GWL_STYLE);
        long extendedStyle = User32.GetWindowLongPtr(window, User32.GWL_EXSTYLE);

        return (extendedStyle & (long)WS.WS_EX_TOOLWINDOW) == 0 && (style & User32.WS_CAPTION) == User32.WS_CAPTION;
    }
}
