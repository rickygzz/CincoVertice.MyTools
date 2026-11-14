using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Models;

namespace CincoVertice.MyTools.Ui.ColorPicker.Services;

/// <summary>
///     Colors made from one: its darker and lighter shades, and dark and light colors of the hues that go well with
///     it (color harmonies), which keep its saturation.
/// </summary>
public static class ColorPalette
{
    /// <summary>How many darker shades, and how many lighter ones.</summary>
    public const int ShadeCount = 3;

    /// <summary>The lightness of the dark and light colors of a harmony.</summary>
    public const double DarkLightness = 0.25;

    public const double LightLightness = 0.85;

    /// <summary>The neutrals are nearly gray, with only a hint of the hue.</summary>
    public const double NeutralSaturation = 0.12;

    public static ColorPaletteModel Of(Color color) => new(color, Shades(color), Harmonies(color));

    /// <summary>
    ///     The darkest shade first, the color in the middle, the lightest last: each step takes a quarter of the way
    ///     to black or white in lightness, so none is black or white.
    /// </summary>
    public static IReadOnlyList<Color> Shades(Color color)
    {
        (double hue, double saturation, double lightness) = ToHsl(color);
        var shades = new List<Color>();

        for (int step = ShadeCount; step >= 1; step--)
        {
            shades.Add(FromHsl(hue, saturation, lightness * (ShadeCount + 1 - step) / (ShadeCount + 1)));
        }

        shades.Add(color);

        for (int step = 1; step <= ShadeCount; step++)
        {
            shades.Add(FromHsl(hue, saturation, lightness + (1 - lightness) * step / (ShadeCount + 1)));
        }

        return shades;
    }

    public static IReadOnlyList<ColorHarmonyModel> Harmonies(Color color)
    {
        (double hue, double saturation, _) = ToHsl(color);
        double neutral = Math.Min(saturation, NeutralSaturation);

        return
        [
            new ColorHarmonyModel("Neutral", FromHsl(hue, neutral, 0.15), FromHsl(hue, neutral, 0.95)),
            Harmony("Complementary", hue + 180, saturation),
            Harmony("Analogous −30°", hue - 30, saturation),
            Harmony("Analogous +30°", hue + 30, saturation),
            Harmony("Triadic −120°", hue - 120, saturation),
            Harmony("Triadic +120°", hue + 120, saturation),
        ];
    }

    /// <returns>Hue in degrees, from 0 to 360; saturation and lightness from 0 to 1.</returns>
    public static (double Hue, double Saturation, double Lightness) ToHsl(Color color)
    {
        // .NET's saturation and brightness are HSL's saturation and lightness
        return (color.GetHue(), color.GetSaturation(), color.GetBrightness());
    }

    /// <returns>Hue in degrees, from 0 to 360; saturation and value from 0 to 1.</returns>
    public static (double Hue, double Saturation, double Value) ToHsv(Color color)
    {
        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));
        double saturation = max == 0 ? 0 : (max - min) / (double)max;

        return (color.GetHue(), saturation, max / 255.0);
    }

    /// <param name="hue">In degrees, any number: it's turned into 0 to 360.</param>
    /// <param name="saturation">From 0 to 1.</param>
    /// <param name="lightness">From 0 to 1.</param>
    public static Color FromHsl(double hue, double saturation, double lightness)
    {
        double h = ((hue % 360) + 360) % 360 / 360;
        double s = Math.Clamp(saturation, 0, 1);
        double l = Math.Clamp(lightness, 0, 1);

        if (s == 0)
        {
            int gray = ToByte(l);

            return Color.FromArgb(gray, gray, gray);
        }

        double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        double p = 2 * l - q;

        return Color.FromArgb(
            ToByte(Channel(p, q, h + 1.0 / 3)),
            ToByte(Channel(p, q, h)),
            ToByte(Channel(p, q, h - 1.0 / 3)));
    }

    private static ColorHarmonyModel Harmony(string name, double hue, double saturation) =>
        new(name, FromHsl(hue, saturation, DarkLightness), FromHsl(hue, saturation, LightLightness));

    private static double Channel(double p, double q, double t)
    {
        t = t < 0 ? t + 1 : t > 1 ? t - 1 : t;

        return t switch
        {
            < 1.0 / 6 => p + (q - p) * 6 * t,
            < 1.0 / 2 => q,
            < 2.0 / 3 => p + (q - p) * (2.0 / 3 - t) * 6,
            _ => p,
        };
    }

    private static int ToByte(double value) => (int)Math.Round(value * 255, MidpointRounding.AwayFromZero);
}
