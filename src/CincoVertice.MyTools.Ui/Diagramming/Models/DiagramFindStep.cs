namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     Which of the shapes and connectors that have the text to go to.
/// </summary>
public enum DiagramFindStep
{
    /// <summary>The first one, as the text being looked for changes.</summary>
    First,

    /// <summary>The one after the one found last, back to the first after the last.</summary>
    Next,

    /// <summary>The one before the one found last, back to the last before the first.</summary>
    Previous,
}
