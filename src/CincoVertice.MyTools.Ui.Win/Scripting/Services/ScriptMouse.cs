using System.Runtime.InteropServices;
using CincoVertice.Common.Application.Scripting.Models;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.Common.WinApi.Libs.Structs;

namespace CincoVertice.MyTools.Ui.Win.Scripting.Services;

/// <summary>
///     The mouse for scripts: where the pointer is and putting it somewhere, in physical screen pixels on every monitor,
///     and its buttons and wheel, sent as the mouse sends them (SendInput), so any program takes them.
/// </summary>
public static class ScriptMouse
{
    // One step of the wheel, as Windows counts it
    private const int WheelDelta = 120;

    public static Point Position()
    {
        using (DpiAwarenessScope.PerMonitorV2())
        {
            User32.GetCursorPos(out POINT point);

            return new Point(point.X, point.Y);
        }
    }

    /// <exception cref="ArgumentException">Windows refused, e.g. while a program running as administrator has the
    /// mouse.</exception>
    public static void MoveTo(Point point)
    {
        using (DpiAwarenessScope.PerMonitorV2())
        {
            if (User32.SetCursorPos(point.X, point.Y) == 0)
            {
                throw new ArgumentException("Windows didn't let the pointer move.");
            }
        }
    }

    public static void Press(ScriptMouseButton button, bool isDown)
    {
        MouseInputDwFlags flags = (button, isDown) switch
        {
            (ScriptMouseButton.Right, true) => MouseInputDwFlags.MOUSEEVENTF_RIGHTDOWN,
            (ScriptMouseButton.Right, false) => MouseInputDwFlags.MOUSEEVENTF_RIGHTUP,
            (ScriptMouseButton.Middle, true) => MouseInputDwFlags.MOUSEEVENTF_MIDDLEDOWN,
            (ScriptMouseButton.Middle, false) => MouseInputDwFlags.MOUSEEVENTF_MIDDLEUP,
            (_, true) => MouseInputDwFlags.MOUSEEVENTF_LEFTDOWN,
            _ => MouseInputDwFlags.MOUSEEVENTF_LEFTUP,
        };

        Send(InputSimulator.MouseInputClick(flags));
    }

    /// <param name="notches">Up, away from the user, for a positive number; down for a negative one.</param>
    public static void Scroll(int notches)
    {
        Send(new INPUT
        {
            Type = InputType.INPUT_MOUSE,
            MKHInput = new MOUSEKEYBDHARDWAREINPUT
            {
                MouseInput = new MOUSEINPUT
                {
                    MouseData = unchecked((MouseInputMouseData)(uint)(WheelDelta * notches)),
                    DwFlags = MouseInputDwFlags.MOUSEEVENTF_WHEEL,
                    DwExtraInfo = User32.GetMessageExtraInfo(),
                },
            },
        });
    }

    private static void Send(INPUT input)
    {
        if (User32.SendInput(1, [input], Marshal.SizeOf<INPUT>()) != 1)
        {
            throw new ArgumentException("Windows didn't take the mouse input, e.g. while a program running as "
                + "administrator is in front.");
        }
    }
}
