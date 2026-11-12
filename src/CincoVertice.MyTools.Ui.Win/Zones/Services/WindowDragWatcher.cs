using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.Common.WinApi.Libs.Structs;
using CincoVertice.MyTools.Ui.Zones.Interfaces;
using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Win.Zones.Services;

/// <summary>
///     Tells when the user drags a window of another program, from Windows' window events, and while they do, where
///     the mouse is and whether Shift is down, a few times a second. A window being resized, not moved, never shows the
///     zones. Start it on the UI thread: the events come through its message loop.
/// </summary>
public sealed class WindowDragWatcher : IWindowDragSource, IDisposable
{
    // Often enough for the highlighted zone to follow the mouse smoothly
    private const int PollMilliseconds = 15;

    private const int KeyDown = 0x8000;

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = PollMilliseconds };

    private WinEventHook? _hook;

    // The window being dragged, 0 for none, and its size when the drag started
    private nint _window;
    private Size _startSize;

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
                _startSize = SizeOf(window);
            }

            DragStarted?.Invoke(this, window);
            _timer.Start();
        }
        else if (eventType == User32.EVENT_SYSTEM_MOVESIZEEND && window == _window)
        {
            _timer.Stop();
            _window = 0;
            DragEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        Point cursor;
        bool isResizing;

        // Physical pixels on every monitor, as the zones are
        using (DpiAwarenessScope.PerMonitorV2())
        {
            User32.GetCursorPos(out POINT point);
            cursor = new Point(point.X, point.Y);
            isResizing = SizeOf(_window) != _startSize;
        }

        bool isShiftDown = (User32.GetAsyncKeyState(KeyCode.VK_SHIFT) & KeyDown) != 0;

        DragMoved?.Invoke(this, new ZoneDragPointerModel(cursor, isShiftDown && !isResizing));
    }

    private static Size SizeOf(nint window)
    {
        User32.GetWindowRect(window, out RECT rect);

        return new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
    }
}
