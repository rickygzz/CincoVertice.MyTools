using System.ComponentModel;
using System.Drawing;
using CincoVertice.MyTools.Ui.Zones.Interfaces;
using CincoVertice.MyTools.Ui.Zones.Models;
using CincoVertice.MyTools.Ui.Zones.Services;
using CincoVertice.MyTools.Ui.Zones.Views;

namespace CincoVertice.MyTools.Ui.Zones.Presenters;

/// <summary>
///     The window zones editor. A drag on a zone's border moves that border, and every zone border on the same line
///     with it, so a grid stays a grid; a drag on a handle of the selected zone, at its corners and the middles of its
///     sides, resizes only that zone, even over others; a drag inside a zone moves the zone; a drag on no zone draws a
///     new one. Borders and zones snap to the edges of the monitor and of the other zones. A right-click on a zone
///     splits it there or deletes it. It edits the default layout or a monitor's own, which a monitor gets at its first
///     change and keeps until the user picks the default one again. Every change can be undone, in whichever layout it
///     was; nothing is saved until the user saves.
/// </summary>
public sealed class ZoneEditorPresenter
{
    /// <summary>The smallest a zone can be, in percent of each side.</summary>
    public const double MinSize = 5;

    /// <summary>The gaps to choose from, in pixels; the one in the file is added when it isn't one of them.</summary>
    public static IReadOnlyList<int> Gaps { get; } = [0, 2, 4, 6, 8, 10, 12, 16, 20, 24, 32, 48];

    // Borders this close are on the same line, in percent: templates of thirds add up with rounding errors
    private const double SameLine = 0.05;

    // Where the zones are kept, in percent: a hundredth of a percent is less than a pixel
    private const int Decimals = 2;

    // When the desktop has no monitors to tell of
    private static readonly Size _defaultMonitor = new(1920, 1040);

    private readonly IZoneEditorView _view;
    private readonly IZoneLayoutStore _store;
    private readonly IZoneDesktop _desktop;
    private readonly Action _openAsText;

    private readonly Stack<Snapshot> _undo = new();
    private readonly Stack<Snapshot> _redo = new();

    // The layouts as they were before the one being edited changed: the default one and the monitors' own
    private ZoneLayoutModel _default = ZoneLayoutModel.Default;
    private List<MonitorZoneLayoutModel> _own = [];

    // The layout being edited: the default one when null, else the monitor's at this index
    private int? _target;

    // Whether the monitor edited has its own layout; it gets one at its first change
    private bool _isOwn;

    // The zones and the gap of the layout being edited
    private List<ZoneModel> _zones = [];
    private int _gap;

    private int? _selected;
    private ZoneLayoutsModel _saved = ZoneLayoutsModel.Default;
    private IReadOnlyList<ZoneMonitorModel> _monitors = [];

    // What the view shows already, so it isn't refilled on every mouse move
    private string _shownTargets = string.Empty;
    private int? _shownGap;

    private Drag? _drag;

    // Where the zone menu was asked for, which is where a split goes
    private (double X, double Y)? _menuPoint;

    // Closing for the config editor, after the changes were dealt with
    private bool _isClosing;

    /// <param name="openAsText">Opens zones.cfg in the config editor.</param>
    public ZoneEditorPresenter(IZoneEditorView view, IZoneLayoutStore store, IZoneDesktop desktop, Action openAsText)
    {
        _view = view;
        _store = store;
        _desktop = desktop;
        _openAsText = openAsText;
    }

    private enum Edge
    {
        Left,
        Top,
        Right,
        Bottom,
    }

    private enum DragKind
    {
        Border,
        Handle,
        Move,
        Draw,
    }

    /// <summary>The sides a handle moves: one for the middle of a side, two for a corner.</summary>
    [Flags]
    private enum Sides
    {
        None = 0,
        Left = 1,
        Top = 2,
        Right = 4,
        Bottom = 8,
    }

    public void Start()
    {
        _view.PointerPressed += OnPointerPressed;
        _view.PointerMoved += OnPointerMoved;
        _view.PointerReleased += OnPointerReleased;
        _view.PointerHovered += OnPointerHovered;
        _view.PointerRightClicked += OnPointerRightClicked;
        _view.TemplateSelected += OnTemplateSelected;
        _view.GapSelected += OnGapSelected;
        _view.TargetSelected += OnTargetSelected;
        _view.UseDefaultRequested += OnUseDefaultRequested;
        _view.SplitVerticallyRequested += (_, _) => Split(vertically: true);
        _view.SplitHorizontallyRequested += (_, _) => Split(vertically: false);
        _view.DeleteRequested += OnDeleteRequested;
        _view.UndoRequested += (_, _) => Undo();
        _view.RedoRequested += (_, _) => Redo();
        _view.SaveRequested += (_, _) => Save();
        _view.EditAsTextRequested += OnEditAsTextRequested;
        _view.CloseRequested += OnCloseRequested;

        ZoneLayoutsModel layouts = _store.Load();
        _default = layouts.DefaultLayout;
        _own = [.. layouts.Monitors];

        IReadOnlyList<ZoneMonitorModel> monitors = _desktop.GetMonitors();
        Rectangle fallback = new(Point.Empty, _defaultMonitor);
        _monitors = monitors.Count > 0 ? monitors : [new ZoneMonitorModel(fallback, fallback)];

        _target = null;
        LoadTarget();
        _saved = Build();
        ShowAll();
    }

    /// <summary>The layout being edited, from the layouts kept, and whether it's the monitor's own.</summary>
    private void LoadTarget()
    {
        MonitorZoneLayoutModel? own = _target is int index
            ? _own.FirstOrDefault(monitor => monitor.Bounds == _monitors[index].Bounds)
            : null;
        ZoneLayoutModel layout = own?.Layout ?? _default;

        _zones = [.. layout.Zones];
        _gap = layout.Gap;
        _isOwn = own is not null;
        _selected = null;
    }

    /// <returns>Every layout, with the one being edited as it is now.</returns>
    private ZoneLayoutsModel Build()
    {
        var editing = new ZoneLayoutModel([.. _zones], _gap);
        List<MonitorZoneLayoutModel> own = [.. _own];

        if (_target is int index)
        {
            Rectangle bounds = _monitors[index].Bounds;
            own.RemoveAll(monitor => monitor.Bounds == bounds);

            if (_isOwn)
            {
                own.Add(new MonitorZoneLayoutModel(bounds, editing));
            }
        }

        return new ZoneLayoutsModel(
            _target is null ? editing : _default,
            [.. own.OrderBy(monitor => monitor.Bounds.X).ThenBy(monitor => monitor.Bounds.Y)]);
    }

    private void OnPointerPressed(object? sender, ZoneEditorPointerModel pointer)
    {
        _menuPoint = null;

        if (HandleAt(pointer) is Sides sides && _selected is int selected)
        {
            _drag = new Drag(DragKind.Handle, Current(), pointer.X, pointer.Y) { Index = selected, Sides = sides };
        }
        else if (BorderAt(pointer) is (int index, Edge edge))
        {
            _selected = index;
            _drag = new Drag(DragKind.Border, Current(), pointer.X, pointer.Y) { Edge = edge, Index = index };
        }
        else if (ZoneAt(pointer.X, pointer.Y) is int zone)
        {
            _selected = zone;
            _drag = new Drag(DragKind.Move, Current(), pointer.X, pointer.Y) { Index = zone };
        }
        else
        {
            _selected = null;
            _drag = new Drag(DragKind.Draw, Current(), Clamp(pointer.X), Clamp(pointer.Y));
        }

        ShowAll();
    }

    private void OnPointerMoved(object? sender, ZoneEditorPointerModel pointer)
    {
        if (_drag is null)
        {
            return;
        }

        List<ZoneModel> zones = [.. _drag.Before.Zones];

        switch (_drag.Kind)
        {
            case DragKind.Border:
                MoveBorder(zones, _drag, pointer);
                break;

            case DragKind.Handle:
                zones[_drag.Index] = Resized(zones, _drag, pointer);
                break;

            case DragKind.Move:
                zones[_drag.Index] = Moved(zones, _drag, pointer);
                break;

            default:
                zones.Add(Drawn(zones, _drag, pointer));
                _selected = zones.Count - 1;
                break;
        }

        _zones = zones;

        // The monitor's first change gives it its own layout; one that changes nothing leaves it as it was
        _isOwn = _target is not null && (_drag.Before.IsOwn || !_zones.SequenceEqual(_drag.Before.Zones));
        ShowAll();
    }

    private void OnPointerReleased(object? sender, ZoneEditorPointerModel pointer)
    {
        if (_drag is null)
        {
            return;
        }

        Drag drag = _drag;
        _drag = null;

        // A click, or a zone drawn too small to use
        bool isTooSmall = drag.Kind == DragKind.Draw
            && _zones.Count > drag.Before.Zones.Count
            && (_zones[^1].Width < MinSize || _zones[^1].Height < MinSize);

        if (isTooSmall)
        {
            _zones = [.. drag.Before.Zones];
            _isOwn = drag.Before.IsOwn;
            _selected = null;
        }

        if (!Current().SameAs(drag.Before))
        {
            _undo.Push(drag.Before);
            _redo.Clear();
        }

        ShowAll();
    }

    private void OnPointerHovered(object? sender, ZoneEditorPointerModel pointer)
    {
        ZoneEditorCursor cursor = HandleAt(pointer) is Sides sides
            ? CursorOf(sides)
            : BorderAt(pointer) is (_, Edge edge)
                ? edge is Edge.Left or Edge.Right ? ZoneEditorCursor.ResizeLeftRight : ZoneEditorCursor.ResizeUpDown
                : ZoneAt(pointer.X, pointer.Y) is not null ? ZoneEditorCursor.Move : ZoneEditorCursor.Draw;

        _view.ShowCursor(cursor);
    }

    private static ZoneEditorCursor CursorOf(Sides sides) => sides switch
    {
        Sides.Left | Sides.Top or Sides.Right | Sides.Bottom => ZoneEditorCursor.ResizeDiagonalDown,
        Sides.Right | Sides.Top or Sides.Left | Sides.Bottom => ZoneEditorCursor.ResizeDiagonalUp,
        Sides.Left or Sides.Right => ZoneEditorCursor.ResizeLeftRight,
        _ => ZoneEditorCursor.ResizeUpDown,
    };

    // The handle of the selected zone under the pointer, the nearest one: its corners and the middles of its sides
    private Sides? HandleAt(ZoneEditorPointerModel pointer)
    {
        if (_selected is not int index)
        {
            return null;
        }

        ZoneModel zone = _zones[index];
        double middleX = zone.Left + (zone.Width / 2);
        double middleY = zone.Top + (zone.Height / 2);
        (Sides Sides, double X, double Y)[] handles =
        [
            (Sides.Left | Sides.Top, zone.Left, zone.Top),
            (Sides.Top, middleX, zone.Top),
            (Sides.Right | Sides.Top, zone.Right, zone.Top),
            (Sides.Right, zone.Right, middleY),
            (Sides.Right | Sides.Bottom, zone.Right, zone.Bottom),
            (Sides.Bottom, middleX, zone.Bottom),
            (Sides.Left | Sides.Bottom, zone.Left, zone.Bottom),
            (Sides.Left, zone.Left, middleY),
        ];

        Sides? nearest = null;
        double best = double.MaxValue;

        foreach ((Sides sides, double x, double y) in handles)
        {
            // In units of the tolerance, so it's the same on both axes
            double dx = Math.Abs(pointer.X - x) / pointer.ToleranceX;
            double dy = Math.Abs(pointer.Y - y) / pointer.ToleranceY;

            if (dx <= 1 && dy <= 1 && dx + dy < best)
            {
                nearest = sides;
                best = dx + dy;
            }
        }

        return nearest;
    }

    // Only this zone's sides move, so it can go over the zones next to it
    private static ZoneModel Resized(List<ZoneModel> zones, Drag drag, ZoneEditorPointerModel pointer)
    {
        ZoneModel zone = zones[drag.Index];
        List<ZoneModel> others = [.. zones.Where((_, i) => i != drag.Index)];
        double x = Snap(Clamp(pointer.X), others.SelectMany(o => new[] { o.Left, o.Right }), pointer.ToleranceX);
        double y = Snap(Clamp(pointer.Y), others.SelectMany(o => new[] { o.Top, o.Bottom }), pointer.ToleranceY);

        double left = zone.Left;
        double top = zone.Top;
        double right = zone.Right;
        double bottom = zone.Bottom;

        if (drag.Sides.HasFlag(Sides.Left))
        {
            left = Math.Min(x, right - MinSize);
        }

        if (drag.Sides.HasFlag(Sides.Right))
        {
            right = Math.Max(x, left + MinSize);
        }

        if (drag.Sides.HasFlag(Sides.Top))
        {
            top = Math.Min(y, bottom - MinSize);
        }

        if (drag.Sides.HasFlag(Sides.Bottom))
        {
            bottom = Math.Max(y, top + MinSize);
        }

        left = Math.Round(left, Decimals);
        top = Math.Round(top, Decimals);

        return new ZoneModel(left, top, Math.Round(right, Decimals) - left, Math.Round(bottom, Decimals) - top);
    }

    private void OnPointerRightClicked(object? sender, ZoneEditorPointerModel pointer)
    {
        if (ZoneAt(pointer.X, pointer.Y) is not int zone)
        {
            return;
        }

        _selected = zone;
        _menuPoint = (pointer.X, pointer.Y);
        ShowAll();
        _view.ShowContextMenu(pointer.X, pointer.Y);
    }

    private void OnTemplateSelected(object? sender, ZoneLayoutTemplate template)
    {
        Change(() =>
        {
            _zones = [.. Zones(template)];
            _selected = null;
        });
    }

    private void OnGapSelected(object? sender, int gap)
    {
        if (gap != _gap)
        {
            Change(() => _gap = gap);
        }
    }

    /// <param name="index">0 for the default layout, then 1 for the first monitor's, and so on.</param>
    private void OnTargetSelected(object? sender, int index)
    {
        int? target = index == 0 ? null : index - 1;

        if (target == _target || index < 0 || index > _monitors.Count)
        {
            return;
        }

        // The layout edited so far is kept as it is, unsaved, while another one is edited
        ZoneLayoutsModel layouts = Build();
        _default = layouts.DefaultLayout;
        _own = [.. layouts.Monitors];
        _target = target;
        LoadTarget();
        ShowAll();
    }

    private void OnUseDefaultRequested(object? sender, EventArgs e)
    {
        if (_target is null || !_isOwn)
        {
            return;
        }

        Snapshot before = Current();
        _isOwn = false;
        _zones = [.. _default.Zones];
        _gap = _default.Gap;
        _selected = null;
        _undo.Push(before);
        _redo.Clear();
        ShowAll();
    }

    private void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (_selected is int index)
        {
            Change(() =>
            {
                _zones.RemoveAt(index);
                _selected = null;
            });
        }
    }

    private void Split(bool vertically)
    {
        if (_selected is not int index)
        {
            return;
        }

        ZoneModel zone = _zones[index];
        double size = vertically ? zone.Width : zone.Height;

        if (size < 2 * MinSize)
        {
            _view.ShowProblem(vertically
                ? "This zone is too narrow to split into two."
                : "This zone is too short to split into two.");

            return;
        }

        double start = vertically ? zone.Left : zone.Top;
        double end = start + size;
        double at = _menuPoint is var (x, y) ? (vertically ? x : y) : start + (size / 2);

        // Where it was clicked, unless one part would be too small
        if (at - start < MinSize || end - at < MinSize)
        {
            at = start + (size / 2);
        }

        at = Math.Round(at, Decimals);

        Change(() =>
        {
            ZoneModel first = vertically ? zone with { Width = at - start } : zone with { Height = at - start };
            ZoneModel second = vertically
                ? zone with { Left = at, Width = end - at }
                : zone with { Top = at, Height = end - at };

            _zones[index] = first;
            _zones.Insert(index + 1, second);
        });
    }

    private void Undo()
    {
        if (_undo.Count > 0)
        {
            _redo.Push(Current());
            Restore(_undo.Pop());
        }
    }

    private void Redo()
    {
        if (_redo.Count > 0)
        {
            _undo.Push(Current());
            Restore(_redo.Pop());
        }
    }

    /// <returns>True when saved.</returns>
    private bool Save()
    {
        ZoneLayoutsModel layouts = Build();

        if (_store.Save(layouts) is string problem)
        {
            _view.ShowProblem(problem);

            return false;
        }

        _saved = layouts;
        ShowAll();

        return true;
    }

    private void OnEditAsTextRequested(object? sender, EventArgs e)
    {
        if (!DealWithChanges())
        {
            return;
        }

        _isClosing = true;
        _openAsText();
        _view.CloseView();
    }

    private void OnCloseRequested(object? sender, CancelEventArgs e)
    {
        if (!_isClosing && !DealWithChanges())
        {
            e.Cancel = true;
        }
    }

    /// <returns>True when the editor can go: nothing unsaved, saved, or dropped.</returns>
    private bool DealWithChanges()
    {
        if (Build().SameAs(_saved))
        {
            return true;
        }

        return _view.AskSaveChanges() switch
        {
            true => Save(),
            false => true,
            null => false,
        };
    }

    private void Change(Action change)
    {
        Snapshot before = Current();
        change();

        // The monitor's first change gives it its own layout; one that changes nothing leaves it as it was
        _isOwn = _target is not null
            && (before.IsOwn || _gap != before.Gap || !_zones.SequenceEqual(before.Zones));

        if (!Current().SameAs(before))
        {
            _undo.Push(before);
            _redo.Clear();
        }

        ShowAll();
    }

    // Back to the layout the snapshot was taken in, so the user sees what was undone
    private void Restore(Snapshot snapshot)
    {
        _default = snapshot.Layouts.DefaultLayout;
        _own = [.. snapshot.Layouts.Monitors];
        _target = snapshot.Target;
        _isOwn = snapshot.IsOwn;
        _zones = [.. snapshot.Zones];
        _gap = snapshot.Gap;
        _selected = null;
        ShowAll();
    }

    private Snapshot Current() => new(Build(), _target, _isOwn, [.. _zones], _gap);

    private void ShowAll()
    {
        ShowTargets();
        ShowGap();
        _view.ShowOwnLayout(_target is not null, _isOwn);
        _view.ShowZones([.. _zones], _selected, _gap);
        _view.ShowUndoRedo(_undo.Count > 0, _redo.Count > 0);
        _view.ShowUnsaved(!Build().SameAs(_saved));
    }

    // The default layout, then each monitor, marked when it has its own layout; and the shape to edit on
    private void ShowTargets()
    {
        ZoneLayoutsModel layouts = Build();
        List<string> names = ["Default layout"];

        for (int i = 0; i < _monitors.Count; i++)
        {
            ZoneMonitorModel monitor = _monitors[i];
            bool hasOwn = layouts.Monitors.Any(own => own.Bounds == monitor.Bounds);
            string own = hasOwn ? ", own layout" : string.Empty;
            names.Add($"Monitor {i + 1} ({monitor.Bounds.Width} x {monitor.Bounds.Height}){own}");
        }

        int selected = _target is int index ? index + 1 : 0;
        string shown = string.Join("\n", names) + "\n" + selected;

        if (shown == _shownTargets)
        {
            return;
        }

        _shownTargets = shown;

        // The default layout is shown on the first monitor's shape
        Size shape = _monitors[_target ?? 0].WorkArea.Size;
        _view.ShowTargets(names, selected, shape);
    }

    // The gaps to choose from, with the one of the layout even when it isn't a usual one
    private void ShowGap()
    {
        if (_shownGap == _gap)
        {
            return;
        }

        _shownGap = _gap;
        List<int> gaps = [.. Gaps];

        if (!gaps.Contains(_gap))
        {
            gaps.Add(_gap);
            gaps.Sort();
        }

        _view.ShowGaps(gaps, _gap);
    }

    // Every border on the line of the one grabbed moves with it, within what keeps each zone large enough
    private static void MoveBorder(List<ZoneModel> zones, Drag drag, ZoneEditorPointerModel pointer)
    {
        bool isX = drag.Edge is Edge.Left or Edge.Right;
        double line = EdgeOf(zones[drag.Index], drag.Edge);
        List<(int Index, Edge Edge)> moving = [];

        for (int i = 0; i < zones.Count; i++)
        {
            foreach (Edge edge in isX ? new[] { Edge.Left, Edge.Right } : [Edge.Top, Edge.Bottom])
            {
                if (Math.Abs(EdgeOf(zones[i], edge) - line) < SameLine)
                {
                    moving.Add((i, edge));
                }
            }
        }

        double low = 0;
        double high = 100;

        foreach ((int index, Edge edge) in moving)
        {
            ZoneModel zone = zones[index];

            if (edge is Edge.Left or Edge.Top)
            {
                high = Math.Min(high, EdgeOf(zone, isX ? Edge.Right : Edge.Bottom) - MinSize);
            }
            else
            {
                low = Math.Max(low, EdgeOf(zone, isX ? Edge.Left : Edge.Top) + MinSize);
            }
        }

        if (low > high)
        {
            return;
        }

        IEnumerable<double> others = zones
            .Where((_, i) => !moving.Any(m => m.Index == i))
            .SelectMany(zone => isX ? new[] { zone.Left, zone.Right } : [zone.Top, zone.Bottom]);
        double to = Snap(isX ? pointer.X : pointer.Y, others, isX ? pointer.ToleranceX : pointer.ToleranceY);
        to = Math.Round(Math.Clamp(to, low, high), Decimals);

        foreach ((int index, Edge edge) in moving)
        {
            zones[index] = WithEdge(zones[index], edge, to);
        }
    }

    private static ZoneModel Moved(List<ZoneModel> zones, Drag drag, ZoneEditorPointerModel pointer)
    {
        ZoneModel zone = zones[drag.Index];
        List<ZoneModel> others = [.. zones.Where((_, i) => i != drag.Index)];

        double left = Math.Clamp(zone.Left + pointer.X - drag.StartX, 0, 100 - zone.Width);
        double top = Math.Clamp(zone.Top + pointer.Y - drag.StartY, 0, 100 - zone.Height);

        left = SnapSpan(left, zone.Width, others.SelectMany(o => new[] { o.Left, o.Right }), pointer.ToleranceX);
        top = SnapSpan(top, zone.Height, others.SelectMany(o => new[] { o.Top, o.Bottom }), pointer.ToleranceY);

        return zone with { Left = Math.Round(left, Decimals), Top = Math.Round(top, Decimals) };
    }

    private static ZoneModel Drawn(List<ZoneModel> zones, Drag drag, ZoneEditorPointerModel pointer)
    {
        double x = Snap(Clamp(pointer.X), zones.SelectMany(o => new[] { o.Left, o.Right }), pointer.ToleranceX);
        double y = Snap(Clamp(pointer.Y), zones.SelectMany(o => new[] { o.Top, o.Bottom }), pointer.ToleranceY);
        double left = Math.Round(Math.Min(x, drag.StartX), Decimals);
        double top = Math.Round(Math.Min(y, drag.StartY), Decimals);

        return new ZoneModel(
            left,
            top,
            Math.Round(Math.Max(x, drag.StartX), Decimals) - left,
            Math.Round(Math.Max(y, drag.StartY), Decimals) - top);
    }

    // To the nearest of the edges, or of the monitor's, when one is near enough
    private static double Snap(double value, IEnumerable<double> edges, double tolerance)
    {
        double best = value;
        double distance = tolerance;

        foreach (double edge in edges.Append(0).Append(100))
        {
            if (Math.Abs(edge - value) <= distance)
            {
                best = edge;
                distance = Math.Abs(edge - value);
            }
        }

        return best;
    }

    // A zone's start, so that its start or its end is on the nearest edge, when one is near enough
    private static double SnapSpan(double start, double size, IEnumerable<double> edges, double tolerance)
    {
        double best = start;
        double distance = tolerance;

        foreach (double edge in edges.Append(0).Append(100))
        {
            foreach (double candidate in new[] { edge, edge - size })
            {
                if (Math.Abs(candidate - start) <= distance && candidate >= 0 && candidate + size <= 100)
                {
                    best = candidate;
                    distance = Math.Abs(candidate - start);
                }
            }
        }

        return best;
    }

    // The border nearest the pointer, of the selected zone first, then of the others from the top one down
    private (int Index, Edge Edge)? BorderAt(ZoneEditorPointerModel pointer)
    {
        IEnumerable<int> order = Enumerable.Range(0, _zones.Count).Reverse();

        if (_selected is int selected)
        {
            order = order.Where(i => i != selected).Prepend(selected);
        }

        foreach (int i in order)
        {
            ZoneModel zone = _zones[i];
            bool isAcross = pointer.Y >= zone.Top - pointer.ToleranceY && pointer.Y <= zone.Bottom + pointer.ToleranceY;
            bool isAlong = pointer.X >= zone.Left - pointer.ToleranceX && pointer.X <= zone.Right + pointer.ToleranceX;
            (Edge Edge, double Distance)? nearest = null;

            void Consider(Edge edge, double distance, double tolerance, bool isInRange)
            {
                if (isInRange && distance <= tolerance && (nearest is null || distance < nearest.Value.Distance))
                {
                    nearest = (edge, distance);
                }
            }

            Consider(Edge.Left, Math.Abs(pointer.X - zone.Left), pointer.ToleranceX, isAcross);
            Consider(Edge.Right, Math.Abs(pointer.X - zone.Right), pointer.ToleranceX, isAcross);
            Consider(Edge.Top, Math.Abs(pointer.Y - zone.Top), pointer.ToleranceY, isAlong);
            Consider(Edge.Bottom, Math.Abs(pointer.Y - zone.Bottom), pointer.ToleranceY, isAlong);

            if (nearest is (Edge found, _))
            {
                return (i, found);
            }
        }

        return null;
    }

    // The smallest zone the point is in, so a zone over another can be reached
    private int? ZoneAt(double x, double y)
    {
        List<int> indexes = [];

        for (int i = 0; i < _zones.Count; i++)
        {
            ZoneModel zone = _zones[i];

            if (x >= zone.Left && x <= zone.Right && y >= zone.Top && y <= zone.Bottom)
            {
                indexes.Add(i);
            }
        }

        int? nearest = ZoneCalculator.Nearest(
            [.. indexes.Select(i => (_zones[i].Left, _zones[i].Top, _zones[i].Width, _zones[i].Height))],
            x,
            y);

        return nearest is int index ? indexes[index] : null;
    }

    private static IReadOnlyList<ZoneModel> Zones(ZoneLayoutTemplate template) => template switch
    {
        ZoneLayoutTemplate.Columns2 => ZoneLayoutModel.Columns(ZoneLayoutModel.Equal(2)),
        ZoneLayoutTemplate.Columns3 => ZoneLayoutModel.Columns(ZoneLayoutModel.Equal(3)),
        ZoneLayoutTemplate.Columns4 => ZoneLayoutModel.Columns(ZoneLayoutModel.Equal(4)),
        ZoneLayoutTemplate.Rows2 => ZoneLayoutModel.Rows(ZoneLayoutModel.Equal(2)),
        ZoneLayoutTemplate.Rows3 => ZoneLayoutModel.Rows(ZoneLayoutModel.Equal(3)),
        ZoneLayoutTemplate.Grid2x2 => ZoneLayoutModel.Grid(ZoneLayoutModel.Equal(2), ZoneLayoutModel.Equal(2)),
        ZoneLayoutTemplate.Grid3x2 => ZoneLayoutModel.Grid(ZoneLayoutModel.Equal(3), ZoneLayoutModel.Equal(2)),
        ZoneLayoutTemplate.Grid3x3 => ZoneLayoutModel.Grid(ZoneLayoutModel.Equal(3), ZoneLayoutModel.Equal(3)),
        _ => ZoneLayoutModel.Columns([25, 50, 25]),
    };

    private static double EdgeOf(ZoneModel zone, Edge edge) => edge switch
    {
        Edge.Left => zone.Left,
        Edge.Top => zone.Top,
        Edge.Right => zone.Right,
        _ => zone.Bottom,
    };

    private static ZoneModel WithEdge(ZoneModel zone, Edge edge, double at) => edge switch
    {
        Edge.Left => zone with { Left = at, Width = zone.Right - at },
        Edge.Top => zone with { Top = at, Height = zone.Bottom - at },
        Edge.Right => zone with { Width = at - zone.Left },
        _ => zone with { Height = at - zone.Top },
    };

    private static double Clamp(double percent) => Math.Clamp(percent, 0, 100);

    /// <summary>Every layout, and the one edited then, to undo to or compare with what's saved.</summary>
    /// <param name="Layouts">Every layout, to undo to or compare with.</param>
    /// <param name="Target">The layout being edited, to go back to with undo.</param>
    /// <param name="IsOwn">Whether the monitor being edited had its own layout.</param>
    /// <param name="Zones">The zones of the layout being edited.</param>
    /// <param name="Gap">Its gap.</param>
    private sealed record Snapshot(
        ZoneLayoutsModel Layouts,
        int? Target,
        bool IsOwn,
        IReadOnlyList<ZoneModel> Zones,
        int Gap)
    {
        public bool SameAs(Snapshot other) => Layouts.SameAs(other.Layouts);
    }

    /// <summary>A drag in progress: what it does, the zones before it, and where it started.</summary>
    private sealed record Drag(DragKind Kind, Snapshot Before, double StartX, double StartY)
    {
        public int Index { get; init; }

        public Edge Edge { get; init; }

        public Sides Sides { get; init; }
    }
}
