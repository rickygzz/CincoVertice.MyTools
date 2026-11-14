using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.ColorPicker.Interfaces;
using CincoVertice.MyTools.Ui.ColorPicker.Models;
using CincoVertice.MyTools.Ui.ColorPicker.Presenters;

namespace CincoVertice.MyTools.Ui.Win.ColorPicker.Services;

/// <summary>
///     Freezes the screen and lets the user pick the color of a pixel on it with
///     <see cref="ColorPickerOverlayForm"/>. One pick at a time.
/// </summary>
public sealed class ScreenColorPicker : IScreenColorPicker
{
    private bool _isPicking;

    /// <returns>The color picked; null when the pick was cancelled or another one is open.</returns>
    public async Task<PickedColorModel?> PickAsync()
    {
        // Started again while picking, it would freeze the overlay itself
        if (_isPicking)
        {
            return null;
        }

        _isPicking = true;

        try
        {
            return await PickOnceAsync();
        }
        finally
        {
            _isPicking = false;
        }
    }

    private static async Task<PickedColorModel?> PickOnceAsync()
    {
        Rectangle bounds;
        Bitmap screenshot;

        // Per-monitor aware, the bounds and the pixels are physical on every monitor, whatever its scaling
        using (DpiAwarenessScope.PerMonitorV2())
        {
            bounds = ScreenCapture.VirtualScreenBounds;
            screenshot = ScreenCapture.Capture(bounds);
        }

        using (screenshot)
        {
            ColorPickerOverlayForm overlay;
            ColorPickerPresenter presenter;

            // The overlay keeps the DPI awareness it's created with, so its pixels are the screenshot's pixels
            using (DpiAwarenessScope.PerMonitorV2())
            {
                overlay = new ColorPickerOverlayForm(screenshot, bounds);
                presenter = new ColorPickerPresenter(
                    overlay,
                    screenshot.Size,
                    pixel => screenshot.GetPixel(pixel.X, pixel.Y),
                    TimeProvider.System);
                overlay.Show();
            }

            using (overlay)
            {
                return await presenter.Completion;
            }
        }
    }
}
