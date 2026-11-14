using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Models;
using CincoVertice.MyTools.Ui.ColorPicker.Services;
using CincoVertice.MyTools.Ui.ColorPicker.Views;

namespace CincoVertice.MyTools.Ui.ColorPicker.Presenters;

/// <summary>
///     One color pick: follows the pointer over the frozen screen with the magnifier, and gives the color picked in
///     <see cref="Completion"/> once the view is closed.
/// </summary>
public sealed class ColorPickerPresenter
{
    /// <summary>How many times larger the magnifier shows the pixels, from the smallest to the largest.</summary>
    public static readonly IReadOnlyList<int> Zooms = [2, 3, 4, 6, 8, 12, 16, 24, 32];

    /// <summary>The zoom the magnifier opens with.</summary>
    public const int DefaultZoom = 8;

    /// <summary>About how wide the magnifier is, in screen pixels, whatever the zoom.</summary>
    public const int LensSide = 176;

    /// <summary>How far Shift+arrow moves the pointer; an arrow alone moves it by one pixel.</summary>
    public const int LargeNudge = 10;

    /// <summary>How long after opening losing the focus isn't the user switching windows (as for a snip).</summary>
    public static readonly TimeSpan FocusGracePeriod = TimeSpan.FromSeconds(1);

    private readonly IColorPickerView _view;
    private readonly Size _screenshotSize;
    private readonly Func<Point, Color> _pixelAt;
    private readonly TimeProvider _time;
    private readonly long _openedAt;

    // Continuations run later, not inside the view's ViewClosed event
    private readonly TaskCompletionSource<PickedColorModel?> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private Point? _pointer;
    private int _zoom = DefaultZoom;
    private PickedColorModel? _result;
    private bool _isClosing;

    /// <param name="view">The view, about to be shown.</param>
    /// <param name="screenshotSize">The size of the screenshot it shows.</param>
    /// <param name="pixelAt">The color of a pixel of the screenshot, given one inside it.</param>
    /// <param name="time">The clock for <see cref="FocusGracePeriod"/>, which starts now.</param>
    public ColorPickerPresenter(
        IColorPickerView view,
        Size screenshotSize,
        Func<Point, Color> pixelAt,
        TimeProvider time)
    {
        _view = view;
        _screenshotSize = screenshotSize;
        _pixelAt = pixelAt;
        _time = time;
        _openedAt = time.GetTimestamp();

        _view.PointerMoved += OnPointerMoved;
        _view.ZoomRequested += OnZoomRequested;
        _view.NudgeRequested += OnNudgeRequested;
        _view.PickRequested += OnPickRequested;
        _view.CancelRequested += OnCancelRequested;
        _view.FocusLost += OnFocusLost;
        _view.ViewClosed += OnViewClosed;
    }

    /// <summary>The color picked once the view is closed; null when the pick was cancelled.</summary>
    public Task<PickedColorModel?> Completion => _completion.Task;

    /// <summary>How many pixels across the magnifier shows at a zoom: an odd number, so one is in the middle.</summary>
    public static int CellsAt(int zoom)
    {
        int cells = LensSide / zoom;

        return Math.Max(3, cells % 2 == 0 ? cells - 1 : cells);
    }

    private void OnPointerMoved(object? sender, Point point)
    {
        if (_isClosing)
        {
            return;
        }

        _pointer = Clamp(point);
        ShowLens();
    }

    private void OnZoomRequested(object? sender, int steps)
    {
        if (_isClosing)
        {
            return;
        }

        int index = Zooms.ToList().IndexOf(_zoom);
        _zoom = Zooms[Math.Clamp(index + steps, 0, Zooms.Count - 1)];
        ShowLens();
    }

    private void OnNudgeRequested(object? sender, Size offset)
    {
        if (_isClosing || _pointer is not Point pointer)
        {
            return;
        }

        _pointer = Clamp(pointer + offset);
        _view.MovePointer(_pointer.Value);
        ShowLens();
    }

    private void OnPickRequested(object? sender, ColorTextFormat format)
    {
        if (_isClosing || _pointer is not Point pointer)
        {
            return;
        }

        Finish(new PickedColorModel(ColorAt(pointer), format));
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

    private void ShowLens()
    {
        if (_pointer is not Point pointer)
        {
            return;
        }

        Color color = ColorAt(pointer);

        _view.ShowLens(new ColorLensModel(
            pointer,
            _zoom,
            CellsAt(_zoom),
            color,
            ColorText.Hex(color),
            ColorText.Rgb(color)));
    }

    /// <summary>The color, as an opaque one: the screenshot's alpha means nothing.</summary>
    private Color ColorAt(Point pixel)
    {
        Color color = _pixelAt(pixel);

        return Color.FromArgb(color.R, color.G, color.B);
    }

    /// <summary>
    ///     Closes the view with the result. Anything the view raises while closing (e.g. losing the focus) is
    ///     ignored, so it can't change the result.
    /// </summary>
    private void Finish(PickedColorModel? result)
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        _result = result;
        _view.Close();
    }

    /// <summary>A pixel inside the screenshot: the pointer can be on its last row or column, never past it.</summary>
    private Point Clamp(Point point) => new(
        Math.Clamp(point.X, 0, _screenshotSize.Width - 1),
        Math.Clamp(point.Y, 0, _screenshotSize.Height - 1));
}
