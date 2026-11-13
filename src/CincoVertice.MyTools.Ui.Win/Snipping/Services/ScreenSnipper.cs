using System.Drawing.Imaging;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Snipping.Presenters;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     Freezes the screen and lets the user drag a rectangle over it with <see cref="SnipOverlayForm"/>. One snip at
///     a time, whichever action starts it.
/// </summary>
public sealed class ScreenSnipper
{
    private bool _isSnipping;

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

    private static async Task<Bitmap?> SnipOnceAsync()
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
    private static async Task<(Bitmap Screenshot, Rectangle Bounds, Rectangle? Selection)> SelectOnceAsync()
    {
        Rectangle bounds;
        Bitmap screenshot;

        // The app is only system DPI aware: with monitors at different scaling, the bounds and the pixels would be
        // scaled. Per-monitor aware, they are physical on every monitor.
        using (DpiAwarenessScope.PerMonitorV2())
        {
            bounds = ScreenCapture.VirtualScreenBounds;
            screenshot = ScreenCapture.Capture(bounds);
        }

        SnipOverlayForm overlay;
        SnipPresenter presenter;

        // The overlay keeps the DPI awareness it's created with, so its pixels are the screenshot's pixels
        using (DpiAwarenessScope.PerMonitorV2())
        {
            overlay = new SnipOverlayForm(screenshot, bounds);
            presenter = new SnipPresenter(overlay, screenshot.Size, TimeProvider.System);
            overlay.Show();
        }

        using (overlay)
        {
            return (screenshot, bounds, await presenter.Completion);
        }
    }
}
