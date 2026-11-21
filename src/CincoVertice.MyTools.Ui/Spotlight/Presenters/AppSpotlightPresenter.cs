using System.Drawing;
using CincoVertice.MyTools.Ui.Spotlight.Views;

namespace CincoVertice.MyTools.Ui.Spotlight.Presenters;

/// <summary>
///     The app spotlight: dims the screens but for the window the user works in, with a frame that starts far around
///     it and closes onto it, so the eye follows it there. It follows the window while it shows, and goes away after
///     <see cref="ShowTime"/>, or when the user clicks or presses a key (<see cref="Hide"/>).
/// </summary>
public sealed class AppSpotlightPresenter
{
    /// <summary>How far around the window the frame starts, in pixels at 100% scale.</summary>
    public const int StartMargin = 400;

    /// <summary>How far around it the frame ends.</summary>
    public const int Margin = 6;

    public static readonly TimeSpan ShrinkTime = TimeSpan.FromMilliseconds(400);

    /// <summary>How long it shows, from when it starts.</summary>
    public static readonly TimeSpan ShowTime = TimeSpan.FromSeconds(2);

    private readonly IAppSpotlightView _view;
    private readonly TimeProvider _time;

    private DateTimeOffset _shownAt;
    private Rectangle _window;

    public AppSpotlightPresenter(IAppSpotlightView view, TimeProvider time)
    {
        _view = view;
        _time = time;
        _view.Tick += OnTick;
    }

    public bool IsShown { get; private set; }

    /// <summary>Shows the spotlight, from its start even when it shows; nothing when there's no window.</summary>
    public void Show()
    {
        if (_view.ActiveWindowBounds is not { } window)
        {
            Hide();

            return;
        }

        _shownAt = _time.GetUtcNow();
        _window = window;
        IsShown = true;
        _view.ShowSpotlight(_window, StartMargin);
    }

    public void Hide()
    {
        if (!IsShown)
        {
            return;
        }

        IsShown = false;
        _view.HideSpotlight();
    }

    /// <summary>The margin this long after it showed: fast at first, slowing down onto <see cref="Margin"/>.</summary>
    public static int MarginAt(TimeSpan elapsed)
    {
        double left = 1 - Math.Clamp(elapsed / ShrinkTime, 0, 1);

        return (int)Math.Round(Margin + (StartMargin - Margin) * left * left);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!IsShown)
        {
            return;
        }

        TimeSpan elapsed = _time.GetUtcNow() - _shownAt;

        if (elapsed >= ShowTime)
        {
            Hide();

            return;
        }

        // The window as it is now, as when it's dragged; the last one seen when it's gone
        _window = _view.ActiveWindowBounds ?? _window;
        _view.ShowSpotlight(_window, MarginAt(elapsed));
    }
}
