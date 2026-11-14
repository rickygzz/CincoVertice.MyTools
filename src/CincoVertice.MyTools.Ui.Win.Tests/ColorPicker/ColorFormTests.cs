using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Interfaces;
using CincoVertice.MyTools.Ui.ColorPicker.Models;
using CincoVertice.MyTools.Ui.ColorPicker.Presenters;
using CincoVertice.MyTools.Ui.Win.ColorPicker;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.ColorPicker;

public class ColorFormTests
{
    [Fact]
    public void Show_MakesTheShadesAndHarmonies_AndMarksTheColorPicked()
    {
        // Arrange
        using var form = new ColorForm();
        var presenter = new ColorPresenter(form, new NoPicker());

        // Act
        presenter.Show(Color.FromArgb(30, 144, 255));

        // Assert: 7 shades, then a dark and a light swatch for each of the 6 harmonies
        List<ColorSwatchControl> swatches = [.. form.Swatches];
        Assert.Equal(7 + 2 * 6, swatches.Count);
        Assert.Equal(Color.FromArgb(30, 144, 255), swatches.Single(swatch => swatch.IsChosen).Color);
        Assert.True(swatches[3].IsBase);
        Assert.Single(swatches, swatch => swatch.IsBase);
    }

    [Fact]
    public void Show_Again_KeepsTheSameSwatches()
    {
        // Arrange
        using var form = new ColorForm();
        var presenter = new ColorPresenter(form, new NoPicker());
        presenter.Show(Color.FromArgb(30, 144, 255));

        // Act
        presenter.Show(Color.FromArgb(200, 40, 40));

        // Assert
        Assert.Equal(19, form.Swatches.Count());
        Assert.Equal(Color.FromArgb(200, 40, 40), form.Swatches.Single(swatch => swatch.IsChosen).Color);
    }

    private sealed class NoPicker : IScreenColorPicker
    {
        public Task<PickedColorModel?> PickAsync() => Task.FromResult<PickedColorModel?>(null);
    }
}
