using System.Drawing;

namespace CincoVertice.MyTools.Ui.ColorPicker.Models;

/// <summary>A color, written each way the color window shows.</summary>
/// <param name="Hex">#1E90FF</param>
/// <param name="Rgb">rgb(30, 144, 255), each from 0 to 255</param>
/// <param name="Hsl">hsl(210, 100%, 56%)</param>
/// <param name="Hsv">hsv(210, 88%, 100%)</param>
public sealed record ColorValuesModel(Color Color, string Hex, string Rgb, string Hsl, string Hsv);
