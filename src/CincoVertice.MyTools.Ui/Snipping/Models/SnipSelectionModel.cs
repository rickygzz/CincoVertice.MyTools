using System.Drawing;

namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>
///     What shows that an annotation is selected.
/// </summary>
/// <param name="Box">The frame around it; null for an arrow, which has only its two ends.</param>
/// <param name="Handles">Where the points that can be dragged to change its size are.</param>
public sealed record SnipSelectionModel(Rectangle? Box, IReadOnlyList<Point> Handles);
