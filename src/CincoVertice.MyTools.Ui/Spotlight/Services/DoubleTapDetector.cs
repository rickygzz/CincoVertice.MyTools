namespace CincoVertice.MyTools.Ui.Spotlight.Services;

/// <summary>
///     Tells when a key is tapped twice quickly: pressed and let go of, twice, with no other key or click in between.
///     Holding the key isn't a tap, so its repeats never count, nor do shortcuts that use it, such as Ctrl+C. It's told
///     of every key and click, with the time each happened.
/// </summary>
public sealed class DoubleTapDetector
{
    /// <summary>The longest a tap holds the key down.</summary>
    public static readonly TimeSpan MaxTapLength = TimeSpan.FromMilliseconds(300);

    /// <summary>The longest from letting go of the first tap to pressing the second.</summary>
    public static readonly TimeSpan MaxGap = TimeSpan.FromMilliseconds(400);

    // When the key went down, while it's held, and when the first tap let go of it, while one is waiting for a second
    private TimeSpan? _pressedAt;
    private TimeSpan? _tappedAt;

    /// <param name="isTheKey">Whether the key that went down is the one tapped.</param>
    /// <param name="time">When it went down.</param>
    public void KeyDown(bool isTheKey, TimeSpan time)
    {
        if (!isTheKey)
        {
            Interrupt();

            return;
        }

        // Held down: Windows repeats the key
        if (_pressedAt is not null)
        {
            return;
        }

        if (_tappedAt is { } tapped && time - tapped > MaxGap)
        {
            _tappedAt = null;
        }

        _pressedAt = time;
    }

    /// <param name="isTheKey">Whether the key that went up is the one tapped.</param>
    /// <param name="time">When it went up.</param>
    /// <returns>Whether this ended the second tap.</returns>
    public bool KeyUp(bool isTheKey, TimeSpan time)
    {
        if (!isTheKey || _pressedAt is not { } pressed)
        {
            return false;
        }

        _pressedAt = null;

        if (time - pressed > MaxTapLength)
        {
            _tappedAt = null;

            return false;
        }

        if (_tappedAt is not null)
        {
            _tappedAt = null;

            return true;
        }

        _tappedAt = time;

        return false;
    }

    /// <summary>Something else happened, such as a click: the taps so far no longer count.</summary>
    public void Interrupt()
    {
        _pressedAt = null;
        _tappedAt = null;
    }
}
