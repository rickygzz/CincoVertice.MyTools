namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A piece of a line of text written the same way all along.
/// </summary>
/// <param name="Text">Its characters.</param>
/// <param name="Format">Bold, italic, both or none.</param>
public sealed record DiagramTextRunModel(string Text, DiagramTextFormat Format);
