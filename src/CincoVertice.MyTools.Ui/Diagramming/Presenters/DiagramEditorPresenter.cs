using System.ComponentModel;
using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using CincoVertice.MyTools.Ui.Diagramming.Views;
using CincoVertice.MyTools.Ui.Snipping.Presenters;

namespace CincoVertice.MyTools.Ui.Diagramming.Presenters;

/// <summary>
///     One diagram editor window: turns what the user does on the view into changes to the diagram, with undo and
///     redo, and opens and saves it through <see cref="IDiagramFileStore"/>. A diagram is immutable: a drag only
///     changes a preview, which becomes the diagram when it ends.
/// </summary>
public sealed class DiagramEditorPresenter
{
    /// <summary>The line widths to choose from, in pixels.</summary>
    public static IReadOnlyList<int> LineWidths { get; } = [1, 2, 3, 4, 5, 6, 8, 10, 12, 16];

    /// <summary>The font sizes to choose from, in pixels.</summary>
    public static IReadOnlyList<int> FontSizes { get; } = [10, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 64, 72];

    /// <summary>The name of a diagram that was never saved.</summary>
    public const string UntitledName = "Untitled";

    private const float MinZoom = 0.25f;
    private const float MaxZoom = 4;
    private const float WheelZoomFactor = 1.1f;
    private const float CommandZoomFactor = 1.25f;
    private const int WheelScrollPixels = 40;

    private const int MinShapeSize = 10;
    private const int MaxLineWidth = 100;
    private const int MinFontSize = 6;
    private const int MaxFontSize = 400;
    private const int PasteOffset = 20;

    // All in canvas pixels, so they feel the same at any zoom
    private const float SnapDistance = 6;
    private const float GrabDistance = 6;
    private const float DragThreshold = 3;

    // Outside the edges, so they don't cover the scale handles on the same edges of a selected shape
    private const float AnchorOffset = 14;
    private const float AnchorGrabDistance = 8;

    // The edge's width when picking its color turns it on
    private const int TurnedOnTextOutlineWidth = 2;

    private static readonly Size _defaultShapeSize = new(120, 60);

    private readonly IDiagramEditorView _view;
    private readonly IDiagramFileStore _fileStore;
    private readonly IDiagramPdfExporter _pdfExporter;
    private readonly IDiagramEditorSettingsStore _settingsStore;
    private readonly Stack<DiagramModel> _undo = new();
    private readonly Stack<DiagramModel> _redo = new();
    private readonly HashSet<int> _selectedShapes = [];
    private readonly HashSet<int> _selectedConnectors = [];

    private DiagramModel _diagram = DiagramModel.Empty;
    private DiagramModel _savedDiagram = DiagramModel.Empty;
    private string? _path;
    private string? _savedVersion;
    private DiagramViewportModel _viewport = new(1, Point.Empty);
    private DiagramTool _tool = DiagramTool.Select;

    // What new shapes and connectors get
    private DiagramStyleModel _style = new(
        Color.FromArgb(255, 255, 255),
        Color.FromArgb(64, 64, 64),
        2,
        false,
        Color.FromArgb(0, 0, 0),
        14);

    // Kept by Copy style, only in this window. A connector's style has no fill to give
    private DiagramStyleModel? _copiedStyle;
    private bool _copiedStyleHasFill;

    private DiagramRouting _routing = DiagramRouting.Elbow;
    private DiagramArrowHeads _heads = DiagramArrowHeads.End;
    private bool _isPropertiesPanelVisible = true;
    private DiagramWindowSizeModel? _windowSize;

    // The drag in progress and what it shows
    private Drag? _drag;
    private DiagramModel? _preview;
    private IReadOnlyList<DiagramGuideModel> _guides = [];
    private Rectangle? _band;
    private (Point From, Point To)? _draftConnector;
    private int? _connectTarget;
    private DiagramSide? _connectTargetSide;

    // The shape showing its circles, and the circle under the mouse
    private int? _hoverShape;
    private DiagramSide? _hoverSide;

    private DiagramPropertiesModel? _shownProperties;

    // What ends the text being typed
    private Action<string>? _commitText;

    private DiagramCursor _cursor = DiagramCursor.Default;
    private Point _lastPointer;
    private int _pasteCount;

    /// <param name="view">The window.</param>
    /// <param name="fileStore">Where diagrams are opened from and saved to.</param>
    /// <param name="settingsStore">
    ///     Where the last used style and the window's layout are kept: the editor starts with the ones saved last, and
    ///     saves them on every change.
    /// </param>
    /// <param name="pdfExporter">Writes the diagram as a PDF file.</param>
    public DiagramEditorPresenter(
        IDiagramEditorView view,
        IDiagramFileStore fileStore,
        IDiagramEditorSettingsStore settingsStore,
        IDiagramPdfExporter pdfExporter)
    {
        _view = view;
        _fileStore = fileStore;
        _settingsStore = settingsStore;
        _pdfExporter = pdfExporter;

        Apply(settingsStore.Load());

        _view.PointerPressed += OnPointerPressed;
        _view.PointerMoved += OnPointerMoved;
        _view.PointerReleased += OnPointerReleased;
        _view.PointerDoubleClicked += OnPointerDoubleClicked;
        _view.PointerRightClicked += OnPointerRightClicked;
        _view.PointerLeft += OnPointerLeft;
        _view.WheelTurned += OnWheelTurned;
        _view.ToolSelected += OnToolSelected;
        _view.CommandRequested += OnCommandRequested;
        _view.NudgeRequested += OnNudgeRequested;
        _view.ColorSelected += OnColorSelected;
        _view.MoreColorsRequested += OnMoreColorsRequested;
        _view.LineWidthSelected += OnLineWidthSelected;
        _view.DashedSelected += OnDashedSelected;
        _view.FontSizeSelected += OnFontSizeSelected;
        _view.FontFamilySelected += OnFontFamilySelected;
        _view.TextOutlineWidthSelected += OnTextOutlineWidthSelected;
        _view.RoutingSelected += OnRoutingSelected;
        _view.ArrowHeadsSelected += OnArrowHeadsSelected;
        _view.BoundsEdited += OnBoundsEdited;
        _view.CornerRadiusEdited += OnCornerRadiusEdited;
        _view.LockToggled += OnLockToggled;
        _view.TextEdited += OnTextEdited;
        _view.TextCommitted += OnTextCommitted;
        _view.Closing += OnClosing;
        _view.PropertiesPanelToggled += OnPropertiesPanelToggled;
        _view.WindowClosed += OnWindowClosed;
    }

    /// <summary>The diagram, without the change being dragged.</summary>
    public DiagramModel Diagram => _diagram;

    public IReadOnlySet<int> SelectedShapeIds => _selectedShapes;

    public IReadOnlySet<int> SelectedConnectorIds => _selectedConnectors;

    private DiagramModel Shown => _preview ?? _diagram;

    private bool HasChanges => !ReferenceEquals(_diagram, _savedDiagram);

    private string Name => _path is null ? UntitledName : Path.GetFileName(_path);

    /// <summary>Shows the choices and the empty diagram.</summary>
    public void Start()
    {
        _view.ShowPalette(SnipEditorPresenter.Palette);
        _view.ShowSizes(LineWidths, FontSizes, SnipEditorPresenter.TextOutlineWidths);
        _view.ShowPropertiesPanel(_isPropertiesPanelVisible);

        if (_windowSize is not null)
        {
            _view.ShowWindowSize(_windowSize);
        }

        _view.ShowTool(_tool);
        ShowSelectedStyle();
        Refresh();
    }

    /// <summary>
    ///     Takes the saved settings that can still be used: a routing, a head or a window size the editor can't use
    ///     (e.g. from a file edited by hand) keeps its default.
    /// </summary>
    private void Apply(DiagramEditorSettingsModel? saved)
    {
        if (saved is null)
        {
            return;
        }

        _style = saved.Style;
        _isPropertiesPanelVisible = saved.IsPropertiesPanelVisible;

        if (Enum.IsDefined(saved.Routing))
        {
            _routing = saved.Routing;
        }

        if (Enum.IsDefined(saved.Heads))
        {
            _heads = saved.Heads;
        }

        if (saved.Window is { Size: { Width: > 0, Height: > 0 } })
        {
            _windowSize = saved.Window;
        }
    }

    private void SaveSettings()
    {
        _settingsStore.Save(new DiagramEditorSettingsModel(
            _style,
            _routing,
            _heads,
            _isPropertiesPanelVisible,
            _windowSize));
    }

    private void OnPropertiesPanelToggled(object? sender, bool isVisible)
    {
        _isPropertiesPanelVisible = isVisible;
        SaveSettings();
    }

    private void OnWindowClosed(object? sender, DiagramWindowSizeModel size)
    {
        _windowSize = size;
        SaveSettings();
    }

    private static Rectangle FromCorners(Point a, Point b) => Rectangle.FromLTRB(
        Math.Min(a.X, b.X),
        Math.Min(a.Y, b.Y),
        Math.Max(a.X, b.X),
        Math.Max(a.Y, b.Y));

    private static Rectangle UnionOf(IEnumerable<Rectangle> boxes) => boxes.Aggregate(Rectangle.Union);

    private static Rectangle Moved(Rectangle box, Size delta) => new(box.Location + delta, box.Size);

    private static Point CenterOf(Rectangle box) => new(box.Left + box.Width / 2, box.Top + box.Height / 2);

    /// <summary>The box after one of its handles is dragged to the point, never smaller than a shape can be.</summary>
    private static Rectangle ScaleBox(Rectangle box, DiagramHandle handle, Point point, bool keepProportions)
    {
        int left = box.Left;
        int top = box.Top;
        int right = box.Right;
        int bottom = box.Bottom;

        if (handle is DiagramHandle.TopLeft or DiagramHandle.Left or DiagramHandle.BottomLeft)
        {
            left = Math.Min(point.X, right - MinShapeSize);
        }

        if (handle is DiagramHandle.TopRight or DiagramHandle.Right or DiagramHandle.BottomRight)
        {
            right = Math.Max(point.X, left + MinShapeSize);
        }

        if (handle is DiagramHandle.TopLeft or DiagramHandle.Top or DiagramHandle.TopRight)
        {
            top = Math.Min(point.Y, bottom - MinShapeSize);
        }

        if (handle is DiagramHandle.BottomLeft or DiagramHandle.Bottom or DiagramHandle.BottomRight)
        {
            bottom = Math.Max(point.Y, top + MinShapeSize);
        }

        bool isCorner = handle is DiagramHandle.TopLeft or DiagramHandle.TopRight
            or DiagramHandle.BottomRight or DiagramHandle.BottomLeft;

        if (!keepProportions || !isCorner)
        {
            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        double scale = Math.Max((right - left) / (double)box.Width, (bottom - top) / (double)box.Height);
        int width = (int)Math.Round(box.Width * scale);
        int height = (int)Math.Round(box.Height * scale);

        // The opposite corner stays where it is
        return new Rectangle(
            left == box.Left ? box.Left : box.Right - width,
            top == box.Top ? box.Top : box.Bottom - height,
            width,
            height);
    }

    /// <summary>Where a shape goes when the box around it changes from one to the other.</summary>
    private static Rectangle ScaleInside(Rectangle shape, Rectangle from, Rectangle to)
    {
        double scaleX = to.Width / (double)from.Width;
        double scaleY = to.Height / (double)from.Height;

        return new Rectangle(
            to.Left + (int)Math.Round((shape.Left - from.Left) * scaleX),
            to.Top + (int)Math.Round((shape.Top - from.Top) * scaleY),
            Math.Max(1, (int)Math.Round(shape.Width * scaleX)),
            Math.Max(1, (int)Math.Round(shape.Height * scaleY)));
    }

    private static DiagramCursor CursorOf(DiagramHandle handle) => handle switch
    {
        DiagramHandle.Top or DiagramHandle.Bottom => DiagramCursor.SizeVertical,
        DiagramHandle.Left or DiagramHandle.Right => DiagramCursor.SizeHorizontal,
        DiagramHandle.TopLeft or DiagramHandle.BottomRight => DiagramCursor.SizeTopLeftBottomRight,
        _ => DiagramCursor.SizeTopRightBottomLeft,
    };

    private static bool IsVerticalGuide(DiagramGuideModel guide) => guide.From.X == guide.To.X;

    private void OnPointerPressed(object? sender, DiagramPointerModel pointer)
    {
        _lastPointer = pointer.Location;

        if (_drag is not null)
        {
            return;
        }

        if (pointer.Pan)
        {
            _drag = new PanDrag(pointer.Location);
            ShowCursor(DiagramCursor.Pan);

            return;
        }

        Point point = _viewport.ToDiagram(pointer.Location);
        (DiagramShapeModel? anchorShape, DiagramSide? anchorSide) = AnchorUnder(point);
        (DiagramConnectorModel? endConnector, DiagramConnectorEnd? connectorEnd) = ConnectorEndUnder(point);

        ClearHover();

        // Before the circles: the end of a selected connector is on a shape, right next to them
        if (endConnector is not null && connectorEnd is DiagramConnectorEnd end)
        {
            // A line's end goes anywhere; a connector's goes to another shape
            _drag = endConnector.IsLine
                ? new LineEndDrag(point, endConnector.Id, end)
                : new ReconnectDrag(point, endConnector.Id, end);
            Refresh();

            return;
        }

        // With Shift or Ctrl, a press on a selected connector takes it off the selection instead
        if (!pointer.Shift && !pointer.Control && SegmentDragAt(point) is { } segmentDrag)
        {
            _drag = segmentDrag;
            Refresh();

            return;
        }

        if (anchorShape is not null && anchorSide is DiagramSide side)
        {
            _drag = new ConnectDrag(anchorShape.Id, side);
            _draftConnector = (Point.Round(DiagramRouter.AnchorPoint(anchorShape, side)), point);
            Refresh();

            return;
        }

        switch (_tool)
        {
            case DiagramTool.Select:
                PressWithSelect(pointer, point);
                break;
            case DiagramTool.Connector:
                PressWithConnector(point);
                break;
            case DiagramTool.Line:
                _drag = new LineDrag(pointer.Alt ? point : Snap(point));
                break;
            case DiagramTool.Text:
                SelectTool(DiagramTool.Select);
                BeginNewText(point);
                break;
            default:
                _drag = new CreateDrag(
                    Enum.Parse<DiagramShapeKind>(_tool.ToString()),
                    pointer.Alt ? point : Snap(point));
                break;
        }

        Refresh();
    }

    private void PressWithSelect(DiagramPointerModel pointer, Point point)
    {
        bool isAdding = pointer.Shift || pointer.Control;

        // Even with Shift held, which keeps a shape's proportions while it scales
        if (_selectedShapes.Count > 0
            && DiagramHitTester.HandleAt(SelectedBox(), point, GrabDistance / _viewport.Zoom) is DiagramHandle handle)
        {
            _drag = new ScaleDrag(point, handle, SelectedBounds(), SelectedBox(), SelectedLines());

            return;
        }

        // Lines are drawn over the shapes, so the one under the mouse is on top
        if (DiagramHitTester.LineAt(_diagram, point, GrabDistance / _viewport.Zoom) is { } line)
        {
            PressOnLine(line, point, isAdding);

            return;
        }

        if (DiagramHitTester.ShapeAt(_diagram, point) is { } shape)
        {
            PressOnShape(shape, point, isAdding);

            return;
        }

        if (DiagramHitTester.ConnectorAt(_diagram, point, GrabDistance / _viewport.Zoom) is { } connector)
        {
            if (!isAdding)
            {
                ClearSelection();
            }

            if (!_selectedConnectors.Remove(connector.Id))
            {
                _selectedConnectors.Add(connector.Id);
            }

            SelectionChanged();

            return;
        }

        if (!isAdding)
        {
            ClearSelection();
            SelectionChanged();
        }

        _drag = new BandDrag(point);
    }

    private void PressOnShape(DiagramShapeModel shape, Point point, bool isAdding)
    {
        IEnumerable<int> withGroup = WithGroup(shape);
        bool isSelected = _selectedShapes.Contains(shape.Id);

        if (isAdding && isSelected)
        {
            _selectedShapes.ExceptWith(withGroup);
            SelectionChanged();

            return;
        }

        if (!isSelected)
        {
            if (!isAdding)
            {
                ClearSelection();
            }

            _selectedShapes.UnionWith(withGroup);
            SelectionChanged();
        }

        _drag = new MoveDrag(point, SelectedBounds(), MoveBox(), SelectedLines());
    }

    /// <summary>Selects a line as a shape is, and drags it with the rest of the selection.</summary>
    private void PressOnLine(DiagramConnectorModel line, Point point, bool isAdding)
    {
        bool isSelected = _selectedConnectors.Contains(line.Id);

        if (isAdding && isSelected)
        {
            _selectedConnectors.Remove(line.Id);
            SelectionChanged();

            return;
        }

        if (!isSelected)
        {
            if (!isAdding)
            {
                ClearSelection();
            }

            _selectedConnectors.Add(line.Id);
            SelectionChanged();
        }

        _drag = new MoveDrag(point, SelectedBounds(), MoveBox(), SelectedLines());
    }

    private void PressWithConnector(Point point)
    {
        if (DiagramHitTester.ShapeAt(_diagram, point) is { } shape)
        {
            _drag = new ConnectDrag(shape.Id, null);
            _draftConnector = (CenterOf(shape.Bounds), point);
            Refresh();
        }
    }

    private void OnPointerMoved(object? sender, DiagramPointerModel pointer)
    {
        _lastPointer = pointer.Location;
        Point point = _viewport.ToDiagram(pointer.Location);

        switch (_drag)
        {
            case null:
                ShowCursor(HoverCursor(point));
                UpdateHover(point);
                return;
            case PanDrag pan:
                _viewport = _viewport with
                {
                    Pan = new Point(
                        _viewport.Pan.X + pointer.Location.X - pan.Last.X,
                        _viewport.Pan.Y + pointer.Location.Y - pan.Last.Y),
                };
                _drag = new PanDrag(pointer.Location);
                break;
            case MoveDrag move:
                DragMove(move, point, pointer.Alt);
                break;
            case ScaleDrag scale:
                DragScale(scale, point, pointer);
                break;
            case CreateDrag create:
                DragCreate(create, point, pointer.Alt);
                break;
            case ConnectDrag connect:
                DragConnect(connect, point);
                break;
            case ReconnectDrag reconnect:
                DragReconnect(reconnect, point);
                break;
            case LineDrag line:
                DragLine(line, point, pointer);
                break;
            case LineEndDrag lineEnd:
                DragLineEnd(lineEnd, point, pointer);
                break;
            case SegmentDrag segment:
                DragSegment(segment, point, pointer.Alt);
                break;
            case BandDrag band:
                _band = FromCorners(band.Start, point);
                break;
        }

        Refresh();
    }

    private void DragConnect(ConnectDrag connect, Point point)
    {
        DiagramShapeModel from = _diagram.Shapes.First(shape => shape.Id == connect.FromShapeId);
        DiagramShapeModel? target = ShapeNear(point);

        if (target?.Id == from.Id)
        {
            target = null;
        }

        _connectTarget = target?.Id;
        _connectTargetSide = target is null ? null : DropSide(target, point);

        Point start = connect.FromSide is DiagramSide fromSide
            ? Point.Round(DiagramRouter.AnchorPoint(from, fromSide))
            : CenterOf(from.Bounds);
        Point end = target is not null && _connectTargetSide is DiagramSide toSide
            ? Point.Round(DiagramRouter.AnchorPoint(target, toSide))
            : point;

        _draftConnector = (start, end);
    }

    /// <summary>
    ///     Shows the connector attached to the shape under the mouse, or else a line from its other end to the mouse.
    ///     It can't attach to the shape its other end is on.
    /// </summary>
    private void DragReconnect(ReconnectDrag reconnect, Point point)
    {
        if (IsStillAClick(reconnect.Start, point))
        {
            return;
        }

        DiagramConnectorModel connector = _diagram.Connectors.First(c => c.Id == reconnect.ConnectorId);
        bool isStart = reconnect.End == DiagramConnectorEnd.Start;
        int? otherShapeId = isStart ? connector.ToShapeId : connector.FromShapeId;
        DiagramShapeModel? target = ShapeNear(point);

        if (target?.Id == otherShapeId)
        {
            target = null;
        }

        _connectTarget = target?.Id;
        _connectTargetSide = target is null ? null : DropSide(target, point);

        if (target is null)
        {
            DiagramConnectorEnd otherEnd = isStart ? DiagramConnectorEnd.End : DiagramConnectorEnd.Start;
            PointF other = DiagramHitTester.EndPoint(DiagramRouter.PathOf(connector, _diagram)!, otherEnd);

            _draftConnector = (Point.Round(other), point);
            _preview = _diagram with { Connectors = [.. _diagram.Connectors.Where(c => c.Id != connector.Id)] };

            return;
        }

        DiagramConnectorModel moved = isStart
            ? connector with { FromShapeId = target.Id, FromSide = _connectTargetSide }
            : connector with { ToShapeId = target.Id, ToSide = _connectTargetSide };

        _draftConnector = null;
        _preview = _diagram with
        {
            Connectors = [.. _diagram.Connectors.Select(c => c.Id == connector.Id ? moved : c)],
        };
    }

    /// <summary>Shows the new line, with the style and heads it gets, from where the drag started to here.</summary>
    private void DragLine(LineDrag line, Point point, DiagramPointerModel pointer)
    {
        Point end = LineEnd(line.Start, point, pointer);

        DiagramConnectorModel drawn = DiagramConnectorModel.Line(_diagram.NextId, line.Start, end, _heads, _style);

        _preview = end == line.Start ? null : _diagram with { Connectors = [.. _diagram.Connectors, drawn] };
    }

    /// <summary>Shows the line with its end here; not on its other end, where it would have no direction.</summary>
    private void DragLineEnd(LineEndDrag drag, Point point, DiagramPointerModel pointer)
    {
        if (IsStillAClick(drag.Start, point))
        {
            return;
        }

        DiagramConnectorModel line = _diagram.Connectors.First(c => c.Id == drag.ConnectorId);
        bool isStart = drag.End == DiagramConnectorEnd.Start;
        Point other = (isStart ? line.ToPoint : line.FromPoint)!.Value;
        Point end = LineEnd(other, point, pointer);

        if (end == other)
        {
            return;
        }

        DiagramConnectorModel moved = isStart ? line with { FromPoint = end } : line with { ToPoint = end };

        _preview = _diagram with { Connectors = [.. _diagram.Connectors.Select(c => c.Id == line.Id ? moved : c)] };
    }

    /// <summary>
    ///     Where a line from the point to the mouse ends: on the edges and middles of the shapes nearby, or with Shift
    ///     along the nearest of level, upright and the diagonals. Alt turns snapping off.
    /// </summary>
    private Point LineEnd(Point from, Point point, DiagramPointerModel pointer)
    {
        if (pointer.Shift)
        {
            return AlongNearestAngle(from, point);
        }

        return pointer.Alt ? point : Snap(point);
    }

    /// <summary>The point moved onto the nearest of the eight directions from the start, as far from it.</summary>
    private static Point AlongNearestAngle(Point from, Point point)
    {
        int dx = point.X - from.X;
        int dy = point.Y - from.Y;
        double angle = Math.Round(Math.Atan2(dy, dx) / (Math.PI / 4)) * (Math.PI / 4);
        double length = Math.Sqrt((double)dx * dx + (double)dy * dy);

        return new Point(
            from.X + (int)Math.Round(Math.Cos(angle) * length),
            from.Y + (int)Math.Round(Math.Sin(angle) * length));
    }

    /// <summary>A drag of the piece of line at the point, of a selected elbow connector.</summary>
    /// <returns>The drag; null when there's no such line at the point.</returns>
    private SegmentDrag? SegmentDragAt(Point point)
    {
        if (_tool != DiagramTool.Select)
        {
            return null;
        }

        IEnumerable<DiagramConnectorModel> elbows = _diagram.Connectors
            .Where(c => _selectedConnectors.Contains(c.Id) && c.Routing == DiagramRouting.Elbow);

        foreach (DiagramConnectorModel connector in elbows)
        {
            if (DiagramRouter.PathOf(connector, _diagram) is { } path
                && DiagramHitTester.SegmentAt(path.Points, point, GrabDistance / _viewport.Zoom) is int segment
                && DiagramRouter.RouteFor(connector, _diagram) is { } route
                && DiagramRouter.IsLevelOrUpright(route[segment], route[segment + 1]))
            {
                return new SegmentDrag(point, connector.Id, route, segment);
            }
        }

        return null;
    }

    private void DragSegment(SegmentDrag drag, Point point, bool isSnapOff)
    {
        if (IsStillAClick(drag.Start, point))
        {
            return;
        }

        bool isLevel = drag.Route[drag.Segment].Y == drag.Route[drag.Segment + 1].Y;
        DiagramSnapModel snap = isSnapOff
            ? new DiagramSnapModel(0, 0, [])
            : DiagramSnapper.SnapPoint(point, [.. _diagram.Shapes.Select(shape => shape.Bounds)], SnapTolerance());
        Point moved = point;

        moved.Offset(snap.DeltaX, snap.DeltaY);
        _guides = [.. snap.Guides.Where(guide => IsVerticalGuide(guide) != isLevel)];

        List<PointF> route = DiagramRouter.WithSegmentMoved(drag.Route, drag.Segment, isLevel ? moved.Y : moved.X);
        List<Point> corners = [.. route.Skip(1).SkipLast(1).Select(Point.Round)];

        _preview = WithBends(_diagram, drag.ConnectorId, corners.Count > 0 ? corners : null);
    }

    private static DiagramModel WithBends(DiagramModel diagram, int connectorId, IReadOnlyList<Point>? bends) => diagram with
    {
        Connectors = [.. diagram.Connectors.Select(c => c.Id == connectorId ? c with { Bends = bends } : c)],
    };

    /// <returns>The selected connector with an end at the point, and that end; both null when there's none.</returns>
    private (DiagramConnectorModel? Connector, DiagramConnectorEnd? End) ConnectorEndUnder(Point point)
    {
        if (_tool != DiagramTool.Select)
        {
            return (null, null);
        }

        foreach (DiagramConnectorModel connector in _diagram.Connectors.Where(c => _selectedConnectors.Contains(c.Id)))
        {
            if (DiagramRouter.PathOf(connector, _diagram) is { } path
                && DiagramHitTester.ConnectorEndAt(path, point, AnchorGrabDistance / _viewport.Zoom) is DiagramConnectorEnd end)
            {
                return (connector, end);
            }
        }

        return (null, null);
    }

    /// <summary>The side of the target a connector dropped here snaps to: near its circle or its edge's middle.</summary>
    private DiagramSide? DropSide(DiagramShapeModel target, Point point)
    {
        float distance = AnchorGrabDistance / _viewport.Zoom;

        return DiagramHitTester.AnchorAt(target, point, AnchorOffset / _viewport.Zoom, distance)
            ?? DiagramHitTester.AnchorAt(target, point, 0, distance);
    }

    /// <summary>The shape at the point, or the one whose circles reach it, just outside its edges.</summary>
    private DiagramShapeModel? ShapeNear(Point point)
    {
        if (DiagramHitTester.ShapeAt(_diagram, point) is { } shape)
        {
            return shape;
        }

        int reach = (int)Math.Ceiling((AnchorOffset + AnchorGrabDistance) / _viewport.Zoom);

        return _diagram.Shapes.LastOrDefault(other =>
        {
            Rectangle area = other.Bounds;

            area.Inflate(reach, reach);

            return area.Contains(point);
        });
    }

    /// <returns>The shape showing circles at the point, and the circle there; the circle is null when none is.</returns>
    private (DiagramShapeModel? Shape, DiagramSide? Side) AnchorUnder(Point point)
    {
        if (_tool is not (DiagramTool.Select or DiagramTool.Connector) || ShapeNear(point) is not { } shape)
        {
            return (null, null);
        }

        return (shape, DiagramHitTester.AnchorAt(
            shape,
            point,
            AnchorOffset / _viewport.Zoom,
            AnchorGrabDistance / _viewport.Zoom));
    }

    private void UpdateHover(Point point)
    {
        (DiagramShapeModel? shape, DiagramSide? side) = AnchorUnder(point);

        if (shape?.Id != _hoverShape || side != _hoverSide)
        {
            _hoverShape = shape?.Id;
            _hoverSide = side;
            Refresh();
        }
    }

    private void ClearHover()
    {
        _hoverShape = null;
        _hoverSide = null;
    }

    private void OnPointerLeft(object? sender, EventArgs e)
    {
        if (_drag is null && _hoverShape is not null)
        {
            ClearHover();
            Refresh();
        }
    }

    private void DragMove(MoveDrag move, Point point, bool isSnapOff)
    {
        var delta = new Size(point.X - move.Start.X, point.Y - move.Start.Y);

        if (IsStillAClick(move.Start, point))
        {
            return;
        }

        var moved = Moved(move.Box, delta);
        DiagramSnapModel snap = isSnapOff
            ? new DiagramSnapModel(0, 0, [])
            : DiagramSnapper.SnapBox(moved, BoundsOfOthers(move.Originals), SnapTolerance());

        delta += new Size(snap.DeltaX, snap.DeltaY);
        _guides = snap.Guides;
        _preview = WithBounds(
            _diagram,
            move.Originals,
            bounds => Moved(bounds, delta),
            bend => bend + delta,
            move.Lines);
    }

    /// <summary>A click that moves a little isn't a drag, or snapping would shift what was only clicked.</summary>
    private bool IsStillAClick(Point start, Point point)
    {
        int distance = Math.Abs(point.X - start.X) + Math.Abs(point.Y - start.Y);

        return _preview is null && distance * _viewport.Zoom < DragThreshold;
    }

    private void DragScale(ScaleDrag scale, Point point, DiagramPointerModel pointer)
    {
        if (IsStillAClick(scale.Start, point))
        {
            return;
        }

        Point target = point;

        _guides = [];

        // With its proportions kept, a box can't also follow what's around it
        if (!pointer.Alt && !pointer.Shift)
        {
            DiagramSnapModel snap = DiagramSnapper.SnapPoint(point, BoundsOfOthers(scale.Originals), SnapTolerance());
            bool movesX = scale.Handle is not (DiagramHandle.Top or DiagramHandle.Bottom);
            bool movesY = scale.Handle is not (DiagramHandle.Left or DiagramHandle.Right);

            target.Offset(snap.DeltaX, snap.DeltaY);
            _guides = [.. snap.Guides.Where(guide => IsVerticalGuide(guide) ? movesX : movesY)];
        }

        Rectangle box = ScaleBox(scale.Box, scale.Handle, target, pointer.Shift);

        _preview = WithBounds(
            _diagram,
            scale.Originals,
            bounds => ScaleInside(bounds, scale.Box, box),
            bend => ScaleInside(new Rectangle(bend, Size.Empty), scale.Box, box).Location,
            scale.Lines);
    }

    private void DragCreate(CreateDrag create, Point point, bool isSnapOff)
    {
        DiagramSnapModel snap = isSnapOff
            ? new DiagramSnapModel(0, 0, [])
            : DiagramSnapper.SnapPoint(point, [.. _diagram.Shapes.Select(shape => shape.Bounds)], SnapTolerance());
        Point end = point;

        end.Offset(snap.DeltaX, snap.DeltaY);
        _guides = snap.Guides;

        var shape = new DiagramShapeModel(
            _diagram.NextId,
            create.Kind,
            FromCorners(create.Start, end),
            string.Empty,
            _style);

        _preview = _diagram with { Shapes = [.. _diagram.Shapes, shape] };
    }

    private void OnPointerReleased(object? sender, DiagramPointerModel pointer)
    {
        // The drag ends where the button was released, even if no move was reported there
        if (_drag is not null)
        {
            OnPointerMoved(sender, pointer);
        }

        Drag? drag = _drag;
        DiagramModel? preview = _preview;
        Point point = _viewport.ToDiagram(pointer.Location);
        Rectangle? band = _band;
        int? target = _connectTarget;
        DiagramSide? targetSide = _connectTargetSide;

        EndDrag();

        switch (drag)
        {
            // Dragged and brought back to where it was is nothing to undo
            case MoveDrag or ScaleDrag or LineEndDrag when preview is not null && IsMoved(preview):
                Commit(preview);
                break;
            case CreateDrag create:
                ReleaseCreate(create, preview);
                break;
            case LineDrag when preview is not null:
                ReleaseLine(preview);
                break;
            case ConnectDrag connect when target is int toShapeId:
                ReleaseConnect(connect, toShapeId, targetSide);
                break;

            // Dropped on no shape, it stays as it was. Dropped where it already was is nothing to undo
            case ReconnectDrag when target is not null
                && preview is not null
                && !preview.Connectors.SequenceEqual(_diagram.Connectors):
                Commit(preview);
                break;
            case SegmentDrag segment when preview is not null && RouteChanged(segment, preview):
                Commit(preview);
                break;
            case BandDrag when band is Rectangle box:
                _selectedShapes.UnionWith(_diagram.Shapes
                    .Where(shape => box.Contains(shape.Bounds))
                    .SelectMany(WithGroup));
                _selectedConnectors.UnionWith(_diagram.Connectors
                    .Where(c => c is { FromPoint: Point from, ToPoint: Point to }
                        && box.Contains(from)
                        && box.Contains(to))
                    .Select(c => c.Id));
                SelectionChanged();
                break;
        }

        ShowCursor(HoverCursor(point));
        UpdateHover(point);
        Refresh();
    }

    /// <summary>
    ///     Whether a shape or a line is somewhere else. Bends go along with the shapes, in new lists that never compare
    ///     equal, so they don't count.
    /// </summary>
    private bool IsMoved(DiagramModel preview) =>
        !preview.Shapes.SequenceEqual(_diagram.Shapes)
        || !preview.Connectors.Where(c => c.IsLine).SequenceEqual(_diagram.Connectors.Where(c => c.IsLine));

    /// <summary>Dragged and brought back to where it was is nothing to undo.</summary>
    private static bool RouteChanged(SegmentDrag drag, DiagramModel preview)
    {
        DiagramConnectorModel connector = preview.Connectors.First(c => c.Id == drag.ConnectorId);

        return !DiagramRouter.RouteFor(connector, preview)!.SequenceEqual(drag.Route);
    }

    private void ReleaseCreate(CreateDrag create, DiagramModel? preview)
    {
        Rectangle bounds = preview?.Shapes[^1].Bounds ?? Rectangle.Empty;

        // A click, or a drag too small to see, makes the usual size around that point
        if (bounds.Width < MinShapeSize || bounds.Height < MinShapeSize)
        {
            bounds = new Rectangle(
                create.Start.X - _defaultShapeSize.Width / 2,
                create.Start.Y - _defaultShapeSize.Height / 2,
                _defaultShapeSize.Width,
                _defaultShapeSize.Height);
        }

        var shape = new DiagramShapeModel(_diagram.NextId, create.Kind, bounds, string.Empty, _style);

        Commit(_diagram with { Shapes = [.. _diagram.Shapes, shape] });
        ClearSelection();
        _selectedShapes.Add(shape.Id);
        SelectTool(DiagramTool.Select);
        SelectionChanged();
    }

    /// <summary>Adds the line drawn, and selects it to change it at once, as a new shape is.</summary>
    private void ReleaseLine(DiagramModel preview)
    {
        DiagramConnectorModel line = preview.Connectors[^1];

        Commit(preview);
        ClearSelection();
        _selectedConnectors.Add(line.Id);
        SelectTool(DiagramTool.Select);
        SelectionChanged();
    }

    private void ReleaseConnect(ConnectDrag connect, int toShapeId, DiagramSide? toSide)
    {
        var connector = new DiagramConnectorModel(
            _diagram.NextId,
            connect.FromShapeId,
            toShapeId,
            _routing,
            _heads,
            string.Empty,
            _style,
            connect.FromSide,
            toSide);

        Commit(_diagram with { Connectors = [.. _diagram.Connectors, connector] });
    }

    /// <summary>
    ///     On a shape or a connector, shows the menu; a right-click on something not selected selects only it (with
    ///     its group) first, and one on what's selected keeps the whole selection. Nothing on an empty spot.
    /// </summary>
    private void OnPointerRightClicked(object? sender, Point location)
    {
        if (_drag is not null)
        {
            return;
        }

        Point point = _viewport.ToDiagram(location);

        if (DiagramHitTester.ShapeAt(_diagram, point) is { } shape)
        {
            if (!_selectedShapes.Contains(shape.Id))
            {
                ClearSelection();
                _selectedShapes.UnionWith(WithGroup(shape));
            }
        }
        else if (DiagramHitTester.ConnectorAt(_diagram, point, GrabDistance / _viewport.Zoom) is { } connector)
        {
            if (!_selectedConnectors.Contains(connector.Id))
            {
                ClearSelection();
                _selectedConnectors.Add(connector.Id);
            }
        }
        else
        {
            return;
        }

        SelectionChanged();
        Refresh();
        _view.ShowContextMenu(location, new DiagramContextMenuModel(_copiedStyle is not null, CanGroup(), CanUngroup()));
    }

    private void OnPointerDoubleClicked(object? sender, DiagramPointerModel pointer)
    {
        if (_tool != DiagramTool.Select)
        {
            return;
        }

        // The press before this one may have started a drag
        EndDrag();

        Point point = _viewport.ToDiagram(pointer.Location);

        if (DiagramHitTester.ShapeAt(_diagram, point) is { } shape)
        {
            BeginShapeText(shape);
        }
        else if (DiagramHitTester.ConnectorAt(_diagram, point, GrabDistance / _viewport.Zoom) is { } connector)
        {
            BeginLabel(connector);
        }
        else
        {
            BeginNewText(point);
        }

        Refresh();
    }

    private void ResetRoute()
    {
        if (_diagram.Connectors.Any(c => _selectedConnectors.Contains(c.Id) && c.Bends is not null))
        {
            ChangeSelectedConnectors(connector => connector with { Bends = null });
        }
    }

    private void BeginShapeText(DiagramShapeModel shape)
    {
        _commitText = text => Commit(_diagram with
        {
            Shapes = [.. _diagram.Shapes.Select(s => s.Id == shape.Id ? s with { Text = text } : s)],
        });

        Point topLeft = _viewport.ToCanvas(shape.Bounds.Location);
        Point bottomRight = _viewport.ToCanvas(new Point(shape.Bounds.Right, shape.Bounds.Bottom));

        _view.BeginTextEdit(
            Rectangle.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y),
            shape.Text,
            CanvasFontSize(shape.Style.FontSize),
            shape.Style.FontFamily,
            shape.Style.TextColor);
    }

    private void BeginLabel(DiagramConnectorModel connector)
    {
        _commitText = text => Commit(_diagram with
        {
            Connectors = [.. _diagram.Connectors.Select(c => c.Id == connector.Id ? c with { Label = text } : c)],
        });

        PointF center = DiagramRouter.PathOf(connector, _diagram)!.LabelCenter;
        int fontSize = CanvasFontSize(connector.Style.FontSize);
        Point canvas = _viewport.ToCanvas(Point.Round(center));
        int width = fontSize * 14;
        int height = fontSize * 2;

        _view.BeginTextEdit(
            new Rectangle(canvas.X - width / 2, canvas.Y - height / 2, width, height),
            connector.Label,
            fontSize,
            connector.Style.FontFamily,
            connector.Style.TextColor);
    }

    /// <summary>A text on its own is only added once it has something in it.</summary>
    private void BeginNewText(Point point)
    {
        _commitText = text =>
        {
            if (text.Length == 0)
            {
                return;
            }

            IReadOnlyList<string> lines = text.Split('\n');
            int width = (int)lines.Max(line => DiagramTextLayout.WidthOf(line, _style.FontSize)) + 16;
            int height = (int)DiagramTextLayout.LayoutAt(text, point, _style.FontSize).Bounds.Height + 8;
            var shape = new DiagramShapeModel(
                _diagram.NextId,
                DiagramShapeKind.Text,
                new Rectangle(point.X, point.Y, width, height),
                text,
                _style);

            Commit(_diagram with { Shapes = [.. _diagram.Shapes, shape] });
            ClearSelection();
            _selectedShapes.Add(shape.Id);
            SelectionChanged();
        };

        int fontSize = CanvasFontSize(_style.FontSize);
        Point canvas = _viewport.ToCanvas(point);

        _view.BeginTextEdit(
            new Rectangle(canvas.X, canvas.Y, fontSize * 14, fontSize * 2),
            string.Empty,
            fontSize,
            _style.FontFamily,
            _style.TextColor);
    }

    private void OnTextCommitted(object? sender, string text)
    {
        Action<string>? commit = _commitText;

        _commitText = null;
        commit?.Invoke(text.ReplaceLineEndings("\n").TrimEnd());
        Refresh();
    }

    private void OnWheelTurned(object? sender, DiagramWheelModel wheel)
    {
        if (wheel.Control)
        {
            ZoomAround(wheel.Location, MathF.Pow(WheelZoomFactor, wheel.Steps));
        }
        else
        {
            int pixels = (int)Math.Round(wheel.Steps * WheelScrollPixels);
            Size scroll = wheel.Shift ? new Size(pixels, 0) : new Size(0, pixels);

            _viewport = _viewport with { Pan = _viewport.Pan + scroll };
            Refresh();
        }
    }

    private void ZoomAround(Point canvas, float factor)
    {
        // The diagram's point under the mouse stays under it
        Point point = _viewport.ToDiagram(canvas);
        float zoom = Math.Clamp(_viewport.Zoom * factor, MinZoom, MaxZoom);

        _viewport = new DiagramViewportModel(
            zoom,
            new Point(canvas.X - (int)Math.Round(point.X * zoom), canvas.Y - (int)Math.Round(point.Y * zoom)));
        Refresh();
    }

    private void OnToolSelected(object? sender, DiagramTool tool)
    {
        SelectTool(tool);
        ShowCursor(tool == DiagramTool.Select ? DiagramCursor.Default : DiagramCursor.Cross);
    }

    private void SelectTool(DiagramTool tool)
    {
        _tool = tool;
        _view.ShowTool(tool);
    }

    private void OnCommandRequested(object? sender, DiagramCommand command)
    {
        // A drag only ends with its release, or with Esc
        if (_drag is not null && command != DiagramCommand.Cancel)
        {
            return;
        }

        switch (command)
        {
            case DiagramCommand.New:
                New();
                break;
            case DiagramCommand.Open:
                Open();
                break;
            case DiagramCommand.Save:
                Save();
                break;
            case DiagramCommand.SaveAs:
                SaveAs();
                break;
            case DiagramCommand.ExportPdf:
                ExportPdf();
                break;
            case DiagramCommand.Undo:
                Undo();
                break;
            case DiagramCommand.Redo:
                Redo();
                break;
            case DiagramCommand.Cut:
                Copy();
                Delete();
                break;
            case DiagramCommand.Copy:
                Copy();
                break;
            case DiagramCommand.Paste:
                Paste();
                break;
            case DiagramCommand.Duplicate:
                Insert(SelectedFragment(), PasteOffset);
                break;
            case DiagramCommand.CopyStyle:
                CopyStyle();
                break;
            case DiagramCommand.PasteStyle:
                PasteStyle();
                break;
            case DiagramCommand.Delete:
                Delete();
                break;
            case DiagramCommand.SelectAll:
                SelectAll();
                break;
            case DiagramCommand.Group:
                Group();
                break;
            case DiagramCommand.Ungroup:
                Ungroup();
                break;
            case DiagramCommand.BringToFront:
                Reorder(selectedFirst: false);
                break;
            case DiagramCommand.SendToBack:
                Reorder(selectedFirst: true);
                break;
            case DiagramCommand.ResetRoute:
                ResetRoute();
                break;
            case DiagramCommand.ZoomIn:
                ZoomAround(_lastPointer, CommandZoomFactor);
                break;
            case DiagramCommand.ZoomOut:
                ZoomAround(_lastPointer, 1 / CommandZoomFactor);
                break;
            case DiagramCommand.ZoomReset:
                _viewport = new DiagramViewportModel(1, _viewport.Pan);
                Refresh();
                break;
            case DiagramCommand.Cancel:
                Cancel();
                break;
        }
    }

    private void Cancel()
    {
        if (_drag is not null)
        {
            EndDrag();
        }
        else if (_tool != DiagramTool.Select)
        {
            SelectTool(DiagramTool.Select);
        }
        else
        {
            ClearSelection();
            SelectionChanged();
        }

        ShowCursor(DiagramCursor.Default);
        Refresh();
    }

    private void New()
    {
        if (ConfirmLeave())
        {
            Replace(DiagramModel.Empty, null);
        }
    }

    private void Open()
    {
        if (!ConfirmLeave() || _view.AskOpenPath() is not string path)
        {
            return;
        }

        try
        {
            Replace(_fileStore.Load(path), path);
            _savedVersion = _fileStore.VersionOf(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _view.ShowError($"Could not open {Path.GetFileName(path)}: {ex.Message}");
        }
    }

    /// <summary>Starts over with another diagram, with its top left corner in view.</summary>
    private void Replace(DiagramModel diagram, string? path)
    {
        _diagram = diagram;
        _savedDiagram = diagram;
        _path = path;
        _undo.Clear();
        _redo.Clear();
        ClearSelection();

        const int margin = 40;
        Rectangle area = diagram.Shapes.Count == 0
            ? Rectangle.Empty
            : UnionOf(diagram.Shapes.Select(shape => shape.Bounds));

        _viewport = new DiagramViewportModel(1, new Point(margin - area.Left, margin - area.Top));
        SelectionChanged();
        Refresh();
    }

    private bool Save() => _path is null ? SaveAs() : SaveTo(_path);

    private bool SaveAs()
    {
        string suggested = _path is null ? "Diagram.svg" : Path.GetFileName(_path);

        return _view.AskSavePath(suggested) is string path && SaveTo(path);
    }

    private bool SaveTo(string path)
    {
        // Another window or program may have saved it. Saving as another file has its own dialog's question
        if (string.Equals(path, _path, StringComparison.OrdinalIgnoreCase)
            && _fileStore.VersionOf(path) != _savedVersion
            && !_view.AskOverwrite(Name))
        {
            return false;
        }

        try
        {
            _fileStore.Save(path, _diagram);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowError($"Could not save {Path.GetFileName(path)}: {ex.Message}");

            return false;
        }

        _path = path;
        _savedVersion = _fileStore.VersionOf(path);
        _savedDiagram = _diagram;
        Refresh();

        return true;
    }

    /// <summary>Writes a copy to share or print; the diagram stays saved, or not, where it was.</summary>
    private void ExportPdf()
    {
        string suggested = Path.ChangeExtension(_path is null ? "Diagram" : Path.GetFileName(_path), ".pdf");

        if (_view.AskExportPdfPath(suggested) is not string path)
        {
            return;
        }

        try
        {
            _pdfExporter.Export(path, _diagram);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowError($"Could not export {Path.GetFileName(path)}: {ex.Message}");
        }
    }

    /// <returns>True when the diagram can be left: nothing is lost, or the user said so.</returns>
    private bool ConfirmLeave()
    {
        if (!HasChanges)
        {
            return true;
        }

        return _view.AskSaveChanges(Name) switch
        {
            DiagramSaveChoice.Save => Save(),
            DiagramSaveChoice.Discard => true,
            _ => false,
        };
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = !ConfirmLeave();
    }

    private void Undo()
    {
        if (_undo.TryPop(out DiagramModel? diagram))
        {
            _redo.Push(_diagram);
            _diagram = diagram;
            KeepSelectionThatExists();
        }
    }

    private void Redo()
    {
        if (_redo.TryPop(out DiagramModel? diagram))
        {
            _undo.Push(_diagram);
            _diagram = diagram;
            KeepSelectionThatExists();
        }
    }

    private void KeepSelectionThatExists()
    {
        _selectedShapes.IntersectWith(_diagram.Shapes.Select(shape => shape.Id));
        _selectedConnectors.IntersectWith(_diagram.Connectors.Select(connector => connector.Id));
        SelectionChanged();
        Refresh();
    }

    private void Copy()
    {
        if (_selectedShapes.Count > 0 || SelectedLines().Count > 0)
        {
            _view.SetClipboardText(DiagramJson.Write(SelectedFragment()));
            _pasteCount = 0;
        }
    }

    private void Paste()
    {
        if (_view.GetClipboardText() is not string text)
        {
            return;
        }

        try
        {
            DiagramModel fragment = DiagramJson.Read(text);

            // So several pastes don't land on top of each other
            _pasteCount++;
            Insert(fragment, PasteOffset * _pasteCount);
        }
        catch (InvalidDataException)
        {
            // Something else is on the clipboard
        }
    }

    /// <summary>The selected shapes, the connectors between them, and the selected lines.</summary>
    private DiagramModel SelectedFragment() => new(
        [.. _diagram.Shapes.Where(shape => _selectedShapes.Contains(shape.Id))],
        [
            .. _diagram.Connectors.Where(connector => connector.IsLine
                ? _selectedConnectors.Contains(connector.Id)
                : IsSelectedShape(connector.FromShapeId) && IsSelectedShape(connector.ToShapeId)),
        ]);

    private bool IsSelectedShape(int? shapeId) => shapeId is int id && _selectedShapes.Contains(id);

    /// <summary>Adds copies of the parts, with new ids and groups, and selects them.</summary>
    private void Insert(DiagramModel fragment, int offset)
    {
        if (fragment.Shapes.Count == 0 && !fragment.Connectors.Any(connector => connector.IsLine))
        {
            return;
        }

        int nextId = _diagram.NextId;
        int nextGroupId = _diagram.NextGroupId;
        Dictionary<int, int> shapeIds = [];
        Dictionary<int, int> groupIds = [];
        List<DiagramShapeModel> shapes = [];

        foreach (DiagramShapeModel shape in fragment.Shapes)
        {
            shapeIds[shape.Id] = nextId++;

            if (shape.GroupId is int groupId && !groupIds.ContainsKey(groupId))
            {
                groupIds[groupId] = nextGroupId++;
            }

            shapes.Add(shape with
            {
                Id = shapeIds[shape.Id],
                Bounds = Moved(shape.Bounds, new Size(offset, offset)),
                GroupId = shape.GroupId is int group ? groupIds[group] : null,
            });
        }

        List<DiagramConnectorModel> connectors = [];

        var shift = new Size(offset, offset);

        foreach (DiagramConnectorModel connector in fragment.Connectors)
        {
            if (connector is { FromPoint: Point from, ToPoint: Point to })
            {
                connectors.Add(connector with { Id = nextId++, FromPoint = from + shift, ToPoint = to + shift });
            }
            else if (connector is { FromShapeId: int fromId, ToShapeId: int toId }
                && shapeIds.TryGetValue(fromId, out int newFromId)
                && shapeIds.TryGetValue(toId, out int newToId))
            {
                connectors.Add(connector with
                {
                    Id = nextId++,
                    FromShapeId = newFromId,
                    ToShapeId = newToId,
                    Bends = connector.Bends?.Select(bend => bend + shift).ToList(),
                });
            }
        }

        Commit(new DiagramModel([.. _diagram.Shapes, .. shapes], [.. _diagram.Connectors, .. connectors]));
        ClearSelection();
        _selectedShapes.UnionWith(shapes.Select(shape => shape.Id));
        _selectedConnectors.UnionWith(connectors.Where(connector => connector.IsLine).Select(line => line.Id));
        SelectionChanged();
        Refresh();
    }

    /// <summary>Deletes what's selected, and the connectors that lose one of their shapes.</summary>
    private void Delete()
    {
        if (_selectedShapes.Count == 0 && _selectedConnectors.Count == 0)
        {
            return;
        }

        Commit(new DiagramModel(
            [.. _diagram.Shapes.Where(shape => !_selectedShapes.Contains(shape.Id))],
            [
                .. _diagram.Connectors.Where(connector => !_selectedConnectors.Contains(connector.Id)
                    && !IsSelectedShape(connector.FromShapeId)
                    && !IsSelectedShape(connector.ToShapeId)),
            ]));
        ClearSelection();
        SelectionChanged();
        Refresh();
    }

    private void SelectAll()
    {
        _selectedShapes.UnionWith(_diagram.Shapes.Select(shape => shape.Id));
        _selectedConnectors.UnionWith(_diagram.Connectors.Select(connector => connector.Id));
        SelectionChanged();
        Refresh();
    }

    /// <summary>Two or more shapes, not all in one group already, which grouping again would leave as they are.</summary>
    private bool CanGroup()
    {
        if (_selectedShapes.Count < 2)
        {
            return false;
        }

        List<int?> groups = [.. _diagram.Shapes
            .Where(shape => _selectedShapes.Contains(shape.Id))
            .Select(shape => shape.GroupId)
            .Distinct()];

        return groups.Count > 1 || groups[0] is null;
    }

    private bool CanUngroup() =>
        _diagram.Shapes.Any(shape => _selectedShapes.Contains(shape.Id) && shape.GroupId is not null);

    private void Group()
    {
        if (!CanGroup())
        {
            return;
        }

        int groupId = _diagram.NextGroupId;

        Commit(_diagram with
        {
            Shapes =
            [
                .. _diagram.Shapes.Select(shape =>
                    _selectedShapes.Contains(shape.Id) ? shape with { GroupId = groupId } : shape),
            ],
        });
    }

    private void Ungroup()
    {
        if (CanUngroup())
        {
            Commit(_diagram with
            {
                Shapes =
                [
                    .. _diagram.Shapes.Select(shape =>
                        _selectedShapes.Contains(shape.Id) ? shape with { GroupId = null } : shape),
                ],
            });
        }
    }

    /// <summary>Moves the selected shapes to the start or the end of the list, keeping their order.</summary>
    private void Reorder(bool selectedFirst)
    {
        if (_selectedShapes.Count == 0)
        {
            return;
        }

        IEnumerable<DiagramShapeModel> selected = _diagram.Shapes.Where(shape => _selectedShapes.Contains(shape.Id));
        IEnumerable<DiagramShapeModel> others = _diagram.Shapes.Where(shape => !_selectedShapes.Contains(shape.Id));

        Commit(_diagram with { Shapes = selectedFirst ? [.. selected, .. others] : [.. others, .. selected] });
    }

    private void OnNudgeRequested(object? sender, Size delta)
    {
        if (_drag is null && (_selectedShapes.Count > 0 || SelectedLines().Count > 0))
        {
            // Shapes locked where they are don't move, and that's nothing to undo
            Commit(
                WithBounds(
                    _diagram,
                    SelectedBounds(),
                    bounds => Moved(bounds, delta),
                    bend => bend + delta,
                    SelectedLines()),
                onlyIfChanged: true);
        }
    }

    private void OnColorSelected(object? sender, DiagramColorChoiceModel choice)
    {
        // The no fill choice is Color.Empty, which stays empty
        Color color = choice.Color.IsEmpty ? choice.Color : Color.FromArgb(255, choice.Color);

        ChangeStyle(style => choice.Target switch
        {
            DiagramColorTarget.Fill => style with { Fill = color },
            DiagramColorTarget.Line => style with { Line = color },
            DiagramColorTarget.TextOutline => WithTextOutlineColor(style, color),
            _ => style with { TextColor = color },
        });
    }

    /// <summary>
    ///     No color turns the edge off, keeping its color for later; a color turns it on when it's off, or the color
    ///     would seem to do nothing.
    /// </summary>
    private static DiagramStyleModel WithTextOutlineColor(DiagramStyleModel style, Color color)
    {
        if (color.IsEmpty)
        {
            return style with { TextOutlineWidth = 0 };
        }

        return style with
        {
            TextOutlineColor = color,
            TextOutlineWidth = style.TextOutlineWidth == 0 ? TurnedOnTextOutlineWidth : style.TextOutlineWidth,
        };
    }

    private void OnMoreColorsRequested(object? sender, DiagramColorTarget target)
    {
        DiagramStyleModel style = SelectedStyle();
        Color current = target switch
        {
            DiagramColorTarget.Fill => style.Fill,
            DiagramColorTarget.Line => style.Line,
            DiagramColorTarget.TextOutline => style.TextOutlineColor,
            _ => style.TextColor,
        };

        if (_view.AskColor(current) is Color color)
        {
            OnColorSelected(sender, new DiagramColorChoiceModel(target, color));
        }
    }

    private void OnLineWidthSelected(object? sender, int width)
    {
        ChangeStyle(style => style with { LineWidth = Math.Clamp(width, 0, MaxLineWidth) });
    }

    /// <summary>Moves and scales the selected shapes as a whole, to where the box around them was typed.</summary>
    private void OnBoundsEdited(object? sender, Rectangle box)
    {
        if (_drag is not null || _selectedShapes.Count == 0)
        {
            return;
        }

        Rectangle from = SelectedBox();
        var to = new Rectangle(box.Location, new Size(Math.Max(box.Width, MinShapeSize), Math.Max(box.Height, MinShapeSize)));

        if (to != from)
        {
            Commit(
                WithBounds(
                    _diagram,
                    SelectedBounds(),
                    bounds => ScaleInside(bounds, from, to),
                    bend => ScaleInside(new Rectangle(bend, Size.Empty), from, to).Location),
                onlyIfChanged: true);
        }
    }

    /// <summary>A change to the diagram like any other, so it can be undone, and it's saved with the shapes.</summary>
    private void OnLockToggled(object? sender, DiagramLockChoiceModel choice)
    {
        if (_drag is not null || _selectedShapes.Count == 0)
        {
            return;
        }

        DiagramLock locks = choice.Lock & DiagramLock.All;

        Commit(
            _diagram with
            {
                Shapes =
                [
                    .. _diagram.Shapes.Select(shape => _selectedShapes.Contains(shape.Id)
                        ? shape with { Locks = choice.IsLocked ? shape.Locks | locks : shape.Locks & ~locks }
                        : shape),
                ],
            },
            onlyIfChanged: true);
    }

    private void OnCornerRadiusEdited(object? sender, int radius)
    {
        int corner = Math.Max(radius, 0);

        bool IsChanged(DiagramShapeModel shape) => _selectedShapes.Contains(shape.Id)
            && DiagramFigures.HasCornerRadius(shape.Kind)
            && (shape.CornerRadius ?? DiagramFigures.DefaultCornerRadius(shape.Kind)) != corner;

        if (_drag is null && _diagram.Shapes.Any(IsChanged))
        {
            Commit(_diagram with
            {
                Shapes = [.. _diagram.Shapes.Select(shape => IsChanged(shape) ? shape with { CornerRadius = corner } : shape)],
            });
        }
    }

    /// <summary>The text of the one thing selected: a shape's text or a connector's label.</summary>
    private void OnTextEdited(object? sender, string text)
    {
        if (_drag is not null || _selectedShapes.Count + _selectedConnectors.Count != 1)
        {
            return;
        }

        string edited = text.ReplaceLineEndings("\n").TrimEnd();

        Commit(new DiagramModel(
            [
                .. _diagram.Shapes.Select(shape =>
                    _selectedShapes.Contains(shape.Id) && shape.Text != edited ? shape with { Text = edited } : shape),
            ],
            [
                .. _diagram.Connectors.Select(connector => _selectedConnectors.Contains(connector.Id) && connector.Label != edited
                    ? connector with { Label = edited }
                    : connector),
            ]),
            onlyIfChanged: true);
    }

    private void OnDashedSelected(object? sender, bool isDashed)
    {
        ChangeStyle(style => style with { Dashed = isDashed });
    }

    private void OnFontSizeSelected(object? sender, int size)
    {
        ChangeStyle(style => style with { FontSize = Math.Clamp(size, MinFontSize, MaxFontSize) });
    }

    private void OnTextOutlineWidthSelected(object? sender, int width)
    {
        ChangeStyle(style => style with { TextOutlineWidth = Math.Clamp(width, 0, DiagramStyleModel.MaxTextOutlineWidth) });
    }

    private void OnFontFamilySelected(object? sender, string fontFamily)
    {
        if (!string.IsNullOrWhiteSpace(fontFamily))
        {
            ChangeStyle(style => style with { FontFamily = fontFamily });
        }
    }

    private void OnRoutingSelected(object? sender, DiagramRouting routing)
    {
        _routing = routing;

        // Choosing how it should go takes the place of the bends
        // A line on its own stays straight
        ChangeSelectedConnectors(connector =>
            connector.IsLine ? connector : connector with { Routing = routing, Bends = null });
        SaveSettings();
    }

    private void OnArrowHeadsSelected(object? sender, DiagramArrowHeads heads)
    {
        _heads = heads;
        ChangeSelectedConnectors(connector => connector with { Heads = heads });
        SaveSettings();
    }

    private void ChangeSelectedConnectors(Func<DiagramConnectorModel, DiagramConnectorModel> change)
    {
        if (_selectedConnectors.Count > 0)
        {
            Commit(_diagram with
            {
                Connectors =
                [
                    .. _diagram.Connectors.Select(connector =>
                        _selectedConnectors.Contains(connector.Id) ? change(connector) : connector),
                ],
            });
        }

        ShowSelectedStyle();
    }

    /// <summary>Changes what comes next, and what's selected.</summary>
    private void ChangeStyle(Func<DiagramStyleModel, DiagramStyleModel> change)
    {
        _style = change(_style);
        SaveSettings();

        if (_selectedShapes.Count > 0 || _selectedConnectors.Count > 0)
        {
            Commit(new DiagramModel(
                [
                    .. _diagram.Shapes.Select(shape =>
                        _selectedShapes.Contains(shape.Id) ? shape with { Style = change(shape.Style) } : shape),
                ],
                [
                    .. _diagram.Connectors.Select(connector => _selectedConnectors.Contains(connector.Id)
                        ? connector with { Style = change(connector.Style) }
                        : connector),
                ]));
        }

        ShowSelectedStyle();
    }

    private void CopyStyle()
    {
        if (_selectedShapes.Count > 0 || _selectedConnectors.Count > 0)
        {
            _copiedStyle = SelectedStyle();
            _copiedStyleHasFill = _selectedShapes.Count > 0;
        }
    }

    /// <summary>
    ///     The whole copied style, except that a style copied from a connector, which has no fill, leaves a shape's
    ///     fill as it is.
    /// </summary>
    private void PasteStyle()
    {
        if (_copiedStyle is not DiagramStyleModel copied)
        {
            return;
        }

        ChangeStyle(style => _copiedStyleHasFill ? copied : copied with { Fill = style.Fill });
    }

    private void SelectionChanged() => ShowSelectedStyle();

    private DiagramStyleModel SelectedStyle()
    {
        return _diagram.Shapes.FirstOrDefault(shape => _selectedShapes.Contains(shape.Id))?.Style
            ?? _diagram.Connectors.FirstOrDefault(connector => _selectedConnectors.Contains(connector.Id))?.Style
            ?? _style;
    }

    private void ShowSelectedStyle()
    {
        DiagramConnectorModel? connector = _diagram.Connectors
            .FirstOrDefault(c => _selectedConnectors.Contains(c.Id));

        _view.ShowStyle(SelectedStyle(), connector?.Routing ?? _routing, connector?.Heads ?? _heads);
    }

    /// <summary>The new diagram becomes current; undo goes back to the one before it, redo is lost.</summary>
    /// <param name="diagram">The new diagram.</param>
    /// <param name="onlyIfChanged">True to leave nothing to undo when its parts are all the same as now.</param>
    private void Commit(DiagramModel diagram, bool onlyIfChanged = false)
    {
        if (onlyIfChanged
            && diagram.Shapes.SequenceEqual(_diagram.Shapes)
            && diagram.Connectors.SequenceEqual(_diagram.Connectors))
        {
            return;
        }

        _undo.Push(_diagram);
        _redo.Clear();
        _diagram = diagram;
        Refresh();
    }

    private void EndDrag()
    {
        _drag = null;
        _preview = null;
        _guides = [];
        _band = null;
        _draftConnector = null;
        _connectTarget = null;
        _connectTargetSide = null;
    }

    private void ClearSelection()
    {
        _selectedShapes.Clear();
        _selectedConnectors.Clear();
    }

    /// <summary>The shape's id, and those of the rest of its group.</summary>
    private IEnumerable<int> WithGroup(DiagramShapeModel shape) => shape.GroupId is null
        ? [shape.Id]
        : _diagram.Shapes.Where(other => other.GroupId == shape.GroupId).Select(other => other.Id);

    private Dictionary<int, Rectangle> SelectedBounds() => _diagram.Shapes
        .Where(shape => _selectedShapes.Contains(shape.Id))
        .ToDictionary(shape => shape.Id, shape => shape.Bounds);

    private Rectangle SelectedBox() => UnionOf(SelectedBounds().Values);

    /// <summary>The ids of the selected lines on their own.</summary>
    private HashSet<int> SelectedLines() =>
        [.. _diagram.Connectors.Where(c => c.IsLine && _selectedConnectors.Contains(c.Id)).Select(c => c.Id)];

    /// <summary>Around everything a move takes along: the selected shapes and lines.</summary>
    private Rectangle MoveBox() => UnionOf(
        SelectedBounds().Values.Concat(_diagram.Connectors
            .Where(c => c.IsLine && _selectedConnectors.Contains(c.Id))
            .Select(c => FromCorners(c.FromPoint!.Value, c.ToPoint!.Value))));

    private List<Rectangle> BoundsOfOthers(IReadOnlyDictionary<int, Rectangle> moving) =>
        [.. _diagram.Shapes.Where(shape => !moving.ContainsKey(shape.Id)).Select(shape => shape.Bounds)];

    /// <summary>
    ///     Changes where some shapes are, except what they have locked, and moves the lines given with them. The bends
    ///     of a connector go along when both of its shapes do, without locks, so a part of a diagram keeps its look
    ///     when it's moved or scaled as a whole.
    /// </summary>
    /// <param name="lines">The ids of the lines on their own whose ends go as the bends do.</param>
    private static DiagramModel WithBounds(
        DiagramModel diagram,
        IReadOnlyDictionary<int, Rectangle> originals,
        Func<Rectangle, Rectangle> change,
        Func<Point, Point> changeBend,
        IReadOnlySet<int>? lines = null)
    {
        HashSet<int> free =
        [
            .. diagram.Shapes
                .Where(shape => originals.ContainsKey(shape.Id) && shape.Locks == DiagramLock.None)
                .Select(shape => shape.Id),
        ];

        return new DiagramModel(
            [
                .. diagram.Shapes.Select(shape => originals.TryGetValue(shape.Id, out Rectangle bounds)
                    ? shape with { Bounds = Locked(bounds, change(bounds), shape.Locks) }
                    : shape),
            ],
            [
                .. diagram.Connectors.Select(connector => connector switch
                {
                    { FromPoint: Point from, ToPoint: Point to } when lines?.Contains(connector.Id) == true =>
                        MovedLine(connector, changeBend(from), changeBend(to)),
                    { Bends: { } bends, FromShapeId: int fromId, ToShapeId: int toId }
                        when free.Contains(fromId) && free.Contains(toId) =>
                        connector with { Bends = [.. bends.Select(changeBend)] },
                    _ => connector,
                }),
            ]);
    }

    /// <summary>A line with its ends here; as it was if they'd meet, since then it would have no direction.</summary>
    private static DiagramConnectorModel MovedLine(DiagramConnectorModel line, Point from, Point to) =>
        from == to ? line : line with { FromPoint = from, ToPoint = to };

    /// <summary>
    ///     The changed bounds with the locked parts as they were. A locked size keeps the edge that didn't move where
    ///     it was: scaling a shape of locked width by its left edge leaves it, rather than sliding it.
    /// </summary>
    private static Rectangle Locked(Rectangle original, Rectangle changed, DiagramLock locks)
    {
        if (locks == DiagramLock.None)
        {
            return changed;
        }

        bool isWidthLocked = locks.HasFlag(DiagramLock.Width);
        bool isHeightLocked = locks.HasFlag(DiagramLock.Height);
        int width = isWidthLocked ? original.Width : changed.Width;
        int height = isHeightLocked ? original.Height : changed.Height;
        int x = locks.HasFlag(DiagramLock.X) || (isWidthLocked && changed.Right == original.Right)
            ? original.X
            : changed.X;
        int y = locks.HasFlag(DiagramLock.Y) || (isHeightLocked && changed.Bottom == original.Bottom)
            ? original.Y
            : changed.Y;

        return new Rectangle(x, y, width, height);
    }

    private Point Snap(Point point)
    {
        DiagramSnapModel snap = DiagramSnapper.SnapPoint(
            point,
            [.. _diagram.Shapes.Select(shape => shape.Bounds)],
            SnapTolerance());

        point.Offset(snap.DeltaX, snap.DeltaY);

        return point;
    }

    private int SnapTolerance() => Math.Max(1, (int)Math.Round(SnapDistance / _viewport.Zoom));

    private int CanvasFontSize(int fontSize) => Math.Max(1, (int)Math.Round(fontSize * _viewport.Zoom));

    private DiagramCursor HoverCursor(Point point)
    {
        if (_tool != DiagramTool.Select
            || AnchorUnder(point).Side is not null
            || ConnectorEndUnder(point).End is not null)
        {
            return DiagramCursor.Cross;
        }

        if (SegmentDragAt(point) is { } segment)
        {
            return segment.Route[segment.Segment].Y == segment.Route[segment.Segment + 1].Y
                ? DiagramCursor.SizeVertical
                : DiagramCursor.SizeHorizontal;
        }

        if (_selectedShapes.Count > 0
            && DiagramHitTester.HandleAt(SelectedBox(), point, GrabDistance / _viewport.Zoom) is DiagramHandle handle)
        {
            return CursorOf(handle);
        }

        bool canMove = DiagramHitTester.LineAt(_diagram, point, GrabDistance / _viewport.Zoom) is not null
            || DiagramHitTester.ShapeAt(_diagram, point) is not null;

        return canMove ? DiagramCursor.Move : DiagramCursor.Default;
    }

    private void ShowCursor(DiagramCursor cursor)
    {
        if (cursor != _cursor)
        {
            _cursor = cursor;
            _view.ShowCursor(cursor);
        }
    }

    private void Refresh()
    {
        DiagramModel shown = Shown;

        // Only what exists: a change may have just removed a shape that's still selected
        List<Rectangle> selected =
        [
            .. shown.Shapes.Where(shape => _selectedShapes.Contains(shape.Id)).Select(shape => shape.Bounds),
        ];
        Rectangle? handleBounds = selected.Count > 0 ? UnionOf(selected) : null;

        _view.ShowScene(new DiagramSceneModel(
            shown,
            _selectedShapes.ToHashSet(),
            _selectedConnectors.ToHashSet(),
            _viewport,
            handleBounds,
            _guides,
            _band,
            _draftConnector,
            _connectTarget,
            AnchorsOf(shown),
            ConnectorEndsOf(shown)));
        ShowProperties(shown);
        _view.ShowUndoRedo(_undo.Count > 0, _redo.Count > 0);
        _view.ShowDocument(Name, HasChanges);
        _view.ShowZoom((int)Math.Round(_viewport.Zoom * 100));
    }

    /// <summary>Only when they changed, since this runs on every mouse move and the fields shouldn't flicker.</summary>
    private void ShowProperties(DiagramModel diagram)
    {
        DiagramPropertiesModel properties = PropertiesOf(diagram);

        if (properties != _shownProperties)
        {
            _shownProperties = properties;
            _view.ShowProperties(properties);
        }
    }

    /// <summary>
    ///     What applies to everything selected; a value that differs among them is taken from the first one.
    /// </summary>
    private DiagramPropertiesModel PropertiesOf(DiagramModel diagram)
    {
        List<DiagramShapeModel> shapes = [.. diagram.Shapes.Where(shape => _selectedShapes.Contains(shape.Id))];
        List<DiagramConnectorModel> connectors =
        [
            .. diagram.Connectors.Where(connector => _selectedConnectors.Contains(connector.Id)),
        ];

        if (shapes.Count == 0 && connectors.Count == 0)
        {
            return new DiagramPropertiesModel("Nothing selected");
        }

        DiagramStyleModel style = shapes.FirstOrDefault()?.Style ?? connectors[0].Style;
        List<DiagramShapeModel> outlined = [.. shapes.Where(shape => shape.Kind != DiagramShapeKind.Text)];
        bool hasLine = outlined.Count > 0 || connectors.Count > 0;
        bool hasCorners = connectors.Count == 0 && shapes.All(shape => DiagramFigures.HasCornerRadius(shape.Kind));

        int? corner = hasCorners
            ? shapes[0].CornerRadius ?? DiagramFigures.DefaultCornerRadius(shapes[0].Kind)
            : null;

        string? text = null;

        if (shapes.Count + connectors.Count == 1)
        {
            text = shapes.Count == 1 ? shapes[0].Text : connectors[0].Label;
        }

        return new DiagramPropertiesModel(
            TitleOf(shapes, connectors),
            shapes.Count > 0 ? UnionOf(shapes.Select(shape => shape.Bounds)) : null,
            corner,
            outlined.Count > 0 ? outlined[0].Style.Fill : null,
            hasLine ? style.Line : null,
            hasLine ? style.LineWidth : null,
            hasLine ? style.Dashed : null,
            style.TextColor,
            style.FontSize,
            text,

            // A line on its own is always straight
            connectors.FirstOrDefault(connector => !connector.IsLine)?.Routing,
            connectors.Count > 0 ? connectors[0].Heads : null,
            style.FontFamily,
            style.TextOutlineWidth,
            style.TextOutlineColor,
            shapes.Count > 0 ? shapes.Aggregate(DiagramLock.All, (locks, shape) => locks & shape.Locks) : null);
    }

    private static string TitleOf(List<DiagramShapeModel> shapes, List<DiagramConnectorModel> connectors)
    {
        int count = shapes.Count + connectors.Count;

        if (count > 1)
        {
            return connectors.Count == 0 ? $"{count} shapes" : $"{count} objects";
        }

        if (connectors.Count == 1)
        {
            return connectors[0].IsLine ? "Line" : "Connector";
        }

        return shapes[0].Kind switch
        {
            DiagramShapeKind.RoundedRectangle => "Rounded rectangle",
            DiagramShapeKind kind => kind.ToString(),
        };
    }

    /// <summary>The circles of the shape a connector is being dragged to, or else of the one under the mouse.</summary>
    private DiagramAnchorsModel? AnchorsOf(DiagramModel diagram)
    {
        int? shapeId = _connectTarget ?? _hoverShape;
        DiagramSide? highlighted = _connectTarget is null ? _hoverSide : _connectTargetSide;

        if (diagram.Shapes.FirstOrDefault(shape => shape.Id == shapeId) is not { } shape)
        {
            return null;
        }

        float offset = AnchorOffset / _viewport.Zoom;

        return new DiagramAnchorsModel(
            [.. Enum.GetValues<DiagramSide>().Select(side => DiagramHitTester.AnchorCenter(shape, side, offset))],
            highlighted is DiagramSide side ? DiagramHitTester.AnchorCenter(shape, side, offset) : null);
    }

    private List<PointF> ConnectorEndsOf(DiagramModel diagram)
    {
        List<PointF> ends = [];

        foreach (DiagramConnectorModel connector in diagram.Connectors.Where(c => _selectedConnectors.Contains(c.Id)))
        {
            if (DiagramRouter.PathOf(connector, diagram) is { } path)
            {
                ends.Add(DiagramHitTester.EndPoint(path, DiagramConnectorEnd.Start));
                ends.Add(DiagramHitTester.EndPoint(path, DiagramConnectorEnd.End));
            }
        }

        return ends;
    }

    private abstract record Drag;

    private sealed record PanDrag(Point Last) : Drag;

    /// <param name="Lines">The ids of the selected lines on their own, which go along.</param>
    private sealed record MoveDrag(
        Point Start,
        IReadOnlyDictionary<int, Rectangle> Originals,
        Rectangle Box,
        IReadOnlySet<int> Lines)
        : Drag;

    /// <param name="Lines">The ids of the selected lines on their own, which go along.</param>
    private sealed record ScaleDrag(
        Point Start,
        DiagramHandle Handle,
        IReadOnlyDictionary<int, Rectangle> Originals,
        Rectangle Box,
        IReadOnlySet<int> Lines)
        : Drag;

    /// <param name="Start">Where the line starts.</param>
    private sealed record LineDrag(Point Start) : Drag;

    /// <param name="Start">Where the drag started.</param>
    /// <param name="ConnectorId">The line being changed.</param>
    /// <param name="End">The end being dragged.</param>
    private sealed record LineEndDrag(Point Start, int ConnectorId, DiagramConnectorEnd End) : Drag;

    private sealed record CreateDrag(DiagramShapeKind Kind, Point Start) : Drag;

    private sealed record ConnectDrag(int FromShapeId, DiagramSide? FromSide) : Drag;

    private sealed record ReconnectDrag(Point Start, int ConnectorId, DiagramConnectorEnd End) : Drag;

    /// <param name="Start">Where the drag started.</param>
    /// <param name="ConnectorId">The connector being changed.</param>
    /// <param name="Route">Its whole line before the drag.</param>
    /// <param name="Segment">The index of the point that starts the piece being dragged.</param>
    private sealed record SegmentDrag(Point Start, int ConnectorId, IReadOnlyList<PointF> Route, int Segment) : Drag;

    private sealed record BandDrag(Point Start) : Drag;
}
