using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;
using System.ComponentModel;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.Common.WinApi.Libs.Structs;
using CincoVertice.MyTools.Ui.Zones.Interfaces;
using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Win.Zones.Services;

/// <summary>
///     Tells when the user drags a window of another program, from Windows' window events, and while they do, where
///     the mouse is and whether Shift, the right mouse button and Esc are down, a few times a second. Start it on the UI
///     thread: the events come through its message loop.
///     A drag is a resize once the window's size has changed twice: moving a maximized or snapped window changes it
///     once, when Windows gives it back its own size, and a resize changes it with every move of the mouse.
/// </summary>
public sealed class WindowDragWatcher : IWindowDragSource, IDisposable
{
    // Often enough for the highlighted zone to follow the mouse smoothly
    private const int PollMilliseconds = 15;

    private const int KeyDown = 0x8000;

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = PollMilliseconds };

    private WinEventHook? _hook;
    private InputHook? _input;
    private bool _isEscapePressed;

    // The window being dragged, 0 for none, its size at the last tick, and how many times that size changed
    private nint _window;
    private Size _lastSize;
    private int _sizeChanges;

    public WindowDragWatcher()
    {
        _timer.Tick += Timer_Tick;
    }

    public event EventHandler<nint>? DragStarted;

    public event EventHandler<ZoneDragPointerModel>? DragMoved;

    public event EventHandler? DragEnded;

    /// <summary>Starts watching, on the UI thread.</summary>
    /// <exception cref="InvalidOperationException">When Windows refuses to tell of window events.</exception>
    public void Start()
    {
        _hook ??= new WinEventHook(User32.EVENT_SYSTEM_MOVESIZESTART, User32.EVENT_SYSTEM_MOVESIZEEND, OnEvent);
    }

    public void Dispose()
    {
        _hook?.Dispose();
        _hook = null;
        _input?.Dispose();
        _input = null;
        _timer.Dispose();
    }

    private void OnEvent(uint eventType, nint window)
    {
        if (eventType == User32.EVENT_SYSTEM_MOVESIZESTART && WindowHelper.IsMovableWindow(window))
        {
            _window = window;

            // In the same pixels as on each tick
            using (DpiAwarenessScope.PerMonitorV2())
            {
                _lastSize = SizeOf(window);
            }

            _sizeChanges = 0;
            _isEscapePressed = false;
            _input?.Dispose();
            _input = null;

            try
            {
                // Remember Esc before Windows consumes it and ends the drag, even between timer ticks.
                _input = new InputHook();
                _input.KeyDown += OnDragKeyDown;
            }
            catch (Win32Exception)
            {
                // Polling, including the final sample, still works when Windows refuses input hooks.
            }

            DragStarted?.Invoke(this, window);
            _timer.Start();
        }
        else if (eventType == User32.EVENT_SYSTEM_MOVESIZEEND && window == _window)
        {
            FinishDrag(ReadPointer());
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        DragMoved?.Invoke(this, ReadPointer());
    }

    internal void OnDragKeyDown(object? sender, Keys key)
    {
        _isEscapePressed |= key == Keys.Escape;
    }

    internal void FinishDrag(ZoneDragPointerModel pointer)
    {
        _timer.Stop();
        _input?.Dispose();
        _input = null;
        DragMoved?.Invoke(this, pointer with { IsEscapeDown = pointer.IsEscapeDown || _isEscapePressed });
        _window = 0;
        DragEnded?.Invoke(this, EventArgs.Empty);
    }

    private ZoneDragPointerModel ReadPointer()
    {
        Point cursor;

        // Physical pixels on every monitor, as the zones are
        using (DpiAwarenessScope.PerMonitorV2())
        {
            User32.GetCursorPos(out POINT point);
            cursor = new Point(point.X, point.Y);

            Size size = SizeOf(_window);

            if (size != _lastSize)
            {
                _sizeChanges++;
                _lastSize = size;
            }
        }

        return new ZoneDragPointerModel(
            cursor,
            IsDown(KeyCode.VK_SHIFT),
            IsResizing: _sizeChanges >= 2,
            IsRightButtonDown: IsDown(KeyCode.VK_RBUTTON),
            IsEscapeDown: _isEscapePressed || IsDown(KeyCode.VK_ESCAPE));
    }

    private static bool IsDown(KeyCode key) => (User32.GetAsyncKeyState(key) & KeyDown) != 0;

    private static Size SizeOf(nint window)
    {
        User32.GetWindowRect(window, out RECT rect);

        return new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
    }
}
