using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Interfaces;
using CincoVertice.MyTools.Ui.ColorPicker.Models;
using CincoVertice.MyTools.Ui.ColorPicker.Services;
using CincoVertice.MyTools.Ui.ColorPicker.Views;

namespace CincoVertice.MyTools.Ui.ColorPicker.Presenters;

/// <summary>
///     The color window: shows a picked color's values with its shades and the colors that go well with it, and the
///     values of whichever of them the user chooses. It can go back to the color picked, or pick another.
/// </summary>
public sealed class ColorPresenter
{
    private readonly IColorView _view;
    private readonly IScreenColorPicker _picker;

    private Color _picked;
    private Color _base;
    private Color _chosen;

    public ColorPresenter(IColorView view, IScreenColorPicker picker)
    {
        _view = view;
        _picker = picker;
        _view.ColorChosen += OnColorChosen;
        _view.UseAsBaseRequested += OnUseAsBaseRequested;
        _view.BackToPickedRequested += OnBackToPickedRequested;
        _view.PickAnotherRequested += OnPickAnotherRequested;
    }

    /// <summary>Shows a color just picked: its values, and the colors made from it.</summary>
    public void Show(Color color)
    {
        // Opaque, so it equals the swatches made from it
        _picked = Color.FromArgb(color.R, color.G, color.B);
        ShowPaletteOf(_picked);
    }

    public static ColorValuesModel ValuesOf(Color color) => new(
        color,
        ColorText.Hex(color),
        ColorText.Rgb(color),
        ColorText.Hsl(color),
        ColorText.Hsv(color));

    private void OnColorChosen(object? sender, Color color)
    {
        ShowChosen(color);
    }

    private void OnUseAsBaseRequested(object? sender, EventArgs e)
    {
        ShowPaletteOf(_chosen);
    }

    private void OnBackToPickedRequested(object? sender, EventArgs e)
    {
        ShowPaletteOf(_picked);
    }

    private async void OnPickAnotherRequested(object? sender, EventArgs e)
    {
        PickedColorModel? picked;

        using (_view.KeepOutOfPick())
        {
            picked = await _picker.PickAsync();
        }

        if (picked is null)
        {
            return;
        }

        // As the hotkey does
        _view.CopyToClipboard(picked.Text);
        Show(picked.Color);
    }

    private void ShowPaletteOf(Color color)
    {
        _base = color;
        _view.ShowPalette(ColorPalette.Of(color));
        ShowChosen(color);
    }

    private void ShowChosen(Color color)
    {
        _chosen = color;
        _view.ShowChosen(ValuesOf(color));
        _view.SetBackToPickedEnabled(_base != _picked || _chosen != _picked);
    }
}
