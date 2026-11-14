using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Models;
using CincoVertice.MyTools.Ui.ColorPicker.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.ColorPicker;

public class ColorPaletteTests
{
    private static readonly Color DodgerBlue = Color.FromArgb(30, 144, 255);

    [Theory]
    [InlineData(0, 1, 0.5, 255, 0, 0)]
    [InlineData(120, 1, 0.5, 0, 255, 0)]
    [InlineData(240, 1, 0.5, 0, 0, 255)]
    [InlineData(-120, 1, 0.5, 0, 0, 255)]
    [InlineData(600, 1, 0.5, 0, 0, 255)]
    [InlineData(0, 0, 0.5, 128, 128, 128)]
    [InlineData(30, 1, 0.25, 128, 64, 0)]
    public void FromHsl_IsTheColor_ForAnyHue(double hue, double saturation, double lightness, int r, int g, int b)
    {
        // Act / Assert
        Assert.Equal(Color.FromArgb(r, g, b), ColorPalette.FromHsl(hue, saturation, lightness));
    }

    [Fact]
    public void FromHsl_OfToHsl_IsTheSameColor()
    {
        // Arrange
        (double hue, double saturation, double lightness) = ColorPalette.ToHsl(DodgerBlue);

        // Act / Assert
        Assert.Equal(DodgerBlue, ColorPalette.FromHsl(hue, saturation, lightness));
    }

    [Fact]
    public void Shades_AreThreeDarker_TheColor_ThenThreeLighter()
    {
        // Act
        IReadOnlyList<Color> shades = ColorPalette.Shades(DodgerBlue);

        // Assert: darker and darker upwards, lighter and lighter downwards, never black or white
        Assert.Equal(2 * ColorPalette.ShadeCount + 1, shades.Count);
        Assert.Equal(DodgerBlue, shades[ColorPalette.ShadeCount]);

        for (int i = 1; i < shades.Count; i++)
        {
            Assert.True(shades[i - 1].GetBrightness() < shades[i].GetBrightness());
        }

        Assert.NotEqual(Color.FromArgb(0, 0, 0), shades[0]);
        Assert.NotEqual(Color.FromArgb(255, 255, 255), shades[^1]);
        Assert.Equal(210, Math.Round(shades[0].GetHue()));
        Assert.Equal(210, Math.Round(shades[^1].GetHue()));
    }

    [Fact]
    public void Harmonies_AreDarkAndLight_InTheHuesThatGoWell()
    {
        // Act
        IReadOnlyList<ColorHarmonyModel> harmonies = ColorPalette.Harmonies(DodgerBlue);

        // Assert
        Assert.Equal(
            ["Neutral", "Complementary", "Analogous −30°", "Analogous +30°", "Triadic −120°", "Triadic +120°"],
            harmonies.Select(harmony => harmony.Name));

        ColorHarmonyModel complementary = harmonies[1];
        Assert.Equal(30, Math.Round(complementary.Dark.GetHue()));
        Assert.Equal(ColorPalette.DarkLightness, complementary.Dark.GetBrightness(), 0.01);
        Assert.Equal(ColorPalette.LightLightness, complementary.Light.GetBrightness(), 0.01);

        Assert.All(harmonies, harmony => Assert.True(harmony.Dark.GetBrightness() < harmony.Light.GetBrightness()));
        Assert.True(harmonies[0].Dark.GetSaturation() <= ColorPalette.NeutralSaturation + 0.01);
    }

    [Fact]
    public void Of_AGray_IsGrays()
    {
        // Act
        ColorPaletteModel palette = ColorPalette.Of(Color.FromArgb(128, 128, 128));

        // Assert: no hue to turn, so every color stays gray
        Assert.All(
            palette.Shades.Concat(palette.Harmonies.SelectMany(harmony => new[] { harmony.Dark, harmony.Light })),
            color => Assert.True(color.R == color.G && color.G == color.B));
    }
}
