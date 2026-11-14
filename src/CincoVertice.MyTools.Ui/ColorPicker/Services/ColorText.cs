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
        // .NET's saturation and brightness are HSL's saturation and lightness
        int hue = Round(color.GetHue()) % 360;
        int saturation = Round(color.GetSaturation() * 100);
        int lightness = Round(color.GetBrightness() * 100);

        return $"hsl({hue}, {saturation}%, {lightness}%)";
    }

    /// <summary>The notification after a pick: what was copied, then the color in the other ways.</summary>
    public static string CopiedMessage(PickedColorModel picked)
    {
        IEnumerable<string> others = new[] { Hex(picked.Color), Rgb(picked.Color), Hsl(picked.Color) }
            .Where(text => text != picked.Text);

        return $"Copied {picked.Text}\n{string.Join("  ·  ", others)}";
    }

    private static int Round(float value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
