using System.Drawing;
using CincoVertice.MyTools.Ui.Zones.Interfaces;
using CincoVertice.MyTools.Ui.Zones.Models;
using CincoVertice.MyTools.Ui.Zones.Services;
using CincoVertice.MyTools.Ui.Zones.Views;

namespace CincoVertice.MyTools.Ui.Zones.Presenters;

/// <summary>
///     Window zones: while the user drags a window holding Shift, shows the zones of every monitor with the one under
///     the mouse highlighted, and when they let go there, moves the window into that zone. Without Shift, dragging is
///     as usual. The layout is read during each drag, the first time Shift is held, so a change applies at the next
///     one.
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
    }

    private void OnDragMoved(object? sender, ZoneDragPointerModel pointer)
    {
        if (_window == 0)
        {
            return;
        }

        if (!pointer.ShowZones)
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
        int? zone = _isShown ? _highlighted : null;

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
        ZoneLayoutModel layout = _layouts.Load();
        _gap = layout.Gap;

        return [.. _desktop.GetWorkAreas().SelectMany(area => ZoneCalculator.Place(layout, area))];
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
