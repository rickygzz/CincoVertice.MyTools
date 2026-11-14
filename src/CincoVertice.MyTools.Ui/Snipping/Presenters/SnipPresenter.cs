using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Views;

namespace CincoVertice.MyTools.Ui.Snipping.Presenters;

/// <summary>
///     One snip: turns the drag on the view into a rectangle inside the screenshot, and gives it in
///     <see cref="Completion"/> once the view is closed. A drag smaller than <see cref="MinimumSize"/> either way is
///     taken as a click and selects nothing.
/// </summary>
public sealed class SnipPresenter
{
    /// <summary>The smallest width and height of a snip, in pixels.</summary>
    public const int MinimumSize = 2;

    /// <summary>
    ///     How long after opening losing the focus isn't the user switching windows. A window opened from a global hotkey
    ///     can lose it right away, as it takes it from the application in front, and closing then shows only a flicker.
    /// </summary>
    public static readonly TimeSpan FocusGracePeriod = TimeSpan.FromSeconds(1);

    private readonly ISnipView _view;
    private readonly Size _screenshotSize;
    private readonly TimeProvider _time;
    private readonly long _openedAt;

    // Continuations run later, not inside the view's ViewClosed event
    private readonly TaskCompletionSource<Rectangle?> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private Point? _start;
    private Rectangle? _result;
    private bool _isClosing;

    /// <param name="view">The view, about to be shown.</param>
    /// <param name="screenshotSize">The size of the screenshot it shows.</param>
    /// <param name="time">The clock for <see cref="FocusGracePeriod"/>, which starts now.</param>
    public SnipPresenter(ISnipView view, Size screenshotSize, TimeProvider time)
    {
        _view = view;
        _screenshotSize = screenshotSize;
        _time = time;
        _openedAt = time.GetTimestamp();

        _view.SelectionStarted += OnSelectionStarted;
        _view.SelectionMoved += OnSelectionMoved;
        _view.SelectionEnded += OnSelectionEnded;
        _view.CancelRequested += OnCancelRequested;
        _view.FocusLost += OnFocusLost;
        _view.ViewClosed += OnViewClosed;
    }

    /// <summary>The selected rectangle once the view is closed; null when the snip was cancelled.</summary>
    public Task<Rectangle?> Completion => _completion.Task;

    private void OnSelectionStarted(object? sender, Point point)
    {
        if (_isClosing)
        {
            return;
        }

        _start = Clamp(point);
        _view.ClearSelection();
    }

    private void OnSelectionMoved(object? sender, Point point)
    {
        if (_isClosing || _start is not Point start)
        {
            return;
        }

        Rectangle selection = FromCorners(start, Clamp(point));

        _view.ShowSelection(selection, $"{selection.Width} × {selection.Height}");
    }

    private void OnSelectionEnded(object? sender, Point point)
    {
        if (_isClosing || _start is not Point start)
        {
            return;
        }

        Rectangle selection = FromCorners(start, Clamp(point));
        _start = null;

        if (selection.Width < MinimumSize || selection.Height < MinimumSize)
        {
            _view.ClearSelection();

            return;
        }

        Finish(selection);
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
}
