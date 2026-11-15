using System.Drawing;
using CincoVertice.MyTools.Ui.Zones.Interfaces;
using CincoVertice.MyTools.Ui.Zones.Models;
using CincoVertice.MyTools.Ui.Zones.Services;
using CincoVertice.MyTools.Ui.Zones.Views;

namespace CincoVertice.MyTools.Ui.Zones.Presenters;

/// <summary>
///     Window zones: while the user drags a window, a press of Shift, or a click of the right mouse button, shows the
///     zones of every monitor, its own layout or the default one, with the one under the mouse highlighted; they stay
///     until the next press, so Shift needn't be held. Letting go of the window over a zone moves it into that zone.
///     Esc cancels the drag: the window goes back, into no zone. A window being resized shows none. The layouts are
///     read during each drag, the first time the zones show, so a change applies at the next one.
/// </summary>
public sealed class ZoneDragPresenter
{
    private readonly IWindowDragSource _drags;
    private readonly IZoneOverlayView _view;
    private readonly IZoneDesktop _desktop;
    private readonly IZoneLayoutSource _layouts;

    // The window being dragged, 0 for none, and the zones of this drag on every monitor, once read
    private nint _window;
    private IReadOnlyList<Rectangle>? _zones;
    private int _gap;

    private bool _isShown;
    private int? _highlighted;

    // Whether the user asked for the zones in this drag, which each press of Shift or the right button turns over;
    // what was down at the last move, to tell a press; and whether Esc cancelled the drag
    private bool _areZonesAsked;
    private bool _wasShiftDown;
    private bool _wasRightButtonDown;
    private bool _isCancelled;

    public ZoneDragPresenter(
        IWindowDragSource drags,
        IZoneOverlayView view,
        IZoneDesktop desktop,
        IZoneLayoutSource layouts)
    {
        _drags = drags;
        _view = view;
        _desktop = desktop;
        _layouts = layouts;
    }

    public void Start()
    {
        _drags.DragStarted += OnDragStarted;
        _drags.DragMoved += OnDragMoved;
        _drags.DragEnded += OnDragEnded;
    }

    private void OnDragStarted(object? sender, nint window)
    {
        Hide();
        _window = window;
        _zones = null;
        _areZonesAsked = false;
        _wasShiftDown = false;
        _wasRightButtonDown = false;
        _isCancelled = false;
    }

    private void OnDragMoved(object? sender, ZoneDragPointerModel pointer)
    {
        if (_window == 0 || _isCancelled)
        {
            return;
        }

        // Windows puts the window back on Esc; it mustn't then go into a zone
        if (pointer.IsEscapeDown)
        {
            _isCancelled = true;
            Hide();

            return;
        }

        bool isPressed = (pointer.IsShiftDown && !_wasShiftDown)
            || (pointer.IsRightButtonDown && !_wasRightButtonDown);

        _wasShiftDown = pointer.IsShiftDown;
        _wasRightButtonDown = pointer.IsRightButtonDown;

        if (isPressed)
        {
            _areZonesAsked = !_areZonesAsked;
        }

        if (!_areZonesAsked || pointer.IsResizing)
        {
            Hide();

            return;
        }

        IReadOnlyList<Rectangle> zones = _zones ??= ReadZones();
        int? highlighted = ZoneCalculator.ZoneAt(zones, pointer.Cursor, _gap);

        if (!_isShown || highlighted != _highlighted)
        {
            _isShown = true;
            _highlighted = highlighted;
            _view.ShowZones(zones, highlighted);
        }
    }

    private void OnDragEnded(object? sender, EventArgs e)
    {
        nint window = _window;
        int? zone = _isShown && !_isCancelled ? _highlighted : null;

        Hide();
        _window = 0;

        if (window == 0 || zone is not int index || _zones is not { } zones)
        {
            return;
        }

        if (_desktop.MoveWindow(window, zones[index]) is string problem)
        {
            _view.ShowProblem(problem);
        }
    }

    // Only once the user asks for them: most drags never do
    private IReadOnlyList<Rectangle> ReadZones()
    {
        ZoneLayoutsModel layouts = _layouts.Load();
        IReadOnlyList<ZoneMonitorModel> monitors = _desktop.GetMonitors();

        // The widest gap: a point in it is in the zones on either side, on any monitor
        _gap = monitors.Select(monitor => layouts.For(monitor.Bounds).Gap).DefaultIfEmpty(0).Max();

        return
        [
            .. monitors.SelectMany(monitor =>
                ZoneCalculator.Place(layouts.For(monitor.Bounds), monitor.WorkArea)),
        ];
    }

    private void Hide()
    {
        if (_isShown)
        {
            _view.HideZones();
        }

        _isShown = false;
        _highlighted = null;
    }
}
