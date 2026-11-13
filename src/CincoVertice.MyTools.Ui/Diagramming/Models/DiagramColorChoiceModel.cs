using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A color the user chose.
/// </summary>
/// <param name="Target">What it is for.</param>
/// <param name="Color">The color; <see cref="Color.Empty"/> for no fill.</param>
public sealed record DiagramColorChoiceModel(DiagramColorTarget Target, Color Color);
