using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     An alignment line shown while a shape snaps to another one, in diagram pixels.
/// </summary>
public sealed record DiagramGuideModel(Point From, Point To);
