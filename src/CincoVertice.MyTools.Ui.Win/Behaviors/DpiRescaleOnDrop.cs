using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.Common.WinApi.Libs.Structs;

namespace CincoVertice.MyTools.Ui.Win.Behaviors;

/// <summary>
///     Rescales a form for another monitor's scaling once, when the user lets go of it, not while they drag it across.
///     WinForms rescales a whole form each time it crosses between monitors with other scaling, a quarter of a second
///     for a large one, and the window grows or shrinks under the mouse, which can put it back on the other monitor
///     and start again: a drag would stutter at every crossing. While the form is moved, it keeps its size and its
///     controls theirs, and the DPI messages Windows sends its child controls wait too; at the drop, the change is
///     played again, as Windows sends it, as if the form had just crossed.
/// </summary>
public sealed class DpiRescaleOnDrop : NativeWindow
{
    // Windows asks it how large to be at another DPI; WinForms answers from its controls
    private const uint WM_GETDPISCALEDSIZE = 0x02E4;

    // What Windows sends each child window around the form's WM_DPICHANGED: a control remakes its font then
    private const int WM_DPICHANGED_BEFOREPARENT = 0x02E2;
    private const int WM_DPICHANGED_AFTERPARENT = 0x02E3;

    private static readonly ConditionalWeakTable<Form, DpiRescaleOnDrop> _attached = new();

    private readonly Form _form;

    // Moved by the user now; the DPI its controls are scaled to; and while the change is played again
    private bool _isMoving;
    private int _scaledDpi;
    private bool _isReplaying;

    // The controls whose own DPI messages wait for the drop too, while the form is moved
    private readonly List<ChildHold> _children = [];

    private DpiRescaleOnDrop(Form form)
    {
        _form = form;
        _scaledDpi = form.DeviceDpi;
    }

    /// <summary>For the form's life; once per form, however often it's called.</summary>
    public static void Attach(Form form)
    {
        if (_attached.TryGetValue(form, out _))
        {
            return;
        }

        var behavior = new DpiRescaleOnDrop(form);
        _attached.Add(form, behavior);

        form.DpiChanged += behavior.OnDpiChanged;
        form.HandleCreated += (_, _) =>
        {
            behavior._scaledDpi = form.DeviceDpi;
            behavior.AssignHandle(form.Handle);
        };
        form.HandleDestroyed += (_, _) => behavior.ReleaseHandle();

        if (form.IsHandleCreated)
        {
            behavior.AssignHandle(form.Handle);
        }
    }

    protected override void WndProc(ref Message m)
    {
        switch ((uint)m.Msg)
        {
            case (uint)WM.WM_ENTERSIZEMOVE:
                _isMoving = true;
                _scaledDpi = _form.DeviceDpi;
                HoldChildren();
                break;

            case (uint)WM.WM_EXITSIZEMOVE:
                base.WndProc(ref m);
                _isMoving = false;
                ReleaseChildren();

                if (_form.DeviceDpi != _scaledDpi)
                {
                    Replay(_scaledDpi, _form.DeviceDpi);
                }

                return;
        }

        base.WndProc(ref m);
    }

    private void OnDpiChanged(object? sender, DpiChangedEventArgs e)
    {
        // Neither the size Windows suggests nor the controls change until the drop
        if (_isMoving || _isReplaying)
        {
            e.Cancel = true;
        }
        else
        {
            _scaledDpi = e.DeviceDpiNew;
        }
    }

    private void HoldChildren()
    {
        ReleaseChildren();

        foreach (Control control in Descendants(_form).Where(control => control.IsHandleCreated))
        {
            var hold = new ChildHold();
            hold.AssignHandle(control.Handle);
            _children.Add(hold);
        }
    }

    private void ReleaseChildren()
    {
        _children.ForEach(hold => hold.ReleaseHandle());
        _children.Clear();
    }

    // Parents before their children
    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;

            foreach (Control descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    ///     The crossing again, as Windows sends it: back to the DPI the controls are at, which changes nothing; then
    ///     to every child, from the bottom up, that its form's DPI changes; to the form, the monitor's DPI, at the size
    ///     WinForms asks for, around the window's middle, so it stays on the monitor it was dropped on; and to every
    ///     child, from the top down, that it did.
    /// </summary>
    private void Replay(int from, int to)
    {
        nint window = _form.Handle;
        Rectangle bounds = _form.Bounds;

        _isReplaying = true;

        try
        {
            SendDpiChanged(window, from, bounds);
        }
        finally
        {
            _isReplaying = false;
        }

        List<nint> children = [.. Descendants(_form).Where(control => control.IsHandleCreated).Select(c => c.Handle)];

        for (int i = children.Count - 1; i >= 0; i--)
        {
            User32.SendMessage(children[i], WM_DPICHANGED_BEFOREPARENT, 0, 0);
        }

        Size size = ScaledSize(window, to, bounds.Size, (float)to / from);
        Rectangle target = _form.WindowState == FormWindowState.Normal
            ? new Rectangle(
                bounds.Left + (bounds.Width - size.Width) / 2,
                bounds.Top + (bounds.Height - size.Height) / 2,
                size.Width,
                size.Height)
            : bounds;

        // Its title bar where it can be grabbed again
        Rectangle area = Screen.FromRectangle(bounds).WorkingArea;
        target.Y = Math.Max(target.Y, area.Top);

        SendDpiChanged(window, to, target);
        _scaledDpi = to;

        foreach (nint child in children)
        {
            User32.SendMessage(child, WM_DPICHANGED_AFTERPARENT, 0, 0);
        }
    }

    // As Windows sends it: the DPI in both words, the suggested bounds in screen pixels
    private static void SendDpiChanged(nint window, int dpi, Rectangle bounds)
    {
        nint rect = Marshal.AllocHGlobal(Marshal.SizeOf<RECT>());

        try
        {
            Marshal.StructureToPtr(
                new RECT { Left = bounds.Left, Top = bounds.Top, Right = bounds.Right, Bottom = bounds.Bottom },
                rect,
                false);
            User32.SendMessage(window, (uint)WM.WM_DPICHANGED, (nint)(dpi | (dpi << 16)), rect);
        }
        finally
        {
            Marshal.FreeHGlobal(rect);
        }
    }

    // What WinForms answers Windows for the new DPI; the size scaled when it doesn't
    private static Size ScaledSize(nint window, int dpi, Size size, float factor)
    {
        nint answer = Marshal.AllocHGlobal(Marshal.SizeOf<WindowSize>());

        try
        {
            Marshal.StructureToPtr(new WindowSize { Width = size.Width, Height = size.Height }, answer, false);

            if (User32.SendMessage(window, WM_GETDPISCALEDSIZE, dpi, answer) != 0)
            {
                WindowSize asked = Marshal.PtrToStructure<WindowSize>(answer);

                if (asked.Width > 0 && asked.Height > 0)
                {
                    return new Size(asked.Width, asked.Height);
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(answer);
        }

        return new Size((int)Math.Round(size.Width * factor), (int)Math.Round(size.Height * factor));
    }

    /// <summary>Keeps a child control's DPI messages from it while its form is moved; the drop sends them again.</summary>
    private sealed class ChildHold : NativeWindow
    {
        protected override void WndProc(ref Message m)
        {
            if (m.Msg is WM_DPICHANGED_BEFOREPARENT or WM_DPICHANGED_AFTERPARENT)
            {
                return;
            }

            base.WndProc(ref m);
        }
    }

    // SIZE, as WM_GETDPISCALEDSIZE reads and writes it
    [StructLayout(LayoutKind.Sequential)]
    private struct WindowSize
    {
        public int Width;
        public int Height;
    }
}
