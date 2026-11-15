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

    /// <summary>
    ///     A color typed as CSS writes it in hex: #1E90FF, or #19F for #1199FF; the # can be left out, and spaces
    ///     around it don't count.
    /// </summary>
    /// <returns>False for anything else.</returns>
    public static bool TryParseHex(string? text, out Color color)
    {
        color = Color.Empty;

        string digits = (text ?? string.Empty).Trim().TrimStart('#');

        if (digits.Length == 3)
        {
            digits = string.Concat(digits.Select(digit => $"{digit}{digit}"));
        }

        if (digits.Length != 6 || !digits.All(Uri.IsHexDigit))
        {
            return false;
        }

        color = Color.FromArgb(255, Color.FromArgb(Convert.ToInt32(digits, 16)));

        return true;
    }

    private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
