using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Services;
using CincoVertice.MyTools.Ui.Snipping.Views;

namespace CincoVertice.MyTools.Ui.Snipping.Presenters;

/// <summary>
///     One snip: turns the drag on the view into a rectangle inside the screenshot, and gives it in
///     <see cref="Completion"/> once the view is closed. A drag smaller than <see cref="MinimumSize"/> either way is
///     taken as a click and selects nothing.
///     <para>Adjustable, letting go keeps the selection, with handles: dragging one resizes it, dragging inside moves
///     it, dragging outside draws another, and Enter takes it. It can start with the area snipped last.</para>
/// </summary>
public sealed class SnipPresenter
{
    /// <summary>The smallest width and height of a snip, in pixels.</summary>
    public const int MinimumSize = 2;

    /// <summary>What the size label adds when the selection waits for Enter.</summary>
    public const string ConfirmHint = "Enter to snip";

    /// <summary>
    ///     How long after opening losing the focus isn't the user switching windows. A window opened from a global hotkey
    ///     can lose it right away, as it takes it from the application in front, and closing then shows only a flicker.
    /// </summary>
    public static readonly TimeSpan FocusGracePeriod = TimeSpan.FromSeconds(1);

    // The handles are reached from 10 pixels away, a little more than in the editor: the whole screen is one target
    private const double HandleReachZoom = 0.6;

    private readonly ISnipView _view;
    private readonly Size _screenshotSize;
    private readonly TimeProvider _time;
    private readonly long _openedAt;
    private readonly bool _isAdjustable;

    // Continuations run later, not inside the view's ViewClosed event
    private readonly TaskCompletionSource<Rectangle?> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    // The drag going on, where it started, and the selection then; the handle dragged, for a resize
    private Drag _drag;
    private Point _dragStart;
    private Rectangle? _beforeDrag;
    private int _handle;

    // Adjustable: the selection kept between drags
    private Rectangle? _selection;
    private SnipCursor _shownCursor = SnipCursor.Cross;

    private Rectangle? _result;
    private bool _isClosing;

    /// <param name="view">The view, about to be shown.</param>
    /// <param name="screenshotSize">The size of the screenshot it shows.</param>
    /// <param name="time">The clock for <see cref="FocusGracePeriod"/>, which starts now.</param>
    /// <param name="isAdjustable">True to keep the selection to adjust until Enter; false to snip when let go.</param>
    /// <param name="initialSelection">
    ///     Adjustable, the selection to start with, e.g. the area snipped last; only its part on the screenshot counts.
    /// </param>
    public SnipPresenter(
        ISnipView view,
        Size screenshotSize,
        TimeProvider time,
        bool isAdjustable = false,
        Rectangle? initialSelection = null)
    {
        _view = view;
        _screenshotSize = screenshotSize;
        _time = time;
        _openedAt = time.GetTimestamp();
        _isAdjustable = isAdjustable;

        _view.SelectionStarted += OnSelectionStarted;
        _view.SelectionMoved += OnSelectionMoved;
        _view.SelectionEnded += OnSelectionEnded;
        _view.PointerMoved += OnPointerMoved;
        _view.ConfirmRequested += OnConfirmRequested;
        _view.CancelRequested += OnCancelRequested;
        _view.FocusLost += OnFocusLost;
        _view.ViewClosed += OnViewClosed;

        if (isAdjustable && initialSelection is Rectangle initial)
        {
            Rectangle shown = Rectangle.Intersect(initial, new Rectangle(Point.Empty, screenshotSize));

            if (shown.Width >= MinimumSize && shown.Height >= MinimumSize)
            {
                _selection = shown;
                Show(shown);
            }
        }
    }

    /// <summary>The selected rectangle once the view is closed; null when the snip was cancelled.</summary>
    public Task<Rectangle?> Completion => _completion.Task;

    private void OnSelectionStarted(object? sender, Point point)
    {
        if (_isClosing)
        {
            return;
        }

        Point start = Clamp(point);
        _dragStart = start;
        _beforeDrag = _selection;
        _drag = Drag.New;

        if (_selection is Rectangle selection)
        {
            if (AnnotationGeometry.HandleAt(AnnotationGeometry.FrameOf(selection), start, HandleReachZoom) is int handle)
            {
                _drag = Drag.Resize;
                _handle = handle;
            }
            else if (selection.Contains(start))
            {
                _drag = Drag.Move;
            }
        }

        if (!_isAdjustable)
        {
            _view.ClearSelection();
        }
    }

    private void OnSelectionMoved(object? sender, Point point)
    {
        if (_isClosing || _drag == Drag.None)
        {
            return;
        }

        Rectangle selection = Dragged(Clamp(point));

        if (_isAdjustable)
        {
            _selection = selection;
        }

        Show(selection);
    }

    private void OnSelectionEnded(object? sender, Point point)
    {
        if (_isClosing || _drag == Drag.None)
        {
            return;
        }

        Rectangle selection = Dragged(Clamp(point));
        bool isClick = _drag == Drag.New && (selection.Width < MinimumSize || selection.Height < MinimumSize);
        _drag = Drag.None;

        if (!_isAdjustable)
        {
            if (isClick)
            {
                _view.ClearSelection();
            }
            else
            {
                Finish(selection);
            }

            return;
        }

        // A click outside the selection keeps it, as it was
        _selection = isClick ? _beforeDrag : selection;

        if (_selection is Rectangle kept)
        {
            Show(kept);
        }
        else
        {
            _view.ClearSelection();
        }

        ShowCursorAt(Clamp(point));
    }

    private void OnPointerMoved(object? sender, Point point)
    {
        if (!_isClosing && _isAdjustable && _drag == Drag.None)
        {
            ShowCursorAt(Clamp(point));
        }
    }

    private void OnConfirmRequested(object? sender, EventArgs e)
    {
        if (_isAdjustable && _drag == Drag.None && _selection is Rectangle selection)
        {
            Finish(selection);
        }
    }

    private void OnCancelRequested(object? sender, EventArgs e)
    {
        Finish(null);
    }

    private void OnFocusLost(object? sender, EventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        if (_time.GetElapsedTime(_openedAt) < FocusGracePeriod)
        {
            _view.TakeFocus();
        }
        else
        {
            Finish(null);
        }
    }

    private void OnViewClosed(object? sender, EventArgs e)
    {
        _isClosing = true;
        _completion.TrySetResult(_result);
    }

    /// <summary>The selection the drag going on gives, with the pointer at the point; always on the screenshot.</summary>
    private Rectangle Dragged(Point point)
    {
        switch (_drag)
        {
            case Drag.Move when _beforeDrag is Rectangle moved:
                // Kept whole on the screenshot: it stops at an edge rather than shrinks
                int x = Math.Clamp(moved.X + point.X - _dragStart.X, 0, _screenshotSize.Width - moved.Width);
                int y = Math.Clamp(moved.Y + point.Y - _dragStart.Y, 0, _screenshotSize.Height - moved.Height);

                return moved with { X = x, Y = y };

            case Drag.Resize when _beforeDrag is Rectangle resized:
                return Rectangle.Intersect(
                    AnnotationGeometry.ResizedBox(resized, _handle, point),
                    new Rectangle(Point.Empty, _screenshotSize));

            default:
                return FromCorners(_dragStart, point);
        }
    }

    private void Show(Rectangle selection)
    {
        string size = $"{selection.Width} × {selection.Height}";

        if (_isAdjustable)
        {
            _view.ShowSelection(selection, $"{size}   {ConfirmHint}", AnnotationGeometry.FrameOf(selection).Handles);
        }
        else
        {
            _view.ShowSelection(selection, size, []);
        }
    }

    /// <summary>A handle's arrows on it, the move arrows inside, the cross elsewhere, to draw another.</summary>
    private void ShowCursorAt(Point point)
    {
        SnipCursor cursor = SnipCursor.Cross;

        if (_selection is Rectangle selection)
        {
            SnipSelectionModel frame = AnnotationGeometry.FrameOf(selection);

            if (AnnotationGeometry.HandleAt(frame, point, HandleReachZoom) is int handle)
            {
                cursor = AnnotationGeometry.CursorOf(frame, handle);
            }
            else if (selection.Contains(point))
            {
                cursor = SnipCursor.Move;
            }
        }

        if (cursor != _shownCursor)
        {
            _shownCursor = cursor;
            _view.ShowCursor(cursor);
        }
    }

    /// <summary>
    ///     Closes the view with the result. Anything the view raises while closing (e.g. losing the focus) is
    ///     ignored, so it can't change the result.
    /// </summary>
    private void Finish(Rectangle? result)
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        _result = result;
        _view.Close();
    }

    private Point Clamp(Point point) => new(
        Math.Clamp(point.X, 0, _screenshotSize.Width),
        Math.Clamp(point.Y, 0, _screenshotSize.Height));

    private static Rectangle FromCorners(Point a, Point b) => Rectangle.FromLTRB(
        Math.Min(a.X, b.X),
        Math.Min(a.Y, b.Y),
        Math.Max(a.X, b.X),
        Math.Max(a.Y, b.Y));

    /// <summary>What the drag going on does: draws a new selection, moves it, or resizes it by a handle.</summary>
    private enum Drag
    {
        None,
        New,
        Move,
        Resize,
    }
}
