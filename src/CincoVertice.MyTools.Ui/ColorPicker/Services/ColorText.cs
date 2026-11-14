using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Models;

namespace CincoVertice.MyTools.Ui.ColorPicker.Services;

/// <summary>A color written as CSS and most programs read it. Alpha is left out: a screen pixel has none.</summary>
public static class ColorText
{
    public static string Of(Color color, ColorTextFormat format) => format switch
    {
        ColorTextFormat.Rgb => Rgb(color),
        _ => Hex(color),
    };

    /// <summary>#1E90FF</summary>
    public static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>rgb(30, 144, 255)</summary>
    public static string Rgb(Color color) => $"rgb({color.R}, {color.G}, {color.B})";

    /// <summary>hsl(210, 100%, 56%): hue in degrees, saturation and lightness in percent, rounded.</summary>
    public static string Hsl(Color color)
    {
        (double hue, double saturation, double lightness) = ColorPalette.ToHsl(color);

        return $"hsl({Round(hue) % 360}, {Round(saturation * 100)}%, {Round(lightness * 100)}%)";
    }

    /// <summary>hsv(210, 88%, 100%): hue in degrees, saturation and value in percent, rounded.</summary>
    public static string Hsv(Color color)
    {
        (double hue, double saturation, double value) = ColorPalette.ToHsv(color);

        return $"hsv({Round(hue) % 360}, {Round(saturation * 100)}%, {Round(value * 100)}%)";
    }

    private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
