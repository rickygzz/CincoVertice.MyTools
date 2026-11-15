using System.Drawing;
using CincoVertice.MyTools.Ui.Spotlight.Views;

namespace CincoVertice.MyTools.Ui.Spotlight.Presenters;

/// <summary>
///     The mouse spotlight: dims the screens but for a circle around the pointer, which starts large and shrinks onto
///     it, so the eye follows it there, then follows the pointer. It goes away once the pointer has stayed still for
///     <see cref="StillTime"/>, or when the user clicks or presses a key (<see cref="Hide"/>).
/// </summary>
public sealed class SpotlightPresenter
{
    /// <summary>The circle's radius when it shows, in pixels at 100% scale.</summary>
    public const int StartRadius = 600;

    /// <summary>Its radius once it has shrunk.</summary>
    public const int Radius = 100;

    public static readonly TimeSpan ShrinkTime = TimeSpan.FromMilliseconds(400);

    public static readonly TimeSpan StillTime = TimeSpan.FromSeconds(3);

    private readonly ISpotlightView _view;
    private readonly TimeProvider _time;

    private DateTimeOffset _shownAt;
    private DateTimeOffset _movedAt;
    private Point _cursor;

    public SpotlightPresenter(ISpotlightView view, TimeProvider time)
    {
        _view = view;
        _time = time;
        _view.Tick += OnTick;
    }

    public bool IsShown { get; private set; }

    /// <summary>Shows the spotlight, from its start even when it shows.</summary>
    public void Show()
    {
        _shownAt = _time.GetUtcNow();
        _movedAt = _shownAt;
        _cursor = _view.CursorPosition;
        IsShown = true;
        _view.ShowSpotlight(_cursor, StartRadius);
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

    /// <summary>The radius this long after it showed: fast at first, slowing down onto <see cref="Radius"/>.</summary>
    public static int RadiusAt(TimeSpan elapsed)
    {
        double left = 1 - Math.Clamp(elapsed / ShrinkTime, 0, 1);

        return (int)Math.Round(Radius + (StartRadius - Radius) * left * left);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!IsShown)
        {
            return;
        }

        DateTimeOffset now = _time.GetUtcNow();
        Point cursor = _view.CursorPosition;

        if (cursor != _cursor)
        {
            _cursor = cursor;
            _movedAt = now;
        }
        else if (now - _movedAt >= StillTime)
        {
            Hide();

            return;
        }

        _view.ShowSpotlight(_cursor, RadiusAt(now - _shownAt));
    }
}
