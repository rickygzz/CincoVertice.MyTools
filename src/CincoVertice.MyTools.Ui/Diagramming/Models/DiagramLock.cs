namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     What of a shape's place and size stays as it is when it's moved or scaled: by dragging, with the arrow keys or
///     in the properties panel. The rest still changes.
/// </summary>
[Flags]
public enum DiagramLock
{
    None = 0,
    X = 1,
    Y = 2,
    Width = 4,
    Height = 8,
    All = X | Y | Width | Height,
}
