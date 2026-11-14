namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     What of a shape's place, size and proportions stays as it is when it's moved or scaled: by dragging, with the
///     arrow keys or in the properties panel. The rest still changes.
/// </summary>
[Flags]
public enum DiagramLock
{
    None = 0,
    X = 1,
    Y = 2,
    Width = 4,
    Height = 8,

    /// <summary>Its width and height change together, keeping its proportions; new images have it.</summary>
    AspectRatio = 16,

    /// <summary>Where it is and how large.</summary>
    Bounds = X | Y | Width | Height,

    All = Bounds | AspectRatio,
}
