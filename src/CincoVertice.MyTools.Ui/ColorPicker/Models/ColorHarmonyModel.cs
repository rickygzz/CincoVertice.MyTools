using System.Drawing;

namespace CincoVertice.MyTools.Ui.ColorPicker.Models;

/// <summary>A hue that goes well with a color, as a dark color and a light one.</summary>
public sealed record ColorHarmonyModel(string Name, Color Dark, Color Light);
