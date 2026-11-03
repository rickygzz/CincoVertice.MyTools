using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     How large an editor window is.
/// </summary>
/// <param name="Size">Its size when it isn't maximized, in pixels.</param>
/// <param name="IsMaximized">True when it fills the screen.</param>
public sealed record DiagramWindowSizeModel(Size Size, bool IsMaximized);
