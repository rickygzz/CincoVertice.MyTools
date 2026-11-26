using System.Drawing.Imaging;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Presenters;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     Freezes the screen and lets the user drag a rectangle over it with <see cref="SnipOverlayForm"/>. One snip at
///     a time, whichever action starts it.
/// </summary>
public sealed class ScreenSnipper
{
    private readonly ISnipEditorSettingsStore _settings;
    private readonly ISnipAreaStore _lastArea;

    private bool _isSnipping;

    /// <param name="settings">Whether to adjust the area before snipping, which the snip editor's Options set.</param>
    /// <param name="lastArea">The area snipped last, which an adjusted snip starts at.</param>
    public ScreenSnipper(ISnipEditorSettingsStore settings, ISnipAreaStore lastArea)
    {
        _settings = settings;
        _lastArea = lastArea;
    }

    /// <returns>
    ///     The part of the screen the user selected, which the caller disposes; null when the snip was cancelled or
    ///     another snip is open.
    /// </returns>
    public async Task<Bitmap?> SnipAsync()
    {
        // Started again while snipping, it would snip the overlay itself
        if (_isSnipping)
        {
            return null;
        }

        _isSnipping = true;

        try
        {
            return await SnipOnceAsync();
        }
        finally
        {
            _isSnipping = false;
        }
    }

    /// <summary>Lets the user drag a rectangle over the frozen screen, as for a snip, e.g. to record it.</summary>
    /// <returns>The area, in physical screen pixels; null when cancelled or another snip is open.</returns>
    public async Task<Rectangle?> SelectAreaAsync()
    {
        if (_isSnipping)
        {
            return null;
        }

        _isSnipping = true;

        try
        {
            (Bitmap screenshot, Rectangle bounds, Rectangle? selection) = await SelectOnceAsync();
            screenshot.Dispose();

            return selection is Rectangle area ? area with { X = area.X + bounds.X, Y = area.Y + bounds.Y } : null;
        }
        finally
        {
            _isSnipping = false;
        }
    }

    private async Task<Bitmap?> SnipOnceAsync()
    {
        (Bitmap screenshot, _, Rectangle? selection) = await SelectOnceAsync();

        using (screenshot)
        {
            // 24 bits: an alpha channel pastes as black or transparent in some programs
            return selection is Rectangle area ? screenshot.Clone(area, PixelFormat.Format24bppRgb) : null;
        }
    }

    /// <returns>
    ///     The frozen screen, which the caller disposes; where it is on the screen; and the part the user selected, in
    ///     the screenshot's pixels, or null when cancelled.
    /// </returns>
    private async Task<(Bitmap Screenshot, Rectangle Bounds, Rectangle? Selection)> SelectOnceAsync()
    {
        Rectangle bounds;
        Bitmap screenshot;

        // Per-monitor aware, the bounds and the pixels are physical on every monitor, whatever its scaling; the app is
        // already, and the scope keeps it so should that change
        using (DpiAwarenessScope.PerMonitorV2())
        {
            bounds = ScreenCapture.VirtualScreenBounds;
            screenshot = ScreenCapture.Capture(bounds);
        }

        SnipOverlayForm overlay;
        SnipPresenter presenter;

        // Read at each snip, as the editor's Options may have changed it since; the area is kept in screen pixels
        bool isAdjustable = _settings.Load()?.AdjustBeforeSnipping == true;
        Rectangle? lastArea = isAdjustable && _lastArea.Load() is Rectangle area
            ? area with { X = area.X - bounds.X, Y = area.Y - bounds.Y }
            : null;

        // The overlay keeps the DPI awareness it's created with, so its pixels are the screenshot's pixels
        using (DpiAwarenessScope.PerMonitorV2())
        {
            overlay = new SnipOverlayForm(screenshot, bounds);
            presenter = new SnipPresenter(overlay, screenshot.Size, TimeProvider.System, isAdjustable, lastArea);
            overlay.Show();
        }

        Rectangle? selection;

        using (overlay)
        {
            selection = await presenter.Completion;
        }

        // Remembered however it was taken, so turning the option on starts at the last snip
        if (selection is Rectangle snipped)
        {
            _lastArea.Save(snipped with { X = snipped.X + bounds.X, Y = snipped.Y + bounds.Y });
        }

        return (screenshot, bounds, selection);
    }
}
